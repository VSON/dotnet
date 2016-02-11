using System;
using System.Runtime.Serialization;

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
	}
}
