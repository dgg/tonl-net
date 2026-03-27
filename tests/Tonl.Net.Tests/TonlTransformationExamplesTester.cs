using System.Text.Json.Nodes;
using Tonl.Net;

namespace Tonl.Net.Tests;

/// <summary>
/// Encoding tests derived from TRANSFORMATION_EXAMPLES.md v2.0.6.
///
/// Known intentional deviations from the document's expected output:
/// 1. Object keys are sorted alphabetically (ordinal). The document preserves JSON insertion order.
/// 2. Embedded double-quotes use backslash escaping (\") rather than the doubling style ("") shown
///    in the document. This matches the reference TypeScript implementation.
/// 3. Triple-quoted strings escape newlines as \n rather than emitting literal newlines.
/// </summary>
[TestFixture]
public class TonlTransformationExamplesTester
{
	private static string Encode(JsonNode? node, TonlEncodeOptions? options = null) =>
		new TonlDocument(node).Encode(options);

	// ── §1 Simple Types ───────────────────────────────────────────────────────

	/// <summary>Example 1.1 — Basic primitives (flat object → single-line).</summary>
	[Test]
	public void Example1_1_BasicPrimitives()
	{
		var node = new JsonObject
		{
			["string"] = "hello",
			["number"] = 42,
			["float"] = 3.14,
			["boolean"] = true,
			["null_value"] = (JsonNode?)null,
		};

		string result = Encode(node);

		// Keys sorted: boolean, float, null_value, number, string
		Assert.That(result, Is.EqualTo(
			"#version 1.0\n" +
			"root{boolean,float,null_value,number,string}: boolean: true float: 3.14 null_value: null number: 42 string: hello"));
	}

	/// <summary>
	/// Example 1.2 — Strings requiring quotes.
	/// Note: embedded " is backslash-escaped (\") not doubled ("") as shown in the document.
	/// </summary>
	[Test]
	public void Example1_2_StringsRequiringQuotes()
	{
		var node = new JsonObject
		{
			["with_comma"] = "Hello, world",
			["with_colon"] = "Key: Value",
			["with_quotes"] = "She said \"hi\"",
			["number_string"] = "123",
			["bool_string"] = "true",
		};

		string result = Encode(node);

		// Keys sorted: bool_string, number_string, with_colon, with_comma, with_quotes
		Assert.That(result, Does.Contain("bool_string: \"true\""));
		Assert.That(result, Does.Contain("number_string: \"123\""));
		Assert.That(result, Does.Contain("with_colon: \"Key: Value\""));
		Assert.That(result, Does.Contain("with_comma: \"Hello, world\""));
		Assert.That(result, Does.Contain("with_quotes: \"She said \\\"hi\\\"\""));
	}

	// ── §2 Complex Objects ────────────────────────────────────────────────────

	/// <summary>Example 2.1 — Nested objects (multi-line because child is an object).</summary>
	[Test]
	public void Example2_1_NestedObjects()
	{
		var node = new JsonObject
		{
			["user"] = new JsonObject
			{
				["name"] = "Alice Smith",
				["profile"] = new JsonObject
				{
					["age"] = 30,
					["city"] = "New York",
				},
			},
		};

		string result = Encode(node);

		// root has 1 key → root: at 0, user at indent 2, user's children at indent 4
		// user has a nested object → multi-line; profile is flat 2-prop → single-line
		Assert.That(result, Does.Contain("\n  user{name,profile}:"));
		Assert.That(result, Does.Contain("\n    name: Alice Smith"));
		Assert.That(result, Does.Contain("\n    profile{age,city}: age: 30 city: New York"));
	}

	/// <summary>Example 2.2 — Flat object encodes as single-line.</summary>
	[Test]
	public void Example2_2_FlatObjectSingleLine()
	{
		var node = new JsonObject
		{
			["config"] = new JsonObject
			{
				["timeout"] = 5000,
				["retries"] = 3,
				["debug"] = false,
			},
		};

		string result = Encode(node);

		// config has 3 primitive properties → single-line
		Assert.That(result, Does.Contain(
			"config{debug,retries,timeout}: debug: false retries: 3 timeout: 5000"));
	}

