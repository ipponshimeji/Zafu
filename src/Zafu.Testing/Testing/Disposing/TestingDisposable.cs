using System;
using System.Diagnostics;
using System.Threading;


namespace Zafu.Testing.Disposing {
	public class TestingDisposable: IDisposable {
		#region data

		private int disposeCount = 0;

		public bool ForbidMultipleDispose { get; private set; } = false;

		public Exception? ExceptionOnDispose { get; private set; } = null;

		#endregion


		#region properties

		public int DisposeCount {
			get {
				return this.disposeCount;
			}
		}

		#endregion


		#region creation & disposal

		public TestingDisposable(bool forbidMultipleDispose = false) {
			// initialize members
			Debug.Assert(this.disposeCount == 0);
			this.ForbidMultipleDispose = forbidMultipleDispose;
			Debug.Assert(this.ExceptionOnDispose == null);
		}

		public TestingDisposable(Exception? exceptionOnDispose) {
			// check argument
			// exceptionOnDispose can be null

			// initialize members
			Debug.Assert(this.disposeCount == 0);
			Debug.Assert(this.ForbidMultipleDispose == false);
			this.ExceptionOnDispose = exceptionOnDispose;
		}

		public void Dispose() {
			int count = Interlocked.Increment(ref this.disposeCount);
			if (this.ExceptionOnDispose != null) {
				throw this.ExceptionOnDispose;
			} else if (1 < count && this.ForbidMultipleDispose) {
				throw new ObjectDisposedException(null);
			}
		}

		#endregion
	}
}
