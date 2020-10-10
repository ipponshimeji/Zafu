using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Disposing;

namespace Zafu.Tasks {
	public interface IRunningTaskMonitor {
		IRunningTask MonitorTask(Action action);

		IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false);

		IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false);

		IRunningTask? MonitorTask(ValueTask valueTask, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check arguments
			if (valueTask.IsCompleted) {
				// nothing to do
				if (doNotDisposeCancellationTokenSource == false) {
					DisposingUtil.DisposeLoggingException(cancellationTokenSource);
				}
				return null;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, doNotDisposeCancellationTokenSource);
		}
	}
}
