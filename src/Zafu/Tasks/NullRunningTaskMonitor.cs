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

		public ITaskCanceler MonitorTask(Action<CancellationToken> action) {
			throw new NotImplementedException();
		}

		public void MonitorTask(Action action) {
			throw new NotImplementedException();
		}

		public ITaskCanceler MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			throw new NotImplementedException();
		}

		public ITaskCanceler MonitorTask(ValueTask task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			throw new NotImplementedException();
		}

		#endregion
	}
}
