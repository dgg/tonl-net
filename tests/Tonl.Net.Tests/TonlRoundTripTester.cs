using System.Text.Json.Nodes;

namespace Tonl.Net.Tests;

/// <summary>
/// Bidirectional round-trip tests: encode then decode (and decode then encode)
/// using the same examples as <see cref="TonlTransformationExamplesTester"/>.
/// </summary>
[TestFixture]
public class TonlRoundTripTester
{
	// -------------------------------------------------------------------------
	// Assertion helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Recursively compares two JSON nodes for structural and value equality.
	/// </summary>
	private static void assertJsonEqual(JsonNode? expected, JsonNode? actual, string path = "root")
	{
		if (expected is null && actual is null) return;

		Assert.That(actual, Is.Not.Null, $"Expected non-null node at {path} but got null");
		Assert.That(expected, Is.Not.Null, $"Expected null node at {path} but got {actual}");

		if (expected is JsonObject expObj)
		{
			Assert.That(actual, Is.InstanceOf<JsonObject>(), $"Expected object at {path}");
			var actObj = (JsonObject)actual!;
			var expKeys = expObj.Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
			var actKeys = actObj.Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
			Assert.That(actKeys, Is.EqualTo(expKeys), $"Key mismatch at {path}");
			foreach (string key in expKeys)
			{
				assertJsonEqual(expObj[key], actObj[key], $"{path}.{key}");
			}
		}
		else if (expected is JsonArray expArr)
		{
			Assert.That(actual, Is.InstanceOf<JsonArray>(), $"Expected array at {path}");
			var actArr = (JsonArray)actual!;
			Assert.That(actArr.Count, Is.EqualTo(expArr.Count), $"Array length mismatch at {path}");
			for (int i = 0; i < expArr.Count; i++)
			{
				assertJsonEqual(expArr[i], actArr[i], $"{path}[{i}]");
			}
		}
		else if (expected is JsonValue expVal)
		{
			Assert.That(actual, Is.InstanceOf<JsonValue>(), $"Expected value at {path}");
			// Compare doubles specially to handle Infinity / NaN which cannot be serialised to JSON
			if (expVal.TryGetValue<double>(out double expDouble))
			{
				Assert.That(actual!.GetValue<double>(), Is.EqualTo(expDouble), $"Value mismatch at {path}");
			}
			else
			{
				// Compare via raw string representation (avoids calling ToJsonString on special doubles)
				string expStr = expected.ToJsonString();
				string actStr = actual!.ToJsonString();
				Assert.That(actStr, Is.EqualTo(expStr), $"Value mismatch at {path}");
			}
		}
	}

	private static void assertRoundTrip(JsonNode? root, TonlEncodeOptions? encOpts = null,
		TonlDecodeOptions? decOpts = null)
	{
		string tonl = new TonlDocument(root).Encode(encOpts);
		TonlDocument decoded = TonlDocument.Decode(tonl, decOpts);
		assertJsonEqual(root, decoded.Root);
	}

