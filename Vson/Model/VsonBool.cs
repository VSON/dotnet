namespace Vson.Model
{
	public class VsonBool : VsonValue
	{
		public static readonly VsonBool True = new VsonBool(true);
		public static readonly VsonBool False = new VsonBool(false);

		private readonly bool value;

		private VsonBool(bool value)
		{
			this.value = value;
		}

		public static implicit operator VsonBool(bool value)
		{
			return value ? True : False;
		}

		public static implicit operator bool(VsonBool value)
		{
			return value.value;
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as VsonBool;
			return other != null && other.value == value;
		}

		public override string ToString()
		{
			return value.ToString().ToLowerInvariant();
		}
	}
}
