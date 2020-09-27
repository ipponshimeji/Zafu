using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Tasks {
	public interface IRunningTaskMonitor {
		IRunningTask MonitorTask(Action action);

		IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false);

		IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false);

		IRunningTask? MonitorTask(ValueTask valueTask, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check argument
			if (valueTask.IsCompletedSuccessfully) {
				// nothing to do
				return null;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, doNotDisposeCancellationTokenSource);
		}
	}
}
