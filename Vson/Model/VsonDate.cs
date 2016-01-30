using System;

namespace Vson.Model
{
	public class VsonDate
	{
		private readonly long year;
		private readonly byte month;
		private readonly byte day;
		private readonly TimeSpan? timeZoneOffset;
	}
}
