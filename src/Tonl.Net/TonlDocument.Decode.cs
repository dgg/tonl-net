using System.Globalization;
using System.Text.Json.Nodes;

namespace Tonl.Net;

public partial class TonlDocument
{
	/// <summary>
	/// Parses a TONL-formatted string and returns a <see cref="TonlDocument"/> whose
	/// <see cref="Root"/> property contains the reconstructed JSON node tree.
	/// </summary>
	/// <param name="tonl">The TONL document string to decode.</param>
	/// <param name="options">
	/// Options controlling decoding behaviour. When <see langword="null"/>, default options are used.
	/// </param>
	/// <returns>
	/// A <see cref="TonlDocument"/> whose <see cref="Root"/> is the decoded JSON node, or
	/// <see langword="null"/> if the document represented a null root.
	/// </returns>
	/// <exception cref="TonlParseException">
	/// Thrown when <see cref="TonlDecodeOptions.StrictMode"/> is <see langword="true"/> and the
	/// input contains invalid or ambiguous TONL.
	/// </exception>
	public static TonlDocument Decode(string tonl, TonlDecodeOptions? options = null)
	{
		var decoder = new TonlDecoder(tonl, options ?? new TonlDecodeOptions());
		JsonNode? root = decoder.Parse();
		return new TonlDocument(root);
	}

	internal enum LineType
	{
		Blank,
		Comment,
		VersionHeader,
		DelimiterHeader,
		ObjectHeader,          // key{col1,col2}:
		ArrayHeader,           // key[N]{col1,...}:  or  key[N]:
		PrimitiveValue,        // key: value
		InlinePrimitiveArray,  // key[N]: v1, v2, v3
		TabularRow,            // indented data row (no key prefix)
		IndexedElement,        // [N]: value  or  [N]{...}:  or  [N][M]:
		EmptyObject,           // key:  (nothing after colon)
	}

	internal readonly record struct LineInfo(
		LineType Type,
		int Indent,
		string Key,
		int ArrayCount,
		string? ColumnsRaw,
		string? ValueRaw
	);

	internal readonly record struct ColumnDef(string Name, string? TypeHint);

	/// <summary>
	/// Isolates all mutable parsing state.
	/// </summary>
	private sealed class TonlDecoder
	{
		private readonly string[] _lines;
		private int _cursor;
		private int _indentSize;
		private ColumnDelimiter _delimiter;
		private readonly bool _strict;
		private readonly TonlDecodeOptions _options;

		internal TonlDecoder(string input, TonlDecodeOptions options)
		{
			// Normalise Windows line endings before splitting
			string normalised = input.Replace("\r\n", "\n", StringComparison.Ordinal)
									 .Replace('\r', '\n');
			_lines = normalised.Split('\n');
			_options = options;
			_strict = options.StrictMode;
			_delimiter = options.Delimiter;
			_indentSize = 2;
		}

		// -------------------------------------------------------------------------
		// Entry point
		// -------------------------------------------------------------------------

		internal JsonNode? Parse()
		{
			parseHeaders(_options);
			detectIndentSize();
			return parseRoot();
		}

		// -------------------------------------------------------------------------
		// Header parsing
		// -------------------------------------------------------------------------

		private void parseHeaders(TonlDecodeOptions opts)
		{
			_delimiter = opts.Delimiter;

			while (_cursor < _lines.Length)
			{
				string line = _lines[_cursor].Trim();

				if (line.StartsWith("#version", StringComparison.Ordinal))
				{
					// accepted — version is informational only
					_cursor++;
				}
				else if (line.StartsWith("#delimiter", StringComparison.Ordinal))
				{
					string delimStr = line["#delimiter".Length..].Trim();
					if (ColumnDelimiterParser.TryParse(delimStr, out ColumnDelimiter parsed))
					{
						_delimiter = parsed;
					}
					else
					{
						throwOrWarn($"Unrecognised delimiter value '{delimStr}' at line {_cursor + 1}", _cursor);
					}

					_cursor++;
				}
				else if (line.StartsWith('#') || line.StartsWith('@'))
				{
					// Unknown directive — skip
					_cursor++;
				}
				else
				{
					break;
				}
			}
		}

