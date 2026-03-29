using System.Text.Json.Nodes;

namespace Tonl.Net.Tests;

/// <summary>
/// Unit tests for <see cref="TonlDocument.Decode"/>.
/// </summary>
[TestFixture]
public class TonlDecodeTester
{
	// -------------------------------------------------------------------------
	// Helper
	// -------------------------------------------------------------------------

	private static JsonNode? Decode(string tonl, TonlDecodeOptions? opts = null) =>
		TonlDocument.Decode(tonl, opts).Root;

	// -------------------------------------------------------------------------
	// Header parsing
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_VersionHeader_Parsed()
	{
		string tonl =
			"""
			#version 1.0
			root: hello
			""";

		JsonNode? result = Decode(tonl);
		Assert.That(result, Is.Not.Null);
		Assert.That(result!.GetValue<string>(), Is.EqualTo("hello"));
	}

	[Test]
	public void Decode_DelimiterHeader_Pipe()
	{
		string tonl =
			"""
			#version 1.0
			#delimiter |
			root[2]{a,b}:
			  1 | alpha
			  2 | beta
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That((int)arr[0]!["a"]!, Is.EqualTo(1));
		Assert.That(arr[0]!["b"]!.GetValue<string>(), Is.EqualTo("alpha"));
		Assert.That((int)arr[1]!["a"]!, Is.EqualTo(2));
		Assert.That(arr[1]!["b"]!.GetValue<string>(), Is.EqualTo("beta"));
	}

	[Test]
	public void Decode_DelimiterHeader_Tab()
	{
		string tonl = "#version 1.0\n#delimiter \\t\nroot[2]{a,b}:\n  1\talpha\n  2\tbeta";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That((int)arr[0]!["a"]!, Is.EqualTo(1));
		Assert.That(arr[0]!["b"]!.GetValue<string>(), Is.EqualTo("alpha"));
	}

	[Test]
	public void Decode_DelimiterHeader_Semicolon()
	{
		string tonl =
			"""
			#version 1.0
			#delimiter ;
			root[2]{a,b}:
			  1; alpha
			  2; beta
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That((int)arr[0]!["a"]!, Is.EqualTo(1));
		Assert.That(arr[0]!["b"]!.GetValue<string>(), Is.EqualTo("alpha"));
	}

