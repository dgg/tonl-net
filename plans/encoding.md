# TONL Encoding Implementation Plan

## Overview

Add a `public string Encode(TonlEncodeOptions? options = null)` method to `TonlDocument` that serializes the underlying `JsonNode?` into a TONL-formatted string per the TONL spec v2.5.2 / implementation reference v2.0.6.

The approach operates directly on `System.Text.Json.Nodes.JsonNode` (the existing data model), walks the JSON tree recursively, and emits TONL text. Correctness and round-trip fidelity are the primary goals; performance optimization is deferred.

---

## 1. New Files

| File | Purpose |
|------|---------|
| `src/Tonl.Net/TonlEncodeOptions.cs` | Public options class (delimiter, type hints, version, indent size) |
| `src/Tonl.Net/TonlEncoder.cs` | Internal workhorse — StringBuilder-based recursive encoder |
| `src/Tonl.Net/TonlStringHelper.cs` | Internal string quoting/escaping utilities |
| `src/Tonl.Net/TonlTypeInference.cs` | Internal type inference and array classification |
| `tests/Tonl.Net.Tests/TonlStringHelperTester.cs` | Unit tests for quoting logic |
| `tests/Tonl.Net.Tests/TonlTypeInferenceTester.cs` | Unit tests for type inference |
| `tests/Tonl.Net.Tests/TonlEncoderTester.cs` | Integration-level encoder tests |

## 2. Files to Modify

- `src/Tonl.Net/TonlDocument.cs` — add `public string Encode(TonlEncodeOptions? options = null)`
- `tests/Tonl.Net.Tests/TonlDocumentTester.cs` — add encoding test cases

---

## 3. Public API

### `TonlEncodeOptions`

```csharp
public class TonlEncodeOptions
{
    public string Delimiter { get; init; } = ",";       // ",", "|", "\t", ";"
    public bool IncludeTypes { get; init; } = false;    // emit type hints in headers
    public string Version { get; init; } = "1.0";
    public int IndentSize { get; init; } = 2;           // spaces per nesting level
}
```

All properties have sensible defaults so `Encode()` with `null` options is identical to `Encode(new TonlEncodeOptions())`.

### `TonlDocument.Encode`

```csharp
public string Encode(TonlEncodeOptions? options = null)
```

Returns the full TONL document string including `#version` header and optional `#delimiter` header. Delegates to an internal `TonlEncoder` instance.

---

## 4. Internal Architecture

### `TonlEncoder` (internal)

**File:** `src/Tonl.Net/TonlEncoder.cs`

Workhorse class, `internal` (visible to tests via `InternalsVisibleTo`).

**Responsibilities:**
- Accept a `JsonNode?` and `TonlEncodeOptions`, produce a `string`.
- Maintain encoding context: current indent level, delimiter, circular reference tracking, recursion depth.
- Dispatch encoding by node type: `JsonValue`, `JsonObject`, `JsonArray`.

**Key design decisions:**
- Use `StringBuilder` for output assembly (correctness-first; can later be replaced with `ArrayBufferWriter<char>` or span-based approach).
- Track visited objects/arrays by reference identity (`HashSet<JsonNode>` using `ReferenceEqualityComparer.Instance`) for circular reference detection. After processing a subtree, remove from the set (allowing shared references at multiple paths, like the reference implementation does).
- Cap recursion depth at 500 (matching reference implementation).

**Skeleton:**

```csharp
internal sealed class TonlEncoder
{
    // Fields: options, StringBuilder, indent level, seen set, depth counter

    public string Encode(JsonNode? root)
    {
        // 1. Emit "#version {version}"
        // 2. If delimiter != ",", emit "#delimiter {escaped}"
        // 3. Call EncodeValue(root, "root")
        // 4. Return builder.ToString()
    }

    private void EncodeValue(JsonNode? value, string key)
    private void EncodePrimitive(JsonValue value, string key)
    private void EncodeObject(JsonObject obj, string key)
    private void EncodeArray(JsonArray arr, string key)
    private void EncodeTabularArray(JsonArray arr, string key, IReadOnlyList<string> columns)
    private void EncodePrimitiveArray(JsonArray arr, string key)
    private void EncodeMixedArray(JsonArray arr, string key)
}
```

