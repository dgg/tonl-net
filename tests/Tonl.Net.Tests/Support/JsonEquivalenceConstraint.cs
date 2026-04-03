using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using NUnit.Framework.Constraints;

namespace Tonl.Net.Tests.Support;

/// <summary>
/// NUnit constraint that recursively compares two <see cref="JsonNode"/> trees for structural
/// and value equality, reporting the JSON path of the first mismatch in the failure message.
/// </summary>
internal sealed class JsonEquivalenceConstraint(JsonNode? expected) : Constraint
{
	public override string Description => "JSON node equivalent to expected";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        JsonNode? actualNode = actual as JsonNode;

        // If actual is not null and not a JsonNode the types are fundamentally incompatible.
        if (actual is not null && actualNode is null)
        {
            return new JsonEquivalenceResult(this, actual, false,
                $"Actual value is not a JsonNode (was '{actual.GetType().Name}')");
        }

        string? mismatch = findMismatch(expected, actualNode, "$");
        return new JsonEquivalenceResult(this, actual, mismatch is null, mismatch);
    }

    private static string prefix(string path) => $"Discrepancy at '{path}':";

    #region recursive comparison: null on success
    
    private static string? findMismatch(JsonNode? expected, JsonNode? actual, string path)
    {
        if (expected is null && actual is null)
            return null;

        if (actual is null)
            return $"{prefix(path)} Expected non-null node but got <null>'";

        if (expected is null)
            return $"{prefix(path)} Expected <null> but got {actual.ToJsonString()}";

        if (expected is JsonObject expObj)
            return findMismatchInObject(expObj, actual, path);

        if (expected is JsonArray expArr)
            return findMismatchInArray(expArr, actual, path);

        if (expected is JsonValue expVal)
            return findMismatchInValue(expVal, actual, path);

        return $"{prefix(path)} Unrecognised JsonNode type '{expected.GetType().Name}'";
    }

    private static string? findMismatchInObject(JsonObject expected, JsonNode actual, string path)
    {
	    if (actual is not JsonObject actObj)
            return $"{prefix(path)} Expected '{nameof(JsonObject)}' but got '{actual.GetType().Name}'";

        var expectedKeys = expected.Select(p => p.Key)
	        .OrderBy(k => k, StringComparer.Ordinal)
	        .ToArray();
        var actualKeys = actObj.Select(p => p.Key)
	        .OrderBy(k => k, StringComparer.Ordinal)
	        .ToArray();

        if (!expectedKeys.SequenceEqual(actualKeys, StringComparer.Ordinal))
        {
	        return $"{prefix(path)} Expected keys [{_join(expectedKeys)}] but were [{_join(actualKeys)}]";
        }

        foreach (string key in expectedKeys)
        {
            string? childMismatch = findMismatch(expected[key], actObj[key], $"{path}.{key}");
            if (childMismatch is not null)
                return childMismatch;
        }

        return null;

        string _join(string[] a) => string.Join(", ", a);
    }

    private static string? findMismatchInArray(JsonArray expArr, JsonNode actual, string path)
    {
        if (actual is not JsonArray actArr)
            return $"{prefix(path)} Expected '{nameof(JsonArray)}' but got '{actual.GetType().Name}'";

        if (actArr.Count != expArr.Count)
            return $"{prefix(path)} Expected array with length {expArr.Count} but was {actArr.Count}";

        for (int i = 0; i < expArr.Count; i++)
        {
            string? childMismatch = findMismatch(expArr[i], actArr[i], $"{path}[{i}]");
            if (childMismatch is not null)
                return childMismatch;
        }

        return null;
    }

    private static string? findMismatchInValue(JsonValue expected, JsonNode actual, string path)
    {
        if (actual is not JsonValue actVal)
            return $"{prefix(path)} Expected '{nameof(JsonValue)}' but got '{actual.GetType().Name}'";

        // Compare doubles especially to handle Infinity / NaN which cannot be serialized to JSON.
        if (findMismatchInDouble(expected, actVal, path, out string? doubleMessage))
        {
	        return doubleMessage;
        }
        
        // Compare via raw string representation for all other value types.
        string expectedRepresentation = expected.ToJsonString();
        string actualRepresentation = actVal.ToJsonString();

        if (!StringComparer.Ordinal.Equals(expectedRepresentation, actualRepresentation))
			return $"{prefix(path)} Expected JSON value {expectedRepresentation} but was {actualRepresentation}";

        return null;
    }

    private static bool findMismatchInDouble(JsonValue expected, JsonValue actual, string path, out string? message)
    {
	    if (expected.TryGetValue(out double expDouble))
	    {
		    if (!actual.TryGetValue(out double actDouble))
		    {
			    message = $"{prefix(path)} Expected double value {expected.ToJsonString()} but actual is not a double";
			    return true;
		    }

		    if (!expDouble.Equals(actDouble))
		    {
			    message = $"{prefix(path)} Expected double value {expected.ToJsonString()} but was {actual.ToJsonString()}";
			    return true;
		    }

		    message = null;
		    return true;
	    }

	    message = null;
	    return false;
    }

    #endregion

    private sealed class JsonEquivalenceResult(
	    IConstraint constraint,
	    object? actualValue,
	    bool isSuccess,
	    string? mismatchMessage)
	    : ConstraintResult(constraint, actualValue, isSuccess)
    {
	    public override void WriteMessageTo(MessageWriter writer)
        {
            if (mismatchMessage is not null)
                writer.Write(mismatchMessage);
            else
                base.WriteMessageTo(writer);
        }
    }
}

/// <summary>
/// Entry point for <see cref="JsonEquivalenceConstraint"/> in Assert expressions.
/// Usage: <c>Assert.That(actual, JsonNodeIs.EqualTo(expected))</c>
/// </summary>
internal partial class Iz: Is
{
    /// <summary>
    /// Returns a constraint that verifies two <see cref="JsonNode"/> trees are structurally
    /// and value-equal, reporting the path of the first mismatch on failure.
    /// </summary>
    public static JsonEquivalenceConstraint JsonEquivalentTo(JsonNode? expected) => new(expected);
}
