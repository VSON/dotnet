using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Vson.Model
{
	public class VsonNumber : VsonValue
	{
		private static readonly char[] ExponentCharacters = { 'e', 'E' };
		private static readonly char[] NonZeroDigits = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		public static readonly VsonNumber NaN = new VsonNumber("NaN");
		public static readonly VsonNumber PositiveInfinity = new VsonNumber("Infinity");
		public static readonly VsonNumber NegativeInfinity = new VsonNumber("-Infinity");

		private readonly string value;
		private string normalizedValue;

		internal VsonNumber(string value)
		{
			this.value = value;
		}

		public VsonNumber(double value)
			: this(value.ToString(CultureInfo.InvariantCulture))
		{
		}

		public double? AsDouble()
		{
			double doubleValue;
			if(!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) return null;
			if(doubleValue == 0 && value[0] == '-')
				doubleValue = -0.0; // must be double literal or negative zero doesn't come through
			return doubleValue;
		}

		public decimal? AsDecimal()
		{
			decimal decimalValue;
			if(!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimalValue)) return null;
			if(decimalValue == 0 && value[0] == '-')
				decimalValue = -0.0m; // must be double literal or negative zero doesn't come through
			return decimalValue;
		}

		public int? AsInt32()
		{
			int intValue;
			if(!int.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out intValue)) return null;
			return intValue;
		}

		public long? AsInt64()
		{
			long longValue;
			if(!long.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out longValue)) return null;
			return longValue;
		}

		public override bool Equals(object obj)
		{
			var other = obj as VsonNumber;
			return other != null && ToNormalizedString().Equals(other.ToNormalizedString(), StringComparison.InvariantCulture);
		}

		public override int GetHashCode()
		{
			return (normalizedValue ?? (normalizedValue = NormalizeValue())).GetHashCode();
		}

		private string NormalizeValue()
		{
			if(value == "NaN" || value == "Infinity" || value == "-Infinity")
				return value;

			var builder = new StringBuilder(value.Length);

			// Find the numeric portion
			var numberStart = 0;
			if(value[0] == '-')
			{
				builder.Append('-');
				numberStart = 1;
			}
			var numberLength = value.Length - numberStart;

			// Adjust for and get any exponent
			var exponent = BigInteger.Zero;
			var exponentStart = value.IndexOfAny(ExponentCharacters);
			if(exponentStart != -1)
			{
				numberLength = exponentStart - numberStart;
				exponent = BigInteger.Parse(value.Substring(exponentStart + 1));
			}

			// Now append the adjusted number
			var number = value.Substring(numberStart, numberLength);
			var decimalPointeIndex = number.IndexOf('.');
			if(decimalPointeIndex != -1)
				number = number.Remove(decimalPointeIndex, 1);
			else
				decimalPointeIndex = number.Length;

			var firstDigitIndex = number.IndexOfAny(NonZeroDigits);
			exponent += decimalPointeIndex - firstDigitIndex - 1;

			number = number.Trim('0');
			if(number.Length > 1)
				number = number.Insert(1, ".");
			else if(number.Length == 0)
			{
				number = "0";
				exponent = BigInteger.Zero;
			}

			builder.Append(number);

			// And append the new exponent
			if(exponent != 0)
			{
				builder.Append('E');
				builder.Append(exponent);
			}

			return builder.ToString();
		}

		public override string ToString()
		{
			return value;
		}

		public string ToNormalizedString()
		{
			return normalizedValue ?? (normalizedValue = NormalizeValue());
		}
	}
}
