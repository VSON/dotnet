using System;
using System.Runtime.Serialization;

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
