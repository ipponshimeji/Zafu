using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

			// create a RunningTask object for the action
			RunningTask runningTask = RunningTask.Create((RunningTask rt) => {
				Debug.Assert(rt != null);
				action();
			});
			try {
				// start the task
				runningTask.Task.Start();
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

			// create a RunningTask object for the action
			RunningTask runningTask = RunningTask.Create((RunningTask rt, CancellationToken ct) => {
				Debug.Assert(rt != null);
				action(ct);
			}, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			try {
				// start the task
				runningTask.Task.Start();
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
			if (task.IsCompletedSuccessfully) {
				// nothing to do
				return null;
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the task
			RunningTask runningTask = RunningTask.Create((RunningTask rt) => {
				Debug.Assert(rt != null);
				task.Wait();
			}, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			try {
				// start the task
				runningTask.Task.Start();
				return runningTask;
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		#endregion
	}
}
