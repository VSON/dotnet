using System;
using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonNumberTests
	{
		[Test]
		public void UppercaseE()
		{
			var value = new VsonNumber("1e-6");
			Assert.AreEqual("1E-6", value.ToString());
		}

		[Test]
		public void Constants()
		{
			Assert.AreEqual("NaN", VsonNumber.NaN.ToString());
			Assert.AreEqual("Infinity", VsonNumber.PositiveInfinity.ToString());
			Assert.AreEqual("-Infinity", VsonNumber.NegativeInfinity.ToString());
		}

		[Test]
		[TestCase("1.1", 1.1)]
		[TestCase("-1.1", -1.1)]
		[TestCase("0.0", 0.0)]
		[TestCase("0", 0.0)]
		[TestCase("1e-06", 0.000001)]
		[TestCase("9e99999999999999", null)]
		[TestCase("NaN", double.NaN)]
		[TestCase("-Infinity", double.NegativeInfinity)]
		[TestCase("Infinity", double.PositiveInfinity)]
		public void AsDouble(string vsonNumber, double? value)
		{
			var number = new VsonNumber(vsonNumber);
			Assert.AreEqual(value, number.AsDouble());
		}

		[Test]
		[TestCase("1.1", 1.1)]
		[TestCase("-1.1", -1.1)]
		[TestCase("0.0", 0.0)]
		[TestCase("0", 0.0)]
		[TestCase("1e-06", 0.000001)]
		[TestCase("9e99999999999999", null)]
		[TestCase("NaN", null)]
		[TestCase("-Infinity", null)]
		[TestCase("Infinity", null)]
		public void AsDecimal(string vsonNumber, double? doubleValue)
		{
			var value = (decimal?)doubleValue;
			var number = new VsonNumber(vsonNumber);
			Assert.AreEqual(value, number.AsDecimal());
		}

		// TODO negative zero for decimal

		[Test]
		[TestCase("1.1", null)]
		[TestCase("-1.1", null)]
		[TestCase("0.0", 0)]
		[TestCase("0", 0)]
		[TestCase("1e-06", null)]
		[TestCase("9e99999999999999", null)]
		[TestCase("NaN", null)]
		[TestCase("-Infinity", null)]
		[TestCase("Infinity", null)]
		public void AsInt32(string vsonNumber, int? value)
		{
			var number = new VsonNumber(vsonNumber);
			Assert.AreEqual(value, number.AsInt32());
		}

		[Test]
		[TestCase("1.1", null)]
		[TestCase("-1.1", null)]
		[TestCase("0.0", 0l)]
		[TestCase("0", 0l)]
		[TestCase("1e-06", null)]
		[TestCase("9e99999999999999", null)]
		[TestCase("NaN", null)]
		[TestCase("-Infinity", null)]
		[TestCase("Infinity", null)]
		public void AsInt64(string vsonNumber, long? value)
		{
			var number = new VsonNumber(vsonNumber);
			Assert.AreEqual(value, number.AsInt64());
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
