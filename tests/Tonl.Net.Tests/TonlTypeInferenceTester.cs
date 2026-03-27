using System.Text.Json.Nodes;
using Tonl.Net;

namespace Tonl.Net.Tests;

[TestFixture]
public class TonlTypeInferenceTester
{
	// ── InferType ─────────────────────────────────────────────────────────────

	[Test]
	public void InferType_Null_Null() =>
		Assert.That(((JsonNode?)null).InferType(), Is.EqualTo("null"));

	[Test]
	public void InferType_True_Bool() =>
		Assert.That(JsonValue.Create(true).InferType(), Is.EqualTo("bool"));

	[Test]
	public void InferType_False_Bool() =>
		Assert.That(JsonValue.Create(false).InferType(), Is.EqualTo("bool"));

	[Test]
	public void InferType_Zero_U32() =>
		Assert.That(JsonValue.Create(0).InferType(), Is.EqualTo("u32"));

	[Test]
	public void InferType_PositiveInt_U32() =>
		Assert.That(JsonValue.Create(42).InferType(), Is.EqualTo("u32"));

	[Test]
	public void InferType_UIntMax_U32() =>
		Assert.That(JsonValue.Create(uint.MaxValue).InferType(), Is.EqualTo("u32"));

	[Test]
	public void InferType_NegativeOne_I32() =>
		Assert.That(JsonValue.Create(-1).InferType(), Is.EqualTo("i32"));

	[Test]
	public void InferType_IntMin_I32() =>
		Assert.That(JsonValue.Create(int.MinValue).InferType(), Is.EqualTo("i32"));

	[Test]
	public void InferType_Float_F64() =>
		Assert.That(JsonValue.Create(3.14).InferType(), Is.EqualTo("f64"));

	[Test]
	public void InferType_LargeFloat_F64() =>
		Assert.That(JsonValue.Create(1e20).InferType(), Is.EqualTo("f64"));

	[Test]
	public void InferType_String_Str() =>
		Assert.That(JsonValue.Create("hello").InferType(), Is.EqualTo("str"));

	[Test]
	public void InferType_JsonObject_Obj() =>
		Assert.That(new JsonObject().InferType(), Is.EqualTo("obj"));

	[Test]
	public void InferType_JsonArray_List() =>
		Assert.That(new JsonArray().InferType(), Is.EqualTo("list"));

	// ── IsUniformObjectArray ──────────────────────────────────────────────────

	[Test]
	public void IsUniformObjectArray_AllSameKeys_True()
	{
		var arr = new JsonArray(
			new JsonObject { ["a"] = 1, ["b"] = 2 },
			new JsonObject { ["a"] = 3, ["b"] = 4 }
		);
		Assert.That(arr.IsUniformObjectArray(), Is.True);
	}

	[Test]
	public void IsUniformObjectArray_DifferentKeys_False()
	{
		var arr = new JsonArray(
			new JsonObject { ["a"] = 1 },
			new JsonObject { ["b"] = 2 }
		);
		Assert.That(arr.IsUniformObjectArray(), Is.False);
	}

	[Test]
	public void IsUniformObjectArray_EmptyArray_True() =>
		Assert.That(new JsonArray().IsUniformObjectArray(), Is.True);

	[Test]
	public void IsUniformObjectArray_NonObjectElement_False()
	{
		var arr = new JsonArray(new JsonObject { ["a"] = 1 }, JsonValue.Create(42));
		Assert.That(arr.IsUniformObjectArray(), Is.False);
	}

	// ── IsSemiUniformObjectArray ──────────────────────────────────────────────

	[Test]
	public void IsSemiUniformObjectArray_SeventyPercentOverlap_True()
	{
		// Union keys: a, b, c, d, e (5). Each object has 4 → overlap = 4/5 = 0.8 per object
		var arr = new JsonArray(
			new JsonObject { ["a"] = 1, ["b"] = 2, ["c"] = 3, ["d"] = 4 },
			new JsonObject { ["a"] = 1, ["b"] = 2, ["c"] = 3, ["e"] = 5 }
		);
		Assert.That(arr.IsSemiUniformObjectArray(), Is.True);
	}

	// ── GetAllColumns ─────────────────────────────────────────────────────────

	[Test]
	public void GetAllColumns_SortedUnionOfAllKeys()
	{
		var arr = new JsonArray(
			new JsonObject { ["z"] = 1, ["a"] = 2 },
			new JsonObject { ["m"] = 3, ["a"] = 4 }
		);
		IReadOnlyList<string> cols = arr.GetAllColumns();
		Assert.That(cols, Is.EqualTo(new[] { "a", "m", "z" }));
	}

	// ── GetUniformColumns ─────────────────────────────────────────────────────

	[Test]
	public void GetUniformColumns_SortedKeysOfFirstElement()
	{
		var arr = new JsonArray(
			new JsonObject { ["z"] = 1, ["a"] = 2 },
			new JsonObject { ["z"] = 3, ["a"] = 4 }
		);
		IReadOnlyList<string> cols = arr.GetUniformColumns();
		Assert.That(cols, Is.EqualTo(new[] { "a", "z" }));
	}
}