		private void detectIndentSize()
		{
			int prevIndent = -1;
			for (int i = _cursor; i < _lines.Length; i++)
			{
				string line = _lines[i];
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				int indent = getIndent(i);
				if (prevIndent >= 0 && indent > prevIndent)
				{
					_indentSize = indent - prevIndent;
					return;
				}

				prevIndent = indent;
			}

			// Default
			_indentSize = 2;
		}

		private JsonNode? parseRoot()
		{
			// Skip blanks / comments
			while (_cursor < _lines.Length && isBlankOrComment(_cursor))
			{
				_cursor++;
			}

			if (_cursor >= _lines.Length)
			{
				return new JsonObject();
			}

			LineInfo info = classifyLine(_cursor);

			// The root key is synthetic — we only care about the value.
			switch (info.Type)
			{
				case LineType.ObjectHeader:
				{
					ColumnDef[]? cols = info.ColumnsRaw is not null
						? parseColumnDefs(info.ColumnsRaw)
						: null;
					_cursor++;
					return parseObjectBody(_indentSize, cols);
				}

				case LineType.ArrayHeader:
				{
					if (info.ColumnsRaw is not null)
					{
						ColumnDef[] cols = parseColumnDefs(info.ColumnsRaw);
						_cursor++;
						return parseTabularRows(_indentSize, info.ArrayCount, cols);
					}

					if (info.ValueRaw is { Length: > 0 })
					{
						_cursor++;
						return parsePrimitiveArrayInline(info.ValueRaw, info.ArrayCount);
					}

					if (info.ArrayCount == 0)
					{
						_cursor++;
						return new JsonArray();
					}

					_cursor++;
					return parseMixedArrayBody(_indentSize, info.ArrayCount);
				}

				case LineType.InlinePrimitiveArray:
				{
					string raw = info.ValueRaw ?? string.Empty;
					_cursor++;
					return parsePrimitiveArrayInline(raw, info.ArrayCount);
				}

				case LineType.PrimitiveValue:
				{
					string raw = info.ValueRaw ?? string.Empty;
					_cursor++;
					return parseValue(raw, null);
				}

				case LineType.EmptyObject:
				{
					_cursor++;
					// Peek: if next non-blank line is at _indentSize, it's an object body
					int peek = _cursor;
					while (peek < _lines.Length && isBlankOrComment(peek))
					{
						peek++;
					}

					if (peek < _lines.Length && getIndent(peek) >= _indentSize)
					{
						return parseObjectBody(_indentSize, null);
					}

					return new JsonObject();
				}

				default:
					throwOrWarn($"Unexpected line format at line {_cursor + 1}", _cursor);
					_cursor++;
					return null;
			}
		}

