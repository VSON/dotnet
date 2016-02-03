using System;
using System.Runtime.Serialization;

namespace Vson.IO
{
	[Serializable]
	public class VsonReaderException : Exception
	{
		public TextPosition Position { get; }

		public VsonReaderException(TextPosition position)
			: base(BuildMessage(position, "Unknown VSON reader exception"))
		{
			Position = position;
		}

		public VsonReaderException(TextPosition position, string message)
			: base(BuildMessage(position, message))
		{
			Position = position;
		}

		public VsonReaderException(TextPosition position, string message, Exception innerException)
			: base(BuildMessage(position, message), innerException)
		{
			Position = position;
		}

		protected VsonReaderException(SerializationInfo info, StreamingContext context, TextPosition position)
			: base(info, context)
		{
			Position = position;
		}

		private static string BuildMessage(TextPosition position, string message)
		{
			return $"{message} at char {position.Offset}, line {position.Line + 1}, column {position.Column}";
		}

		#region Factory Methods
		internal static VsonReaderException UnexpectedEndOfFile(TextPosition position)
		{
			return new VsonReaderException(position, "Unexpected end of file");
		}

		internal static VsonReaderException UnexpectedCharacter(TextPosition position, char character)
		{
			return new VsonReaderException(position, $"Unexpected character '{character}' encountered");
		}

		internal static VsonReaderException InvalidToken(TextPosition position, string token)
		{
			return new VsonReaderException(position, $"Invalid token '{token}'");
		}
		#endregion
	}
}
