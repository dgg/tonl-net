# TONL Decoder Implementation Plan

## Overview

Add a static `TonlDocument.Decode(string tonl, TonlDecodeOptions? options = null)` method that parses a TONL-formatted string and returns a `TonlDocument` with its `Root` property populated as a `JsonNode?` tree. The decoder must be the inverse of the existing encoder, producing JSON structures that round-trip faithfully: `Encode(Decode(tonl))` yields semantically equivalent output (modulo key ordering, which the encoder already sorts).

---

## 1. New Files to Create

| File | Purpose |
|------|---------|
| `src/Tonl.Net/TonlDecodeOptions.cs` | Record with decode configuration |
| `src/Tonl.Net/TonlDocument.Decode.cs` | Partial class containing the `Decode` static method and all private parsing logic |
| `src/Tonl.Net/TonlParseException.cs` | Custom exception for strict-mode parse errors |
| `tests/Tonl.Net.Tests/TonlDecodeTester.cs` | Focused unit tests for the decoder |
| `tests/Tonl.Net.Tests/TonlRoundTripTester.cs` | Round-trip tests: encode then decode (and decode then encode) using existing test data |

---

## 2. `TonlDecodeOptions` Record

```
File: src/Tonl.Net/TonlDecodeOptions.cs
```

Properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Delimiter` | `ColumnDelimiter` | `ColumnDelimiter.Comma` | Fallback delimiter when no `#delimiter` header is present |
| `StrictMode` | `bool` | `false` | When true, ambiguous or invalid input throws `TonlParseException`; when false, best-effort parsing is used |

Implementation note: The `#delimiter` header in the document always overrides the `Delimiter` option. The option is only a fallback for headerless input.

---

## 3. `TonlParseException`

```
File: src/Tonl.Net/TonlParseException.cs
```

A simple exception class inheriting from `FormatException`. It should carry:
- `int LineNumber` -- the 1-based line number where the error occurred
- `string? LineContent` -- the raw line text (for diagnostics)
- Standard `message` via base constructor

This keeps the exception lightweight and informative. Use `FormatException` as the base since TONL parsing is fundamentally a format/deserialization concern.

---

## 4. Parser Architecture

The decoder works in three passes over the input string, all happening inside a single top-to-bottom scan. There is no separate tokenizer stage -- lines are classified and consumed in a streaming fashion.

### 4.1 High-Level Flow

```
Decode(string tonl, TonlDecodeOptions? options)
  1. Split input into lines (preserving line numbers for error reporting)
  2. Parse header lines (#version, #delimiter) -- advance line cursor past them
  3. Resolve the effective delimiter (header > option > comma default)
  4. Resolve the effective indent size (detect from first indented line)
  5. Parse the data section using a recursive-descent approach driven by indentation
  6. Return new TonlDocument(rootNode)
```

### 4.2 Internal State (Private Fields)

Following the existing naming conventions (`_camelCase` for private fields):

| Field | Type | Purpose |
|-------|------|---------|
| `_lines` | `string[]` | All lines from the input |
| `_cursor` | `int` | Current line index (0-based) |
| `_indentSize` | `int` | Detected or default indent size (default 2) |
| `_delimiter` | `ColumnDelimiter` | Effective delimiter after header parsing |
| `_strict` | `bool` | Cached strict mode flag |

### 4.3 Private Method Inventory

All private methods use camelCase per project conventions:

