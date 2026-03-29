using System.Diagnostics.CodeAnalysis;

namespace Tonl.Net;

/// <summary>
/// Supported column separators to tabular data.
/// </summary>
public enum ColumnDelimiter : byte
{
	/// <summary>
	/// Most compact and standard. Default delimiter.
	/// </summary>
	Comma,

	/// <summary>
	/// Useful when data may contain commas.
	/// </summary>
	Pipe,

	/// <summary>
	/// Useful for visual alignment, spreadsheet-like.
	/// </summary>
	Tab,

	/// <summary>
	/// Useful when data may contain commas or pipes, CSV-like.
	/// </summary>
	Semicolon
}

internal static class ColumnDelimiterExtensions
{
	extension(ColumnDelimiter delimiter)
	{
		private char asChar()
		{
			char c = delimiter switch
			{
				ColumnDelimiter.Comma => ',',
				ColumnDelimiter.Pipe => '|',
				ColumnDelimiter.Tab => '\t',
				ColumnDelimiter.Semicolon => ';',
				_ => throw new ArgumentOutOfRangeException(nameof(delimiter), delimiter, null)
			};
			return c;
		}
		
		internal bool TryScape([NotNullWhen(true)] out string? escapedDelimiter)
		{
			switch (delimiter)
			{
				// no delimiter header needed for default
				case ColumnDelimiter.Comma:
					escapedDelimiter = null;
					return false;
				case ColumnDelimiter.Tab:
					escapedDelimiter = "\\t";
					return true;
				case ColumnDelimiter.Pipe:
					escapedDelimiter = "|";
					return true;
				case ColumnDelimiter.Semicolon:
					escapedDelimiter = ";";
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(delimiter));
			}
		}

		internal string JoinComfortably(List<string> parts)
		{
			string separator = delimiter switch
			{
				ColumnDelimiter.Pipe => " | ",
				ColumnDelimiter.Tab => "\t",
				_ => delimiter.asChar() + " "
			};
			string line = string.Join(separator, parts);
			return line;
		}

		public bool ContainedIn(string value)
		{
			bool contained = value.Contains(delimiter.asChar(), StringComparison.Ordinal);
			return contained;
		}

		internal char AsChar() => delimiter.asChar();
	}
}

internal static class ColumnDelimiterParser
{
	/// <summary>
	/// Attempts to parse the escaped delimiter string found in a <c>#delimiter</c> header.
	/// Recognises <c>","</c>, <c>"|"</c>, <c>";"</c>, <c>"\\t"</c> (two-character escape sequence),
	/// and a literal tab character.
	/// </summary>
	/// <param name="escaped">The escaped delimiter value from the header.</param>
	/// <param name="result">The parsed <see cref="ColumnDelimiter"/> when successful.</param>
	/// <returns><see langword="true"/> if the value was recognised; otherwise <see langword="false"/>.</returns>
	internal static bool TryParse(string escaped, out ColumnDelimiter result)
	{
		switch (escaped)
		{
			case ",":
				result = ColumnDelimiter.Comma;
				return true;
			case "|":
				result = ColumnDelimiter.Pipe;
				return true;
			case ";":
				result = ColumnDelimiter.Semicolon;
				return true;
			case "\\t":
			case "\t":
				result = ColumnDelimiter.Tab;
				return true;
			default:
				result = ColumnDelimiter.Comma;
				return false;
		}
	}
}