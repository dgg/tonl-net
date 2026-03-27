using System.Text.Json.Nodes;

namespace Tonl.Net;

/// <summary>
/// Extension methods on <see cref="JsonNode"/> and <see cref="JsonArray"/> for TONL
/// type inference and array classification.
/// </summary>
internal static class NodeExtensions
{
	/// <summary>
	/// Infers the TONL type hint string for this node.
	/// Returns one of <c>"null"</c>, <c>"bool"</c>, <c>"u32"</c>, <c>"i32"</c>,
	/// <c>"f64"</c>, <c>"str"</c>, <c>"obj"</c>, or <c>"list"</c>.
	/// </summary>
	/// <param name="value">The JSON node to classify. May be <see langword="null"/>.</param>
	public static string InferType(this JsonNode? value)
	{
		if (value is null)
		{
			return "null";
		}

		if (value is JsonObject)
		{
			return "obj";
		}

		if (value is JsonArray)
		{
			return "list";
		}

		if (value is JsonValue jsonValue)
		{
			// Boolean must be tested before numeric types because some JSON backends
			// will also successfully parse a bool as an integer (0/1).
			if (jsonValue.TryGetValue<bool>(out _))
			{
				return "bool";
			}

			// Probe integer types from narrowest to widest so that a C# literal `42`
			// (stored as int) is matched by TryGetValue<int>, not a wider type.
			if (jsonValue.TryGetValue<int>(out int intVal))
			{
				return intVal >= 0 ? "u32" : "i32";
			}

			if (jsonValue.TryGetValue<uint>(out _))
			{
				return "u32";
			}

			if (jsonValue.TryGetValue<long>(out long longVal))
			{
				if (longVal >= 0 && longVal <= uint.MaxValue)
				{
					return "u32";
				}

				return longVal >= int.MinValue ? "i32" : "f64";
			}

			if (jsonValue.TryGetValue<ulong>(out ulong ulongVal))
			{
				return ulongVal <= uint.MaxValue ? "u32" : "f64";
			}

			if (jsonValue.TryGetValue<double>(out _))
			{
				return "f64";
			}

			if (jsonValue.TryGetValue<string>(out _))
			{
				return "str";
			}
		}

		return "str";
	}

	/// <summary>
	/// Returns <see langword="true"/> if all elements of this array are
	/// <see cref="JsonObject"/> instances with identical sorted key sets.
	/// An empty array returns <see langword="true"/>.
	/// </summary>
	/// <param name="arr">The array to test.</param>
	public static bool IsUniformObjectArray(this JsonArray arr)
	{
		if (arr.Count == 0)
		{
			return true;
		}

		foreach (JsonNode? element in arr)
		{
			if (element is not JsonObject)
			{
				return false;
			}
		}

		var first = (JsonObject)arr[0]!;
		var referenceKeys = new SortedSet<string>(first.Select(p => p.Key), StringComparer.Ordinal);

		for (int i = 1; i < arr.Count; i++)
		{
			var obj = (JsonObject)arr[i]!;
			var keys = new SortedSet<string>(obj.Select(p => p.Key), StringComparer.Ordinal);

			if (!referenceKeys.SetEquals(keys))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the average key-overlap ratio across all elements
	/// meets or exceeds <paramref name="threshold"/>. All elements must be
	/// <see cref="JsonObject"/> instances.
	/// </summary>
	/// <param name="arr">The array to test.</param>
	/// <param name="threshold">
	/// The minimum average overlap ratio (default <c>0.6</c>, matching the reference implementation).
	/// </param>
	public static bool IsSemiUniformObjectArray(this JsonArray arr, double threshold = 0.6)
	{
		if (arr.Count == 0)
		{
			return true;
		}

		foreach (JsonNode? element in arr)
		{
			if (element is not JsonObject)
			{
				return false;
			}
		}

		var unionKeys = arr.GetAllColumns();
		if (unionKeys.Count == 0)
		{
			return true;
		}

		int totalUnion = unionKeys.Count;
		double totalOverlap = 0.0;

		foreach (JsonNode? element in arr)
		{
			var obj = (JsonObject)element!;
			int matchCount = obj.Count(p => unionKeys.Contains(p.Key));
			totalOverlap += (double)matchCount / totalUnion;
		}

		double averageOverlap = totalOverlap / arr.Count;
		return averageOverlap >= threshold;
	}

	/// <summary>
	/// Returns the sorted union of all keys from every object element in this array.
	/// </summary>
	/// <param name="arr">The array of objects.</param>
	public static IReadOnlyList<string> GetAllColumns(this JsonArray arr)
	{
		var unionKeys = new SortedSet<string>(StringComparer.Ordinal);
		foreach (JsonNode? element in arr)
		{
			if (element is JsonObject obj)
			{
				foreach (var property in obj)
				{
					unionKeys.Add(property.Key);
				}
			}
		}

		return unionKeys.ToList();
	}

	/// <summary>
	/// Returns the sorted keys of the first element in this array.
	/// Intended for strictly uniform arrays where all objects share identical key sets.
	/// </summary>
	/// <param name="arr">The uniform array of objects.</param>
	public static IReadOnlyList<string> GetUniformColumns(this JsonArray arr)
	{
		if (arr.Count == 0)
		{
			return Array.Empty<string>();
		}

		if (arr[0] is not JsonObject first)
		{
			return Array.Empty<string>();
		}

		return first.Select(p => p.Key)
					.OrderBy(k => k, StringComparer.Ordinal)
					.ToList();
	}
}