### `TonlStringHelper` (internal)

**File:** `src/Tonl.Net/TonlStringHelper.cs`

Encapsulates all quoting/escaping logic. Independently testable.

```csharp
internal static class TonlStringHelper
{
    public static bool NeedsQuoting(string value, string delimiter)
    public static string Quote(string value)
    public static string TripleQuote(string value)
    public static string QuoteIfNeeded(string value, string delimiter)
    public static string TripleQuoteIfNeeded(string value, string delimiter)
    public static bool KeyNeedsQuoting(string key)
    public static string QuoteKey(string key)
}
```

### `TonlTypeInference` (internal)

**File:** `src/Tonl.Net/TonlTypeInference.cs`

Maps `JsonNode` values to TONL type hint strings.

```csharp
internal static class TonlTypeInference
{
    // Returns "null", "bool", "u32", "i32", "f64", "str", "obj", "list"
    public static string InferType(JsonNode? value)

    // Checks if a JsonArray is a uniform object array (all elements are JsonObject with identical sorted key sets)
    public static bool IsUniformObjectArray(JsonArray arr)

    // Checks semi-uniform (>= 60% key overlap threshold, matching reference impl)
    public static bool IsSemiUniformObjectArray(JsonArray arr, double threshold = 0.6)

    // Gets the sorted union of all keys from objects in the array
    public static IReadOnlyList<string> GetAllColumns(JsonArray arr)

    // Gets sorted keys from first element (for strictly uniform arrays)
    public static IReadOnlyList<string> GetUniformColumns(JsonArray arr)
}
```

---

## 5. Encoding Logic Detail

### Root encoding (`Encode`)

1. Emit `#version {version}` line.
2. If delimiter is not `,`, emit `#delimiter {escapedDelimiter}` (tab becomes `\t`).
3. If `root` is null, emit `root: null`.
4. Otherwise call `EncodeValue(root, "root")`.
5. Join all lines with `\n` (no trailing newline).

### Value dispatch (`EncodeValue`)

Given a `JsonNode? value` and `string key`:

- **null**: emit `{key}: null`
- **JsonValue (bool)**: emit `{key}: true` or `{key}: false`
- **JsonValue (number)**: Check if finite. Non-finite maps to `null` per the reference implementation. Otherwise emit `{key}: {number}`.
- **JsonValue (string)**: emit `{key}: {QuoteIfNeeded(value, delimiter)}`
- **JsonArray**: delegate to `EncodeArray`
- **JsonObject**: delegate to `EncodeObject`

### Object encoding (`EncodeObject`)

1. **Circular reference check**: If object already in `seen` set, throw `InvalidOperationException`. Add to set; wrap body in try/finally that removes from set.
2. **Depth check**: If `currentDepth >= 500`, throw.
3. **Sort keys alphabetically** (ordinal, case-sensitive — matches reference impl).
4. **Build column definitions**: For each key, optionally append `:typeHint` if `IncludeTypes` is true and the inferred type is not `obj` or `list`. Quote column names that contain structural characters.
5. **Build header**:
   - Single property: `{key}:` (no column notation).
   - Otherwise: `{key}{col1,col2,...}:`.
6. **Always use multi-line format** for objects.
7. **Emit child values**: For each key in sorted order, indent by `(currentIndent + 1) * indentSize` spaces.

### Array encoding (`EncodeArray`)