		private JsonObject parseObjectBody(int bodyIndent, ColumnDef[]? columns)
		{
			var obj = new JsonObject();

			while (_cursor < _lines.Length)
			{
				// Skip blank / comment lines regardless of indent
				if (isBlankOrComment(_cursor))
				{
					_cursor++;
					continue;
				}

				int lineIndent = getIndent(_cursor);
				if (lineIndent < bodyIndent)
				{
					break;
				}

				if (lineIndent > bodyIndent)
				{
					// Orphan child line — skip or throw
					throwOrWarn($"Unexpected indentation at line {_cursor + 1}", _cursor);
					_cursor++;
					continue;
				}

				LineInfo info = classifyLine(_cursor);
				string key = info.Key;

				switch (info.Type)
				{
					case LineType.ObjectHeader:
					{
						ColumnDef[]? cols = info.ColumnsRaw is not null
							? parseColumnDefs(info.ColumnsRaw)
							: columns;
						_cursor++;
						obj[key] = parseObjectBody(bodyIndent + _indentSize, cols);
						break;
					}

					case LineType.ArrayHeader:
					{
						if (info.ColumnsRaw is not null)
						{
							ColumnDef[] cols = parseColumnDefs(info.ColumnsRaw);
							_cursor++;
							obj[key] = parseTabularRows(bodyIndent + _indentSize, info.ArrayCount, cols);
						}
						else if (info.ValueRaw is { Length: > 0 })
						{
							_cursor++;
							obj[key] = parsePrimitiveArrayInline(info.ValueRaw, info.ArrayCount);
						}
						else if (info.ArrayCount == 0)
						{
							_cursor++;
							obj[key] = new JsonArray();
						}
						else
						{
							_cursor++;
							obj[key] = parseMixedArrayBody(bodyIndent + _indentSize, info.ArrayCount);
						}

						break;
					}

					case LineType.InlinePrimitiveArray:
					{
						string raw = info.ValueRaw ?? string.Empty;
						_cursor++;
						obj[key] = parsePrimitiveArrayInline(raw, info.ArrayCount);
						break;
					}

					case LineType.PrimitiveValue:
					{
						_cursor++;
						ColumnDef? hint = findColumnDef(columns, key);
						obj[key] = parseValue(info.ValueRaw ?? string.Empty, hint);
						break;
					}

					case LineType.EmptyObject:
					{
						_cursor++;
						int peek = _cursor;
						while (peek < _lines.Length && isBlankOrComment(peek))
						{
							peek++;
						}

						if (peek < _lines.Length && getIndent(peek) >= bodyIndent + _indentSize)
						{
							obj[key] = parseObjectBody(bodyIndent + _indentSize, null);
						}
						else
						{
							obj[key] = new JsonObject();
						}

						break;
					}

					case LineType.Blank:
					case LineType.Comment:
						_cursor++;
						break;

					default:
						throwOrWarn($"Unexpected line format at line {_cursor + 1}", _cursor);
						_cursor++;
						break;
				}
			}

			return obj;
		}

		private JsonArray parseTabularRows(int rowIndent, int count, ColumnDef[] columns)
		{
			var arr = new JsonArray();
			int rowsParsed = 0;

			while (_cursor < _lines.Length && rowsParsed < count)
			{
				if (isBlankOrComment(_cursor))
				{
					_cursor++;
					continue;
				}

				int lineIndent = getIndent(_cursor);
				if (lineIndent < rowIndent)
				{
					break;
				}

				string rawLine = _lines[_cursor][rowIndent..];
				List<string> fields = splitDelimited(rawLine);

				var rowObj = new JsonObject();
				for (int i = 0; i < columns.Length; i++)
				{
					if (i < fields.Count)
					{
						string cell = fields[i];
						rowObj[columns[i].Name] = cell == string.Empty
							? null
							: parseValue(cell, columns[i]);
					}
					else
					{
						rowObj[columns[i].Name] = null;
					}
				}

				if (_strict && fields.Count > columns.Length)
				{
					throw new TonlParseException(
						$"Row has {fields.Count} fields but only {columns.Length} columns at line {_cursor + 1}",
						_cursor + 1,
						_lines[_cursor]);
				}

				arr.Add(rowObj);
				rowsParsed++;
				_cursor++;
			}

			if (_strict && rowsParsed != count)
			{
				throw new TonlParseException(
					$"Expected {count} elements but found {rowsParsed} at line {_cursor + 1}",
					_cursor + 1);
			}

			return arr;
		}

