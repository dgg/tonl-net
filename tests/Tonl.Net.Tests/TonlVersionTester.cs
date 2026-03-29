using Subject = Tonl.Net.TonlVersion;

namespace Tonl.Net.Tests;

[TestFixture]
public class TonlVersionTester
{
	#region construction

	[Test]
	public void Ctor_MajorMinor_PropsSet()
	{
		var subject = new Subject(1, 2);

		Assert.That(subject.Major, Is.EqualTo(1));
		Assert.That(subject.Minor, Is.EqualTo(2));
	}
	
	[Test]
	public void Ctor_Major_ZeroMinor()
	{
		var subject = new Subject(7);

		Assert.That(subject.Major, Is.EqualTo(7));
		Assert.That(subject.Minor, Is.Zero);
	}

	[Test]
	public void Default_OnePointOh()
	{
		var onePointOh = new TonlVersion(1);
		Assert.That(Subject.Default, Is.EqualTo(onePointOh));
	}

	[Test]
	public void FromVersion_AllComponents_OnlyMajorMinor()
	{
		var version = new Version(1, 2, 3, 4);
		
		Assert.That(Subject.FromVersion(version), Is.EqualTo(new Subject(1, 2)));
	}

	#endregion
	
	#region representation

	[Test]
	public void ToString_MajorDotMinor()
	{	
		var subject = new Subject(7, 8);
		Assert.That(subject.ToString(), Is.EqualTo("7.8"));
	}
	
	[Test]
	public void ToTonl_HeaderRepresentation()
	{	
		var subject = new Subject(7, 8);
		string tonl = "#version 7.8";
		Assert.That(subject.ToTonl(), Is.EqualTo(tonl));
	}
	
	#endregion
}