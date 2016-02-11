using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonDateTimeTests
	{
		private readonly short Utc = 0;
		private readonly short FiveAndHalf = 530;
		private readonly short NegFiveAndHalf = -530;

		[Test]
		[TestCase(1, 0, 0, "", "01:00:00")]
		[TestCase(0, 0, 0, "", "00:00:00")]
		[TestCase(24, 0, 0, "", "24:00:00")]
		[TestCase(7, 2, 2, "03221648746105458403", "07:02:02.03221648746105458403")]
		public void ToStringValue(byte hours, byte min, byte sec, string frac, string expected)
		{
			Assert.AreEqual("2016-02-10T" + expected, new VsonDateTime(2016, 2, 10, hours, min, sec, frac, null).ToString());
			Assert.AreEqual("2016-02-10T" + expected + "Z", new VsonDateTime(2016, 2, 10, hours, min, sec, frac, Utc).ToString());
			Assert.AreEqual("2016-02-10T" + expected + "+05:30", new VsonDateTime(2016, 2, 10, hours, min, sec, frac, FiveAndHalf).ToString());
			Assert.AreEqual("2016-02-10T" + expected + "-05:30", new VsonDateTime(2016, 2, 10, hours, min, sec, frac, NegFiveAndHalf).ToString());
		}
	}
}