		private JsonArray parseMixedArrayBody(int bodyIndent, int count)
		{
			// Pre-size with nulls
			JsonNode?[] elements = new JsonNode?[count];

			while (_cursor < _lines.Length)
			{
				if (isBlankOrComment(_cursor))
				{
					_cursor++;
					continue;
				}

				int lineIndent = getIndent(_cursor);
				if (lineIndent < bodyIndent)
				{
					break;
				}

				// Re-read the raw content after stripping indent to handle all sub-cases
				string content = _lines[_cursor][lineIndent..];

				// Must start with '['
				if (content.Length == 0 || content[0] != '[')
				{
					throwOrWarn($"Expected indexed element at line {_cursor + 1}", _cursor);
					_cursor++;
					continue;
				}

				int closeBracket = content.IndexOf(']', 1);
				if (closeBracket < 0 || !int.TryParse(content[1..closeBracket], out int idx))
				{
					throwOrWarn($"Expected indexed element at line {_cursor + 1}", _cursor);
					_cursor++;
					continue;
				}

				if (idx < 0 || idx >= count)
				{
					throwOrWarn($"Index {idx} is out of range [0,{count}) at line {_cursor + 1}", _cursor);
					_cursor++;
					continue;
				}

				string afterIndex = content[(closeBracket + 1)..];

				if (afterIndex.StartsWith('{'))
				{
					// [N]{cols}: — object with column hints
					LineInfo info = classifyLine(_cursor);
					ColumnDef[]? cols = info.ColumnsRaw is not null
						? parseColumnDefs(info.ColumnsRaw)
						: null;
					_cursor++;
					elements[idx] = parseObjectBody(bodyIndent + _indentSize, cols);
				}
				else if (afterIndex.StartsWith('['))
				{
					// [N][M]: ... — sub-array (primitive inline or mixed)
					int closeM = afterIndex.IndexOf(']', 1);
					if (closeM >= 0 && int.TryParse(afterIndex[1..closeM], out int subCount))
					{
						string afterSubBracket = afterIndex[(closeM + 1)..].TrimStart();
						if (afterSubBracket.StartsWith(':'))
						{
							string subValues = afterSubBracket[1..].TrimStart();
							_cursor++;
							if (subValues.Length > 0)
							{
								elements[idx] = parsePrimitiveArrayInline(subValues, subCount);
							}
							else if (subCount == 0)
							{
								elements[idx] = new JsonArray();
							}
							else
							{
								elements[idx] = parseMixedArrayBody(bodyIndent + _indentSize, subCount);
							}
						}
						else
						{
							_cursor++;
							elements[idx] = null;
						}
					}
					else
					{
						_cursor++;
						elements[idx] = null;
					}
				}
				else if (afterIndex.StartsWith(':'))
				{
					string value = afterIndex[1..].TrimStart();
					if (value.Length > 0)
					{
						// [N]: value — primitive or null
						_cursor++;
						elements[idx] = value == "null" ? null : parseValue(value, null);
					}
					else
					{
						// [N]: — empty, peek for object body
						_cursor++;
						int peek = _cursor;
						while (peek < _lines.Length && isBlankOrComment(peek))
						{
							peek++;
						}

						if (peek < _lines.Length && getIndent(peek) >= bodyIndent + _indentSize)
						{
							elements[idx] = parseObjectBody(bodyIndent + _indentSize, null);
						}
						else
						{
							elements[idx] = new JsonObject();
						}
					}
				}
				else
				{
					throwOrWarn($"Unexpected indexed element format at line {_cursor + 1}", _cursor);
					_cursor++;
				}
			}

			var result = new JsonArray();
			for (int i = 0; i < count; i++)
			{
				result.Add(elements[i]);
			}

			return result;
		}

		private JsonArray parsePrimitiveArrayInline(string rawValues, int count)
		{
			List<string> fields = splitDelimited(rawValues);
			var arr = new JsonArray();
			foreach (string field in fields)
			{
				arr.Add(parseValue(field.Trim(), null));
			}

			if (_strict && arr.Count != count)
			{
				throw new TonlParseException(
					$"Expected {count} elements but found {arr.Count}",
					_cursor + 1);
			}

			return arr;
		}

