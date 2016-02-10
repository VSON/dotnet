using NUnit.Framework;
using Vson.Model;

namespace Vson.Tests.Model
{
	[TestFixture]
	public class VsonBoolTests
	{
		[Test]
		public void ConversionToBool()
		{
			// Use assignment to test implicit conversion
			bool value = VsonBool.True;
			Assert.AreEqual(true, value);
			value = VsonBool.False;
			Assert.AreEqual(false, value);
		}

		[Test]
		public void ConversionFromBool()
		{
			// Use assignment to test implicit conversion
			VsonBool value = true;
			Assert.AreSame(VsonBool.True, value);
			value = false;
			Assert.AreSame(VsonBool.False, value);
		}
	}
}