1. **Circular reference check + depth check** (same pattern as objects).
2. **Empty array**: emit `{key}[0]:` and return.
3. **Uniform object array** (`IsUniformObjectArray`): If yes, and all objects contain only primitive values (no nested objects/arrays), use tabular format.
4. **Semi-uniform** (`IsSemiUniformObjectArray` with 0.6 threshold): If yes, and all objects contain only primitive values, use tabular format with `GetAllColumns`.
5. **All primitives** (all elements are `JsonValue`): use primitive array encoding.
6. **Otherwise**: mixed array encoding.

**Tabular array encoding:**
- Header: `{key}[{count}]{col1,col2,...}:`
- Each row: indent + values joined by delimiter.
- Missing fields (semi-uniform case) emit empty string.
- Null values emit `null`. Booleans/numbers unquoted. Strings: `TripleQuoteIfNeeded`.

**Primitive array encoding:**
- Single line: `{key}[{count}]: val1, val2, val3`
- Null items emit `null`. Non-finite numbers emit `null`.

**Mixed array encoding:**
- Header: `{key}[{count}]:`
- Each element on its own indented line as `[{index}]: {value}` for primitives, `[{index}]{...}: ...` for objects, `[{index}][{subcount}]: ...` for nested primitive arrays.

### String quoting (`TonlStringHelper`)

**`NeedsQuoting(value, delimiter)`** returns true if:
- Empty string
- Equals `"true"`, `"false"`, `"null"`, `"undefined"`
- Equals `"Infinity"`, `"-Infinity"`, `"NaN"`
- Matches integer pattern: `^-?\d+$`
- Matches decimal pattern: `^-?\d*\.\d+$`
- Matches scientific notation: `^-?\d+\.?\d*[eE][+-]?\d+$`
- Contains: delimiter char, `:`, `{`, `}`, `#`, `"`, `\n`, `\t`, `\r`
- Starts or ends with space

**`Quote(value)`**: Escapes `\` → `\\`, `\r` → `\r` literal, `\n` → `\n` literal, `\t` → `\t` literal, `"` → `\"`, then wraps in double quotes.

**`TripleQuoteIfNeeded(value, delimiter)`**: If value contains `\n` or `"""`, use triple-quoting. Otherwise delegate to `QuoteIfNeeded`.

> **Note on quoting style**: The reference TypeScript implementation uses backslash escaping (`\"`, `\\`, `\n`, `\r`, `\t`). The .NET implementation must match this for interoperability.

---

## 6. Edge Cases

| Edge Case | Handling |
|-----------|----------|
| `null` root | Emit `root: null` |
| Empty object `{}` | Header only, no children |
| Empty array `[]` | `key[0]:` |
| Empty string value | Quoted: `""` |
| String `"true"` vs bool `true` | String gets quoted: `"true"` vs unquoted `true` |
| String `"123"` vs number `123` | String gets quoted: `"123"` vs unquoted `123` |
| String `"null"` vs actual null | String gets quoted: `"null"` vs unquoted `null` |
| String with delimiter chars | Quoted |
| String with newlines | Triple-quoted with `\n` escaping |
| String with `"""` inside | Triple-quoted with `\"""` escaping |
| String with leading/trailing spaces | Quoted |
| String with backslashes | Backslash-escaped in quotes |
| Keys with special chars (`:`, `,`, `{`, `}`, `"`, `#`, `@`, whitespace) | Key name quoted in header and key-value lines |
| Circular reference (same `JsonNode` in ancestry) | `InvalidOperationException` thrown |
| Shared references (same node at two non-ancestral paths) | Allowed (seen set uses try/finally removal) |
| Deep nesting (> 500 levels) | `InvalidOperationException` thrown |
| Semi-uniform arrays (objects with overlapping but not identical keys) | Tabular format with all columns, missing fields as empty |
| Mixed arrays (primitives + objects + arrays) | Index-based notation |
| Uniform object arrays where objects have nested objects/arrays | Falls through to mixed array format |

---

## 7. Implementation Phases

### Phase 1: Foundation
Build `TonlStringHelper` + `TonlTypeInference` + their unit tests.