	/// <summary>Example 2.3 — Mixed nesting: primitive array + nested object inline.</summary>
	[Test]
	public void Example2_3_MixedNesting()
	{
		var node = new JsonObject
		{
			["app"] = new JsonObject
			{
				["name"] = "MyApp",
				["version"] = "2.0",
				["settings"] = new JsonObject { ["theme"] = "dark", ["language"] = "en" },
				["features"] = new JsonArray("auth", "api", "cache"),
			},
		};

		string result = Encode(node);

		// root has 1 key → root: at 0, app at indent 2, app's children at indent 4
		// app has nested object + array → multi-line
		Assert.That(result, Does.Contain("app{features,name,settings,version}:"));
		Assert.That(result, Does.Contain("\n    features[3]: auth, api, cache"));
		// settings is flat 2-prop → single-line inline
		Assert.That(result, Does.Contain("\n    settings{language,theme}: language: en theme: dark"));
	}

	// ── §3 Arrays ─────────────────────────────────────────────────────────────

	/// <summary>Example 3.1 — Simple primitive arrays.</summary>
	[Test]
	public void Example3_1_PrimitiveArrays()
	{
		var node = new JsonObject
		{
			["numbers"] = new JsonArray(1, 2, 3, 4, 5),
			["tags"] = new JsonArray("urgent", "review", "bug-fix"),
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("numbers[5]: 1, 2, 3, 4, 5"));
		Assert.That(result, Does.Contain("tags[3]: urgent, review, bug-fix"));
	}

	/// <summary>Example 3.2 — Uniform object array encodes as tabular (columns sorted).</summary>
	[Test]
	public void Example3_2_UniformObjectArrayTabular()
	{
		var node = new JsonObject
		{
			["users"] = new JsonArray(
				new JsonObject { ["id"] = 1, ["name"] = "Alice", ["role"] = "admin", ["active"] = true },
				new JsonObject { ["id"] = 2, ["name"] = "Bob", ["role"] = "user", ["active"] = true },
				new JsonObject { ["id"] = 3, ["name"] = "Carol", ["role"] = "editor", ["active"] = false }
			),
		};

		string result = Encode(node);

		// Columns sorted: active, id, name, role; root wrapper adds 2 spaces, users adds 2 more
		Assert.That(result, Does.Contain("users[3]{active,id,name,role}:"));
		Assert.That(result, Does.Contain("\n    true, 1, Alice, admin"));
		Assert.That(result, Does.Contain("\n    true, 2, Bob, user"));
		Assert.That(result, Does.Contain("\n    false, 3, Carol, editor"));
	}

	/// <summary>Example 3.3 — Mixed array uses index notation; flat object elements are single-line.</summary>
	[Test]
	public void Example3_3_MixedArray()
	{
		var node = new JsonObject
		{
			["items"] = new JsonArray(
				JsonValue.Create("text"),
				JsonValue.Create(42),
				new JsonObject { ["id"] = 1, ["name"] = "Object" },
				JsonValue.Create(true),
				new JsonArray(1, 2, 3)
			),
		};

		string result = Encode(node);

		// root: at 0, items at indent 2, elements at indent 4
		Assert.That(result, Does.Contain("items[5]:"));
		Assert.That(result, Does.Contain("\n    [0]: text"));
		Assert.That(result, Does.Contain("\n    [1]: 42"));
		// Flat 2-prop object → single-line inline
		Assert.That(result, Does.Contain("\n    [2]{id,name}: id: 1 name: Object"));
		Assert.That(result, Does.Contain("\n    [3]: true"));
		Assert.That(result, Does.Contain("\n    [4][3]: 1, 2, 3"));
	}

	/// <summary>Example 3.4 — Primitive array with null values.</summary>
	[Test]
	public void Example3_4_ArrayWithNullValues()
	{
		var node = new JsonObject
		{
			["data"] = new JsonArray(JsonValue.Create(1), null, JsonValue.Create(3), null, JsonValue.Create(5)),
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("data[5]: 1, null, 3, null, 5"));
	}

	/// <summary>Example 3.5 — Empty array.</summary>
	[Test]
	public void Example3_5_EmptyArray()
	{
		var node = new JsonObject
		{
			["empty_array"] = new JsonArray(),
			["other_field"] = "value",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("empty_array[0]:"));
		Assert.That(result, Does.Contain("other_field: value"));
	}

	// ── §4 Nested Structures ──────────────────────────────────────────────────

	/// <summary>Example 4.1 — Deep nesting (5 levels); single-prop objects stay multi-line.</summary>
	[Test]
	public void Example4_1_DeepNesting()
	{
		var node = new JsonObject
		{
			["level1"] = new JsonObject
			{
				["level2"] = new JsonObject
				{
					["level3"] = new JsonObject
					{
						["level4"] = new JsonObject
						{
							["level5"] = "deep value",
						},
					},
				},
			},
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("\n  level1:"));
		Assert.That(result, Does.Contain("\n    level2:"));
		Assert.That(result, Does.Contain("\n      level3:"));
		Assert.That(result, Does.Contain("\n        level4:"));
		Assert.That(result, Does.Contain("\n          level5: deep value"));
	}

