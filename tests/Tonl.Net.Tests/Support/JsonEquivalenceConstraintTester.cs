using System.Text.Json.Nodes;

using Subject = Tonl.Net.Tests.Support.JsonEquivalenceConstraint;

namespace Tonl.Net.Tests.Support;

[TestFixture]
public class JsonEquivalenceConstraintTester : ConstraintTesterBase
{
	#region null-handling
	
	[Test]
	public void ApplyTo_BothNull_True()
	{
		var subject = new Subject(null);
		JsonNode? nil = null;
		Assert.That(Matches(subject, nil), Is.True);
	}

	[Test]
	public void ApplyTo_ExpectedNullActualNonNull_False()
	{
		var subject = new Subject(null);
		var notNull = JsonValue.Create("hello");
		
		Assert.Multiple(() =>
		{
			Assert.That(Matches(subject, notNull), Is.False);
			Assert.That(ExtractMessage(subject, notNull), Does.Contain("at '$'").And
				.Contains("Expected <null>").And
				.Contains("got \"hello\""));
		});
	}

	[Test]
	public void ApplyTo_ExpectedNonNullActualNull_False()
	{
		var subject = new Subject(JsonValue.Create("hello"));
		var nil = (JsonNode?)null;
		Assert.Multiple(() =>
		{
			Assert.That(Matches(subject, nil), Is.False);
			Assert.That(ExtractMessage(subject, nil), Does.Contain("at '$'").And
				.Contains("Expected non-null").And
				.Contains("got <null>"));
		});
	}
	
	#endregion

	#region primitives
	
	[Test]
	public void ApplyTo_IdenticalStrings_True()
	{
		var hello = JsonValue.Create("hello");
		var alsoHello = JsonValue.Create("hello");
		
		var subject = new Subject(hello);
		Assert.That(Matches(subject, alsoHello), Is.True);
	}

	[Test]
	public void ApplyTo_DifferentStrings_False()
	{
		var hello = JsonValue.Create("hello");
		var hi = JsonValue.Create("hi");
		
		var subject = new Subject(hello);
		
		Assert.That(Matches(subject, hi), Is.False);
		Assert.That(ExtractMessage(subject, hi), Does.Contain("at '$'").And
			.Contains("Expected JSON value \"hello\"").And
			.Contains("was \"hi\""));
	}

	[Test]
	public void ApplyTo_IdenticalIntegers_True()
	{
		var fortyTwo = JsonValue.Create(42);
		var alsoFortyTwo = JsonValue.Create(42);
		
		var subject = new Subject(fortyTwo);
		
		Assert.That(Matches(subject, alsoFortyTwo), Is.True);
	}

	[Test]
	public void ApplyTo_DifferentIntegers_False()
	{
		var fortyTwo = JsonValue.Create(42);
		var thirtyThree = JsonValue.Create(33);
		
		var subject = new Subject(fortyTwo);
		
		Assert.That(Matches(subject, thirtyThree), Is.False);
		Assert.That(ExtractMessage(subject, thirtyThree), Does.Contain("at '$'").And
			.Contains("Expected JSON value 42").And
			.Contains("was 33"));
	}
	
	[Test]
	public void ApplyTo_IdenticalBooleans_True()
	{
		var notTrue = JsonValue.Create(false);
		
		var subject	= new Subject(false);
		
		Assert.That(Matches(subject, notTrue), Is.True);
	}

	[Test]
	public void ApplyTo_DifferentBooleans_False()
	{
		var notTrue = JsonValue.Create(false);
		
		var subject = new Subject(true);
		
		Assert.That(Matches(subject, notTrue), Is.False);
		Assert.That(ExtractMessage(subject, notTrue), Does.Contain("at '$'").And
			.Contains("Expected JSON value true").And
			.Contains("was false"));
	}

	[Test]
	public void ApplyTo_IdenticalDoubles_True()
	{
		var pi = JsonValue.Create(3.14);
		var subject = new Subject(3.14);
		Assert.That(Matches(subject, pi), Is.True);
	}

	[Test]
	public void ApplyTo_DifferentDoubles_False()
	{
		var twoPointOne = JsonValue.Create(2.1d);
		
		var subject = new Subject(1.1d);
		
		Assert.That(Matches(subject, twoPointOne), Is.False);
		Assert.That(ExtractMessage(subject, twoPointOne), Does.Contain("at '$'").And
			.Contains("Expected double value 1.1").And
			.Contains("was 2.1"));
	}

	[Test]
	[TestCase(double.PositiveInfinity)]
	[TestCase(double.NegativeInfinity)]
	[TestCase(double.NaN)]
	public void ApplyTo_EqualEdgeNumbers_True(double edgeNumbers)
	{
		Assert.That(Matches(new Subject(edgeNumbers), JsonValue.Create(edgeNumbers)), Is.True);
	}

