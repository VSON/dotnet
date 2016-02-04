using NUnit.Framework;
using Vson.IO;
using Vson.Model;

namespace Vson.Tests.IO
{
	[TestFixture]
	public class VsonTextReaderTests
	{
		private static void AssertTokenIs(VsonToken? token, VsonTokenType expectedType, VsonValue expectedValue = null)
		{
			Assert.IsNotNull(token, "Token");
			Assert.AreEqual(expectedType, token.Value.Type);
			Assert.AreEqual(expectedValue, token.Value.Value);
		}

		private static void AssertIsEOF(VsonToken? token)
		{
			Assert.IsNull(token, "Expected EOF");
		}

		private static void AssertNextTokenThrows(VsonTextReader reader, string message)
		{
			var ex = Assert.Throws<VsonReaderException>(() => reader.NextToken());
			Assert.AreEqual(message, ex.Message);
		}

		[Test]
		public void ParseEmpty()
		{
			var reader = new VsonTextReader("");
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		[TestCase("1.1", "1.1")]
		[TestCase("-1.1", "-1.1")]
		[TestCase("0.0", "0.0")]
		[TestCase("-0.0", "-0.0")]
		[TestCase("1E-06", "1E-06")]
		[TestCase("1e-06", "1E-06")]
		[TestCase("NaN", "NaN")]
		[TestCase("Infinity", "Infinity")]
		[TestCase("-Infinity", "-Infinity")]
		public void ParseNumbers(string vson, string expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber(expected));
		}

		[Test]
		[TestCase("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd", "Invalid token '9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd' at char 0, line 1, column 1")]
		public void ParseNumbersInvalid(string vson, string expectedMessage)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, expectedMessage);
		}

		[Test]
		[TestCase("\"Somebody's Stuff\"", "Somebody's Stuff")]
		[TestCase("\"A surrogate pair: \uD835\uDEE2\"", "A surrogate pair: \uD835\uDEE2")]
		public void ParseStrings(string vson, string expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString(expected));
		}

		[Test]
		public void ParseEmptyArray()
		{
			var reader = new VsonTextReader("[]");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseArray()
		{
			var reader = new VsonTextReader("[1,2,3]");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("3"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}
	}
}
