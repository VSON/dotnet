namespace Vson.Model
{
	public class VsonNumber : VsonValue
	{
		private readonly double value;

		public VsonNumber(double value)
		{
			this.value = value;
		}

		public static readonly VsonNumber NaN = new VsonNumber(double.NaN);
		public static readonly VsonNumber PositiveInfinity = new VsonNumber(double.PositiveInfinity);
		public static readonly VsonNumber NegativeInfinity = new VsonNumber(double.NegativeInfinity);
	}
}