| Method | Signature | Purpose |
|--------|-----------|---------|
| `parseHeaders` | `void parseHeaders(TonlDecodeOptions opts)` | Consume `#version` and `#delimiter` lines, set `_delimiter` |
| `detectIndentSize` | `void detectIndentSize()` | Scan forward to find first indented line, measure space count |
| `parseRoot` | `JsonNode? parseRoot()` | Entry point: determine if root is object, array, or primitive |
| `parseBlock` | `JsonNode? parseBlock(int expectedIndent)` | Parse a multi-line block (object or array body) at a given indent |
| `parseObjectBody` | `JsonObject parseObjectBody(int bodyIndent, ColumnDef[]? columns)` | Parse child key-value pairs at the given indent level |
| `parseTabularRows` | `JsonArray parseTabularRows(int rowIndent, int count, ColumnDef[] columns)` | Parse N tabular data rows into an array of objects |
| `parsePrimitiveArrayInline` | `JsonArray parsePrimitiveArrayInline(string rawValues, int count)` | Parse `key[N]: v1, v2, v3` inline values |
| `parseMixedArrayBody` | `JsonArray parseMixedArrayBody(int bodyIndent, int count)` | Parse indexed `[0]: ...` elements |
| `classifyLine` | `LineInfo classifyLine(int lineIndex)` | Classify a line and extract structural components |
| `splitDelimited` | `List<string> splitDelimited(string raw)` | Split a line on the delimiter respecting quoted fields |
| `parseValue` | `JsonNode? parseValue(string raw, ColumnDef? typeHint)` | Convert a raw string token into a typed `JsonNode` |
| `unquote` | `string unquote(string raw)` | Remove quotes and process escape sequences |
| `parseColumnDefs` | `ColumnDef[] parseColumnDefs(string columnsRaw)` | Parse `col1:type,col2,...` into structured definitions |
| `getIndent` | `int getIndent(int lineIndex)` | Count leading spaces on a line |
| `throwOrWarn` | `void throwOrWarn(string message, int lineIndex)` | Throw in strict mode, ignore/log in lenient mode |

---

## 5. Line Classification

Each line is classified into one of these types. The `classifyLine` method returns a `LineInfo` struct (or readonly record struct) containing the classification and extracted components.

### 5.1 `LineType` Enum

```csharp
internal enum LineType
{
    Blank,
    Comment,
    VersionHeader,
    DelimiterHeader,
    ObjectHeader,           // key{col1,col2}:
    ArrayHeader,            // key[N]{col1,...}:  or  key[N]:
    PrimitiveValue,         // key: value
    InlinePrimitiveArray,   // key[N]: v1, v2, v3
    TabularRow,             // indented data row (no key prefix)
    IndexedElement,         // [N]: value  or  [N]{...}:  or  [N][M]:
    EmptyObject,            // key:  (with nothing after colon, no columns, no array bracket)
}
```

### 5.2 `LineInfo` Readonly Record Struct

```csharp
internal readonly record struct LineInfo(
    LineType Type,
    int Indent,          // number of leading spaces
    string Key,          // extracted key name (empty for rows/blanks)
    int ArrayCount,      // -1 if not an array header
    string? ColumnsRaw,  // raw column string inside {} or null
    string? ValueRaw     // raw value string after ": " or null
);
```

### 5.3 Classification Logic

The classification runs against the trimmed-then-analyzed content of each line. Use a combination of regex and manual character scanning for performance:

1. **Blank line**: line is empty or whitespace-only.
2. **Comment**: first non-whitespace is `#` and line does not match a header pattern. Treat any `#`-prefixed line that is not a recognized header as a comment.
3. **`#version` header**: matches `^\s*#version\s+(.+)$`.
4. **`#delimiter` header**: matches `^\s*#delimiter\s+(.+)$`.
5. **Array header with columns**: `^(\S+)\[(\d+)\]\{([^}]*)\}:\s*$` (applied to trimmed content after stripping indent).
6. **Array header without columns** (primitive or mixed): `^(\S+)\[(\d+)\]:\s*(.*)$`. If there is content after `: `, it is an inline primitive array. If empty, it is a multi-line mixed/empty array.
7. **Object header with columns**: `^(\S+)\{([^}]*)\}:\s*$`.
8. **Indexed element**: `^\[(\d+)\](.*)$` -- further sub-classify based on what follows.
9. **Primitive value line**: `^(\S+):\s+(.+)$` or `^(\S+):\s*$`.
10. **Tabular row**: A line at the expected indent level under a tabular header that does not match any of the above patterns. This is a positional classification.

**Approach**: Rather than pure regex, use a manual character-scanning `classifyLine` method that:
1. Measures leading spaces (indent).
2. Extracts the key (handling quoted keys with `\"` escape support).
3. Looks at the character after the key: `{`, `[`, `:`, or space.
4. Based on that, determines the line type and extracts components.

---

## 6. Indentation Tracking

### 6.1 Indent Detection

The indent size is not declared in headers. Detect it from the first line that has a greater indent than the line before it. Scan through lines looking for the first transition from indent N to indent M where M > N; the indent size is `M - N`. Default to 2 if no indented lines exist.

### 6.2 Scope Management

