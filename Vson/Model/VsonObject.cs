using System;
using System.Collections.Generic;

namespace Vson.Model
{
	public class VsonObject : VsonValue
	{
		private readonly Dictionary<string, VsonValue> values = new Dictionary<string, VsonValue>();

		public void Add(string propertyName, VsonValue value)
		{
			if(value == null) throw new ArgumentNullException(nameof(value));
			values.Add(propertyName, value);
		}
	}
}
