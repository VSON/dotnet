using NUnit.Framework;
using Vson.IO;
using Vson.Model;

namespace Vson.Tests.IO
{
	[TestFixture]
	public class VsonTextReaderTests
	{
		[Test]
		public void ParseDoubles()
		{
			VsonTextReader reader;
			VsonToken token;

			reader = new VsonTextReader("1.1");
			token = reader.NextToken().Value;
			Assert.AreEqual(VsonTokenType.Number, token.Type);
			Assert.AreEqual(new VsonNumber(1.1), token.Value);
		}
	}
}