	/// <summary>Example 4.2 — Array of arrays (matrix).</summary>
	[Test]
	public void Example4_2_ArrayOfArrays()
	{
		var node = new JsonObject
		{
			["matrix"] = new JsonArray(
				new JsonArray(1, 2, 3),
				new JsonArray(4, 5, 6),
				new JsonArray(7, 8, 9)
			),
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("matrix[3]:"));
		Assert.That(result, Does.Contain("\n    [0][3]: 1, 2, 3"));
		Assert.That(result, Does.Contain("\n    [1][3]: 4, 5, 6"));
		Assert.That(result, Does.Contain("\n    [2][3]: 7, 8, 9"));
	}

	/// <summary>Example 4.3 — Array of objects with nested arrays (not tabular).</summary>
	[Test]
	public void Example4_3_ArrayOfObjectsWithArrays()
	{
		var node = new JsonObject
		{
			["users"] = new JsonArray(
				new JsonObject { ["id"] = 1, ["name"] = "Alice", ["tags"] = new JsonArray("admin", "verified") },
				new JsonObject { ["id"] = 2, ["name"] = "Bob", ["tags"] = new JsonArray("user") }
			),
		};

		string result = Encode(node);

		// Not tabular because elements have nested arrays
		// root: at 0, users at indent 2, elements at indent 4, element children at indent 6
		Assert.That(result, Does.Contain("users[2]:"));
		Assert.That(result, Does.Contain("\n    [0]{id,name,tags}:"));
		Assert.That(result, Does.Contain("\n      tags[2]: admin, verified"));
		Assert.That(result, Does.Contain("\n    [1]{id,name,tags}:"));
		Assert.That(result, Does.Contain("\n      tags[1]: user"));
	}

	/// <summary>Example 4.4 — Object with mixed content: inline flat child objects.</summary>
	[Test]
	public void Example4_4_ObjectWithMixedContent()
	{
		var node = new JsonObject
		{
			["data"] = new JsonObject
			{
				["simple_field"] = "value",
				["nested_object"] = new JsonObject { ["x"] = 1, ["y"] = 2 },
				["array_field"] = new JsonArray(1, 2, 3),
				["another_simple"] = 42,
			},
		};

		string result = Encode(node);

		// root: at 0, data at indent 2, data's children at indent 4
		Assert.That(result, Does.Contain("data{another_simple,array_field,nested_object,simple_field}:"));
		Assert.That(result, Does.Contain("\n    array_field[3]: 1, 2, 3"));
		Assert.That(result, Does.Contain("\n    nested_object{x,y}: x: 1 y: 2"));
		Assert.That(result, Does.Contain("\n    simple_field: value"));
	}

	// ── §5 Special Characters ─────────────────────────────────────────────────

	/// <summary>Example 5.1 — Delimiter in values (comma delimiter → quoting in tabular rows).</summary>
	[Test]
	public void Example5_1_DelimiterInValues_CommaDelimiter()
	{
		var node = new JsonObject
		{
			["items"] = new JsonArray(
				new JsonObject { ["name"] = "Item, A", ["price"] = 10 },
				new JsonObject { ["name"] = "Item B", ["price"] = 20 }
			),
		};

		string result = Encode(node);

		// name contains comma → must be quoted in tabular row
		Assert.That(result, Does.Contain("items[2]{name,price}:"));
		Assert.That(result, Does.Contain("\"Item, A\","));
		Assert.That(result, Does.Contain("Item B,"));
	}

	/// <summary>Example 5.1 — Pipe delimiter avoids quoting for comma-containing values.</summary>
	[Test]
	public void Example5_1_DelimiterInValues_PipeDelimiter()
	{
		var node = new JsonObject
		{
			["items"] = new JsonArray(
				new JsonObject { ["name"] = "Item, A", ["price"] = 10 },
				new JsonObject { ["name"] = "Item B", ["price"] = 20 }
			),
		};

		string result = Encode(node, new TonlEncodeOptions { Delimiter = ColumnDelimiter.Pipe });

		Assert.That(result, Does.Contain("#delimiter |"));
		// With pipe delimiter, commas in values don't need quoting
		Assert.That(result, Does.Contain("Item, A|"));
	}

	/// <summary>
	/// Example 5.2 — Quotes in values.
	/// Note: embedded " uses backslash escaping (\") not doubling ("") as shown in the document.
	/// </summary>
	[Test]
	public void Example5_2_QuotesInValues()
	{
		var node = new JsonObject
		{
			["quote1"] = "She said \"hello\"",
			["quote2"] = "It's a \"test\"",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("quote1: \"She said \\\"hello\\\"\""));
		Assert.That(result, Does.Contain("quote2: \"It's a \\\"test\\\"\""));
	}

