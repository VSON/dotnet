using System;
using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonNumberTests
	{
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
		[TestCase("1E-06", 0.000001)]
		[TestCase("9e99999999999999", null)]
		[TestCase("1.7976931348623157E+308", 1.7976931348623157E+308)]
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
		[TestCase("1E-06", 0.000001)]
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
		[TestCase("1E-06", null)]
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
		[TestCase("0.0", 0L)]
		[TestCase("0", 0L)]
		[TestCase("1e-06", null)]
		[TestCase("1E-06", null)]
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

		[Test]
		[TestCase("1e-06", "1E-06")]
		[TestCase("1e-06", "1e-6")]
		[TestCase("1e-06", "0.000001")]
		[TestCase("1.0e1", "10")]
		[TestCase("1e1", "10")]
		[TestCase("1.0e5", "10e4")]
		public void Equality(string value1, string value2)
		{
			var number1 = new VsonNumber(value1);
			var number2 = new VsonNumber(value2);
			Assert.AreEqual(number1.GetHashCode(), number2.GetHashCode(), $"Hash Codes of '{value1}' and '{value2}'");
			Assert.AreEqual(number1, number2);
		}

		[Test]
		[TestCase("1e-06", "1E-6")]
		[TestCase("1e-6", "1E-6")]
		[TestCase("0.000001", "1E-6")]
		[TestCase("1.0e1", "1E1")]
		[TestCase("10", "1E1")]
		[TestCase("10e4", "1E5")]
		[TestCase("11e4", "1.1E5")]
		public void ToNormalizedString(string number, string normalized)
		{
			Assert.AreEqual(normalized, new VsonNumber(number).ToNormalizedString());
		}
	}
}
