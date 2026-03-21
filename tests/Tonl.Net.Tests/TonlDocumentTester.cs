using Subject = Tonl.Net.TonlDocument;

namespace Tonl.Net.Tests;

[TestFixture]
public class TonlDocumentTester
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void Success()
    {
	    Assert.Pass();
    }
    
    [Test, Explicit]
    public void Failure()
    {
	    Assert.Fail("this test always fails");
    }
}
