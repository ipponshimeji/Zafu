using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Tasks {
	public class NullRunningTaskMonitor: IRunningTaskMonitor {
		#region data

		public static readonly NullRunningTaskMonitor Instance = new NullRunningTaskMonitor();

		#endregion


		#region creation & disposable

		private NullRunningTaskMonitor() {
		}

		#endregion


		#region IRunningTaskMonitor

		public IRunningTask MonitorTask(Action<CancellationToken> action) {
			throw new NotImplementedException();
		}

		public IRunningTask MonitorTask(Action action) {
			throw new NotImplementedException();
		}

		public IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			throw new NotImplementedException();
		}

		public IRunningTask? MonitorTask(ValueTask task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			throw new NotImplementedException();
		}

		#endregion
	}
}