	/// <summary>Example 5.3 — Backslashes and paths.</summary>
	[Test]
	public void Example5_3_BackslashesAndPaths()
	{
		var node = new JsonObject
		{
			["windows_path"] = @"C:\Users\Alice\Documents",
			["regex"] = @"\d+\.\d+",
			["normal"] = "No backslash",
		};

		string result = Encode(node);

		// Backslash triggers quoting; within quotes backslash is doubled
		Assert.That(result, Does.Contain("windows_path: \"C:\\\\Users\\\\Alice\\\\Documents\""));
		Assert.That(result, Does.Contain("regex: \"\\\\d+\\\\.\\\\d+\""));
		// "No backslash" has no special chars → unquoted
		Assert.That(result, Does.Contain("normal: No backslash"));
	}

	/// <summary>Example 5.4 — Unicode and emoji pass through unescaped.</summary>
	[Test]
	public void Example5_4_UnicodeAndEmoji()
	{
		var node = new JsonObject
		{
			["emoji"] = "Hello 👋 World 🌍",
			["unicode"] = "Héllo Wörld",
			["chinese"] = "你好世界",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("emoji: Hello 👋 World 🌍"));
		Assert.That(result, Does.Contain("unicode: Héllo Wörld"));
		Assert.That(result, Does.Contain("chinese: 你好世界"));
	}

	// ── §6 Edge Cases ─────────────────────────────────────────────────────────

	/// <summary>Example 6.1 — Empty string and whitespace strings are quoted.</summary>
	[Test]
	public void Example6_1_EmptyAndWhitespace()
	{
		var node = new JsonObject
		{
			["empty_string"] = "",
			["space"] = " ",
			["leading"] = "  text",
			["trailing"] = "text  ",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("empty_string: \"\""));
		Assert.That(result, Does.Contain("space: \" \""));
		Assert.That(result, Does.Contain("leading: \"  text\""));
		Assert.That(result, Does.Contain("trailing: \"text  \""));
	}

	/// <summary>Example 6.2 — Reserved words as strings must be quoted.</summary>
	[Test]
	public void Example6_2_ReservedWordsAsStrings()
	{
		var node = new JsonObject
		{
			["true_string"] = "true",
			["false_string"] = "false",
			["null_string"] = "null",
			["undefined_string"] = "undefined",
			["infinity_string"] = "Infinity",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("true_string: \"true\""));
		Assert.That(result, Does.Contain("false_string: \"false\""));
		Assert.That(result, Does.Contain("null_string: \"null\""));
		Assert.That(result, Does.Contain("undefined_string: \"undefined\""));
		Assert.That(result, Does.Contain("infinity_string: \"Infinity\""));
	}

	/// <summary>Example 6.3 — Number-like strings quoted; others with non-numeric chars not.</summary>
	[Test]
	public void Example6_3_NumberLikeStrings()
	{
		var node = new JsonObject
		{
			["integer_string"] = "123",
			["decimal_string"] = "3.14",
			["scientific_string"] = "1e10",
			["phone_number"] = "555-1234",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("integer_string: \"123\""));
		Assert.That(result, Does.Contain("decimal_string: \"3.14\""));
		Assert.That(result, Does.Contain("scientific_string: \"1e10\""));
		Assert.That(result, Does.Contain("phone_number: 555-1234"));
	}

	/// <summary>
	/// Example 6.4 — Multiline strings use triple-quoting.
	/// Note: newlines are escaped as \n (not emitted as literal newlines) per the backslash
	/// escaping convention used by the reference implementation.
	/// </summary>
	[Test]
	public void Example6_4_MultilineStrings()
	{
		var node = new JsonObject
		{
			["poem"] = "Line 1\nLine 2\nLine 3",
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("poem: \"\"\"Line 1\\nLine 2\\nLine 3\"\"\""));
	}

	// ── §7 Real-World Examples ────────────────────────────────────────────────

