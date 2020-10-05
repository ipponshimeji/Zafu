using System;
using System.Diagnostics;
using System.Threading;

namespace Zafu.Testing.Tasks {
	public class TestingCancellationTokenSource: CancellationTokenSource {
		#region data

		private int disposeCount = 0;

		#endregion


		#region properties

		public int DisposeCount => this.disposeCount;

		#endregion


		#region creation & disposal

		public TestingCancellationTokenSource() : base() {
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Interlocked.Increment(ref this.disposeCount);
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}
