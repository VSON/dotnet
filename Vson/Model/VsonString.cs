namespace Vson.Model
{
	public class VsonString : VsonValue
	{
		private readonly string value;

		public VsonString(string value)
		{
			this.value = value;
		}
	}
}
