using System;
using System.Collections.Generic;

namespace Vson.Model
{
	public class VsonArray : VsonValue
	{
		private readonly List<VsonValue> values = new List<VsonValue>();

		public void Add(VsonValue value)
		{
			if(value == null) throw new ArgumentNullException(nameof(value));
			values.Add(value);
		}
	}
}