		private LineInfo classifyLine(int lineIndex)
		{
			string line = _lines[lineIndex];
			int indent = getIndent(lineIndex);
			string content = line[indent..]; // content without leading spaces

			// Blank
			if (content.Length == 0)
			{
				return new LineInfo(LineType.Blank, indent, string.Empty, -1, null, null);
			}

			// Comment / version / delimiter headers
			if (content[0] == '#')
			{
				if (content.StartsWith("#version", StringComparison.Ordinal))
				{
					return new LineInfo(LineType.VersionHeader, indent, string.Empty, -1, null,
						content["#version".Length..].Trim());
				}

				if (content.StartsWith("#delimiter", StringComparison.Ordinal))
				{
					return new LineInfo(LineType.DelimiterHeader, indent, string.Empty, -1, null,
						content["#delimiter".Length..].Trim());
				}

				return new LineInfo(LineType.Comment, indent, string.Empty, -1, null, null);
			}

			// Indexed element: starts with [
			if (content[0] == '[')
			{
				return classifyIndexedElement(content, indent);
			}

			// Key-based lines: extract the key first
			string key = extractKey(content, out int keyEnd);
			if (keyEnd >= content.Length)
			{
				return new LineInfo(LineType.Comment, indent, string.Empty, -1, null, null);
			}

			char charAfterKey = content[keyEnd];

			// key[N]{cols}: or key[N]: or key[N]: values
			if (charAfterKey == '[')
			{
				return classifyArrayHeader(content, indent, key, keyEnd);
			}

			// key{cols}:
			if (charAfterKey == '{')
			{
				return classifyObjectHeader(content, indent, key, keyEnd);
			}

			// key: value  or  key:
			if (charAfterKey == ':')
			{
				string afterColon = content[(keyEnd + 1)..];
				if (afterColon.Length == 0 || afterColon == " " || afterColon.Trim().Length == 0)
				{
					return new LineInfo(LineType.EmptyObject, indent, key, -1, null, null);
				}

				// There is content after ": "
				string value = afterColon.TrimStart();
				return new LineInfo(LineType.PrimitiveValue, indent, key, -1, null, value);
			}

			// Tabular row (no recognised key prefix)
			return new LineInfo(LineType.TabularRow, indent, string.Empty, -1, null, content);
		}

		private LineInfo classifyIndexedElement(string content, int indent)
		{
			// content starts with '['
			int closeBracket = content.IndexOf(']', 1);
			if (closeBracket < 0)
			{
				return new LineInfo(LineType.TabularRow, indent, string.Empty, -1, null, content);
			}

			string indexStr = content[1..closeBracket];
			if (!int.TryParse(indexStr, out int idx))
			{
				return new LineInfo(LineType.TabularRow, indent, string.Empty, -1, null, content);
			}

			string afterBracket = content[(closeBracket + 1)..].TrimStart();

			// [N]{cols}:
			if (afterBracket.StartsWith('{'))
			{
				int closeCol = afterBracket.IndexOf('}');
				if (closeCol >= 0)
				{
					string colsRaw = afterBracket[1..closeCol];
					return new LineInfo(LineType.IndexedElement, indent, $"[{idx}]", idx, colsRaw, null);
				}
			}

			// [N][M]: values
			if (afterBracket.StartsWith('['))
			{
				int closeM = afterBracket.IndexOf(']', 1);
				if (closeM >= 0 && int.TryParse(afterBracket[1..closeM], out int subCount))
				{
					string afterSubBracket = afterBracket[(closeM + 1)..].TrimStart();
					if (afterSubBracket.StartsWith(':'))
					{
						string subValues = afterSubBracket[1..].TrimStart();
						// Encode subCount in ArrayCount for the outer element, ValueRaw carries inline values
						// We use a special negative encoding: store the sub-array info in ValueRaw
						// Actually the parent parseMixedArrayBody handles [N][M]: specially by re-reading the line.
						// We signal "indexed element with sub-array" via null ColumnsRaw and a special ValueRaw.
						return new LineInfo(LineType.IndexedElement, indent, $"[{idx}]", idx, null,
							subValues.Length > 0 ? $"[{subCount}]:{subValues}" : $"[{subCount}]:");
					}
				}
			}

			// [N]: value
			if (afterBracket.StartsWith(':'))
			{
				string value = afterBracket[1..].TrimStart();
				return new LineInfo(LineType.IndexedElement, indent, $"[{idx}]", idx, null,
					value.Length > 0 ? value : null);
			}

			return new LineInfo(LineType.TabularRow, indent, string.Empty, -1, null, content);
		}