	// -------------------------------------------------------------------------
	// Example 1 — Basic primitives
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example1_1_BasicPrimitives()
	{
		var node = new JsonObject
		{
			{ "string", "hello" },
			{ "number", 42 },
			{ "float", 3.14 },
			{ "boolean", true },
			{ "null_value", null },
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example1_2_StringsRequiringQuotes()
	{
		var node = new JsonObject
		{
			{ "with_comma", "Hello, world" },
			{ "with_colon", "Key: Value" },
			{ "with_quotes", "She said \"hi\"" },
			{ "number_string", "123" },
			{ "bool_string", "true" },
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example1_3_SpecialNumericValues()
	{
		var node = new JsonObject
		{
			{ "infinity", double.PositiveInfinity },
			{ "negative_infinity", double.NegativeInfinity },
			{ "nan", double.NaN },
			{ "infinity_string", "Infinity" }
		};
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 2 — Complex objects
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example2_1_NestedObjects()
	{
		var node = new JsonObject
		{
			{
				"user", new JsonObject
				{
					{ "name", "Alice Smith" },
					{ "profile", new JsonObject { { "age", 30 }, { "city", "New York" } } }
				}
			}
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example2_2_FlatObject()
	{
		var node = new JsonObject
		{
			{ "config", new JsonObject { { "timeout", 5000 }, { "retries", 3 }, { "debug", false } } }
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example2_3_MixedNesting()
	{
		var node = new JsonObject
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
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 3 — Arrays
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example3_1_PrimitiveArrays()
	{
		var node = new JsonObject
		{
			{ "numbers", new JsonArray(1, 2, 3, 4, 5) },
			{ "tags", new JsonArray("urgent", "review", "bug-fix") }
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example3_2_UniformObjectArrayTabular()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example3_3_MixedArray()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example3_4_ArrayWithNullValues()
	{
		var node = new JsonObject
		{
			{ "data", new JsonArray(JsonValue.Create(1), null, JsonValue.Create(3), null, JsonValue.Create(5)) }
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example3_5_EmptyArray()
	{
		var node = new JsonObject
		{
			{ "empty_array", new JsonArray() },
			{ "other_field", "value" }
		};
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 4 — Nested structures
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example4_1_DeepNesting()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example4_2_ArrayOfArrays()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example4_3_ArrayOfObjectsWithArrays()
	{
		var node = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "tags", new JsonArray("admin", "verified") } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "tags", new JsonArray("user") } })
			}
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example4_4_ObjectWithMixedContent()
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
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 5 — Special characters
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example5_1_DelimiterInValues_Comma()
	{
		var node = new JsonObject
		{
			{
				"items", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "price", 10 } },
					new JsonObject { { "name", "Item B" }, { "price", 20 } })
			}
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example5_1_DelimiterInValues_ChangeDelimiter()
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
		assertRoundTrip(node, encOpts, decOpts);
	}

	[Test]
	public void RoundTrip_Example5_2_QuotesInValues()
	{
		var node = new JsonObject
		{
			{ "quote1", "She said \"hello\"" },
			{ "quote2", "It's a \"test\"" },
			{ "triple", "Has \"\"\" triple quotes" }
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example5_3_BackslashesAndPaths()
	{
		var node = new JsonObject
		{
			{ "windows_path", @"C:\Users\Alice\Documents" },
			{ "regex", @"\d+\.\d+" },
			{ "normal", "No backslash" },
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example5_4_UnicodeAndEmoji()
	{
		var node = new JsonObject
		{
			{ "emoji", "Hello 👋 World 🌍" },
			{ "unicode", "Héllo Wörld" },
			{ "chinese", "你好世界" }
		};
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 6 — Edge cases
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example6_1_EmptyAndWhitespace()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example6_2_ReservedWordsAsStrings()
	{
		var node = new JsonObject
		{
			{ "true_string", "true" },
			{ "false_string", "false" },
			{ "null_string", "null" },
			{ "undefined_string", "undefined" },
			{ "infinity_string", "Infinity" }
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example6_3_NumberLikeStrings()
	{
		var node = new JsonObject
		{
			{ "integer", "123" },
			{ "decimal", "3.14" },
			{ "scientific", "1e10" },
			{ "phone_number", "555-1234" },
		};
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example6_4_MultilineStrings()
	{
		var node = new JsonObject { { "poem", "Line 1\nLine 2\nLine 3" } };
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 7 — Real-world examples
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example7_1_UserDatabase()
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
		assertRoundTrip(node);
	}

	[Test]
	public void RoundTrip_Example7_2_ApiResponse()
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
		assertRoundTrip(node);
	}

	// -------------------------------------------------------------------------
	// Example 8 — Delimiter comparison
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example8_1_TabDelimiter()
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
		var encOpts = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Tab };
		var decOpts = new TonlDecodeOptions { Delimiter = ColumnDelimiter.Tab };
		assertRoundTrip(node, encOpts, decOpts);
	}

	// -------------------------------------------------------------------------
	// Example 9 — Type hints
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_Example9_1_TypeHints()
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
		// Encode with type hints; decode with default options
		var encOpts = new TonlEncodeOptions { IncludeTypes = true };
		assertRoundTrip(node, encOpts);
	}

	// -------------------------------------------------------------------------
	// Decode-then-encode direction
	// -------------------------------------------------------------------------

	[Test]
	public void RoundTrip_DecodeEncode_BasicObject()
	{
		string tonl =
			"""
			#version 1.0
			root{a,b}:
			  a: 1
			  b: hello
			""";

		TonlDocument doc = TonlDocument.Decode(tonl);
		string reEncoded = doc.Encode();
		TonlDocument doc2 = TonlDocument.Decode(reEncoded);
		assertJsonEqual(doc.Root, doc2.Root);
	}

	[Test]
	public void RoundTrip_DecodeEncode_TabularArray()
	{
		string tonl =
			"""
			#version 1.0
			root[3]{active,id,name,role}:
			  true, 1, Alice, admin
			  true, 2, Bob, user
			  false, 3, Carol, editor
			""";

		TonlDocument doc = TonlDocument.Decode(tonl);
		string reEncoded = doc.Encode();
		TonlDocument doc2 = TonlDocument.Decode(reEncoded);
		assertJsonEqual(doc.Root, doc2.Root);
	}

	[Test]
	public void RoundTrip_DecodeEncode_WindowsLineEndings()
	{
		// Ensure \r\n is normalised correctly
		string tonl = "#version 1.0\r\nroot{a,b}:\r\n  a: 1\r\n  b: hello\r\n";

		TonlDocument doc = TonlDocument.Decode(tonl);
		var obj = (JsonObject)doc.Root!;
		Assert.That((int)obj["a"]!, Is.EqualTo(1));
		Assert.That(obj["b"]!.GetValue<string>(), Is.EqualTo("hello"));
	}
}
