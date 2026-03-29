namespace Tonl.Net;

/// <summary>
/// Options that control TONL decoding behavior.
/// </summary>
public record TonlDecodeOptions
{
	/// <summary>
	/// Gets the fallback delimiter used when no <c>#delimiter</c> header is present in the document.
	/// When a <c>#delimiter</c> header is found it always overrides this option.
	/// Defaults to <see cref="ColumnDelimiter.Comma"/>.
	/// </summary>
	public ColumnDelimiter Delimiter { get; init; } = ColumnDelimiter.Comma;

	/// <summary>
	/// Gets a value indicating whether the decoder operates in strict mode.
	/// When <see langword="true"/>, ambiguous or invalid input throws <see cref="TonlParseException"/>.
	/// When <see langword="false"/>, best-effort parsing is used and malformed input is silently tolerated.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	public bool StrictMode { get; init; }
}
