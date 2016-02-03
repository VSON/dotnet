namespace Vson.IO
{
	/// <summary>
	/// Represents the position of a character or token in a text reader.  All values are zero based
	/// and count char values.  So surragate pairs count as two characters and each combining mark etc
	/// counts as a character.
	/// </summary>
	public struct TextPosition
	{
		public readonly long Offset;
		public readonly int Line; // 0 based
		public readonly int Column; // 0 based

		public TextPosition(long offset, int line, int column)
		{
			Offset = offset;
			Line = line;
			Column = column;
		}

		/// <summary>
		/// Returns a new position advanced by the given number of characters without
		/// starting a new line
		/// </summary>
		/// <returns></returns>
		public TextPosition Advance(int chars = 1)
		{
			return new TextPosition(Offset + chars, Line, Column + chars);
		}

		/// <summary>
		/// Advances to the next line.  For mult-char newline sequences, pass the number
		/// of chars in the newline sequence
		/// </summary>
		public TextPosition NewLine(int chars = 1)
		{
			return new TextPosition(Offset + chars, Line + 1, 0);
		}
	}
}
