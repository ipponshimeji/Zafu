using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTaskTable: DisposableObject, IRunningTaskTable, IRunningTaskMonitor {
		#region constants

		public const string DefaultName = "RunningTaskMonitor";

		#endregion


		#region data

		private readonly HashSet<RunningTask> runningTasks = new HashSet<RunningTask>();

		#endregion


		#region properties

		public int Count {
			get {
				return this.runningTasks.Count;
			}
		}

		#endregion


		#region creation & disposable

		public RunningTaskTable(IRunningContext? runningContext = null, string? name = null) : base(runningContext, null, name) {
		}

		public RunningTaskTable(IRunningContext? runningContext = null) : base(runningContext, null, DefaultName) {
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Dispose(IRunningTaskTable.DefaultDisposeWaitingTimeout, IRunningTaskTable.DefaultDisposeCancelingTimeout);
			}
		}

		#endregion


		#region IRunningTaskTable

		public virtual IRunningTaskMonitor RunningTaskMonitor => this;

		public virtual bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// Note that waitingTimeout and cancelingTimeOut may be -1 millisecond, which means "infinite".
			bool completed = false;
			Task[] tasks;

			static Task[] getTasks(HashSet<RunningTask> set) {
				return set.Select(rt => rt.Task).ToArray();
			}

			static bool waitForTasks(Task[] t, TimeSpan timeout) {
				return (0 < t.Length) ? Task.WaitAll(t, timeout) : true;
			}

			// try to wait for completion of the current running tasks
			if (waitingTimeout != TimeSpan.Zero) {
				// get the current running tasks
				lock (this.InstanceLocker) {
					tasks = getTasks(this.runningTasks);
				}

				// wait for completion of the tasks
				completed = waitForTasks(tasks, waitingTimeout);
			}

			// cancell the all running tasks and wait for completion of them
			if (completed == false && cancelingTimeOut != TimeSpan.Zero) {
				lock (this.InstanceLocker) {
					// cancel the all running tasks
					foreach (RunningTask runningTask in this.runningTasks) {
						runningTask.Cancel();
					}

					// get the remaining running tasks
					tasks = getTasks(this.runningTasks);
				}

				// wait for completion of the tasks
				completed = waitForTasks(tasks, cancelingTimeOut);
			}

			return completed;
		}

		#endregion


		#region IRunningTaskMonitor

		public virtual IRunningTask MonitorTask(Action action) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			// create a RunningTask object for the action
			RunningTask runningTask = RunningTask.Create((RunningTask rt) => {
				Debug.Assert(rt != null);
				try {
					action();
				} catch (Exception exception) {
					OnTaskException(rt, exception);
					throw;
				} finally {
					UnregisterRunningTask(rt);
				}
			});
			try {
				// register the task to the running task table and start it
				return RegisterRunningTaskAndStart(runningTask);
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		public virtual IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			// create a RunningTask object for the action
			RunningTask runningTask = RunningTask.Create((RunningTask rt, CancellationToken ct) => {
				Debug.Assert(rt != null);
				try {
					action(ct);
				} catch (Exception exception) {
					OnTaskException(rt, exception);
					throw;
				} finally {
					UnregisterRunningTask(rt);
				}
			}, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			try {
				// register the task to the running task table and start it
				return RegisterRunningTaskAndStart(runningTask);
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		public virtual IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
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
				try {
					task.Wait();
				} catch (Exception exception) {
					// TODO: should unwrap AggregateException?
					OnTaskException(rt, exception);
					throw;
				} finally {
					UnregisterRunningTask(rt);
				}
			}, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			try {
				// register the task to the running task table and start it
				return RegisterRunningTaskAndStart(runningTask);
			} catch {
				runningTask.DisposeCancellationTokenSource();
				throw;
			}
		}

		#endregion


		#region methods

		protected void Log(RunningTask? runningTask, LogLevel logLevel, string message, Exception? exception = null) {
			// check argument
			if (runningTask == null) {
				throw new ArgumentNullException(nameof(runningTask));
			}

			// log
			Log<int>(logLevel, message, "task-id", runningTask.Task.Id, exception, default(EventId));
		}

		#endregion


		#region overridables

		protected virtual void OnTaskException(RunningTask runningTask, Exception exception) {
			if (this.LoggingLevel <= LogLevel.Error) {
				Log(runningTask, LogLevel.Error, "The running task threw an exception.", exception);
			}
		}

		#endregion


		#region privates

		private void RegisterRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			lock (this.InstanceLocker) {
				// check state
				EnsureNotDisposed();

				// register the running task to the running task table
				Debug.Assert(this.runningTasks.Contains(runningTask) == false);
				this.runningTasks.Add(runningTask);
			}

			// log registration
			LogLevel logLevel = LogLevel.Debug;
			if (this.LoggingLevel <= logLevel) {
				Log(runningTask, logLevel, "A running task was registered.");
			}
		}

		private void UnregisterRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			// unregister the running task from the running task table
			lock (this.InstanceLocker) {
				this.runningTasks.Remove(runningTask);
			}

			// log unregistration
			LogLevel logLevel = LogLevel.Debug;
			if (this.LoggingLevel <= logLevel) {
				Log(runningTask, logLevel, "The running task was unregistered.");
			}
		}

		private IRunningTask RegisterRunningTaskAndStart(RunningTask runningTask) {
			// check arguments
			Debug.Assert(runningTask != null);

			// register the task to the running task table
			RegisterRunningTask(runningTask);
			try {
				// start the task
				// Make sure that the task starts after it is registered to the running task table.
				// Otherwise, RegisterRunningTask() may run after UnregisterRunningTask() and
				// the entry would remain forever if the task finishes immediately.
				runningTask.Task.Start();
			} catch (Exception exception) {
				UnregisterRunningTask(runningTask);
				if (this.LoggingLevel <= LogLevel.Error) {
					Log(runningTask, LogLevel.Error, "The task failed to start.", exception);
				}
				throw;
			}

			return runningTask;
		}

		#endregion
	}
}
