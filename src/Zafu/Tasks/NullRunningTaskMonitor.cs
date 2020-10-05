using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Disposing;

namespace Zafu.Tasks {
	/// <summary>
	/// <para>
	/// The class to provide simple <see cref="IRunningTaskMonitor"/> feature,
	/// which runs tasks but does not trace their state.
	/// In the other hand, <see cref="RunningTaskTable"/> monitors the tasks in its table and
	/// writes log if an exception happens in a task.
	/// </para>
	/// <para>
	/// This class does not maintain running task table, so it does not implement <see cref="IRunningTaskTable"/>.
	/// </para>
	/// </summary>
	public class NullRunningTaskMonitor: IRunningTaskMonitor {
		#region data

		public static readonly NullRunningTaskMonitor Instance = new NullRunningTaskMonitor();

		#endregion


		#region creation & disposable

		private NullRunningTaskMonitor() {
		}

		#endregion


		#region IRunningTaskMonitor

		public IRunningTask MonitorTask(Action action) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			// create a RunningTask object for the action and start its task
			RunningTask runningTask = new RunningTask(
				runningContext: null,
				cancellationTokenSource: null,
				doNotDisposeCancellationTokenSource: false,
				action: (RunningTask rt) => {
					action();
				}
			);
			try {
				// start the task
				runningTask.Start();
				return runningTask;
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		public IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			if (cancellationTokenSource == null) {
				cancellationTokenSource = new CancellationTokenSource();
				doNotDisposeCancellationTokenSource = false;
			}

			// create a RunningTask object for the action and start its task
			RunningTask runningTask = new RunningTask(
				runningContext: null,
				cancellationTokenSource: cancellationTokenSource,
				doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
				action: (RunningTask rt) => {
					action(rt.CancellationToken);
				}
			);
			try {
				// start the task
				runningTask.Start();
				return runningTask;
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		public IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			if (task.IsCompleted) {
				// nothing to do
				if (doNotDisposeCancellationTokenSource == false) {
					DisposingUtil.DisposeLoggingException(cancellationTokenSource);
				}
				return null;
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the task
			RunningTask runningTask = new RunningTask(
				runningContext: null,
				cancellationTokenSource: cancellationTokenSource,
				doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
				action: (RunningTask rt) => {
					task.Sync();
				}
			);
			try {
				// start the task
				runningTask.Start();
				return runningTask;
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		#endregion
	}
}
