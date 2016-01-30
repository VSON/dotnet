﻿namespace Vson.IO
{
	public enum VsonTokenType
	{
		None = 0,
		String,
		Number,
		Date,
		DateTime,
		StartObject = 1,
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
	}
}
