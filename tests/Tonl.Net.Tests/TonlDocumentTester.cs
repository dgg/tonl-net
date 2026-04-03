using System.Text.Json.Nodes;

using Iz = Tonl.Net.Tests.Support.Iz;
using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public partial class TonlDocumentTester
{
	[Test]
	public void Ctor_PrimitiveValue_SetRoot()
	{
		var primitive = JsonValue.Create(42);
		var subject = new Subject(primitive);
		
		Assert.That(subject.Root, Iz.JsonEquivalentTo(primitive));
	}
	
	[Test]
	public void Ctor_ObjectValue_SetRoot()
	{
		var obj = new JsonObject { { "prop", true } };
		
		var subject = new Subject(obj);
		
		Assert.That(subject.Root, Iz.JsonEquivalentTo(obj));
	}
	
	[Test]
	public void Ctor_ArrayValue_SetRoot()
	{
		var array = new JsonArray(1, false, "hello");
		
		var subject = new Subject(array);
		
		Assert.That(subject.Root, Iz.JsonEquivalentTo(array));
	}
	
	[Test]
	public void Ctor_ComplexValue_SetRoot()
	{
		var obj = new JsonObject { { "prop", true } };
		var complex = new JsonArray(1, false, obj);
		
		var subject = new Subject(complex);
		
		Assert.That(subject.Root, Iz.JsonEquivalentTo(complex));
	}
	
	[Test]
	public void Ctor_Null_SetNullRoot()
	{
		JsonNode? nil = null;
		
		var subject = new Subject(nil);
		
		Assert.That(subject.Root, Is.Null);
	}
}
