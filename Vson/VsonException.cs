using System;
using System.Runtime.Serialization;
using Vson.IO;

namespace Vson
{
	[Serializable]
	public abstract class VsonException : Exception
	{
		protected VsonException()
		{
		}

		protected VsonException(string message) 
			: base(message)
		{
		}

		protected VsonException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}

		protected VsonException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}

		protected static string BuildMessage(TextPosition position, string message)
		{
			return $"{message} at char {position.Offset}, line {position.Line + 1}, column {position.Column + 1}";
		}
	}
}