	[Test]
	public void ApplyTo_ExpectedDoubleActualNotDouble_False()
	{
		var notNumeric = JsonValue.Create("not a number");
		
		var subject = new Subject(double.NegativeZero);
		
		Assert.That(Matches(subject, notNumeric), Is.False);
		Assert.That(ExtractMessage(subject, notNumeric), Does.Contain("at '$'").And
			.Contains("Expected double value -0").And
			.Contains("not a double"));
	}

	[Test]
	public void ApplyTo_ExpectedValueActualObject_False()
	{
		var notAValue = new JsonObject { { "key", "value" } };
		var subject = new Subject(JsonValue.Create("text"));
		
		Assert.That(Matches(subject, notAValue), Is.False);
		Assert.That(ExtractMessage(subject, notAValue), Does.Contain("at '$'").And
			.Contains("Expected 'JsonValue'").And
			.Contains("got 'JsonObject'"));
	}
	
	#endregion

	#region objects

	[Test]
	public void ApplyTo_IdenticalFlatObjects_True()
	{
		var obj = new JsonObject { { "a", "x" }, { "b", 1 } };
		var sameObj = new JsonObject { { "a", "x" }, { "b", 1 } };
		
		var subject = new Subject(obj);
		
		Assert.That(Matches(subject, sameObj), Is.True);
	}

	[Test]
	public void ApplyTo_ObjectsWithDifferentValues_False()
	{
		var obj = new JsonObject { { "name", "Alice" } };
		var differentName = new JsonObject { { "name", "Bob" } };
		
		var subject = new Subject(obj);
		
		Assert.That(Matches(subject, differentName), Is.False);
		Assert.That(ExtractMessage(subject, differentName), Does.Contain("at '$.name'").And
			.Contains("Expected JSON value \"Alice\"").And
			.Contains("was \"Bob\""));
	}

	[Test]
	public void ApplyTo_ActualMissingKey_False()
	{
		var missingKey = new JsonObject { { "a", 1 } };
		
		var subject = new Subject(new JsonObject { { "a", 1 }, { "b", 2 } });
		
		Assert.That(Matches(subject, missingKey), Is.False);
		Assert.That(ExtractMessage(subject, missingKey), Does.Contain("at '$'").And
			.Contains("Expected keys [a, b]").And
			.Contains("were [a]"));
	}

	[Test]
	public void ApplyTo_ActualExtraKey_False()
	{
		var extraKey = new JsonObject { { "a", 1 }, { "b", 2 } };
		
		var subject = new Subject(new JsonObject { { "a", 1 } });
		
		Assert.That(Matches(subject, extraKey), Is.False);
		Assert.That(ExtractMessage(subject, extraKey), Does.Contain("at '$'").And
			.Contains("Expected keys [a]").And
			.Contains("were [a, b]"));
	}

	[Test]
	public void ApplyTo_NestedObjectValueMismatch_False()
	{
		var expected = new JsonObject { { "a", new JsonObject { { "b", 1 } } } };
		var nestedMismatch = new JsonObject { { "a", new JsonObject { { "b", 2 } } } };
		
		var subject = new Subject(expected);
		
		Assert.That(Matches(subject, nestedMismatch), Is.False);
		Assert.That(ExtractMessage(subject, nestedMismatch), Does.Contain("at '$.a.b'").And
			.Contains("Expected JSON value 1").And
			.Contains("was 2"));
	}
	
	#endregion

	#region arrays
	
	[Test]
	public void ApplyTo_IdenticalArrays_True()
	{
		var arr = new JsonArray(1, 2, 3);
		var sameArr = new JsonArray(1, 2, 3);
		
		var subject = new Subject(arr);
		
		Assert.That(Matches(subject, sameArr), Is.True);
	}

	[Test]
	public void ApplyTo_ArraysDifferentLength_False()
	{
		var shorter = new JsonArray(1, 2);
		
		var subject = new Subject(new JsonArray(1, 2, 3));
		
		Assert.That(Matches(subject, shorter), Is.False);
		Assert.That(ExtractMessage(subject, shorter), Does.Contain("at '$'").And
			.Contains("with length 3").And
			.Contains("was 2"));
	}

	[Test]
	public void ApplyTo_ArrayElementMismatch_False()
	{
		var diffAtIndexOne = new JsonArray(10, 99, 30);
		
		var subject = new Subject(new JsonArray(10, 20, 30));
		
		Assert.That(Matches(subject, diffAtIndexOne), Is.False);
		Assert.That(ExtractMessage(subject, diffAtIndexOne), Does.Contain("at '$[1]'").And
			.Contains("JSON value 20").And
			.Contains("was 99"));
	}

