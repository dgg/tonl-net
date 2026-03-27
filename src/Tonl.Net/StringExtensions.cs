using System.Text.RegularExpressions;

namespace Tonl.Net;

/// <summary>
/// Internal string quoting and escaping utilities for TONL encoding.
/// </summary>
internal static partial class StringExtensions
{
	// Regex patterns for numeric-like strings that must be quoted.
	[GeneratedRegex(@"^-?\d+$")]
	private static partial Regex integerPattern();

	[GeneratedRegex(@"^-?\d*\.\d+$")]
	private static partial Regex decimalPattern();

	[GeneratedRegex(@"^-?\d+\.?\d*[eE][+-]?\d+$")]
	private static partial Regex scientificPattern();


	/// <param name="value">The string value.</param>
	extension(string value)
	{
		private bool needsQuoting(ColumnDelimiter delimiter)
		{
			if (value.Length == 0) return true;
			switch (value)
			{
				case "true" or "false":
				case "null" or "undefined":
				case "Infinity" or "-Infinity" or "NaN":
					return true;
			}

			if (integerPattern().IsMatch(value)) return true;
			if (decimalPattern().IsMatch(value)) return true;
			if (scientificPattern().IsMatch(value)) return true;

			bool result = delimiter.ContainedIn(value)
				|| value.Contains(':')
				|| value.Contains('{')
				|| value.Contains('}')
				|| value.Contains('#')
				|| value.Contains('"')
				|| value.Contains('\\')
				|| value.Contains('\n')
				|| value.Contains('\t')
				|| value.Contains('\r')
				|| value.StartsWith(' ')
				|| value.EndsWith(' ');
			return result;
		}

		private string quote()
		{
			var escaped = value
				.Replace("\\", "\\\\", StringComparison.Ordinal)
				.Replace("\r", "\\r", StringComparison.Ordinal)
				.Replace("\n", "\\n", StringComparison.Ordinal)
				.Replace("\t", "\\t", StringComparison.Ordinal)
				.Replace("\"", "\\\"", StringComparison.Ordinal);
			return $"\"{escaped}\"";
		}

		private string tripleQuote()
		{
			var escaped = value
				.Replace("\\", "\\\\", StringComparison.Ordinal)
				.Replace("\r", "\\r", StringComparison.Ordinal)
				.Replace("\n", "\\n", StringComparison.Ordinal)
				.Replace("\t", "\\t", StringComparison.Ordinal)
				.Replace("\"\"\"", "\\\"\"\"", StringComparison.Ordinal);
			return $"\"\"\"{escaped}\"\"\"";
		}

		private string quoteIfNeeded(ColumnDelimiter delimiter) => value.needsQuoting(delimiter)
			? value.quote()
			: value;

		/// <summary>
		/// Uses triple-quoting when the value contains newlines or embedded triple-quotes;
		/// otherwise single-quotes if the value would be ambiguous without quotes.
		/// </summary>
		/// <param name="delimiter">The active field delimiter.</param>
		/// <returns>The value, triple-quoted, single-quoted, or unquoted as needed.</returns>
		public string QuoteIfNeeded(ColumnDelimiter delimiter)
		{
			if (value.Contains('\n') || value.Contains("\"\"\"", StringComparison.Ordinal))
			{
				return value.tripleQuote();
			}

			return value.quoteIfNeeded(delimiter);
		}
	}

	/// <param name="key">The key name.</param>
	extension(string key)
	{
		private bool needsQuoting()
		{
			return key.Contains(':')
				|| key.Contains(',')
				|| key.Contains('{')
				|| key.Contains('}')
				|| key.Contains('"')
				|| key.Contains('#')
				|| key.Contains('@')
				|| key.Trim().Length == 0
				|| key.StartsWith(' ')
				|| key.EndsWith(' ')
				|| key.Contains('\t')
				|| key.Contains('\n')
				|| key.Contains('\r');
		}

		/// <summary>
		/// Quotes a key name if it contains any structural characters, otherwise returns it unchanged.
		/// </summary>
		/// <returns>The key, quoted if necessary.</returns>
		public string QuoteKeyIfNeeded()
		{
			if (key.needsQuoting())
			{
				var escaped = key
					.Replace("\\", "\\\\", StringComparison.Ordinal)
					.Replace("\r", "\\r", StringComparison.Ordinal)
					.Replace("\n", "\\n", StringComparison.Ordinal)
					.Replace("\t", "\\t", StringComparison.Ordinal)
					.Replace("\"", "\\\"", StringComparison.Ordinal);
				return $"\"{escaped}\"";
			}

			return key;
		}
	}
}