The parser uses indent level to determine when a block ends:

- When parsing children at indent level `L`, keep consuming lines while `getIndent(currentLine) >= L`.
- A line at indent `< L` means the current block is done -- return to the parent.
- A line at indent `== L` is a sibling within the same parent.
- A line at indent `> L` belongs to a child block (only valid if the previous line opened a block header).

### 6.3 Root Line Handling

The first data line (after headers) is the root -- typically `root:` or `root{...}:` at indent 0. The `root` key is synthetic. The decoder strips this wrapper: the `Root` property of `TonlDocument` is the value associated with the `root` key, not an object containing a `root` key.

---

## 7. Value Parsing

The `parseValue(string raw, ColumnDef? typeHint)` method converts a raw string token into a `JsonNode?`.

### 7.1 Decision Order (Without Type Hint)

1. **`null`** (literal, unquoted, case-sensitive): return `null`.
2. **`true`** / **`false`** (literal, unquoted): return `JsonValue.Create(bool)`.
3. **Quoted string** (starts with `"` or `"""`): call `unquote()`, return `JsonValue.Create(string)`.
4. **Numeric**: attempt parsing as integer first, then double.
   - Integer pattern (`-?\d+`): try `int.TryParse`, then `uint.TryParse`, then `long.TryParse`, then `ulong.TryParse`. Use the narrowest type that fits.
   - Decimal/scientific pattern: `double.TryParse` with `InvariantCulture`.
   - Special literals: `Infinity` → `double.PositiveInfinity`, `-Infinity` → `double.NegativeInfinity`, `NaN` → `double.NaN`.
5. **Unquoted string**: return `JsonValue.Create(string)` with the raw value as-is.

### 7.2 Decision Order (With Type Hint)

| Hint | Parsing |
|------|---------|
| `str` | If quoted, unquote. Otherwise treat raw as string. Never parse as number/bool. |
| `bool` | Must be `true` or `false`. Strict: throw on failure. Lenient: fall through to default. |
| `u32` | Parse as `uint`. Strict: throw on failure. |
| `i32` | Parse as `int`. Strict: throw on failure. |
| `f64` | Parse as `double`. Handle `Infinity`/`-Infinity`/`NaN`. Strict: throw on failure. |
| `null` | Must be literal `null` or empty. Strict: throw on anything else. |
| `obj` / `list` | Structural -- ignore hint for value parsing. |

### 7.3 Empty Cell Handling

In tabular rows, an empty cell means the property is set to `null` in the resulting `JsonObject` (not omitted, not empty string). This matches the encoder's behavior: null/missing properties emit an empty cell.

---

## 8. Unquoting / String Parsing

### 8.1 `unquote(string raw)` Method

**Triple-quoted** (`"""..."""`):
- Strip leading and trailing `"""`.
- Process escape sequences: `\\` → `\`, `\"` → `"`, `\n` → newline, `\r` → CR, `\t` → tab.

**Single-quoted** (`"..."`):
- Strip leading and trailing `"`.
- Process escape sequences: `\\` → `\`, `\"` → `"`, `\n` → newline, `\r` → CR, `\t` → tab.

The encoder uses backslash-based escaping (`\"`, `\\`, `\n`, `\r`, `\t`), not the doubling convention (`""` for quotes).

---

## 9. Tabular Row Parsing

### 9.1 `splitDelimited(string raw)` State Machine

```
States: Normal, InSingleQuote, InTripleQuote

Normal:
  - delimiter char → emit current field (trimmed), start new field
  - '"' → check if next two chars are also '"':
      yes → enter InTripleQuote, advance past """
      no  → enter InSingleQuote
  - other → append to current field

InSingleQuote:
  - '\' followed by '"' → append literal ", advance
  - '\' followed by '\' → append literal \, advance
  - '\' followed by 'n' → append newline, advance
  - '\' followed by 'r' → append CR, advance
  - '\' followed by 't' → append tab, advance
  - '"' (unescaped) → exit to Normal
  - other → append to current field

InTripleQuote:
  - '\' followed by '"""' → append literal """, advance
  - '\' followed by '\' → append literal \, advance
  - '"""' (unescaped) → exit to Normal
  - other → append to current field
```

After all characters consumed, emit the final field. Each field is trimmed of surrounding whitespace before value parsing.

