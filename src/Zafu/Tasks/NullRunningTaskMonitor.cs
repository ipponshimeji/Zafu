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
	/// In the other hand, <see cref="RunningTaskTable"/> monitors the tasks with its table and
	/// writes log if an exception happens in a task.
	/// </para>
	/// <para>
	/// This class does not implement <see cref="IRunningTaskTable"/> because it does not maintain running task table.
	/// </para>
	/// <para>
	/// An instance of this class is stateless. Use <see cref="Instance"/> static field to access shared instance.
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

			// create a RunningTask object for the action
			RunningTask runningTask = new RunningTask(
				runningContext: null,
				action: (RunningTask rt) => action()
			);

			// start the RunningTask
			runningTask.Start(cancellationTokenSource: null, doNotDisposeCancellationTokenSource: false);
			return runningTask;
		}

		public IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check arguments
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the action
			RunningTask runningTask = new RunningTask(
				runningContext: null,
				action: (RunningTask rt) => action(rt.CancellationToken)
			);

			// start the RunningTask
			CancellationTokenSource? createdCancellationTokenSource = null;
			if (cancellationTokenSource == null) {
				createdCancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource = createdCancellationTokenSource;
				doNotDisposeCancellationTokenSource = false;
			}
			try {
				runningTask.Start(cancellationTokenSource, doNotDisposeCancellationTokenSource);
				return runningTask;
			} catch {
				if (createdCancellationTokenSource != null) {
					createdCancellationTokenSource.Dispose();
				}
				throw;
			}
		}

		public IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check arguments
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
				action: (RunningTask rt) => task.Sync()
			);

			// start the RunningTask
			runningTask.Start(cancellationTokenSource, doNotDisposeCancellationTokenSource);
			return runningTask;
		}

		#endregion
	}
}