**Acceptance criteria:**
- `NeedsQuoting` correctly identifies all cases.
- `Quote` and `TripleQuote` produce output matching reference implementation.
- `IsUniformObjectArray` correctly classifies arrays.
- `InferType` maps JSON values to correct TONL type strings.

### Phase 2: Core encoder (primitives + objects)
Build `TonlEncodeOptions`, `TonlEncoder` with `Encode`/`EncodeValue`/`EncodePrimitive`/`EncodeObject`, and `TonlDocument.Encode` wiring.

**Acceptance criteria:**
- Simple objects round-trip correctly.
- Headers (`#version`, `#delimiter`) are correctly emitted.
- Nested objects produce correct indentation.
- Keys are alphabetically sorted.

### Phase 3: Array encoding
Build `EncodeArray`, `EncodePrimitiveArray`, `EncodeTabularArray`, `EncodeMixedArray`.

**Acceptance criteria:**
- Primitive arrays produce single-line output.
- Uniform object arrays produce tabular format with sorted columns.
- Semi-uniform arrays include all columns with missing field markers.
- Mixed arrays use index-based notation.

### Phase 4: Safety + edge cases
Add circular reference detection, depth limiting, and all edge case tests.

**Acceptance criteria:**
- Circular reference throws.
- Shared references allowed.
- Depth > 500 throws.
- All edge-case tests pass.

### Phase 5: Compliance validation
All 17 required test cases from the implementation reference. Diff against reference TypeScript encoder output for known inputs.

**Acceptance criteria:**
- All compliance test scenarios produce correct output.
- Output for standard inputs matches the reference implementation.

---

## 8. Test Scenarios

### String Helper Tests (`TonlStringHelperTester`) — 20 cases
1. Empty string needs quoting → `""`
2. `"true"` needs quoting → `"true"`
3. `"false"` needs quoting → `"false"`
4. `"null"` needs quoting → `"null"`
5. `"123"` (number-like) needs quoting → `"123"`
6. `"3.14"` (decimal-like) needs quoting → `"3.14"`
7. `"1e10"` (scientific) needs quoting → `"1e10"`
8. `"Infinity"` needs quoting → `"Infinity"`
9. String containing comma (with comma delimiter) needs quoting
10. String containing colon needs quoting
11. String containing `{` or `}` needs quoting
12. String containing `#` needs quoting
13. String containing `"` needs quoting → escaped with `\"`
14. String with leading space needs quoting
15. String with trailing space needs quoting
16. String with `\n` uses triple quoting
17. String with `"""` inside uses triple quoting with escaping
18. String with backslashes correctly escaped
19. Plain string (no special chars) does NOT get quoted
20. String containing pipe with comma delimiter does NOT need quoting

### Type Inference Tests (`TonlTypeInferenceTester`) — 19 cases
1. null → `"null"`
2. true/false → `"bool"`
3. 0 → `"u32"`
4. 42 → `"u32"`
5. 4294967295 (uint max) → `"u32"`
6. -1 → `"i32"`
7. -2147483648 (int min) → `"i32"`
8. 3.14 → `"f64"`
9. 1e20 → `"f64"`
10. string → `"str"`
11. JsonObject → `"obj"`
12. JsonArray → `"list"`
13. Uniform array: all objects same keys → true
14. Uniform array: different keys → false
15. Uniform array: empty array → true
16. Uniform array: non-object elements → false
17. Semi-uniform: 70% overlap → true
18. `GetAllColumns` returns sorted union of keys
19. `GetUniformColumns` returns sorted keys of first element

### Encoder Tests — 32 cases

**Primitives (4):**
1. Null root → `#version 1.0\nroot: null`
2. Object with bool field
3. Object with integer/float field
4. Object with string (plain and needing quoting)

**Objects (6):**
5. Simple flat object → sorted keys, multi-line format
6. Nested object → proper indentation
7. Object with single property → `key:` format (no column notation)
8. Object with mixed value types
9. Object with keys requiring quoting
10. Empty object → just header line

