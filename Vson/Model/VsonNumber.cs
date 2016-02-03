using System;
using System.Globalization;

namespace Vson.Model
{
	public class VsonNumber : VsonValue
	{
		public static readonly VsonNumber NaN = new VsonNumber("NaN");
		public static readonly VsonNumber PositiveInfinity = new VsonNumber("Infinity");
		public static readonly VsonNumber NegativeInfinity = new VsonNumber("-Infinity");

		private readonly string value;

		internal VsonNumber(string value)
		{
			this.value = value.Contains("e") ? value.ToUpperInvariant() : value;
		}

		public VsonNumber(double value)
			: this(value.ToString(CultureInfo.InvariantCulture))
		{
		}

		public double? AsDouble()
		{
			double doubleValue;
			if(!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue)) return null;
			if(doubleValue == 0 && value[0] == '-')
				doubleValue = -0.0; // must be double literal or negative zero doesn't come through
			return doubleValue;
		}

		// TODO this doesn't account for numbers that are equal but in different form
		public override bool Equals(object obj)
		{
			var other = obj as VsonNumber;
			return other != null && value.Equals(other.value, StringComparison.InvariantCultureIgnoreCase);
		}

		// TODO this doesn't account for numbers that are equal but in different form
		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return value;
		}
	}
}
