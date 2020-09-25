using System;
using System.Threading;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public interface IRunningTaskMonitor {
		public static readonly TimeSpan DefaultDisposeWaitingTimeout = TimeSpan.FromSeconds(2);

		public static readonly TimeSpan DefaultDisposeCancelingTimeout = TimeSpan.FromSeconds(3);

		ITaskCanceler MonitorTask(Action<CancellationToken> action);

		void MonitorTask(Action action);

		ITaskCanceler MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false);

		ITaskCanceler MonitorTask(ValueTask task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false);
	}
}
