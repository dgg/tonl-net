namespace Tonl.Net;

/// <summary>
/// Represents a TONL document version.
/// </summary>
/// <param name="Major">The major version number.</param>
/// <param name="Minor">The minor version number.</param>
public readonly record struct TonlVersion(ushort Major, ushort Minor = 0)
{
	/// <summary>
	/// Converts the value of the current <see cref="TonlVersion"/> to its equivalent <see cref="String"/> representation.
	/// </summary>
	/// <returns>The <see cref="String"/> representation of the values of the major and minor components of the current <see cref="TonlVersion"/>.
	/// Each component is separated by a period character ('.').</returns>
	public override string ToString() => $"{Major:N0}.{Minor:N0}";

	/// <summary>
	/// Converts the value of the current <see cref="TonlVersion"/> to its equivalent TONL string representation.
	/// </summary>
	/// <returns>The TONL string representation to be rendered in the header.</returns>
	public string ToTonl() => $"#version {this}";

	/// <summary>
	/// The default TONL version (1.0).
	/// </summary>
	public static readonly TonlVersion Default = new(1, 0);

	/// <summary>
	/// Creates a new TONL version from a <see cref="Version"/>.
	/// </summary>
	/// <param name="version">The version.</param>
	/// <returns>The equivalent <see cref="TonlVersion"/> to <paramref name="version"/>.</returns>
	public static TonlVersion FromVersion(Version version) =>
		new(Convert.ToUInt16(version.Major), Convert.ToUInt16(version.Minor));
}