using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.ObjectModel {
	public class DisposableObject: LockableObject, IDisposable {
		#region data

		private bool disposed = false;

		#endregion


		#region properties

		public bool IsDisposed {
			get {
				return this.disposed;
			}
		}

		#endregion


		#region creation

		public DisposableObject(object? instanceLocker = null, string? name = null, IRunningContext? runningContext = null): base(instanceLocker, name, runningContext) {
		}

		public void Dispose() {
			lock (this.InstanceLocker) {
				// check state
				if (this.disposed == false) {
					try {
						Dispose(disposing: true);
					} finally {
						this.disposed = true;
					}
					GC.SuppressFinalize(this);
				}
			}
		}

		/// <remarks>
		/// This method is called while the lock on the instance locker is being acquired.
		/// If Dispose operation on some resource cause deadlock,
		/// those resources should be protected by another locker.
		/// </remarks>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
		}

		#endregion


		#region methods

		protected ObjectDisposedException CreateObjectDisposedException() {
			return new ObjectDisposedException(GetName(Use.Message));
		}

		protected void EnsureNotDisposed() {
			if (this.disposed) {
				throw CreateObjectDisposedException();
			}
		}

		#endregion
	}
}
