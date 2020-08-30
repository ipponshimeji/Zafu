using System;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTaskMonitor: IDisposable, IRunningTaskMonitor {
		#region data

		private readonly object instanceLocker = new object();

		private bool disposed = false;

		#endregion


		#region creation & disposable

		public RunningTaskMonitor(IRunningContext context) {
		}

		public void Dispose() {
			Dispose(IRunningTaskMonitor.DefaultDisposeWaitingTimeout, IRunningTaskMonitor.DefaultDisposeCancelingTimeout);
		}

		public bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// check state
			lock (this.instanceLocker) {
				if (this.disposed) {
					return true;
				} else {
					this.disposed = true;
				}
			}

			return DisposeImpl(waitingTimeout, cancelingTimeOut);
		}

		protected virtual bool DisposeImpl(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			throw new NotImplementedException();
		}

		#endregion


		#region IRunningTaskMonitor
		#endregion
	}
}
