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
	}
}