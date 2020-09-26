using System;
using System.Threading;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public interface IRunningTaskMonitor {
		public static readonly TimeSpan DefaultDisposeWaitingTimeout = TimeSpan.FromSeconds(2);

		public static readonly TimeSpan DefaultDisposeCancelingTimeout = TimeSpan.FromSeconds(3);

		IRunningTask MonitorTask(Action<CancellationToken> action);

		IRunningTask MonitorTask(Action action);

		IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false);

		IRunningTask? MonitorTask(ValueTask task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false);
	}
}