### 9.2 Tabular Row to JsonObject

```
for i in 0..columns.Length:
    if i < fields.Count:
        raw = fields[i]
        obj[columns[i].Name] = raw == "" ? null : parseValue(raw, columns[i])
    else:
        obj[columns[i].Name] = null   // fewer fields than columns

// In strict mode: if fields.Count > columns.Length, throw
```

---

## 10. `ColumnDef` Structure

```csharp
internal readonly record struct ColumnDef(string Name, string? TypeHint);
```

### `parseColumnDefs(string columnsRaw)` Method

Parse the content between `{` and `}`. Split on `,` (always comma, regardless of data delimiter). For each segment:
1. Trim whitespace.
2. Check if it contains `:` -- if so, split on the first `:` to get `name` and `typeHint`.
3. If the name is quoted, unquote it.
4. Return `ColumnDef(name, typeHint)`.

---

## 11. Recursive Descent Parser Detail

### 11.1 `parseRoot()`

1. Skip blank lines and comments.
2. Read the first data line at indent 0.
3. Based on classification:
   - **ObjectHeader** (`root{...}:`): `parseObjectBody(_indentSize, columns)`
   - **ArrayHeader** (`root[N]{...}:`): `parseTabularRows(_indentSize, N, columns)`
   - **ArrayHeader** (`root[N]:`) with inline value: `parsePrimitiveArrayInline(value, N)`
   - **ArrayHeader** (`root[N]:`) without inline and N > 0: `parseMixedArrayBody(_indentSize, N)`
   - **ArrayHeader** (`root[0]:`): return `new JsonArray()`
   - **PrimitiveValue** (`root: value`): `parseValue(value, null)`
   - **EmptyObject** (`root:`): peek next line; if at `_indentSize`, `parseObjectBody(_indentSize, null)`, else return `new JsonObject()`

### 11.2 `parseObjectBody(int bodyIndent, ColumnDef[]? columns)`

```
obj = new JsonObject()
while cursor < lines.Length && getIndent(cursor) >= bodyIndent:
    info = classifyLine(cursor)
    switch info.Type:
        ObjectHeader       → cursor++; obj[key] = parseObjectBody(bodyIndent + _indentSize, columns)
        ArrayHeader+cols   → cursor++; obj[key] = parseTabularRows(bodyIndent + _indentSize, N, cols)
        ArrayHeader+inline → cursor++; obj[key] = parsePrimitiveArrayInline(value, N)
        ArrayHeader empty  → cursor++; obj[key] = new JsonArray() or parseMixedArrayBody(...)
        PrimitiveValue     → cursor++; obj[key] = parseValue(value, null)
        EmptyObject        → cursor++; obj[key] = new JsonObject() or parseObjectBody(...)
        Blank/Comment      → cursor++
return obj
```

### 11.3 `parseTabularRows(int rowIndent, int count, ColumnDef[] columns)`

```
arr = new JsonArray()
rowsParsed = 0
while cursor < lines.Length && getIndent(cursor) >= rowIndent && rowsParsed < count:
    rawLine = lines[cursor].Substring(rowIndent)
    fields = splitDelimited(rawLine)
    obj = new JsonObject() populated from fields + columns
    arr.Add(obj)
    rowsParsed++; cursor++
// Strict: validate rowsParsed == count
return arr
```

### 11.4 `parseMixedArrayBody(int bodyIndent, int count)`

Pre-size array with nulls, then fill by index. The `[N]` key on each line determines position. Supports nested objects, primitive sub-arrays, and mixed sub-arrays as element values.

### 11.5 `parsePrimitiveArrayInline(string rawValues, int count)`

```
fields = splitDelimited(rawValues)
arr = new JsonArray()
for each field: arr.Add(parseValue(field.Trim(), null))
// Strict: validate arr.Count == count
return arr
```

---

## 12. Header Parsing

### `parseHeaders(TonlDecodeOptions opts)`

```
_delimiter = opts.Delimiter

while cursor < lines.Length:
    line = lines[cursor].Trim()
    if starts with "#version"   → extract version, cursor++
    if starts with "#delimiter" → parse delimiter string, set _delimiter, cursor++
    if starts with "#" or "@"   → unknown directive, skip, cursor++
    else                        → break (first non-header line)
```