		private LineInfo classifyArrayHeader(string content, int indent, string key, int keyEnd)
		{
			// content[keyEnd] == '['
			int closeCount = content.IndexOf(']', keyEnd + 1);
			if (closeCount < 0)
			{
				return new LineInfo(LineType.PrimitiveValue, indent, key, -1, null,
					content[(keyEnd + 1)..]);
			}

			string countStr = content[(keyEnd + 1)..closeCount];
			if (!int.TryParse(countStr, out int count))
			{
				return new LineInfo(LineType.PrimitiveValue, indent, key, -1, null,
					content[(keyEnd + 1)..]);
			}

			string afterClose = content[(closeCount + 1)..];

			// key[N]{cols}:
			if (afterClose.StartsWith('{'))
			{
				int closeCol = afterClose.IndexOf('}');
				if (closeCol >= 0)
				{
					string colsRaw = afterClose[1..closeCol];
					// remainder after '}' should be ':'
					return new LineInfo(LineType.ArrayHeader, indent, key, count, colsRaw, null);
				}
			}

			// key[N]: values or key[N]:
			if (afterClose.StartsWith(':'))
			{
				string value = afterClose[1..].TrimStart();
				if (value.Length > 0)
				{
					return new LineInfo(LineType.InlinePrimitiveArray, indent, key, count, null, value);
				}

				return new LineInfo(LineType.ArrayHeader, indent, key, count, null, null);
			}

			// Fallback
			return new LineInfo(LineType.ArrayHeader, indent, key, count, null, null);
		}

		private static LineInfo classifyObjectHeader(string content, int indent, string key, int keyEnd)
		{
			// content[keyEnd] == '{'
			int closeCol = content.IndexOf('}', keyEnd + 1);
			if (closeCol < 0)
			{
				// Malformed — treat as primitive
				return new LineInfo(LineType.PrimitiveValue, indent, key, -1, null,
					content[(keyEnd + 1)..]);
			}

			string colsRaw = content[(keyEnd + 1)..closeCol];
			return new LineInfo(LineType.ObjectHeader, indent, key, -1, colsRaw, null);
		}

		// <summary>
		/// Extracts a key from the start of <paramref name="content"/>, handling quoted keys.
		/// Returns the key (unquoted) and sets <paramref name="keyEnd"/> to the character position
		/// immediately after the key in <paramref name="content"/>.
		/// </summary>
		private string extractKey(string content, out int keyEnd)
		{
			if (content.Length == 0)
			{
				keyEnd = 0;
				return string.Empty;
			}

			if (content[0] == '"')
			{
				// Quoted key
				int i = 1;
				var sb = new System.Text.StringBuilder();
				while (i < content.Length)
				{
					char c = content[i];
					if (c == '\\' && i + 1 < content.Length)
					{
						char next = content[i + 1];
						sb.Append(next switch
						{
							'"' => '"',
							'\\' => '\\',
							'n' => '\n',
							'r' => '\r',
							't' => '\t',
							_ => next
						});
						i += 2;
					}
					else if (c == '"')
					{
						i++;
						break;
					}
					else
					{
						sb.Append(c);
						i++;
					}
				}

				keyEnd = i;
				return sb.ToString();
			}
			else
			{
				// Unquoted key: read until structural character
				int i = 0;
				while (i < content.Length)
				{
					char c = content[i];
					if (c == ':' || c == '{' || c == '[' || c == ' ' || c == '\t')
					{
						break;
					}

					i++;
				}

				keyEnd = i;
				return content[..i];
			}
		}

