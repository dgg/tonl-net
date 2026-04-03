using System.Text.Json.Nodes;

using Iz = Tonl.Net.Tests.Support.Iz;
using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public partial class TonlDocumentTester
{
	#region primitive roots

	[Test]
	public void Decode_StringValue_String()
	{
		string stringRoot =
			"""
			#version 1.0
			root: hello
			""";

		Subject decoded = Subject.Decode(stringRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("hello"));
	}

	[Test]
	public void Decode_QuotedStringValue_String()
	{
		string quotedRoot =
			"""
			#version 1.0
			root: "hello world"
			""";

		Subject decoded = Subject.Decode(quotedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("hello world"));
	}

	[Test]
	public void Decode_TripleQuotedStringValue_String()
	{
		string tripleQuotedRoot =
			""""
			#version 1.0
			root: """Line 1\nLine 2"""
			"""";
		Subject decoded = Subject.Decode(tripleQuotedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("Line 1\nLine 2"));
	}

	[Test]
	public void Decode_IntegerValue_Int()
	{
		string intRoot =
			"""
			#version 1.0
			root: 42
			""";

		Subject decoded = Subject.Decode(intRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(42));
	}

	[Test]
	public void Decode_FloatValue_Double()
	{
		string doubleRoot =
			"""
			#version 1.0
			root: 3.14
			""";
		Subject decoded = Subject.Decode(doubleRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(3.14d));
	}

	[Test]
	public void Decode_BoolTrue_Bool()
	{
		string boolRoot =
			"""
			#version 1.0
			root: true
			""";

		Subject decoded = Subject.Decode(boolRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(true));
	}

	[Test]
	public void Decode_NullValue_Null()
	{
		string nullRoot =
			"""
			#version 1.0
			root: null
			""";

		Subject decoded = Subject.Decode(nullRoot);
		Assert.That(decoded.Root, Is.Null);
	}

	[Test]
	public void Decode_Infinity_Double()
	{
		string infinityRoot =
			"""
			#version 1.0
			root: Infinity
			""";

		Subject decoded = Subject.Decode(infinityRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(double.PositiveInfinity));
	}

	[Test]
	public void Decode_NegativeInfinity_Double()
	{
		string minusInfinity =
			"""
			#version 1.0
			root: -Infinity
			""";

		Subject decoded = Subject.Decode(minusInfinity);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(double.NegativeInfinity));
	}

	[Test]
	public void Decode_NaN_Double()
	{
		string nanRoot =
			"""
			#version 1.0
			root: NaN
			""";

		Subject decoded = Subject.Decode(nanRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(double.NaN));
	}

	#region quoted values

	[Test]
	public void Decode_QuotedTrue_String()
	{
		string quotedBool =
			"""
			#version 1.0
			root: "true"
			""";

		Subject decoded = Subject.Decode(quotedBool);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("true"));
	}

	[Test]
	public void Decode_QuotedNull_String()
	{
		string quotedNull =
			"""
			#version 1.0
			root: "null"
			""";

		Subject decoded = Subject.Decode(quotedNull);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("null"));
	}

	[Test]
	public void Decode_QuotedNumber_String()
	{
		string quotedNumber =
			"""
			#version 1.0
			root: "123"
			""";

		Subject decoded = Subject.Decode(quotedNumber);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo("123"));
	}

	#endregion

	#endregion

	#region object roots

	[Test]
	public void Decode_SimpleObject_JsonObject()
	{
		var obj = new JsonObject { { "a", 1 }, { "b", "hello" } };
		string objRoot =
			"""
			#version 1.0
			root{a,b}:
			  a: 1
			  b: hello
			""";

		Subject decoded = Subject.Decode(objRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(obj));
	}

	[Test]
	public void Decode_NestedObject_JsonObject()
	{
		var nested = new JsonObject { { "outer", new JsonObject { { "x", 99 } } } };
		string nestedRoot =
			"""
			#version 1.0
			root:
			  outer{x}:
			    x: 99
			""";

		Subject decoded = Subject.Decode(nestedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(nested));
	}

	[Test]
	public void Decode_EmptyObject_EmptyJsonObject()
	{
		var empty = new JsonObject();
		string emptyRot =
			"""
			#version 1.0
			root:
			""";

		Subject decoded = Subject.Decode(emptyRot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(empty));
	}

	#endregion

	#region array roots

	#region delimiters

	private readonly JsonObject _alpha = new() { { "a", 1 }, { "b", "alpha" } };
	private readonly JsonObject _beta = new() { { "a", 2 }, { "b", "beta" } };

	[Test]
	public void Decode_DelimiterHeader_Pipe()
	{
		var arr = new JsonArray(_alpha.DeepClone(), _beta.DeepClone());

		string piped =
			"""
			#version 1.0
			#delimiter |
			root[2]{a,b}:
			  1 | alpha
			  2 | beta
			""";

		Subject decoded = Subject.Decode(piped);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_DelimiterHeader_Tab()
	{
		var arr = new JsonArray(_alpha.DeepClone(), _beta.DeepClone());

		string tabbed =
			"""
			#version 1.0
			#delimiter \t
			root[2]{a,b}:
			  1	alpha
			  2	beta
			""";

		Subject decoded = Subject.Decode(tabbed);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_DelimiterHeader_Semicolon()
	{
		var arr = new JsonArray(_alpha.DeepClone(), _beta.DeepClone());

		string semicolonNed =
			"""
			#version 1.0
			#delimiter ;
			root[2]{a,b}:
			  1; alpha
			  2; beta
			""";

		Subject decoded = Subject.Decode(semicolonNed);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_NoDelimiterHeader_DefaultComma()
	{
		var arr = new JsonArray(_alpha.DeepClone(), _beta.DeepClone());

		string defaultSeparator =
			"""
			#version 1.0
			root[2]{a,b}:
			  1, alpha
			  2, beta
			""";

		Subject decoded = Subject.Decode(defaultSeparator);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	#endregion

	[Test]
	public void Decode_PrimitiveArrayInline_JsonArray()
	{
		var arr = new JsonArray { 1, 2, 3 };
		string inlineRoot =
			"""
			#version 1.0
			root[3]: 1, 2, 3
			""";

		Subject decoded = Subject.Decode(inlineRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_EmptyArray_EmptyJsonArray()
	{
		var empty = new JsonArray();
		string emptyRoot =
			"""
			#version 1.0
			root[0]:
			""";

		Subject decoded = Subject.Decode(emptyRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(empty));
	}

	[Test]
	public void Decode_TabularArray_JsonArray()
	{
		var alice = new JsonObject { { "id", 1 }, { "name", "Alice" } };
		var bob = new JsonObject { { "id", 2 }, { "name", "Bob" } };
		var arr = new JsonArray { alice, bob };
		string tabularRoot =
			"""
			#version 1.0
			root[2]{id,name}:
			  1, Alice
			  2, Bob
			""";

		Subject decoded = Subject.Decode(tabularRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_MixedArray_JsonArray()
	{
		var mixed = new JsonArray { "hello", 42, true };
		string mixedRoot =
			"""
			#version 1.0
			root[3]:
			  [0]: hello
			  [1]: 42
			  [2]: true
			""";

		Subject decoded = Subject.Decode(mixedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(mixed));
	}

	[Test]
	public void Decode_ArrayOfArrays_JsonArray()
	{
		var matrix = new JsonArray { new JsonArray { 1, 2, 3 }, new JsonArray { 4, 5, 6 } };
		string matrixRoot =
			"""
			#version 1.0
			root[2]:
			  [0][3]: 1, 2, 3
			  [1][3]: 4, 5, 6
			""";

		Subject decoded = Subject.Decode(matrixRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(matrix));
	}

	[Test]
	public void Decode_ArrayWithNulls_JsonArray()
	{
		var sparse = new JsonArray
		{
			1,
			null,
			3,
			null,
			5
		};
		string sparseRoot =
			"""
			#version 1.0
			root[5]: 1, null, 3, null, 5
			""";

		Subject decoded = Subject.Decode(sparseRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(sparse));
	}

	#region semi-uniform

	[Test]
	public void Decode_TabularWithEmptyCells_NullProperties()
	{
		var withGaps = new JsonArray
		{
			new JsonObject { { "a", 1 }, { "b", null }, { "c", 3 } },
			new JsonObject { { "a", 4 }, { "b", 5 }, { "c", null } }
		};
		string rootWithGaps =
			"""
			#version 1.0
			root[2]{a,b,c}:
			  1, , 3
			  4, 5,
			""";

		Subject decoded = Subject.Decode(rootWithGaps);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(withGaps));
	}

	[Test]
	public void Decode_TabularWithQuotedFields_Unquoted()
	{
		var quoted = new JsonArray(new JsonObject { { "name", "Item, A" }, { "desc", "has a comma" } });
		string quotedRoot =
			"""
			#version 1.0
			root[1]{name,desc}:
			  "Item, A", "has a comma"
			""";

		Subject decoded = Subject.Decode(quotedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(quoted));
	}

	#endregion

	#endregion

	#region type hints

	[Test]
	public void Decode_TypeHintU32_Uint()
	{
		var alice = new JsonObject { { "id", 42u }, { "name", "Alice" } };
		var bob = new JsonObject { { "id", 99u }, { "name", "Bob" } };
		var hinted = new JsonArray(alice, bob);
		string hintedRoot =
			"""
			#version 1.0
			root[2]{id:u32,name:str}:
			  42, Alice
			  99, Bob
			""";

		Subject decoded = Subject.Decode(hintedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(hinted));
		Assert.That(decoded.Root![0]!["id"]!.GetValue<uint>(), Is.EqualTo(42u));
	}

	[Test]
	public void Decode_TypeHintStr_String()
	{
		var arr = new JsonArray(new JsonObject { { "code", "123" } });
		string hintedRoot =
			"""
			#version 1.0
			root[1]{code:str}:
			  123
			""";

		Subject decoded = Subject.Decode(hintedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_TypeHintBool_Boolean()
	{
		var arr = new JsonArray(
			new JsonObject { { "active", true } },
			new JsonObject { { "active", false } });
		string hintedRoot =
			"""
			#version 1.0
			root[2]{active:bool}:
			  true
			  false
			""";

		Subject decoded = Subject.Decode(hintedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	[Test]
	public void Decode_TypeHintF64_Double()
	{
		var arr = new JsonArray(new JsonObject { { "score", 95.5 } });
		string hintedRoot =
			"""
			#version 1.0
			root[1]{score:f64}:
			  95.5
			""";

		Subject decoded = Subject.Decode(hintedRoot);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(arr));
	}

	#endregion

	#region edge cases

	[Test]
	public void Decode_EmptyString_QuotedEmpty()
	{
		var root = JsonValue.Create(string.Empty);
		string tonl =
			"""
			#version 1.0
			root: ""
			""";
		Subject decoded = Subject.Decode(tonl);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Decode_LeadingTrailingSpaces_Preserved()
	{
		var root = JsonValue.Create("  text  ");
		string notTrimmed =
			"""
			#version 1.0
			root: "  text  "
			""";

		Subject decoded = Subject.Decode(notTrimmed);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Decode_UnicodeAndEmoji_Preserved()
	{
		var root = JsonValue.Create("Hello 👋 World 🌍");
		string tonl =
			"""
			#version 1.0
			root: Hello 👋 World 🌍
			""";

		Subject decoded = Subject.Decode(tonl);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Decode_BackslashEscapes_Unescaped()
	{
		var root = JsonValue.Create(@"C:\Users\Alice");
		string path =
			"""
			#version 1.0
			root: "C:\\Users\\Alice"
			""";

		Subject decoded = Subject.Decode(path);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Decode_DeepNesting_Reconstructed()
	{
		var root = new JsonObject
		{
			{
				"level1",
				new JsonObject
				{
					{
						"level2",
						new JsonObject
						{
							{
								"level3", new JsonObject { { "level4", new JsonObject { { "level5", "deep value" } } } }
							}
						}
					}
				}
			}
		};
		string deep =
			"""
			#version 1.0
			root:
			  level1:
			    level2:
			      level3:
			        level4:
			          level5: deep value
			""";

		Subject decoded = Subject.Decode(deep);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	#endregion

	#region strict mode

	[Test]
	public void Decode_StrictMode_ArrayCountMismatch_Throws()
	{
		string countMismatch =
			"""
			#version 1.0
			root[5]: 1, 2, 3
			""";

		var strictOpts = new TonlDecodeOptions { StrictMode = true };
		Assert.That(() => Subject.Decode(countMismatch, strictOpts), Throws.TypeOf<TonlParseException>());
	}

	[Test]
	public void Decode_StrictMode_TypeHintViolation_Exception()
	{
		string hintViolation =
			"""
			#version 1.0
			root[1]{val:u32}:
			  not_a_number
			""";

		var strictOpts = new TonlDecodeOptions { StrictMode = true };
		Assert.That(() => Subject.Decode(hintViolation, strictOpts), Throws.TypeOf<TonlParseException>());
	}

	#endregion
}