	/// <summary>Example 7.1 — User database: uniform tabular array (columns sorted).</summary>
	[Test]
	public void Example7_1_UserDatabase()
	{
		var node = new JsonObject
		{
			["users"] = new JsonArray(
				new JsonObject
				{
					["id"] = 1001, ["username"] = "alice_smith", ["email"] = "alice@company.com",
					["firstName"] = "Alice", ["lastName"] = "Smith", ["age"] = 30,
					["role"] = "admin", ["verified"] = true, ["lastLogin"] = "2025-11-04T10:30:00Z",
				},
				new JsonObject
				{
					["id"] = 1002, ["username"] = "bob.jones", ["email"] = "bob@company.com",
					["firstName"] = "Bob", ["lastName"] = "Jones", ["age"] = 25,
					["role"] = "user", ["verified"] = true, ["lastLogin"] = "2025-11-04T09:15:00Z",
				},
				new JsonObject
				{
					["id"] = 1003, ["username"] = "carol_w", ["email"] = "carol@personal.com",
					["firstName"] = "Carol", ["lastName"] = "White", ["age"] = 35,
					["role"] = "editor", ["verified"] = false, ["lastLogin"] = (JsonNode?)null,
				}
			),
		};

		string result = Encode(node);

		// Columns: ordinal sort — lastLogin('L') < lastName('N') at position 4
		// Sorted: age,email,firstName,id,lastLogin,lastName,role,username,verified
		// root: at 0, users at indent 2, tabular rows at indent 4
		Assert.That(result, Does.Contain("users[3]{age,email,firstName,id,lastLogin,lastName,role,username,verified}:"));
		// ISO timestamps contain ':' → quoted in tabular rows; null lastLogin → empty cell
		Assert.That(result, Does.Contain("\n    30, alice@company.com, Alice, 1001, \"2025-11-04T10:30:00Z\", Smith, admin, alice_smith, true"));
		Assert.That(result, Does.Contain("\n    25, bob@company.com, Bob, 1002, \"2025-11-04T09:15:00Z\", Jones, user, bob.jones, true"));
		Assert.That(result, Does.Contain("\n    35, carol@personal.com, Carol, 1003, , White, editor, carol_w, false"));
	}

	/// <summary>Example 7.2 — API response: nested objects with inline flat children.</summary>
	[Test]
	public void Example7_2_ApiResponse()
	{
		var node = new JsonObject
		{
			["status"] = "success",
			["timestamp"] = 1699123456,
			["data"] = new JsonObject
			{
				["total"] = 150,
				["page"] = 1,
				["pageSize"] = 10,
				["results"] = new JsonArray(
					new JsonObject { ["id"] = "abc123", ["title"] = "First Result", ["score"] = 0.95 },
					new JsonObject { ["id"] = "def456", ["title"] = "Second Result", ["score"] = 0.87 }
				),
			},
			["meta"] = new JsonObject { ["processingTime"] = 45, ["cacheHit"] = true },
		};

		string result = Encode(node);

		Assert.That(result, Does.Contain("results[2]{id,score,title}:"));
		// meta is flat 2-prop → single-line
		Assert.That(result, Does.Contain("meta{cacheHit,processingTime}: cacheHit: true processingTime: 45"));
	}

	// ── §8 Delimiter Comparison ───────────────────────────────────────────────

	/// <summary>Example 8.1 — Tab delimiter: no quoting needed for comma-containing values.</summary>
	[Test]
	public void Example8_1_TabDelimiter()
	{
		var node = new JsonObject
		{
			["data"] = new JsonArray(
				new JsonObject { ["name"] = "Item, A", ["category"] = "Tools, Hardware", ["price"] = 99.99 },
				new JsonObject { ["name"] = "Item B", ["category"] = "Electronics", ["price"] = 149.99 }
			),
		};

		string result = Encode(node, new TonlEncodeOptions { Delimiter = ColumnDelimiter.Tab });

		Assert.That(result, Does.Contain("#delimiter \\t"));
		// With tab delimiter, comma-containing values need no quoting
		Assert.That(result, Does.Contain("Item, A\t"));
		Assert.That(result, Does.Contain("Tools, Hardware\t"));
	}

	// ── §9 Type Hints ─────────────────────────────────────────────────────────

	/// <summary>Example 9.1 — Type hints in header.</summary>
	[Test]
	public void Example9_1_TypeHints()
	{
		var node = new JsonObject
		{
			["user"] = new JsonObject
			{
				["id"] = 123,
				["name"] = "Alice",
				["age"] = 30,
				["score"] = 95.5,
				["active"] = true,
			},
		};

		string result = Encode(node, new TonlEncodeOptions { IncludeTypes = true });

		// user is flat → single-line; columns sorted: active,age,id,name,score
		Assert.That(result, Does.Contain("active:bool"));
		Assert.That(result, Does.Contain("age:u32"));
		Assert.That(result, Does.Contain("id:u32"));
		Assert.That(result, Does.Contain("name:str"));
		Assert.That(result, Does.Contain("score:f64"));
	}
}
