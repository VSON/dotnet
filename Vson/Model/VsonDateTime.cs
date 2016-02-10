using System.Numerics;

namespace Vson.Model
{
	public class VsonDateTime : VsonDate
	{
		private readonly byte hours;
		private readonly byte minutes;
		private readonly decimal seconds;

		internal VsonDateTime(BigInteger year, byte month, byte day)
			: base(year, month, day)
		{
		}
	}
}
