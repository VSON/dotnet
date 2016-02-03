using System;
using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonNumberTests
	{
		[Test]
		[TestCase("1.1", 1.1)]
		[TestCase("-1.1", -1.1)]
		[TestCase("0.0", 0.0)]
		[TestCase("0", 0.0)]
		[TestCase("1E-06", 0.000001)]
		[TestCase("9e99999999999999", null)]
		public void AsDouble(string vsonNumber, double? value)
		{
			var number = new VsonNumber(vsonNumber);
			Assert.AreEqual(value, number.AsDouble());
		}

		[Test]
		public void ZeroAsDouble()
		{
			var zero = new VsonNumber("0").AsDouble().Value;
			Assert.AreEqual(0, zero);
			Assert.IsFalse(IsNegativeZero(zero));

			var negZero = new VsonNumber("-0").AsDouble().Value;
			Assert.AreEqual(0, negZero);
			Assert.IsTrue(IsNegativeZero(negZero));
		}

		private static readonly long NegativeZeroBits = BitConverter.DoubleToInt64Bits(-0.0);

		public static bool IsNegativeZero(double x)
		{
			return BitConverter.DoubleToInt64Bits(x) == NegativeZeroBits;
		}
	}
}