Delimiter string mapping:
- `","` → `ColumnDelimiter.Comma`
- `"|"` → `ColumnDelimiter.Pipe`
- `"\\t"` or actual tab → `ColumnDelimiter.Tab`
- `";"` → `ColumnDelimiter.Semicolon`
- Unknown → strict: throw; lenient: use option default

---

## 13. Thread Safety

All mutable parser state (`_lines`, `_cursor`, etc.) lives in a **private nested class `TonlDecoder`** inside `TonlDocument.Decode.cs`. The static `Decode` method instantiates it per call -- inherently thread-safe.

```csharp
public static TonlDocument Decode(string tonl, TonlDecodeOptions? options = null)
{
    var decoder = new TonlDecoder(tonl, options ?? new TonlDecodeOptions());
    JsonNode? root = decoder.parse();
    return new TonlDocument(root);
}

private sealed class TonlDecoder(string input, TonlDecodeOptions options)
{
    private readonly string[] _lines = ...;
    private int _cursor;
    // ... all parsing methods here
}
```

---

## 14. Error Handling

### Strict Mode Errors (`TonlParseException`)

| Condition | Message |
|-----------|---------|
| Unrecognized line format | `"Unexpected line format at line {N}"` |
| Array count mismatch | `"Expected {N} elements but found {M} at line {N}"` |
| Type hint violation | `"Value '{raw}' cannot be parsed as {type} at line {N}"` |
| Inconsistent indentation | `"Unexpected indentation at line {N}: expected {X} spaces, found {Y}"` |
| Extra fields beyond column count | `"Row has {N} fields but only {M} columns at line {N}"` |
| Unterminated quoted string | `"Unterminated quoted string at line {N}"` |

### Lenient Mode Fallbacks

| Condition | Behavior |
|-----------|----------|
| Unrecognized line | Skip it |
| Array count mismatch | Use actual count found |
| Type hint violation | Fall through to default type inference |
| Inconsistent indentation | Round to nearest indent level |
| Missing `#version` | Continue with defaults |
| Extra fields | Ignore extras |
| Unterminated quote | Treat rest of line as unquoted string |

---

## 15. Test Strategy

### 15.1 `TonlDecodeTester.cs` -- Unit Tests

**Header Parsing:**
- `Decode_VersionHeader_Parsed`
- `Decode_DelimiterHeader_Pipe`
- `Decode_DelimiterHeader_Tab`
- `Decode_DelimiterHeader_Semicolon`
- `Decode_NoDelimiterHeader_DefaultComma`

**Primitive Values:**
- `Decode_StringValue_String`
- `Decode_QuotedStringValue_String`
- `Decode_TripleQuotedStringValue_String`
- `Decode_IntegerValue_Int`
- `Decode_FloatValue_Double`
- `Decode_BoolTrue_Bool`
- `Decode_BoolFalse_Bool`
- `Decode_NullValue_Null`
- `Decode_Infinity_Double`
- `Decode_NegativeInfinity_Double`
- `Decode_NaN_Double`

**Reserved Words as Strings:**
- `Decode_QuotedTrue_String`
- `Decode_QuotedNull_String`
- `Decode_QuotedNumber_String`

**Objects:**
- `Decode_SimpleObject_JsonObject`
- `Decode_NestedObject_JsonObject`
- `Decode_EmptyObject_EmptyJsonObject`
- `Decode_ObjectWithColumnHeaders_JsonObject`

**Arrays:**
- `Decode_PrimitiveArrayInline_JsonArray`
- `Decode_EmptyArray_EmptyJsonArray`
- `Decode_TabularArray_JsonArray`
- `Decode_MixedArray_JsonArray`
- `Decode_ArrayOfArrays_JsonArray`
- `Decode_ArrayWithNulls_JsonArray`

**Tabular/Semi-Uniform:**
- `Decode_TabularWithEmptyCells_NullProperties`
- `Decode_TabularWithQuotedFields_Unquoted`

**Type Hints:**
- `Decode_TypeHintU32_Uint`
- `Decode_TypeHintStr_String`
- `Decode_TypeHintBool_Boolean`
- `Decode_TypeHintF64_Double`

**Edge Cases:**
- `Decode_EmptyString_QuotedEmpty`
- `Decode_LeadingTrailingSpaces_Preserved`
- `Decode_UnicodeAndEmoji_Preserved`
- `Decode_BackslashEscapes_Unescaped`
- `Decode_DeepNesting_Reconstructed`

