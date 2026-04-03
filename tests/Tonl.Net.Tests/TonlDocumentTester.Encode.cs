using System.Text.Json.Nodes;

using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public partial class TonlDocumentTester
{
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
		Assert.That(new Subject(outer).Encode(), Is.EqualTo(encoded));
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
		Assert.That(new Subject(withQuotationNeeded).Encode(), Is.EqualTo(encoded));
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
	
	#region transformation examples
	
	#region simple types

	[Test]
	public void Example1_1_BasicPrimitives()
	{
		var basicPrimitives = new JsonObject
		{
			{ "string", "hello" },
			{ "number", 42 },
			{ "float", 3.14 },
			{ "boolean", true },
			{ "null_value", null },
		};

		// Keys sorted: boolean, float, null_value, number, string
		string encoded =
			"""
			#version 1.0
			root{boolean,float,null_value,number,string}:
			  boolean: true
			  float: 3.14
			  null_value: null
			  number: 42
			  string: hello
			""";

		Assert.That(new Subject(basicPrimitives).Encode(), Is.EqualTo(encoded));
	}

	// TODO: check with specification
	[Test]
	public void Example1_2_StringsRequiringQuotes()
	{
		var quotesRequired = new JsonObject
		{
			{ "with_comma", "Hello, world" },
			{ "with_colon", "Key: Value" },
			{ "with_quotes", "She said \"hi\"" },
			{ "number_string", "123" },
			{ "bool_string", "true" },
		};

		string encoded =
			"""
			#version 1.0
			root{bool_string,number_string,with_colon,with_comma,with_quotes}:
			  bool_string: "true"
			  number_string: "123"
			  with_colon: "Key: Value"
			  with_comma: "Hello, world"
			  with_quotes: "She said \"hi\""
			""";

		Assert.That(new Subject(quotesRequired).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Sample1_3_SpecialNumericValues()
	{
		var specialNumerics = new JsonObject
		{
			{ "infinity", double.PositiveInfinity },
			{ "negative_infinity", double.NegativeInfinity },
			{ "nan", double.NaN },
			{ "infinity_string", "Infinity" }
		};

		string encoded =
			"""
			#version 1.0
			root{infinity,infinity_string,nan,negative_infinity}:
			  infinity: Infinity
			  infinity_string: "Infinity"
			  nan: NaN
			  negative_infinity: -Infinity
			""";
		Assert.That(new Subject(specialNumerics).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region complex objects

	[Test]
	public void Example2_1_NestedObjects()
	{
		var nested = new JsonObject
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

		string encoded =
			"""
			#version 1.0
			root:
			  user{name,profile}:
			    name: Alice Smith
			    profile{age,city}:
			      age: 30
			      city: New York
			""";

		Assert.That(new Subject(nested).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example2_2_FlatObject()
	{
		var flat = new JsonObject
		{
			{ "config", new JsonObject { { "timeout", 5000 }, { "retries", 3 }, { "debug", false } } }
		};

		string encoded =
			"""
			#version 1.0
			root:
			  config{debug,retries,timeout}:
			    debug: false
			    retries: 3
			    timeout: 5000
			""";
		Assert.That(new Subject(flat).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example2_3_MixedNesting()
	{
		var mixedNesting = new JsonObject
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

		string encoded =
			"""
			#version 1.0
			root:
			  app{features,name,settings,version}:
			    features[3]: auth, api, cache
			    name: MyApp
			    settings{language,theme}:
			      language: en
			      theme: dark
			    version: "2.0"
			""";

		Assert.That(new Subject(mixedNesting).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region arrays

	[Test]
	public void Example3_1_PrimitiveArrays()
	{
		var primitiveArrays = new JsonObject
		{
			{ "numbers", new JsonArray(1, 2, 3, 4, 5) }, { "tags", new JsonArray("urgent", "review", "bug-fix") }
		};

		string encoded =
			"""
			#version 1.0
			root{numbers,tags}:
			  numbers[5]: 1, 2, 3, 4, 5
			  tags[3]: urgent, review, bug-fix
			""";

		Assert.That(new Subject(primitiveArrays).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example3_1_LongPrimitiveArrays()
	{
		JsonNode?[] manyNumbers = Enumerable.Range(1, 100)
			.Select(i => JsonValue.Create(i))
			.ToArray<JsonNode?>();

		var longPrimitiveArrays = new JsonObject
		{
			{ "numbers", new JsonArray(manyNumbers) }, { "tags", new JsonArray("urgent", "review", "bug-fix") }
		};

		string encoded =
			"""
			#version 1.0
			root{numbers,tags}:
			  numbers[100]: 
			""";
		encoded += string.Join(", ", manyNumbers);
		encoded += "\n  tags[3]: urgent, review, bug-fix";

		Assert.That(new Subject(longPrimitiveArrays).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example3_2_UniformObjectArrayTabular()
	{
		var uniformObjectArray = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "role", "admin" }, { "active", true } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "role", "user" }, { "active", true } },
					new JsonObject { { "id", 3 }, { "name", "Carol" }, { "role", "editor" }, { "active", false } })
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  users[3]{active,id,name,role}:
			    true, 1, Alice, admin
			    true, 2, Bob, user
			    false, 3, Carol, editor
			""";

		Assert.That(new Subject(uniformObjectArray).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example3_3_MixedArray()
	{
		var nonUniform = new JsonObject
		{
			{
				"items", new JsonArray(
					"text",
					42,
					new JsonObject { ["id"] = 1, ["name"] = "Object" },
					true,
					new JsonArray(1, 2, 3))
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  items[5]:
			    [0]: text
			    [1]: 42
			    [2]{id,name}:
			      id: 1
			      name: Object
			    [3]: true
			    [4][3]: 1, 2, 3
			""";

		Assert.That(new Subject(nonUniform).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example3_4_ArrayWithNullValues()
	{
		var withNulls = new JsonObject
		{
			{ "data", new JsonArray(JsonValue.Create(1), null, JsonValue.Create(3), null, JsonValue.Create(5)) }
		};

		string encoded =
			"""
			#version 1.0
			root:
			  data[5]: 1, null, 3, null, 5
			""";

		Assert.That(new Subject(withNulls).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example3_5_EmptyArray()
	{
		var withEmptyArray = new JsonObject { { "empty_array", new JsonArray() }, { "other_field", "value" } };

		string encoded =
			"""
			#version 1.0
			root{empty_array,other_field}:
			  empty_array[0]:
			  other_field: value
			""";

		Assert.That(new Subject(withEmptyArray).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region nested structures

	[Test]
	public void Example4_1_DeepNesting()
	{
		var deepNested = new JsonObject
		{
			["level1"] = new JsonObject
			{
				["level2"] = new JsonObject
				{
					["level3"] = new JsonObject
					{
						["level4"] = new JsonObject { ["level5"] = "deep value", },
					},
				},
			},
		};

		string encoded =
			"""
			#version 1.0
			root:
			  level1:
			    level2:
			      level3:
			        level4:
			          level5: deep value
			""";

		Assert.That(new Subject(deepNested).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example4_2_ArrayOfArrays()
	{
		var matrix = new JsonObject
		{
			{
				"matrix", new JsonArray(
					new JsonArray(1, 2, 3),
					new JsonArray(4, 5, 6),
					new JsonArray(7, 8, 9)
				)
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  matrix[3]:
			    [0][3]: 1, 2, 3
			    [1][3]: 4, 5, 6
			    [2][3]: 7, 8, 9
			""";

		Assert.That(new Subject(matrix).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example4_3_ArrayOfObjectsWithArrays()
	{
		var arrayOfObjectsWithArrays = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "tags", new JsonArray("admin", "verified") } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "tags", new JsonArray("user") } })
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  users[2]:
			    [0]{id,name,tags}:
			      id: 1
			      name: Alice
			      tags[2]: admin, verified
			    [1]{id,name,tags}:
			      id: 2
			      name: Bob
			      tags[1]: user
			""";

		Assert.That(new Subject(arrayOfObjectsWithArrays).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example4_4_ObjectWithMixedContent()
	{
		var withMixedContent = new JsonObject
		{
			{
				"data",
				new JsonObject
				{
					{ "simple_field", "value" },
					{ "nested_object", new JsonObject { { "x", 1 }, { "y", 2 } } },
					{ "array_field", new JsonArray(1, 2, 3) },
					{ "another_simple", 42 }
				}
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  data{another_simple,array_field,nested_object,simple_field}:
			    another_simple: 42
			    array_field[3]: 1, 2, 3
			    nested_object{x,y}:
			      x: 1
			      y: 2
			    simple_field: value
			""";

		Assert.That(new Subject(withMixedContent).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region special characters

	[Test]
	public void Example5_1_DelimiterInValues_Comma()
	{
		var delimiterInValue = new JsonObject
		{
			{
				"items", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "price", 10 } },
					new JsonObject { { "name", "Item B" }, { "price", 20 } })
			}
		};

		string encoded =
			"""
			#version 1.0
			root:
			  items[2]{name,price}:
			    "Item, A", 10
			    Item B, 20
			""";

		Assert.That(new Subject(delimiterInValue).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example5_1_DelimiterInValues_ChangeDelimiter()
	{
		var nonDefaultDelimiterNotInValue = new JsonObject
		{
			{
				"items", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "price", 10 } },
					new JsonObject { { "name", "Item B" }, { "price", 20 } })
			}
		};

		string encoded =
			"""
			#version 1.0
			#delimiter |
			root:
			  items[2]{name,price}:
			    Item, A | 10
			    Item B | 20
			""";

		var pipeDelimited = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Pipe };
		Assert.That(new Subject(nonDefaultDelimiterNotInValue).Encode(pipeDelimited), Is.EqualTo(encoded));
	}

	[Test]
	public void Example5_2_QuotesInValues()
	{
		var node = new JsonObject
		{
			{ "quote1", "She said \"hello\"" },
			{ "quote2", "It's a \"test\"" },
			{ "triple", "Has \"\"\" triple quotes" }
		};

		string encoded =
			""""
			#version 1.0
			root{quote1,quote2,triple}:
			  quote1: "She said \"hello\""
			  quote2: "It's a \"test\""
			  triple: """Has \""" triple quotes"""
			"""";

		Assert.That(new Subject(node).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example5_3_BackslashesAndPaths()
	{
		var node = new JsonObject
		{
			{ "windows_path", @"C:\Users\Alice\Documents" }, { "regex", @"\d+\.\d+" }, { "normal", "No backslash" },
		};

		string encoded =
			"""
			#version 1.0
			root{normal,regex,windows_path}:
			  normal: No backslash
			  regex: "\\d+\\.\\d+"
			  windows_path: "C:\\Users\\Alice\\Documents"
			""";

		Assert.That(new Subject(node).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example5_4_UnicodeAndEmoji()
	{
		var node = new JsonObject
		{
			{ "emoji", "Hello 👋 World 🌍" }, { "unicode", "Héllo Wörld" }, { "chinese", "你好世界" }
		};

		string encoded =
			"""
			#version 1.0
			root{chinese,emoji,unicode}:
			  chinese: 你好世界
			  emoji: Hello 👋 World 🌍
			  unicode: Héllo Wörld
			""";

		Assert.That(new Subject(node).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region edge cases

	[Test]
	public void Example6_1_EmptyAndWhitespace()
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

		string encoded =
			"""
			#version 1.0
			root{both,empty_string,leading,space,spaces,trailing}:
			  both: "  text  "
			  empty_string: ""
			  leading: "  text"
			  space: " "
			  spaces: "   "
			  trailing: "text  "
			""";

		Assert.That(new Subject(node).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example6_2_ReservedWordsAsStrings()
	{
		var node = new JsonObject
		{
			{ "true_string", "true" },
			{ "false_string", "false" },
			{ "null_string", "null" },
			{ "undefined_string", "undefined" },
			{ "infinity_string", "Infinity" }
		};

		string encoded =
			"""
			#version 1.0
			root{false_string,infinity_string,null_string,true_string,undefined_string}:
			  false_string: "false"
			  infinity_string: "Infinity"
			  null_string: "null"
			  true_string: "true"
			  undefined_string: "undefined"
			""";

		Assert.That(new Subject(node).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example6_3_NumberLikeStrings()
	{
		var numberLike = new JsonObject
		{
			{ "integer", "123" }, { "decimal", "3.14" }, { "scientific", "1e10" }, { "phone_number", "555-1234" },
		};

		string encoded =
			"""
			#version 1.0
			root{decimal,integer,phone_number,scientific}:
			  decimal: "3.14"
			  integer: "123"
			  phone_number: 555-1234
			  scientific: "1e10"
			""";

		Assert.That(new Subject(numberLike).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example6_4_MultilineStrings()
	{
		var multiLine = new JsonObject { { "poem", "Line 1\nLine 2\nLine 3" } };

		string encoded = "#version 1.0\nroot:\n  poem: \"\"\"Line 1\\nLine 2\\nLine 3\"\"\"";

		Assert.That(new Subject(multiLine).Encode(), Is.EqualTo(encoded));
	}

	#endregion


	#region real-world examples

	[Test]
	public void Example7_1_UserDatabase()
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
			{ "lastLogin", null },
		};
		var userDatabase = new JsonObject { { "users", new JsonArray(alice, bob, carol) }, };

		string encoded =
			"""
			#version 1.0
			root:
			  users[3]{age,email,firstName,id,lastLogin,lastName,role,username,verified}:
			    30, alice@company.com, Alice, 1001, "2025-11-04T10:30:00Z", Smith, admin, alice_smith, true
			    25, bob@company.com, Bob, 1002, "2025-11-04T09:15:00Z", Jones, user, bob.jones, true
			    35, carol@personal.com, Carol, 1003, , White, editor, carol_w, false
			""";

		Assert.That(new Subject(userDatabase).Encode(), Is.EqualTo(encoded));
	}

	[Test]
	public void Example7_2_ApiResponse()
	{
		var results = new JsonArray(
			new JsonObject { { "id", "abc123" }, { "title", "First Result" }, { "score", 0.95 } },
			new JsonObject { { "id", "def456" }, { "title", "Second Result" }, { "score", 0.87 } });
		var data = new JsonObject { { "total", 150 }, { "page", 1 }, { "pageSize", 10 }, { "results", results } };
		var apiResponse = new JsonObject
		{
			{ "status", "success" },
			{ "timestamp", 1699123456 },
			{ "data", data },
			{ "meta", new JsonObject { { "processingTime", 45 }, { "cacheHit", true } } }
		};

		string encoded =
			"""
			#version 1.0
			root{data,meta,status,timestamp}:
			  data{page,pageSize,results,total}:
			    page: 1
			    pageSize: 10
			    results[2]{id,score,title}:
			      abc123, 0.95, First Result
			      def456, 0.87, Second Result
			    total: 150
			  meta{cacheHit,processingTime}:
			    cacheHit: true
			    processingTime: 45
			  status: success
			  timestamp: 1699123456
			""";

		Assert.That(new Subject(apiResponse).Encode(), Is.EqualTo(encoded));
	}

	#endregion

	#region delimiter comparison

	[Test]
	public void Example8_1_TabDelimiter()
	{
		var node = new JsonObject
		{
			{
				"data", new JsonArray(
					new JsonObject { { "name", "Item, A" }, { "category", "Tools, Hardware" }, { "price", 99.99 } },
					new JsonObject { { "name", "Item B" }, { "category", "Electronics" }, { "price", 149.99 } }
				)
			},
		};

		string withTabDelimiter =
			"""
			#version 1.0
			#delimiter \t
			root:
			  data[2]{category,name,price}:
			    Tools, Hardware	Item, A	99.99
			    Electronics	Item B	149.99
			""";

		var withTabs = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Tab };

		// better to encode with tabs or semicolons than with commas because there is no need to quote
		Assert.That(new Subject(node).Encode(withTabs), Is.EqualTo(withTabDelimiter)/*.And
			.Not.Contains("\"")*/, "not quoted");

		var withSemicolons = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Semicolon };
		// better to encode with tabs or semicolons than with commas because there is no need to quote
		Assert.That(new Subject(node).Encode(withSemicolons), Does.Not.Contain("\""), "not quoted");
	}

	#endregion

	#region type hints

	[Test]
	public void Example9_1_TypeHints()
	{
		var user = new JsonObject
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

		string encoded =
			"""
			#version 1.0
			root:
			  user{active:bool,age:u32,id:u32,name:str,score:f64}:
			    active: true
			    age: 30
			    id: 123
			    name: Alice
			    score: 95.5
			""";

		var withTypes = new TonlEncodeOptions { IncludeTypes = true };
		Assert.That(new Subject(user).Encode(withTypes), Is.EqualTo(encoded));
	}

	#endregion

	#region delimiter selection

	[Test]
	public void Example10_1_CsvLike()
	{
		var ne = new JsonObject { { "date", "2025-01-01" }, { "amount", 1500.0 }, { "region", "North, East" } };
		var s = new JsonObject { { "date", "2025-01-02" }, { "amount", 2300.0 }, { "region", "South" } };
		var sales = new JsonObject { { "sales", new JsonArray(ne, s) } };

		string pipeEncoded =
			"""
			#version 1.0
			#delimiter |
			root:
			  sales[2]{amount,date,region}:
			    1500 | 2025-01-01 | North, East
			    2300 | 2025-01-02 | South
			""";
		
		var withPipes = new TonlEncodeOptions { Delimiter = ColumnDelimiter.Pipe };
		// pipe is great for this data because it does not quote string
		Assert.That(new Subject(sales).Encode(withPipes), Is.EqualTo(pipeEncoded));
	}

	#endregion
	
	#endregion
}
