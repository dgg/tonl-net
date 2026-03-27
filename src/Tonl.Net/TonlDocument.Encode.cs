using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Tonl.Net;

public partial class TonlDocument
{
	/// <summary>
	/// Serializes this document to a TONL-formatted string.
	/// </summary>
	/// <param name="options">Options controlling encoding behavior. When <see langword="null"/>, default options are used.</param>
	/// <returns>The full TONL document string including the <c>#version</c> header.</returns>
	public string Encode(TonlEncodeOptions? options = null)
	{
		TonlEncodeOptions opts = options ?? new TonlEncodeOptions();
		_sb.Clear();
		encodeHeader(opts);
		encodeValue(Root, "root", 0, opts);
		return _sb.ToString();
	}

	private readonly StringBuilder _sb = new ();
	private readonly HashSet<JsonNode> _seen = new (ReferenceEqualityComparer.Instance);
	private int _depth;

	private void encodeHeader(TonlEncodeOptions options)
	{
		_sb.Append('#').Append("version ").Append(options.Version);
		if (options.Delimiter.TryScape(out string? escapedDelimiter))
		{
			_sb.Append('\n').Append('#').Append("delimiter ").Append(escapedDelimiter);
		}
	}

	private void encodeValue(JsonNode? value, string key, int indent, TonlEncodeOptions options)
	{
		switch (value)
		{
			case null:
				appendLine(indent, key, ": null");
				break;
			case JsonObject obj:
				encodeObject(obj, key, indent, options);
				break;
			case JsonArray arr:
				encodeArray(arr, key, indent, options);
				break;
			case JsonValue jsonValue:
				encodePrimitive(jsonValue, key, indent, options.Delimiter);
				break;
		}
	}

	private void encodePrimitive(JsonValue value, string key, int indent, ColumnDelimiter delimiter)
	{
		string raw = getPrimitiveString(value, delimiter);
		appendLine(indent, key, ": " + raw);
	}

