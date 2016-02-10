using System;
using System.Numerics;

namespace Vson.Model
{
	public class VsonDate : VsonValue
	{

		private static readonly byte[] DaysInMonths = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

		public readonly BigInteger Year;
		public readonly byte Month;
		public readonly byte Day;
		public readonly short? TimeZoneOffset;

		internal VsonDate(BigInteger year, byte month, byte day, short? timeZoneOffset = null)
		{
			Year = year;
			Month = month;
			Day = day;
			TimeZoneOffset = timeZoneOffset;
		}

		public override bool Equals(object obj)
		{
			var other = obj as VsonDate;
			return other != null && Year.Equals(other.Year) && Month == other.Month && Day == other.Day && TimeZoneOffset == other.TimeZoneOffset;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Year.GetHashCode();
				hashCode = (hashCode * 397) ^ Month.GetHashCode();
				hashCode = (hashCode * 397) ^ Day.GetHashCode();
				hashCode = (hashCode * 397) ^ TimeZoneOffset.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			if(TimeZoneOffset == null)
				return $"{Year:0000}-{Month:00}-{Day:00}";
			if(TimeZoneOffset == 0)
				return $"{Year:0000}-{Month:00}-{Day:00}Z";
			return $"{Year:0000}-{Month:00}-{Day:00}{TimeZoneOffset:+00:00;-00:00}";
		}

		internal static bool IsLeapYear(BigInteger year)
		{
			if(year % 4 != 0) return false;
			if(year % 100 != 0) return true;
			if(year % 400 != 0) return false;
			return true;
		}

		internal static byte DaysInMonth(BigInteger year, byte month)
		{
			if(month == 2 && IsLeapYear(year))
				return 29;
			return DaysInMonths[month - 1];
		}
	}
}
