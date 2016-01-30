using System;

namespace Vson.IO
{
	public abstract class VsonReader : IDisposable
	{
		public abstract VsonToken? NextToken();

		public virtual void Close()
		{
		}

		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				Close();
			}
		}
	}
}
