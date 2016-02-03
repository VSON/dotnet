namespace Vson.Model
{
	public class VsonString : VsonValue
	{
		private readonly string value;

		public VsonString(string value)
		{
			this.value = value;
		}

		public override bool Equals(object obj)
		{
			var other = obj as VsonString;
			return other != null && value == other.value;
		}

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
