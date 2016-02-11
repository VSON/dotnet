using System.Numerics;

namespace Vson.Model
{
	public class VsonDateTime : VsonDate
	{
		public readonly byte Hours;
		public readonly byte Minutes;
		public readonly byte Seconds;
		public readonly string FractionsOfSecond;

		internal VsonDateTime(BigInteger year, byte month, byte day, byte hours, byte minutes, byte seconds, string fractionsOfSecond, short? timeZoneOffset)
			: base(year, month, day, timeZoneOffset)
		{
			Hours = hours;
			Minutes = minutes;
			Seconds = seconds;
			FractionsOfSecond = fractionsOfSecond;
		}

		public override string ToString()
		{
			return ToDateString() + ToTimeString() + ToTimeZoneOffsetString();
		}

		internal string ToTimeString()
		{
			return FractionsOfSecond == "" ? $"T{Hours:00}:{Minutes:00}:{Seconds:00}" : $"T{Hours:00}:{Minutes:00}:{Seconds:00}.{FractionsOfSecond}";
		}
	}
}
