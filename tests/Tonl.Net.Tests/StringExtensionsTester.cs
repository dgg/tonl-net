namespace Tonl.Net.Tests;

[TestFixture]
public class StringExtensionsTester
{
	#region QuoteIfNeeded
	
	#region quoting triggers (via needsQuoting / quote)

	[Test]
	public void QuoteIfNeeded_EmptyString_Quoted() =>
		Assert.That("".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"\""));

	[Test]
	public void QuoteIfNeeded_TrueLiteral_Quoted() =>
		Assert.That("true".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"true\""));

	[Test]
	public void QuoteIfNeeded_FalseLiteral_Quoted() =>
		Assert.That("false".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"false\""));

	[Test]
	public void QuoteIfNeeded_NullLiteral_Quoted() =>
		Assert.That("null".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"null\""));

	[Test]
	public void QuoteIfNeeded_IntegerLike_Quoted() =>
		Assert.That("123".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"123\""));

	[Test]
	public void QuoteIfNeeded_DecimalLike_Quoted() =>
		Assert.That("3.14".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"3.14\""));

	[Test]
	public void QuoteIfNeeded_ScientificNotation_Quoted() =>
		Assert.That("1e10".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"1e10\""));

	[Test]
	public void QuoteIfNeeded_InfinityLiteral_Quoted() =>
		Assert.That("Infinity".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"Infinity\""));

	[Test]
	public void QuoteIfNeeded_ContainsDelimiter_Quoted() =>
		Assert.That("a,b".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"a,b\""));

	[Test]
	public void QuoteIfNeeded_ContainsColon_Quoted() =>
		Assert.That("key:value".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"key:value\""));

	[Test]
	public void QuoteIfNeeded_ContainsBrace_Quoted() =>
		Assert.That("a{b}".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"a{b}\""));

	[Test]
	public void QuoteIfNeeded_ContainsHash_Quoted() =>
		Assert.That("#comment".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"#comment\""));

	[Test]
	public void QuoteIfNeeded_ContainsDoubleQuote_Quoted() =>
		Assert.That("say \"hello\"".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"say \\\"hello\\\"\""));

	[Test]
	public void QuoteIfNeeded_ContainsBackslash_Quoted() =>
		Assert.That(@"a\b".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"a\\\\b\""));

	[Test]
	public void QuoteIfNeeded_LeadingSpace_Quoted() =>
		Assert.That(" leading".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\" leading\""));

	[Test]
	public void QuoteIfNeeded_TrailingSpace_Quoted() =>
		Assert.That("trailing ".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"trailing \""));

	[Test]
	public void QuoteIfNeeded_PlainString_Unquoted() =>
		Assert.That("hello".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("hello"));

	[Test]
	public void QuoteIfNeeded_PipeWithCommaDelimiter_Unquoted() =>
		Assert.That("a|b".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("a|b"));

	#endregion
	
	#region quote path

	[Test]
	public void QuoteIfNeeded_StringWithNewline_Quoted() =>
		Assert.That("line1\nline2".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo("\"\"\"line1\\nline2\"\"\""));

	[Test]
	public void QuoteIfNeeded_EmbeddedQuote_QuotedAndEscaped() =>
		Assert.That("before \"\"\" after".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo(@"""""""before \"""""" after"""""""));
	
	#endregion

	#region escape sequences in single-quoted path

	[Test]
	public void QuoteIfNeeded_Tab_Quoted() =>
		Assert.That("\t".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo(@"""\t"""));

	[Test]
	public void QuoteIfNeeded_CarriageReturn_Quoted() =>
		Assert.That("\r".QuoteIfNeeded(ColumnDelimiter.Comma), Is.EqualTo(@"""\r"""));
	
	#endregion

	#region delimiter sensitivity

	[Test]
	public void QuoteIfNeeded_PipeWithPipeDelimiter_Quoted() =>
		Assert.That("a|b".QuoteIfNeeded(ColumnDelimiter.Pipe), Is.EqualTo(@"""a|b"""));

	[Test]
	public void QuoteIfNeeded_SemicolonWithSemicolonDelimiter_Quoted() =>
		Assert.That("a;b".QuoteIfNeeded(ColumnDelimiter.Semicolon), Is.EqualTo("\"a;b\""));

	#endregion
	
	#endregion
	
	#region QuoteKeyIfNeeded

	[Test]
	public void QuoteKeyIfNeeded_PlainKey_Unchanged() =>
		Assert.That("name".QuoteKeyIfNeeded(), Is.EqualTo("name"));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithColon_Quoted() =>
		Assert.That("my:key".QuoteKeyIfNeeded(), Is.EqualTo("\"my:key\""));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithComma_Quoted() =>
		Assert.That("a,b".QuoteKeyIfNeeded(), Is.EqualTo("\"a,b\""));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithHash_Quoted() =>
		Assert.That("#key".QuoteKeyIfNeeded(), Is.EqualTo("\"#key\""));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithAt_Quoted() =>
		Assert.That("@key".QuoteKeyIfNeeded(), Is.EqualTo("\"@key\""));

	[Test]
	public void QuoteKeyIfNeeded_EmptyKey_Quoted() =>
		Assert.That("".QuoteKeyIfNeeded(), Is.EqualTo("\"\""));

	[Test]
	public void QuoteKeyIfNeeded_WhitespaceOnlyKey_Quoted() =>
		Assert.That("  ".QuoteKeyIfNeeded(), Is.EqualTo("\"  \""));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithLeadingSpace_Quoted() =>
		Assert.That(" key".QuoteKeyIfNeeded(), Is.EqualTo("\" key\""));

	[Test]
	public void QuoteKeyIfNeeded_KeyWithEmbeddedQuote_Escaped() =>
		Assert.That("say \"hi\"".QuoteKeyIfNeeded(), Is.EqualTo("\"say \\\"hi\\\"\""));
	
	#endregion
}
