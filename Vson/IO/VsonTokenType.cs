namespace Vson.IO
{
	public enum VsonTokenType : byte
	{
		None = 0,
		String,
		Number,
		Date,
		DateTime,
		StartObject,
		PropertyName,
		EndObject,
		StartArray,
		EndArray,
		Bool,
		Null,
		WhiteSpace,
		LineComment,
		BlockComment,
		NewLine,
		Comma,
		Colon,
	}
}
