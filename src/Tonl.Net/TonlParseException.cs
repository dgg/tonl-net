namespace Tonl.Net;

/// <summary>
/// The exception that is thrown when a TONL document cannot be parsed in strict mode.
/// </summary>
public class TonlParseException : FormatException
{
	/// <summary>
	/// Gets the 1-based line number where the parse error occurred.
	/// </summary>
	public int LineNumber { get; }

	/// <summary>
	/// Gets the raw content of the line where the parse error occurred, if available.
	/// </summary>
	public string? LineContent { get; }

	/// <summary>
	/// Initializes a new instance of <see cref="TonlParseException"/> with the specified message,
	/// line number, and optional line content.
	/// </summary>
	/// <param name="message">A message that describes the parse error.</param>
	/// <param name="lineNumber">The 1-based line number where the error occurred.</param>
	/// <param name="lineContent">The raw text of the offending line, or <see langword="null"/> if not available.</param>
	public TonlParseException(string message, int lineNumber, string? lineContent = null)
		: base(message)
	{
		LineNumber = lineNumber;
		LineContent = lineContent;
	}
}
