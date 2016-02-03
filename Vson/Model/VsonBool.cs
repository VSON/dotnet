namespace Vson.Model
{
	public class VsonBool : VsonValue
	{
		public static readonly VsonBool True = new VsonBool(true);
		public static readonly VsonBool False = new VsonBool(false);

		public readonly bool Value;

		private VsonBool(bool value)
		{
			Value = value;
		}
	}
}
