namespace Tonl.Net;

/// <summary>
/// Options that control TONL encoding behavior.
/// </summary>
public record TonlEncodeOptions
{
	/// <summary>
	/// Gets the delimiter character used to separate values in arrays and tabular rows.
	/// Supported values are <c>","</c>, <c>"|"</c>, <c>"\t"</c>, and <c>";"</c>.
	/// Defaults to <c>","</c>.
	/// </summary>
	public ColumnDelimiter Delimiter { get; init; } = ColumnDelimiter.Comma;

	/// <summary>
	/// Gets a value indicating whether type hints are emitted in object and array headers.
	/// When <see langword="true"/>, column definitions include a <c>:type</c> suffix
	/// (e.g., <c>age:u32</c>, <c>name:str</c>). Structural types (<c>obj</c> and <c>list</c>)
	/// are never emitted as type hints. Defaults to <see langword="false"/>.
	/// </summary>
	public bool IncludeTypes { get; init; } = false;

	/// <summary>
	/// Gets the TONL version string written to the <c>#version</c> header.
	/// Defaults to <c>"1.0"</c>.
	/// </summary>
	public string Version { get; init; } = "1.0";

	/// <summary>
	/// Gets the number of spaces used per nesting level when indenting object and array bodies.
	/// Defaults to <c>2</c>.
	/// </summary>
	public byte IndentSize { get; init; } = 2;

	/// <summary>
	/// Gets the maximum allowed depth of nested objects and arrays. If exceeded, encoding will throw an exception.
	/// </summary>
	public static readonly ushort MaxDepth = 500;
}
