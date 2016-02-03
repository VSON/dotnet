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
		public void ParseNumbers()
		{
			VsonTextReader reader;
			VsonToken token;

			reader = new VsonTextReader("1.1");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber("1.1"), token.Value);

			reader = new VsonTextReader("-1.1");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber("-1.1"), token.Value);

			reader = new VsonTextReader("0.0");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber("0.0"), token.Value);

			reader = new VsonTextReader("-0.0");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber("-0.0"), token.Value);

			reader = new VsonTextReader("1E-06");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber("1E-06"), token.Value);

			reader = new VsonTextReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd");
			Assert.Throws<VsonReaderException>(() => reader.NextToken());

			reader = new VsonTextReader("-");
			Assert.Throws<VsonReaderException>(() => reader.NextToken());
		}
	}
}
