using NUnit.Framework;
using Vson.IO;

namespace Vson.Tests
{
	[TestFixture]
	public class VsonSerializerTests
	{
		[Test]
		public void DeserializeInMiddle()
		{
			var reader = new VsonTextReader("[4]");
			reader.NextToken(); // [
			var ex = Assert.Throws<VsonSerializationException>(() => new VsonSerializer().Deserialize(reader));
			Assert.AreEqual("Unexpected token EndArray at char 2, line 1, column 3", ex.Message);
		}
	}
}
