using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tonl.Net;

/// <summary>
/// Provides the high-level interface for working with TONL documents.
/// </summary>
/// <param name="node">The underlying document.</param>
public class TonlDocument(JsonNode? node)
{
	/// <summary>
	/// Gets the underlying document associated with the TONL document.
	/// </summary>
	public JsonNode? Node => node;

	/// <summary>
	/// Converts the specified data to a TONL document.
	/// </summary>
	/// <param name="data">The data to convert.</param>
	/// <param name="options">Options to control the conversion behavior.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns>A representation of the data in as a TONL document.</returns>
	public static TonlDocument FromData<T>(T data, JsonSerializerOptions? options = null)
	{
		JsonNode? node = JsonSerializer.SerializeToNode(data, options);
		var doc = new TonlDocument(node);
		return doc;
	}
}
