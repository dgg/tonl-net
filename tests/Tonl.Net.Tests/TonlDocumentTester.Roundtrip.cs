using System.Text.Json.Nodes;

using Iz = Tonl.Net.Tests.Support.Iz;
using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public partial class TonlDocumentTester
{
	[Test]
	public void Example1_1_BasicPrimitives_Roundtrips()
	{
		var root = new JsonObject
		{
			{ "string", "hello" },
			{ "number", 42 },
			{ "float", 3.14 },
			{ "boolean", true },
			{ "null_value", null },
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Example1_2_StringsRequiringQuotes_Roundtrips()
	{
		var root = new JsonObject
		{
			{ "with_comma", "Hello, world" },
			{ "with_colon", "Key: Value" },
			{ "with_quotes", "She said \"hi\"" },
			{ "number_string", "123" },
			{ "bool_string", "true" },
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Sample1_3_SpecialNumericValues_Roundtrips()
	{
		var root = new JsonObject
		{
			{ "infinity", double.PositiveInfinity },
			{ "negative_infinity", double.NegativeInfinity },
			{ "nan", double.NaN },
			{ "infinity_string", "Infinity" }
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Example2_1_NestedObjects_Roundtrips()
	{
		var root = new JsonObject
		{
			{
				"user",
				new JsonObject
				{
					{ "name", "Alice Smith" },
					{ "profile", new JsonObject { { "age", 30 }, { "city", "New York" } } }
				}
			}
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}

	[Test]
	public void Example2_2_FlatObject_Roundtrips()
	{
		var root = new JsonObject
		{
			{ "config", new JsonObject { { "timeout", 5000 }, { "retries", 3 }, { "debug", false } } }
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}
	
	[Test]
	public void Example2_3_MixedNesting_Roundtrips()
	{
		var root = new JsonObject
		{
			{
				"app", new JsonObject
				{
					{ "name", "MyApp" },
					{ "version", "2.0" },
					{ "settings", new JsonObject { { "theme", "dark" }, { "language", "en" } } },
					{ "features", new JsonArray("auth", "api", "cache") },
				}
			}
		};
		string encoded = new Subject(root).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(root));
	}
	[Test]
	public void Example3_1_PrimitiveArrays_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "numbers", new JsonArray(1, 2, 3, 4, 5) },
			{ "tags", new JsonArray("urgent", "review", "bug-fix") }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example3_2_UniformObjectArrayTabular_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "role", "admin" }, { "active", true } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "role", "user" }, { "active", true } },
					new JsonObject { { "id", 3 }, { "name", "Carol" }, { "role", "editor" }, { "active", false } })
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example3_3_MixedArray_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"items", new JsonArray(
					"text",
					42,
					new JsonObject { { "id", 1 }, { "name", "Object" } },
					true,
					new JsonArray(1, 2, 3))
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example3_4_ArrayWithNullValues_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "data", new JsonArray(JsonValue.Create(1), null, JsonValue.Create(3), null, JsonValue.Create(5)) }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example3_5_EmptyArray_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "empty_array", new JsonArray() },
			{ "other_field", "value" }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	
	[Test]
	public void Example4_1_DeepNesting_Roundtrips()
	{
		var node = new JsonObject
		{
			["level1"] = new JsonObject
			{
				["level2"] = new JsonObject
				{
					["level3"] = new JsonObject
					{
						["level4"] = new JsonObject { ["level5"] = "deep value" },
					},
				},
			},
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example4_2_ArrayOfArrays_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"matrix", new JsonArray(

					new JsonArray(1, 2, 3),
					new JsonArray(4, 5, 6),
					new JsonArray(7, 8, 9))
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example4_3_ArrayOfObjectsWithArrays_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "tags", new JsonArray("admin", "verified") } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "tags", new JsonArray("user") } })
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example4_4_ObjectWithMixedContent_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"data", new JsonObject
				{
					{ "simple_field", "value" },
					{ "nested_object", new JsonObject { { "x", 1 }, { "y", 2 } } },
					{ "array_field", new JsonArray(1, 2, 3) },
					{ "another_simple", 42 }
				}
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example5_1_DelimiterInValues_Comma_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"items", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "price", 10 } },
					new JsonObject { { "name", "Item B" }, { "price", 20 } })
			}
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example5_1_DelimiterInValues_ChangeDelimiter_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"items", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "price", 10 } },
					new JsonObject { { "name", "Item B" }, { "price", 20 } })
			}
		};
		var encOpts = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Pipe };
		var decOpts = new TonlDecodeOptions { Delimiter = ColumnDelimiter.Pipe };
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example5_2_QuotesInValues_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "quote1", "She said \"hello\"" },
			{ "quote2", "It's a \"test\"" },
			{ "triple", "Has \"\"\" triple quotes" }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example5_3_BackslashesAndPaths_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "windows_path", @"C:\Users\Alice\Documents" },
			{ "regex", @"\d+\.\d+" },
			{ "normal", "No backslash" },
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example5_4_UnicodeAndEmoji_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "emoji", "Hello 👋 World 🌍" },
			{ "unicode", "Héllo Wörld" },
			{ "chinese", "你好世界" }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example6_1_EmptyAndWhitespace_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "empty_string", "" },
			{ "space", " " },
			{ "spaces", "   " },
			{ "leading", "  text" },
			{ "trailing", "text  " },
			{ "both", "  text  " }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example6_2_ReservedWordsAsStrings_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "true_string", "true" },
			{ "false_string", "false" },
			{ "null_string", "null" },
			{ "undefined_string", "undefined" },
			{ "infinity_string", "Infinity" }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example6_3_NumberLikeStrings_Roundtrips()
	{
		var node = new JsonObject
		{
			{ "integer", "123" },
			{ "decimal", "3.14" },
			{ "scientific", "1e10" },
			{ "phone_number", "555-1234" },
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example6_4_MultilineStrings_Roundtrips()
	{
		var node = new JsonObject { { "poem", "Line 1\nLine 2\nLine 3" } };
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example7_1_UserDatabase_Roundtrips()
	{
		var alice = new JsonObject
		{
			{ "id", 1001 },
			{ "username", "alice_smith" },
			{ "email", "alice@company.com" },
			{ "firstName", "Alice" },
			{ "lastName", "Smith" },
			{ "age", 30 },
			{ "role", "admin" },
			{ "verified", true },
			{ "lastLogin", "2025-11-04T10:30:00Z" }
		};
		var bob = new JsonObject
		{
			{ "id", 1002 },
			{ "username", "bob.jones" },
			{ "email", "bob@company.com" },
			{ "firstName", "Bob" },
			{ "lastName", "Jones" },
			{ "age", 25 },
			{ "role", "user" },
			{ "verified", true },
			{ "lastLogin", "2025-11-04T09:15:00Z" },
		};
		var carol = new JsonObject
		{
			{ "id", 1003 },
			{ "username", "carol_w" },
			{ "email", "carol@personal.com" },
			{ "firstName", "Carol" },
			{ "lastName", "White" },
			{ "age", 35 },
			{ "role", "editor" },
			{ "verified", false },
			{ "lastLogin", (JsonNode?)null },
		};
		var node = new JsonObject { { "users", new JsonArray(alice, bob, carol) } };
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}

	[Test]
	public void Example7_2_ApiResponse_Roundtrips()
	{
		var results = new JsonArray(
			new JsonObject { { "id", "abc123" }, { "title", "First Result" }, { "score", 0.95 } },
			new JsonObject { { "id", "def456" }, { "title", "Second Result" }, { "score", 0.87 } });
		var data = new JsonObject
		{
			{ "total", 150 },
			{ "page", 1 },
			{ "pageSize", 10 },
			{ "results", results }
		};
		var node = new JsonObject
		{
			{ "status", "success" },
			{ "timestamp", 1699123456 },
			{ "data", data },
			{ "meta", new JsonObject { { "processingTime", 45 }, { "cacheHit", true } } }
		};
		string encoded = new Subject(node).Encode();
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example8_1_TabDelimiter_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"data", new JsonArray(
					new JsonObject
					{
						{ "name", "Item, A" }, { "category", "Tools, Hardware" }, { "price", 99.99 }
					},
					new JsonObject { { "name", "Item B" }, { "category", "Electronics" }, { "price", 149.99 } })
			},
		};
		var tabEncoding = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Tab };
		var tabDecoding = new TonlDecodeOptions { Delimiter = ColumnDelimiter.Tab };
		string encoded = new Subject(node).Encode(tabEncoding);
		Subject decoded = Subject.Decode(encoded, tabDecoding);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
	[Test]
	public void Example9_1_TypeHints_Roundtrips()
	{
		var node = new JsonObject
		{
			{
				"user", new JsonObject
				{
					{ "id", 123 },
					{ "name", "Alice" },
					{ "age", 30 },
					{ "score", 95.5 },
					{ "active", true },
				}
			},
		};
		var hinting = new TonlEncodeOptions { IncludeTypes = true };
		string encoded = new Subject(node).Encode(hinting);
		Subject decoded = Subject.Decode(encoded);
		Assert.That(decoded.Root, Iz.JsonEquivalentTo(node));
	}
}