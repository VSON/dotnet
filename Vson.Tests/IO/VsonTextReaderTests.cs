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
		public void ParseNumbers()
		{
			VsonTextReader reader;

			reader = new VsonTextReader("1.1");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1.1"));

			reader = new VsonTextReader("-1.1");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("-1.1"));

			reader = new VsonTextReader("0.0");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("0.0"));

			reader = new VsonTextReader("-0.0");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("-0.0"));

			reader = new VsonTextReader("1E-06");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1E-06"));

			reader = new VsonTextReader("NaN");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("NaN"));

			reader = new VsonTextReader("Infinity");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("Infinity"));

			reader = new VsonTextReader("-Infinity");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("-Infinity"));

			reader = new VsonTextReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd");
			AssertNextTokenThrows(reader, "Invalid token '9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd' at char 0, line 1, column 1");

			reader = new VsonTextReader("-");
			AssertNextTokenThrows(reader, "Invalid token '-' at char 0, line 1, column 1");
		}

		[Test]
		[TestCase("\"Somebody's Stuff\"", "Somebody's Stuff")]
		[TestCase("\"A surrogate pair: \uD835\uDEE2\"", "A surrogate pair: \uD835\uDEE2")]
		public void ParseStrings(string vson, string expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString(expected));
		}
	}
}