**Primitive Arrays (4):**
11. Array of integers → `key[N]: 1, 2, 3`
12. Array of strings → proper quoting
13. Array of mixed primitives (string + number + bool + null)
14. Empty array → `key[0]:`

**Tabular Arrays (3):**
15. Uniform object array → tabular format with sorted columns
16. Semi-uniform array → tabular with missing field markers
17. Tabular with values needing quoting

**Mixed Arrays (2):**
18. Array of mixed types → index notation
19. Nested array within mixed array

**Headers (4):**
20. Default delimiter (comma) → no `#delimiter` header
21. Pipe delimiter → `#delimiter |`
22. Tab delimiter → `#delimiter \t`
23. Semicolon delimiter → `#delimiter ;`

**Edge Cases (8):**
24. String `"true"` vs bool `true` distinction
25. String `"123"` vs number `123` distinction
26. String `"null"` vs null distinction
27. Empty string encoded as `""`
28. Circular reference throws `InvalidOperationException`
29. Shared reference (same object at two paths) does NOT throw
30. Depth limit exceeded throws `InvalidOperationException`
31. String with newlines → triple-quoted
32. String with embedded `"""` → triple-quoted with escaping

**Compliance (17):**
33–49. All 17 required compliance test cases from the implementation reference.

**Type Hints (2):**
50. With `IncludeTypes = true`, headers include `:u32`, `:str`, `:bool`, etc.
51. Type hints omit `obj` and `list` (implied by structure)

---

## 9. Design Decisions & Open Questions

| # | Question | Recommendation |
|---|---------|----------------|
| 1 | **Quoting style**: `""` (doubled, spec prose) vs `\"` (backslash, reference TS impl) | Follow reference implementation (backslash escaping) — confirm as intentional. |
| 2 | **Numeric formatting** | `value.ToString("G", CultureInfo.InvariantCulture)` — verify edge cases (`0.1`, `1e-7`, large integers). |
| 3 | **Non-object/array root** | Wrap as `root: value`, matching reference's `encodeValue(input, "root", context)`. |
| 4 | **`prettyDelimiters`/`compactTables` options** | Omit from initial implementation; `TonlEncodeOptions` extensible later. |
| 5 | **Error type** | `InvalidOperationException` for circular refs and depth overflow (idiomatic .NET). |

### Known Risks

| Risk | Mitigation |
|------|------------|
| Spec ambiguity between SPECIFICATION.md and IMPLEMENTATION_REFERENCE.md | Follow reference TypeScript encoder code as authoritative. |
| `JsonNode` API quirks (e.g., `JsonValue` wrapping different underlying types) | Use `JsonValue.TryGetValue<T>()` with explicit type probing: null, bool, numeric types (long/int then double), string. |
| Single-property object format differs from multi-property | Match reference: single-property uses `key:` header without column notation. |
| .NET `double.ToString()` format differs from JavaScript's `Number.toString()` | Use `ToString("G", CultureInfo.InvariantCulture)`. |
| Semi-uniform threshold (0.6) producing surprising results | Document the threshold; match reference implementation exactly. |

---

## 10. Future Optimization Hooks

- `StringBuilder` → `ValueStringBuilder` (stack-allocated small buffer with heap fallback) or `ArrayBufferWriter<char>`.
- `Encode(TextWriter)` streaming variant for large documents.
- Cached sorted key lists for large objects.
- Short-circuit on first mismatch in uniform array detection (verify existing LINQ `All` already does this).

---

## 11. Success Criteria

1. `TonlDocument.Encode()` produces valid TONL for all JSON data types.
2. Output matches the reference TypeScript encoder for a defined set of test inputs.
3. All 17 compliance test cases from the implementation reference pass.
4. String quoting correctly distinguishes literals from their string representations.
5. Circular references are detected and reported.
6. Code compiles with zero warnings under `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild`.
7. XML documentation is complete on all public API surface (required by `GenerateDocumentationFile`).
