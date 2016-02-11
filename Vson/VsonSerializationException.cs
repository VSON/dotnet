using System;
using System.Runtime.Serialization;
using Vson.IO;

namespace Vson
{
	[Serializable]
	public class VsonSerializationException : VsonException
	{
		public VsonSerializationException()
		{
		}

		public VsonSerializationException(string message)
			: base(message)
		{
		}

		public VsonSerializationException(TextPosition position, string message)
			: base(BuildMessage(position, message))
		{
		}

		public VsonSerializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public VsonSerializationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
