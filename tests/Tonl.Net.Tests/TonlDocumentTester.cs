using System.Text.Json.Nodes;

using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public class TonlDocumentTester
{
	#region Encode

	[Test]
	public void Encode_NullRoot_NullLine()
	{
		JsonNode? nil = null;
		string encoded =
			"""
			#version 1.0
			root: null
			""";

		Assert.That(new Subject(nil).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	[TestCase(false, "#version 1.0\nroot: false")]
	[TestCase(42, "#version 1.0\nroot: 42")]
	[TestCase(3.14d, "#version 1.0\nroot: 3.14")]
	[TestCase("Alice", "#version 1.0\nroot: Alice", Description = "Unquoted value")]
	[TestCase("123", "#version 1.0\nroot: \"123\"", Description = "Ambiguous -> quoted value")]
	// TODO: single-precision ? [TestCase(2.71828f, "#version 1.0\nroot: 2.71828")]
	public void Encode_Primitive_ScalarRoot<T>(T primitive, string encoded)
	{
		JsonNode? value = JsonValue.Create(primitive);
		Assert.That(new Subject(value).Encode(), Is.EqualTo(encoded));
	}

	#region objects

	[Test]
	public void Encode_EmptyObject_SingleLineNoCols()
	{
		var empty = new JsonObject();
		string encoded = "#version 1.0\nroot:";
		Assert.That(new Subject(empty).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	[TestCase("flag", true, "#version 1.0\nroot:\n  flag: true")]
	[TestCase("count", 42, "#version 1.0\nroot:\n  count: 42")]
	[TestCase("pi", 3.14, "#version 1.0\nroot:\n  pi: 3.14")]
	[TestCase("name", "Alice", "#version 1.0\nroot:\n  name: Alice", Description = "Plain string -> unquoted")]
	[TestCase("value", "123", "#version 1.0\nroot:\n  value: \"123\"", Description = "Ambiguous string -> quoted")]
	public void Encode_ObjectWithSinglePrimitive_RootWithoutColumns<T>(string key, T value, string encoded)
	{
		var withSinglePrimitive = new JsonObject
		{
			{ key, JsonValue.Create(value) }
		};
		Assert.That(new Subject(withSinglePrimitive).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_FlatObject_SortedKeysSingleLine()
	{
		var unsortedKeys = new JsonObject { { "z", 1 }, { "a", 2 } };

		string encoded =
		"""
		#version 1.0
		root{a,z}:
		  a: 2
		  z: 1
		""";

		// Flat 2-property object → single-line; keys sorted alphabetically
		Assert.That(new Subject(unsortedKeys).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_NestedObject_IndentedCorrectly()
	{
		var nested = new JsonObject { { "inner", 1 } };
		var outer = new JsonObject { { "out", nested } };
		string encoded =
		"""
		#version 1.0
		root:
		  out:
		    inner: 1
		""";
		Assert.That(new Subject(outer).Encode(null), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_QuotedKeys_QuotedInHeader()
	{
		var withQuotationNeeded = new JsonObject
		{
			{ "quotes:needed", true },
			{ "not", "needed" }
		};
		string encoded =
		"""
		#version 1.0
		root{not,"quotes:needed"}:
		  not: needed
		  "quotes:needed": true
		""";
		Assert.That(new Subject(withQuotationNeeded).Encode(null), Is.EqualTo(encoded));
	}

	#region array props

	[Test]
	public void Encode_EmptyPropArray_ZeroCount()
	{
		var emptyArray = new JsonObject { { "empty", new JsonArray() } };
		string encoded =
		"""
		#version 1.0
		root:
		  empty[0]:
		""";
		Assert.That(new Subject(emptyArray).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_MixedPrimitivesPropArray_CorrectFormat()
	{
		var mixed = new JsonArray("text", 1, true, null, "2.5");
		var mixedValues = new JsonObject { { "mixed", mixed } };
		string encoded =
		"""
		#version 1.0
		root:
		  mixed[5]: text, 1, true, null, "2.5"
		""";
		Assert.That(new Subject(mixedValues).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region edge cases

	[Test]
	public void Encode_StringNullVsActualNull_Distinct()
	{
		var multiNull = new JsonObject {
			{ "a", JsonValue.Create("null") },
			{ "b", null }
		};
		string encoded =
		"""
		#version 1.0
		root{a,b}:
		  a: "null"
		  b: null
		""";
		Assert.That(new Subject(multiNull).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_EmptyString_Quoted()
	{
		var withEmpty = new JsonObject { { "s", "" } };
		string encoded =
		"""
		#version 1.0
		root:
		  s: ""
		""";
		Assert.That(new Subject(withEmpty).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_StringWithNewlines_TripleQuoted()
	{
		var multiline = new JsonObject { { "multi", "line1\nline2" } };
		string encoded = "#version 1.0\nroot:\n  multi: \"\"\"line1\\nline2\"\"\"";
		Assert.That(new Subject(multiline).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_StringWithEmbeddedTripleQuote_TripleQuotedAndEscaped()
	{
		var tripleQuoted = new JsonObject { { "msg", "before \"\"\" after" } };
		string quoted =
		""""
		#version 1.0
		root:
		  msg: """before \""" after"""
		"""";
		Assert.That(new Subject(tripleQuoted).Encode(), Is.EqualTo(quoted));
	}

	#endregion

	#endregion

	#region arrays

	[Test]
	public void Encode_EmptyArray_ZeroRoot()
	{
		var empty = new JsonArray();
		string encoded =
		"""
		#version 1.0
		root[0]:
		""";
		Assert.That(new Subject(empty).Encode(), Is.EqualTo(encoded));
	}

	#region primitives

	[Test]
	public void Encode_IntegerArray_SingleLine()
	{
		var integers = new JsonArray(1, 2, 3);
		string encoded =
		"""
		#version 1.0
		root[3]: 1, 2, 3
		""";
		Assert.That(new Subject(integers).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_StringArray_UnquotedSingleLine()
	{
		var strings = new JsonArray("Alice", "Bob", "Charlie");
		string encoded =
		"""
		#version 1.0
		root[3]: Alice, Bob, Charlie
		""";
		Assert.That(new Subject(strings).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_QuotableStringsArray_QuotedWhenNeeded()
	{
		var someQuotes = new JsonArray("noQuotes", "123", "true");
		string encoded =
		"""
		#version 1.0
		root[3]: noQuotes, "123", "true"
		""";
		Assert.That(new Subject(someQuotes).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_MixedPrimitivesArray_CorrectFormat()
	{
		var mixed = new JsonArray("text", 1, true, null, "2.5");
		string encoded =
		"""
		#version 1.0
		root[5]: text, 1, true, null, "2.5"
		""";
		Assert.That(new Subject(mixed).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region of objects

	[Test]
	public void Encode_UniformObjectArray_TabularFormat()
	{
		var uniformObjects = new JsonArray(
			new JsonObject { { "a", 1 }, { "b", 2 } },
			new JsonObject { { "a", 3 }, { "b", 4 } });
		string encoded =
		"""
		#version 1.0
		root[2]{a,b}:
		  1, 2
		  3, 4
		""";
		Assert.That(new Subject(uniformObjects).Encode(), Is.EqualTo(encoded));
	}

	// TODO: fix to match spec (missing props don't have anything between separators or after trailing separator)
	[Test, Explicit]
	public void Encode_SemiUniformObjectArray_TabularWithAllColumns()
	{
		var semiUniformObjects = new JsonArray(
			new JsonObject { { "a", 1 }, { "b", 2 } },
			new JsonObject { { "a", 3 }, { "c", 5 } });

		string encoded =
		"""
		#version 1.0
		root[2]{a,b,c}:
		  1, 2,
		  3,, 5
		""";

		// Should use all columns a, b, c; missing fields are empty
		Assert.That(new Subject(semiUniformObjects).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_ArrayWithQuotedValues_SortedQuotedValues()
	{
		var someQuotedValues = new JsonArray(
				new JsonObject { { "name", "Alice" }, { "id", "1" } },
				new JsonObject { { "name", "Bob" }, { "id", "2" } });
		string encoded =
		"""
		#version 1.0
		root[2]{id,name}:
		  "1", Alice
		  "2", Bob
		""";
		Assert.That(new Subject(someQuotedValues).Encode(), Is.EqualTo(encoded));
	}

	// TODO: fix to match spec (index notation should be used for non-uniform arrays with primitives and the columns need to be present)
	[Test, Explicit]
	public void Encode_ArrayWithPrimitivesAndObjects_IndexNotation()
	{
		var mixedPrimitivesAndObjects = new JsonArray(
			JsonValue.Create(1),
			new JsonObject { { "x", 2 } },
			new JsonObject { { "x", 3 } });
		string encoded =
		"""
		#version 1.0
		root[3]:
		  [0]: 1
		  [1]{x}:
		    x: 2
		  [2]{x}:
		    x: 3
		""";
		Assert.That(new Subject(mixedPrimitivesAndObjects).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region of arrays

	[Test]
	public void Encode_ArrayWithPrimitivesAndArrays_IndexNotation()
	{
		var mixedPrimitivesAndArrays = new JsonArray(
			JsonValue.Create(1),
			new JsonArray(2, 3, 4));
		string encoded =
		"""
		#version 1.0
		root[2]:
		  [0]: 1
		  [1][3]: 2, 3, 4
		""";
		Assert.That(new Subject(mixedPrimitivesAndArrays).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#endregion

	#region options

	[Test]
	public void Encode_DefaultDelimiter_NoDelimiterHeader()
	{
		Assert.That(new Subject(new JsonObject()).Encode(), Does.Not.Contain("#delimiter"));

		var defaultDelimiter = new TonlEncodeOptions();
		Assert.That(new Subject(new JsonObject()).Encode(defaultDelimiter), Does.Not.Contain("#delimiter"));

		defaultDelimiter = defaultDelimiter with { Delimiter = ColumnDelimiter.Comma };
		Assert.That(new Subject(new JsonObject()).Encode(defaultDelimiter), Does.Not.Contain("#delimiter"));
	}

	[Test]
	[TestCase(ColumnDelimiter.Tab, @"\t")]
	[TestCase(ColumnDelimiter.Pipe, "|")]
	[TestCase(ColumnDelimiter.Semicolon, ";")]
	public void Encode_NonDefaultDelimiter_DelimiterHeader(ColumnDelimiter notDefault, string delimiter)
	{
		var notDefaultDelimiter = new TonlEncodeOptions { Delimiter = notDefault };
		Assert.That(new Subject(new JsonObject()).Encode(notDefaultDelimiter), Does.Contain($"#delimiter {delimiter}"));
	}

	[Test]
	public void Encode_TooDeep_Exception()
	{
		// Build a deeply nested object > 500 levels
		JsonObject deepest = new();
		JsonObject current = deepest;
		for (int i = 0; i < TonlEncodeOptions.MaxDepth; i++)
		{
			var next = new JsonObject();
			current["child"] = next;
			current = next;
		}

		Assert.That(() => new Subject(deepest).Encode(), Throws.InvalidOperationException);
	}

	// TODO: verify that circular references are possible


	[Test]
	public void Encode_WithIncludeTypes_HintedHeader()
	{
		var obj = new JsonObject
		{
			{ "age", 30 },
			{ "name", "Alice" }
		};
		var includingTypes = new TonlEncodeOptions { IncludeTypes = true };
		string encoded =
		"""
		#version 1.0
		root{age:u32,name:str}:
		  age: 30
		  name: Alice
		""";
		Assert.That(new Subject(obj).Encode(includingTypes), Is.EqualTo(encoded));
	}

	[Test]
	public void Encode_WithIncludeTypes_SimpleObjAndListOmitted()
	{
		var nested = new JsonObject
		{
			{ "child", new JsonObject { { "x", 1 } } },
			{ "items", new JsonArray(1, 2) }
		};
		var includingTypes = new TonlEncodeOptions { IncludeTypes = true };
		string encoded =
		"""
		#version 1.0
		root{child,items}:
		  child:
		    x: 1
		  items[2]: 1, 2
		""";
		Assert.That(new Subject(nested).Encode(includingTypes), Is.EqualTo(encoded));
	}

	#endregion


	#endregion
}