	[Test]
	public void ApplyTo_NestedArrayElementMismatch_False()
	{
		var nestedDiffAtTwo = new JsonArray(new JsonArray(1, 2, 99));
		
		var subject = new Subject(new JsonArray(new JsonArray(1, 2, 3)));
		
		Assert.That(Matches(subject, nestedDiffAtTwo), Is.False);
		Assert.That(ExtractMessage(subject, nestedDiffAtTwo), Does.Contain("at '$[0][2]'").And
			.Contains("JSON value 3").And
			.Contains("was 99"));
	}
	
	#endregion

	#region type mismatches
	
	[Test]
	public void ApplyTo_ExpectedObjectActualArray_False()
	{
		var arr = new JsonArray(1, 2, 3);
		var obj = new JsonObject { { "x", 1 } };
		
		var subject = new Subject(obj);
		
		Assert.That(Matches(subject, arr), Is.False);
		Assert.That(ExtractMessage(subject, arr), Does.Contain("at '$'").And
			.Contains("Expected 'JsonObject'").And
			.Contains("got 'JsonArray'"));
	}

	[Test]
	public void ApplyTo_ExpectedArrayActualObject_False()
	{
		var obj = new JsonObject { { "x", 1 } };
		var arr = new JsonArray(1, 2, 3);
		
		var subject = new Subject(arr);
		
		Assert.That(Matches(subject, obj), Is.False);
		Assert.That(ExtractMessage(subject, obj), Does.Contain("at '$'").And
			.Contains("Expected 'JsonArray'").And
			.Contains("got 'JsonObject'"));
	}

	[Test]
	public void ApplyTo_ExpectedValueActualArray_False()
	{
		var arr = new JsonArray(1, 2, 3);
		
		var subject = new Subject(JsonValue.Create("text"));
		
		Assert.That(Matches(subject, arr), Is.False);
		Assert.That(ExtractMessage(subject, arr), Does.Contain("at '$'").And
			.Contains("Expected 'JsonValue'").And
			.Contains("got 'JsonArray'"));
	}

	[Test]
	public void ApplyTo_NonJsonNodeActual_False()
	{
		string notAJsonNode = "not a JsonNode";
		
		var subject = new Subject(JsonValue.Create("hello"));
		
		Assert.That(Matches(subject, notAJsonNode), Is.False);
		Assert.That(ExtractMessage(subject, notAJsonNode), Does.Contain("not a JsonNode").And
			.Contains("was 'String'"));
	}
	
	#endregion

	#region deep combinations

	[Test]
	public void ApplyTo_ComplexNestedTreeEqual_True()
	{
		var complex = new JsonObject
		{
			{
				"users", new JsonArray(
					new JsonObject { { "id", 1 }, { "name", "Alice" }, { "tags", new JsonArray("admin", "verified") } },
					new JsonObject { { "id", 2 }, { "name", "Bob" }, { "tags", new JsonArray("user") } })
			},
			{ "meta", new JsonObject { { "total", 2 }, { "page", 1 } } }
		};
		JsonNode alsoNested = complex.DeepClone();

		var subject = new Subject(complex);

		Assert.That(Matches(subject, alsoNested), Is.True);
	}

	[Test]
	public void ApplyTo_ComplexNestedTreeDeepMismatch_False()
	{
		var newYorker = new JsonObject { { "city", "New York" } };
		var londoner = new JsonObject { { "city", "London" } };
		var withNewYorker = new JsonObject
		{
			{
				"users", new JsonArray(new JsonObject { { "id", 1 }, { "profile", newYorker } })
			}
		};
		var withLondoner = new JsonObject
		{
			{
				"users", new JsonArray(new JsonObject { { "id", 1 }, { "profile", londoner } })
			}
		};
		
		var subject = new Subject(withNewYorker);
		
		Assert.That(Matches(subject, withLondoner), Is.False);
		Assert.That(ExtractMessage(subject, withLondoner), Does.Contain("at '$.users[0].profile.city'").And
			.Contains("JSON value \"New York\"").And
			.Contains("was \"London\""));
	}
	
	#endregion

	#region entry-point

	[Test]
	public void IzJsonEquivalentTo_WorksViaAssertThat()
	{
		var obj = new JsonObject { { "key", "value" }, { "num", 42 } };
		var equivalent = new JsonObject { { "key", "value" }, { "num", 42 } };
		Assert.That(equivalent, Iz.JsonEquivalentTo(obj));
	}

	[Test]
	public void IzJsonEquivalentTo_FailsViaAssertThat()
	{
		var obj = new JsonObject { { "key", "expected" } };
		var notEquivalent = new JsonObject { { "key", "actual" } };
		Assert.That(() => Assert.That(notEquivalent, Iz.JsonEquivalentTo(obj)),
			Throws.InstanceOf<AssertionException>());
	}

	#endregion
}