**Strict Mode:**
- `Decode_StrictMode_InvalidLine_Throws`
- `Decode_StrictMode_ArrayCountMismatch_Throws`
- `Decode_StrictMode_TypeHintViolation_Throws`

### 15.2 `TonlRoundTripTester.cs` -- Round-Trip Tests

One test per transformation example, in both directions (encode→decode and decode→encode):

- `RoundTrip_Example1_1` through `RoundTrip_Example6_4` (existing examples)
- `RoundTrip_Example7_1_UserDatabase`
- `RoundTrip_Example7_2_ApiResponse`
- `RoundTrip_Example8_1_TabDelimiter`
- `RoundTrip_Example9_1_TypeHints`

Include a private `assertJsonEqual(JsonNode? expected, JsonNode? actual)` helper that recursively compares `JsonObject` (key-by-key), `JsonArray` (element-by-element), and `JsonValue` (by extracted value).

---

## 16. Implementation Order

| Step | What to Build | Checkpoint |
|------|--------------|------------|
| 1 | Scaffold files, `NotImplementedException` stub | Project compiles |
| 2 | `parseHeaders`, delimiter detection | Delimiter header tests pass |
| 3 | `LineType`, `LineInfo`, `ColumnDef`, `classifyLine`, `parseColumnDefs` | Classification unit tests pass |
| 4 | `parseValue`, `unquote` | Primitive value tests pass |
| 5 | `splitDelimited` state machine | Field splitting edge case tests pass |
| 6 | `parseRoot`, `parseObjectBody` | Round-trips for Examples 1.1, 2.1, 2.2, 4.1 |
| 7 | `parsePrimitiveArrayInline` | Round-trips for Examples 3.1, 3.4, 3.5 |
| 8 | `parseTabularRows` | Round-trips for Examples 3.2, 7.1, 7.2 |
| 9 | `parseMixedArrayBody` | Round-trips for Examples 3.3, 4.2, 4.3 |
| 10 | Non-default delimiters | Round-trips for Examples 5.1, 8.1 |
| 11 | Type hint enforcement | Round-trip for Example 9.1 |
| 12 | Strict mode errors, lenient fallbacks | Strict mode tests pass |
| 13 | Edge cases (quoted keys, empty objects, `\r\n`, no `#version`) | All existing tests still pass |
| 14 | `TonlRoundTripTester.cs` complete | Full round-trip suite passes |

---

## 17. Files to Modify

| File Path | Action | Reason |
|-----------|--------|--------|
| `src/Tonl.Net/TonlDecodeOptions.cs` | Create | Decode options record |
| `src/Tonl.Net/TonlParseException.cs` | Create | Custom parse exception |
| `src/Tonl.Net/TonlDocument.Decode.cs` | Create | Static method + nested `TonlDecoder` class |
| `tests/Tonl.Net.Tests/TonlDecodeTester.cs` | Create | Focused decoder unit tests |
| `tests/Tonl.Net.Tests/TonlRoundTripTester.cs` | Create | Bidirectional round-trip tests |
| `src/Tonl.Net/ColumnDelimiter.cs` | Possibly extend | May need a `FromString` / `Parse` helper for header parsing |

---

## 18. Open Questions

1. **Single-line objects** (encoder has this commented out): Should the decoder support parsing this format defensively? Recommendation: yes -- future-proofs the decoder.

2. **Root primitives** (`root: 42`): Should the decoder handle a bare primitive root symmetrically with the encoder? Recommendation: yes.

3. **Duplicate keys**: Strict mode → throw `TonlParseException`. Lenient mode → last-wins via indexer assignment.

4. **Empty root object**: `root:` with no children must return an empty `JsonObject`, not null.

---

## 19. Success Criteria

1. `TonlDocument.Decode(tonl)` correctly parses all output produced by the existing encoder.
2. All 28+ round-trip tests pass in both directions.
3. All decoder-specific unit tests pass.
4. Strict mode throws `TonlParseException` for all documented error conditions.
5. Lenient mode handles malformed input without crashing.
6. Project compiles with zero warnings (`TreatWarningsAsErrors` is on).
7. All public API members have XML documentation.