	private string getPrimitiveString(JsonValue value, ColumnDelimiter delimiter)
	{
		if (value.TryGetValue(out bool boolVal))
		{
			return boolVal ? "true" : "false";
		}

		if (value.TryGetValue(out int intVal))
		{
			return intVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out uint uintVal))
		{
			return uintVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out long longVal))
		{
			return longVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out ulong ulongVal))
		{
			return ulongVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out double doubleVal))
		{
			return !double.IsFinite(doubleVal)
				? "null"
				: doubleVal.ToString("G", CultureInfo.InvariantCulture);
		}

		return value.TryGetValue(out string? strVal)
			? strVal.QuoteIfNeeded(delimiter)
			: "null";
	}

	private void encodeObject(JsonObject obj, string key, int indent, TonlEncodeOptions options)
	{
		checkCycleAndDepth(obj);
		_seen.Add(obj);
		_depth++;
		try
		{
			var sortedKeys = obj.Select(p => p.Key)
				.OrderBy(k => k, StringComparer.Ordinal)
				.ToList();

			if (sortedKeys.Count == 0)
			{
				appendLine(indent, key, ":");
				return;
			}

			// Single-line format: objects with ≥2 properties where ALL values are
			// primitives (no nested objects or arrays) — matches spec decision tree.
			//bool flat = sortedKeys.Count >= 2 && sortedKeys.All(k => obj[k] is JsonValue or null);
			//if (flat)
			//{
			//	encodeSingleLineObject(obj, key, indent, sortedKeys);
			//}
			//else
			//{
				encodeMultiLineObject(obj, key, indent, sortedKeys, options);
			//}
		}
		finally
		{
			_seen.Remove(obj);
			_depth--;
		}
	}

	private void encodeSingleLineObject(JsonObject obj, string key, int indent, IReadOnlyList<string> sortedKeys, TonlEncodeOptions options)
	{
		string header = buildObjectHeader(key, obj, sortedKeys, options.IncludeTypes);
		var parts = sortedKeys.Select(k =>
		{
			string quotedKey = k.QuoteKeyIfNeeded();
			JsonNode? child = obj[k];
			string val = child is JsonValue v ? getPrimitiveString(v, options.Delimiter) : "null";
			return quotedKey + ": " + val;
		});
		_sb.Append('\n').Append(' ', indent).Append(header).Append(": ").Append(string.Join(" ", parts));
	}

	private void encodeMultiLineObject(JsonObject obj, string key, int indent, IReadOnlyList<string> sortedKeys, TonlEncodeOptions options)
	{
		string header = buildObjectHeader(key, obj, sortedKeys, options.IncludeTypes);
		appendLine(indent, header, ":");
		int childIndent = indent + options.IndentSize;
		foreach (string childKey in sortedKeys)
		{
			JsonNode? child = obj[childKey];
			string quotedKey = childKey.QuoteKeyIfNeeded();
			encodeValue(child, quotedKey, childIndent, options);
		}
	}

	private string buildObjectHeader(string key, JsonObject obj, IReadOnlyList<string> sortedKeys, bool includeTypes)
	{
		if (sortedKeys.Count <= 1)
		{
			return key;
		}

		var cols = sortedKeys.Select(k =>
		{
			string colName = k.QuoteKeyIfNeeded();
			if (includeTypes)
			{
				string type = obj[k].InferType();
				if (type is not "obj" and not "list")
				{
					return colName + ":" + type;
				}
			}

			return colName;
		});

		return key + "{" + string.Join(",", cols) + "}";
	}

	private void encodeArray(JsonArray arr, string key, int indent, TonlEncodeOptions options)
	{
		checkCycleAndDepth(arr);
		_seen.Add(arr);
		_depth++;
		try
		{
			if (arr.Count == 0)
			{
				appendLine(indent, key + "[0]", ":");
				return;
			}

			bool allPrimitives = arr.All(e => e is JsonValue or null);
			if (allPrimitives)
			{
				encodePrimitiveArray(arr, key, indent, options.Delimiter);
				return;
			}

			if (arr.IsUniformObjectArray() || arr.IsSemiUniformObjectArray())
			{
				bool allObjectsHaveOnlyPrimitives = arr.All(e =>
					e is JsonObject obj2 && obj2.All(p => p.Value is JsonValue or null));

				if (allObjectsHaveOnlyPrimitives)
				{
					IReadOnlyList<string> columns = arr.IsUniformObjectArray()
						? arr.GetUniformColumns()
						: arr.GetAllColumns();
					encodeTabularArray(arr, key, indent, columns, options);
					return;
				}
			}

			encodeMixedArray(arr, key, indent, options);
		}
		finally
		{
			_seen.Remove(arr);
			_depth--;
		}
	}

	private void encodePrimitiveArray(JsonArray arr, string key, int indent, ColumnDelimiter delimiter)
	{
		var parts = new List<string>(arr.Count);
		foreach (JsonNode? element in arr)
		{
			if (element is null)
			{
				parts.Add("null");
			}
			else if (element is JsonValue v)
			{
				parts.Add(getPrimitiveString(v, delimiter));
			}
			else
			{
				parts.Add("null");
			}
		}

		string line = delimiter.JoinComfortably(parts);
		appendLine(indent, key + "[" + arr.Count + "]", ": " + line);
	}

	private void encodeTabularArray(JsonArray arr, string key, int indent, IReadOnlyList<string> columns, TonlEncodeOptions options)
	{
		var colDefs = columns.Select(c =>
		{
			string colName = c.QuoteKeyIfNeeded();
			if (options.IncludeTypes)
			{
				foreach (JsonNode? el in arr)
				{
					if (el is JsonObject obj2 && obj2.TryGetPropertyValue(c, out JsonNode? val) && val is not null)
					{
						string type = val.InferType();
						if (type is not "obj" and not "list")
						{
							return colName + ":" + type;
						}

						break;
					}
				}
			}

			return colName;
		}).ToList();

		string header = key + "[" + arr.Count + "]{" + string.Join(",", colDefs) + "}";
		appendLine(indent, header, ":");

		int rowIndent = indent + options.IndentSize;
		foreach (JsonNode? element in arr)
		{
			var obj = (JsonObject)element!;
			var rowValues = columns.Select(col =>
			{
				if (!obj.TryGetPropertyValue(col, out JsonNode? val) || val is null)
				{
					return string.Empty;
				}

				return val is JsonValue v ? getTabularPrimitiveString(v, options.Delimiter) : string.Empty;
			});

			string row = options.Delimiter.JoinComfortably(rowValues.ToList());
			_sb.Append('\n').Append(' ', rowIndent).Append(row);
		}
	}

	private string getTabularPrimitiveString(JsonValue value, ColumnDelimiter delimiter)
	{
		if (value.TryGetValue(out bool boolVal))
		{
			return boolVal ? "true" : "false";
		}

		if (value.TryGetValue(out int intVal))
		{
			return intVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out uint uintVal))
		{
			return uintVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out long longVal))
		{
			return longVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out ulong ulongVal))
		{
			return ulongVal.ToString(CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out double doubleVal))
		{
			if (!double.IsFinite(doubleVal))
			{
				return "null";
			}

			return doubleVal.ToString("G", CultureInfo.InvariantCulture);
		}

		if (value.TryGetValue(out string? strVal))
		{
			return strVal.QuoteIfNeeded(delimiter);
		}

		return string.Empty;
	}

	private void encodeMixedArray(JsonArray arr, string key, int indent, TonlEncodeOptions options)
	{
		appendLine(indent, key + "[" + arr.Count + "]", ":");
		int childIndent = indent + options.IndentSize;

		for (int i = 0; i < arr.Count; i++)
		{
			JsonNode? element = arr[i];
			string indexKey = "[" + i + "]";

			switch (element)
			{
				case null:
					appendLine(childIndent, indexKey, ": null");
					break;
				case JsonValue v:
					string raw = getPrimitiveString(v, options.Delimiter);
					appendLine(childIndent, indexKey, ": " + raw);
					break;
				case JsonObject obj:
					encodeObject(obj, indexKey, childIndent, options);
					break;
				case JsonArray subArr:
					if (subArr.Count == 0 || subArr.All(e => e is JsonValue or null))
					{
						encodePrimitiveArray(subArr, indexKey, childIndent, options.Delimiter);
					}
					else
					{
						encodeArray(subArr, indexKey, childIndent, options);
					}

					break;
			}
		}
	}

	private void checkCycleAndDepth(JsonNode node)
	{
		if (_seen.Contains(node))
		{
			throw new InvalidOperationException("Circular reference detected during TONL encoding.");
		}

		if (_depth >= TonlEncodeOptions.MaxDepth)
		{
			throw new InvalidOperationException($"Maximum encoding depth of {TonlEncodeOptions.MaxDepth} exceeded.");
		}
	}

	private void appendLine(int indent, string key, string suffix)
	{
		_sb.Append('\n').Append(' ', indent).Append(key).Append(suffix);
	}
}