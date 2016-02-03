using System;
using NUnit.Framework;
using Vson.IO;
using Vson.Model;

namespace Vson.Tests.IO
{
	[TestFixture]
	public class VsonTextReaderTests
	{
		[Test]
		public void ParseEmpty()
		{
			var reader = new VsonTextReader("");
			Assert.IsNull(reader.NextToken());
		}

		[Test]
		public void ParseDoubles()
		{
			VsonTextReader reader;
			VsonToken token;

			reader = new VsonTextReader("1.1");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(1.1, ((VsonNumber)token.Value).AsDouble());

			reader = new VsonTextReader("-1.1");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(-1.1, ((VsonNumber)token.Value).AsDouble());

			reader = new VsonTextReader("0.0");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(0.0, ((VsonNumber)token.Value).AsDouble());

			reader = new VsonTextReader("-0.0");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			// To be safe check for negative zero becuase I bet Assert.AreEqual() doesn't
			Assert.IsTrue(IsNegativeZero(((VsonNumber)token.Value).AsDouble().Value));

			reader = new VsonTextReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd");
			Assert.Throws<VsonReaderException>(() => reader.NextToken());

			reader = new VsonTextReader("1E-06");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber(0.000001d), token.Value);

			reader = new VsonTextReader("-");
			Assert.Throws<VsonReaderException>(() => reader.NextToken());
		}

		private static readonly long NegativeZeroBits = BitConverter.DoubleToInt64Bits(-0.0);

		public static bool IsNegativeZero(double x)
		{
			return BitConverter.DoubleToInt64Bits(x) == NegativeZeroBits;
		}
	}
}
