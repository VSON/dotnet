using System;
using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonDateTests
	{
		private readonly short Utc = 0;
		private readonly short FiveAndHalf = 530;
		private readonly short NegFiveAndHalf = -530;

		[Test]
		[TestCase(1, 1, 1, "0001-01-01")]
		[TestCase(2016, 2, 10, "2016-02-10")]
		[TestCase(987518, 1, 1, "987518-01-01")]
		[TestCase(0, 12, 10, "0000-12-10")]
		[TestCase(-1, 1, 1, "-0001-01-01")]
		[TestCase(-4568791, 1, 1, "-4568791-01-01")]
		public void ToStringValue(long year, byte month, byte day, string expected)
		{
			Assert.AreEqual(expected, new VsonDate(year, month, day, null).ToString());
			Assert.AreEqual(expected + "Z", new VsonDate(year, month, day, Utc).ToString());
			Assert.AreEqual(expected + "+05:30", new VsonDate(year, month, day, FiveAndHalf).ToString());
			Assert.AreEqual(expected + "-05:30", new VsonDate(year, month, day, NegFiveAndHalf).ToString());
		}

		[Test]
		[TestCase(2015, false)]
		[TestCase(2016, true)]
		[TestCase(100, false)]
		[TestCase(400, true)]
		[TestCase(0, true)]
		[TestCase(-1, false)]
		[TestCase(-2015, false)]
		[TestCase(-2016, true)]
		[TestCase(-100, false)]
		[TestCase(-400, true)]
		public void IsLeapYear(long year, bool expected)
		{
			Assert.AreEqual(expected, VsonDate.IsLeapYear(year));
		}

		[Test]
		public void DaysInMonth()
		{
			for(var year = 2015; year < 2016; year++)
				for(byte month = 1; month <= 12; month++)
					Assert.AreEqual(DateTime.DaysInMonth(year, month), VsonDate.DaysInMonth(year, month), $"{year}-{month}");
		}
	}
}