		private List<string> splitDelimited(string raw)
		{
			var fields = new List<string>();
			var current = new System.Text.StringBuilder();
			char delim = _delimiter.AsChar();
			int i = 0;

			while (i < raw.Length)
			{
				char c = raw[i];

				// Delimiter
				if (c == delim)
				{
					fields.Add(current.ToString().Trim());
					current.Clear();
					i++;
					continue;
				}

				// Start of quoted string
				if (c == '"')
				{
					// Triple quote?
					if (i + 2 < raw.Length && raw[i + 1] == '"' && raw[i + 2] == '"')
					{
						i += 3; // skip opening """
						// InTripleQuote state
						while (i < raw.Length)
						{
							if (raw[i] == '\\' && i + 1 < raw.Length)
							{
								char next = raw[i + 1];
								// Check for escaped """
								if (next == '"' && i + 3 < raw.Length && raw[i + 2] == '"' && raw[i + 3] == '"')
								{
									current.Append("\"\"\"");
									i += 4;
								}
								else
								{
									current.Append(next switch
									{
										'\\' => '\\',
										'"' => '"',
										'n' => '\n',
										'r' => '\r',
										't' => '\t',
										_ => next
									});
									i += 2;
								}
							}
							else if (raw[i] == '"' && i + 2 < raw.Length && raw[i + 1] == '"' && raw[i + 2] == '"')
							{
								i += 3; // closing """
								break;
							}
							else
							{
								current.Append(raw[i]);
								i++;
							}
						}
					}
					else
					{
						i++; // skip opening "
						// InSingleQuote state
						while (i < raw.Length)
						{
							char sc = raw[i];
							if (sc == '\\' && i + 1 < raw.Length)
							{
								char next = raw[i + 1];
								current.Append(next switch
								{
									'"' => '"',
									'\\' => '\\',
									'n' => '\n',
									'r' => '\r',
									't' => '\t',
									_ => next
								});
								i += 2;
							}
							else if (sc == '"')
							{
								i++; // closing "
								break;
							}
							else
							{
								current.Append(sc);
								i++;
							}
						}
					}
				}
				else
				{
					current.Append(c);
					i++;
				}
			}

			fields.Add(current.ToString().Trim());
			return fields;
		}

		// -------------------------------------------------------------------------
		// parseValue
		// -------------------------------------------------------------------------

		private JsonNode? parseValue(string raw, ColumnDef? hint)
		{
			if (hint is not null)
			{
				return parseValueWithHint(raw, hint.Value);
			}

			return parseValueDefault(raw);
		}

		private JsonNode? parseValueDefault(string raw)
		{
			if (raw == "null")
			{
				return null;
			}

			if (raw == "true")
			{
				return JsonValue.Create(true);
			}

			if (raw == "false")
			{
				return JsonValue.Create(false);
			}

			if (raw.Length > 0 && (raw[0] == '"' || raw.StartsWith("\"\"\"", StringComparison.Ordinal)))
			{
				return JsonValue.Create(unquote(raw));
			}

			if (raw == "Infinity")
			{
				return JsonValue.Create(double.PositiveInfinity);
			}

			if (raw == "-Infinity")
			{
				return JsonValue.Create(double.NegativeInfinity);
			}

			if (raw == "NaN")
			{
				return JsonValue.Create(double.NaN);
			}

			// Try integer types from narrowest to widest
			if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
			{
				return JsonValue.Create(intVal);
			}

			if (uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintVal))
			{
				return JsonValue.Create(uintVal);
			}

			if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
			{
				return JsonValue.Create(longVal);
			}

			if (ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulongVal))
			{
				return JsonValue.Create(ulongVal);
			}

