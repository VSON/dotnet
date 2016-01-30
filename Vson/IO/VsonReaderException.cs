using System;
using System.Runtime.Serialization;

namespace Vson.IO
{
	[Serializable]
	public class VsonReaderException : Exception
	{
		public VsonReaderException()
		{
		}

		public VsonReaderException(string message) : base(message)
		{
		}

		public VsonReaderException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected VsonReaderException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		#region Factory Methods
		internal static VsonReaderException UnexpectedEndOfFile()
		{
			return new VsonReaderException("Unexpected end of file");
		}

		internal static VsonReaderException UnexpectedCharacter(char character)
		{
			return new VsonReaderException($"Unexpected character '{character}' encountered");
		}
		#endregion

		public static VsonReaderException InvalidNumber(string value)
		{
			return new VsonReaderException($"'{value}' is not a valid VSON number");
		}
	}
}