	[Test]
	public void Decode_NoDelimiterHeader_DefaultComma()
	{
		string tonl =
			"""
			#version 1.0
			root[2]{a,b}:
			  1, alpha
			  2, beta
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That((int)arr[0]!["a"]!, Is.EqualTo(1));
		Assert.That(arr[0]!["b"]!.GetValue<string>(), Is.EqualTo("alpha"));
	}

	// -------------------------------------------------------------------------
	// Primitive values
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_StringValue_String()
	{
		string tonl =
			"""
			#version 1.0
			root: hello
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("hello"));
	}

	[Test]
	public void Decode_QuotedStringValue_String()
	{
		string tonl =
			"""
			#version 1.0
			root: "hello world"
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("hello world"));
	}

	[Test]
	public void Decode_TripleQuotedStringValue_String()
	{
		string tonl = "#version 1.0\nroot: \"\"\"Line 1\\nLine 2\"\"\"";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("Line 1\nLine 2"));
	}

	[Test]
	public void Decode_IntegerValue_Int()
	{
		string tonl =
			"""
			#version 1.0
			root: 42
			""";

		Assert.That(Decode(tonl)!.GetValue<int>(), Is.EqualTo(42));
	}

	[Test]
	public void Decode_FloatValue_Double()
	{
		string tonl =
			"""
			#version 1.0
			root: 3.14
			""";

		Assert.That(Decode(tonl)!.GetValue<double>(), Is.EqualTo(3.14));
	}

	[Test]
	public void Decode_BoolTrue_Bool()
	{
		string tonl =
			"""
			#version 1.0
			root: true
			""";

		Assert.That(Decode(tonl)!.GetValue<bool>(), Is.True);
	}

	[Test]
	public void Decode_BoolFalse_Bool()
	{
		string tonl =
			"""
			#version 1.0
			root: false
			""";

		Assert.That(Decode(tonl)!.GetValue<bool>(), Is.False);
	}

	[Test]
	public void Decode_NullValue_Null()
	{
		string tonl =
			"""
			#version 1.0
			root: null
			""";

		Assert.That(Decode(tonl), Is.Null);
	}

	[Test]
	public void Decode_Infinity_Double()
	{
		string tonl =
			"""
			#version 1.0
			root: Infinity
			""";

		Assert.That(Decode(tonl)!.GetValue<double>(), Is.EqualTo(double.PositiveInfinity));
	}

	[Test]
	public void Decode_NegativeInfinity_Double()
	{
		string tonl =
			"""
			#version 1.0
			root: -Infinity
			""";

		Assert.That(Decode(tonl)!.GetValue<double>(), Is.EqualTo(double.NegativeInfinity));
	}

	[Test]
	public void Decode_NaN_Double()
	{
		string tonl =
			"""
			#version 1.0
			root: NaN
			""";

		Assert.That(Decode(tonl)!.GetValue<double>(), Is.EqualTo(double.NaN));
	}

	// -------------------------------------------------------------------------
	// Reserved words as strings
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_QuotedTrue_String()
	{
		string tonl =
			"""
			#version 1.0
			root: "true"
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("true"));
	}

	[Test]
	public void Decode_QuotedNull_String()
	{
		string tonl =
			"""
			#version 1.0
			root: "null"
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("null"));
	}

	[Test]
	public void Decode_QuotedNumber_String()
	{
		string tonl =
			"""
			#version 1.0
			root: "123"
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("123"));
	}

	// -------------------------------------------------------------------------
	// Objects
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_SimpleObject_JsonObject()
	{
		string tonl =
			"""
			#version 1.0
			root{a,b}:
			  a: 1
			  b: hello
			""";

		var obj = (JsonObject)Decode(tonl)!;
		Assert.That((int)obj["a"]!, Is.EqualTo(1));
		Assert.That(obj["b"]!.GetValue<string>(), Is.EqualTo("hello"));
	}

	[Test]
	public void Decode_NestedObject_JsonObject()
	{
		string tonl =
			"""
			#version 1.0
			root:
			  outer{x}:
			    x: 99
			""";

		var obj = (JsonObject)Decode(tonl)!;
		Assert.That((int)obj["outer"]!["x"]!, Is.EqualTo(99));
	}

	[Test]
	public void Decode_EmptyObject_EmptyJsonObject()
	{
		string tonl =
			"""
			#version 1.0
			root:
			""";

		var obj = (JsonObject)Decode(tonl)!;
		Assert.That(obj, Is.Not.Null);
		Assert.That(obj.Count, Is.EqualTo(0));
	}

	[Test]
	public void Decode_ObjectWithColumnHeaders_JsonObject()
	{
		string tonl =
			"""
			#version 1.0
			root{name:str,age:u32}:
			  name: Alice
			  age: 30
			""";

		var obj = (JsonObject)Decode(tonl)!;
		Assert.That(obj["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
		Assert.That((uint)obj["age"]!, Is.EqualTo(30u));
	}

	// -------------------------------------------------------------------------
	// Arrays
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_PrimitiveArrayInline_JsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[3]: 1, 2, 3
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(3));
		Assert.That((int)arr[0]!, Is.EqualTo(1));
		Assert.That((int)arr[1]!, Is.EqualTo(2));
		Assert.That((int)arr[2]!, Is.EqualTo(3));
	}

	[Test]
	public void Decode_EmptyArray_EmptyJsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[0]:
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(0));
	}

	[Test]
	public void Decode_TabularArray_JsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[2]{id,name}:
			  1, Alice
			  2, Bob
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(2));
		Assert.That((int)arr[0]!["id"]!, Is.EqualTo(1));
		Assert.That(arr[0]!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
		Assert.That((int)arr[1]!["id"]!, Is.EqualTo(2));
		Assert.That(arr[1]!["name"]!.GetValue<string>(), Is.EqualTo("Bob"));
	}

	[Test]
	public void Decode_MixedArray_JsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[3]:
			  [0]: hello
			  [1]: 42
			  [2]: true
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(3));
		Assert.That(arr[0]!.GetValue<string>(), Is.EqualTo("hello"));
		Assert.That((int)arr[1]!, Is.EqualTo(42));
		Assert.That((bool)arr[2]!, Is.True);
	}

	[Test]
	public void Decode_ArrayOfArrays_JsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[2]:
			  [0][3]: 1, 2, 3
			  [1][3]: 4, 5, 6
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(2));
		var inner0 = (JsonArray)arr[0]!;
		Assert.That((int)inner0[0]!, Is.EqualTo(1));
		Assert.That((int)inner0[2]!, Is.EqualTo(3));
		var inner1 = (JsonArray)arr[1]!;
		Assert.That((int)inner1[0]!, Is.EqualTo(4));
	}

	[Test]
	public void Decode_ArrayWithNulls_JsonArray()
	{
		string tonl =
			"""
			#version 1.0
			root[5]: 1, null, 3, null, 5
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr.Count, Is.EqualTo(5));
		Assert.That((int)arr[0]!, Is.EqualTo(1));
		Assert.That(arr[1], Is.Null);
		Assert.That((int)arr[2]!, Is.EqualTo(3));
		Assert.That(arr[3], Is.Null);
		Assert.That((int)arr[4]!, Is.EqualTo(5));
	}

	// -------------------------------------------------------------------------
	// Tabular / semi-uniform
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_TabularWithEmptyCells_NullProperties()
	{
		string tonl =
			"""
			#version 1.0
			root[2]{a,b,c}:
			  1, , 3
			  4, 5,
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr[0]!["b"], Is.Null);
		Assert.That(arr[1]!["c"], Is.Null);
	}

	[Test]
	public void Decode_TabularWithQuotedFields_Unquoted()
	{
		string tonl =
			"""
			#version 1.0
			root[1]{name,desc}:
			  "Item, A", "has a comma"
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr[0]!["name"]!.GetValue<string>(), Is.EqualTo("Item, A"));
		Assert.That(arr[0]!["desc"]!.GetValue<string>(), Is.EqualTo("has a comma"));
	}

	// -------------------------------------------------------------------------
	// Type hints
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_TypeHintU32_Uint()
	{
		string tonl =
			"""
			#version 1.0
			root[2]{id:u32,name:str}:
			  42, Alice
			  99, Bob
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr[0]!["id"]!.GetValue<uint>(), Is.EqualTo(42u));
		Assert.That(arr[1]!["id"]!.GetValue<uint>(), Is.EqualTo(99u));
	}

	[Test]
	public void Decode_TypeHintStr_String()
	{
		string tonl =
			"""
			#version 1.0
			root[1]{code:str}:
			  123
			""";

		var arr = (JsonArray)Decode(tonl)!;
		// With str hint the value must remain a string, not become an int
		Assert.That(arr[0]!["code"]!.GetValue<string>(), Is.EqualTo("123"));
	}

	[Test]
	public void Decode_TypeHintBool_Boolean()
	{
		string tonl =
			"""
			#version 1.0
			root[2]{active:bool}:
			  true
			  false
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr[0]!["active"]!.GetValue<bool>(), Is.True);
		Assert.That(arr[1]!["active"]!.GetValue<bool>(), Is.False);
	}

	[Test]
	public void Decode_TypeHintF64_Double()
	{
		string tonl =
			"""
			#version 1.0
			root[1]{score:f64}:
			  95.5
			""";

		var arr = (JsonArray)Decode(tonl)!;
		Assert.That(arr[0]!["score"]!.GetValue<double>(), Is.EqualTo(95.5));
	}

	// -------------------------------------------------------------------------
	// Edge cases
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_EmptyString_QuotedEmpty()
	{
		string tonl =
			"""
			#version 1.0
			root: ""
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo(string.Empty));
	}

	[Test]
	public void Decode_LeadingTrailingSpaces_Preserved()
	{
		string tonl =
			"""
			#version 1.0
			root: "  text  "
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("  text  "));
	}

	[Test]
	public void Decode_UnicodeAndEmoji_Preserved()
	{
		string tonl =
			"""
			#version 1.0
			root: Hello 👋 World 🌍
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo("Hello 👋 World 🌍"));
	}

	[Test]
	public void Decode_BackslashEscapes_Unescaped()
	{
		string tonl =
			"""
			#version 1.0
			root: "C:\\Users\\Alice"
			""";

		Assert.That(Decode(tonl)!.GetValue<string>(), Is.EqualTo(@"C:\Users\Alice"));
	}

	[Test]
	public void Decode_DeepNesting_Reconstructed()
	{
		string tonl =
			"""
			#version 1.0
			root:
			  level1:
			    level2:
			      level3:
			        level4:
			          level5: deep value
			""";

		var obj = (JsonObject)Decode(tonl)!;
		string? val = (string?)obj["level1"]!["level2"]!["level3"]!["level4"]!["level5"];
		Assert.That(val, Is.EqualTo("deep value"));
	}

	// -------------------------------------------------------------------------
	// Strict mode
	// -------------------------------------------------------------------------

	[Test]
	public void Decode_StrictMode_ArrayCountMismatch_Throws()
	{
		string tonl =
			"""
			#version 1.0
			root[5]: 1, 2, 3
			""";

		var strictOpts = new TonlDecodeOptions { StrictMode = true };
		Assert.Throws<TonlParseException>(() => TonlDocument.Decode(tonl, strictOpts));
	}

	[Test]
	public void Decode_StrictMode_TypeHintViolation_Throws()
	{
		string tonl =
			"""
			#version 1.0
			root[1]{val:u32}:
			  not_a_number
			""";

		var strictOpts = new TonlDecodeOptions { StrictMode = true };
		Assert.Throws<TonlParseException>(() => TonlDocument.Decode(tonl, strictOpts));
	}
}