			if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleVal))
			{
				return JsonValue.Create(doubleVal);
			}

			// Unquoted string
			return JsonValue.Create(raw);
		}

		private JsonNode? parseValueWithHint(string raw, ColumnDef hint)
		{
			switch (hint.TypeHint)
			{
				case "str":
					if (raw.Length > 0 && (raw[0] == '"' || raw.StartsWith("\"\"\"", StringComparison.Ordinal)))
					{
						return JsonValue.Create(unquote(raw));
					}

					return JsonValue.Create(raw);

				case "bool":
					if (raw == "true") return JsonValue.Create(true);
					if (raw == "false") return JsonValue.Create(false);
					if (_strict)
					{
						throw new TonlParseException(
							$"Value '{raw}' cannot be parsed as bool at line {_cursor + 1}",
							_cursor + 1,
							_lines[_cursor]);
					}

					return parseValueDefault(raw);

				case "u32":
					if (uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint u32Val))
					{
						return JsonValue.Create(u32Val);
					}

					if (_strict)
					{
						throw new TonlParseException(
							$"Value '{raw}' cannot be parsed as u32 at line {_cursor + 1}",
							_cursor + 1,
							_lines[_cursor]);
					}

					return parseValueDefault(raw);

				case "i32":
					if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i32Val))
					{
						return JsonValue.Create(i32Val);
					}

					if (_strict)
					{
						throw new TonlParseException(
							$"Value '{raw}' cannot be parsed as i32 at line {_cursor + 1}",
							_cursor + 1,
							_lines[_cursor]);
					}

					return parseValueDefault(raw);

				case "f64":
					if (raw == "Infinity") return JsonValue.Create(double.PositiveInfinity);
					if (raw == "-Infinity") return JsonValue.Create(double.NegativeInfinity);
					if (raw == "NaN") return JsonValue.Create(double.NaN);
					if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double f64Val))
					{
						return JsonValue.Create(f64Val);
					}

					if (_strict)
					{
						throw new TonlParseException(
							$"Value '{raw}' cannot be parsed as f64 at line {_cursor + 1}",
							_cursor + 1,
							_lines[_cursor]);
					}

					return parseValueDefault(raw);

				case "null":
					if (raw == "null" || raw == string.Empty) return null;
					if (_strict)
					{
						throw new TonlParseException(
							$"Value '{raw}' cannot be parsed as null at line {_cursor + 1}",
							_cursor + 1,
							_lines[_cursor]);
					}

					return parseValueDefault(raw);

				default:
					return parseValueDefault(raw);
			}
		}

		private static string unquote(string raw)
		{
			if (raw.StartsWith("\"\"\"", StringComparison.Ordinal) && raw.Length >= 6)
			{
				string inner = raw[3..^3];
				return processEscapes(inner);
			}

			if (raw.StartsWith('"') && raw.Length >= 2)
			{
				string inner = raw[1..^1];
				return processEscapes(inner);
			}

			return raw;
		}

		private static string processEscapes(string s)
		{
			if (!s.Contains('\\', StringComparison.Ordinal))
			{
				return s;
			}

			var sb = new System.Text.StringBuilder(s.Length);
			int i = 0;
			while (i < s.Length)
			{
				if (s[i] == '\\' && i + 1 < s.Length)
				{
					char next = s[i + 1];
					sb.Append(next switch
					{
						'\\' => '\\',
						'"' => '"',
						'n' => '\n',
						'r' => '\r',
						't' => '\t',
						_ => next
					});
					i += 2;
				}
				else
				{
					sb.Append(s[i]);
					i++;
				}
			}

			return sb.ToString();
		}

		private ColumnDef[] parseColumnDefs(string columnsRaw)
		{
			if (columnsRaw.Trim().Length == 0)
			{
				return Array.Empty<ColumnDef>();
			}

			// Split on comma (always comma for column definitions, regardless of data delimiter)
			string[] segments = columnsRaw.Split(',');
			var defs = new ColumnDef[segments.Length];
			for (int i = 0; i < segments.Length; i++)
			{
				string seg = segments[i].Trim();
				int colonIdx = seg.IndexOf(':', StringComparison.Ordinal);
				if (colonIdx >= 0)
				{
					string name = seg[..colonIdx].Trim();
					string typeHint = seg[(colonIdx + 1)..].Trim();
					if (name.StartsWith('"'))
					{
						name = unquote(name);
					}

					defs[i] = new ColumnDef(name, typeHint);
				}
				else
				{
					string name = seg;
					if (name.StartsWith('"'))
					{
						name = unquote(name);
					}

					defs[i] = new ColumnDef(name, null);
				}
			}

			return defs;
		}

		

		private int getIndent(int lineIndex)
		{
			string line = _lines[lineIndex];
			int i = 0;
			while (i < line.Length && line[i] == ' ')
			{
				i++;
			}

			return i;
		}

		private bool isBlankOrComment(int lineIndex)
		{
			string line = _lines[lineIndex];
			string trimmed = line.TrimStart();
			if (trimmed.Length == 0)
			{
				return true;
			}

			if (trimmed[0] == '#')
			{
				// But NOT header lines — those were already consumed
				return true;
			}

			return false;
		}

		private void throwOrWarn(string message, int lineIndex)
		{
			if (_strict)
			{
				throw new TonlParseException(message, lineIndex + 1,
					lineIndex < _lines.Length ? _lines[lineIndex] : null);
			}
		}

		private static ColumnDef? findColumnDef(ColumnDef[]? columns, string key)
		{
			if (columns is null) return null;
			foreach (ColumnDef col in columns)
			{
				if (col.Name == key) return col;
			}

			return null;
		}
	}
}
