using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zafu.ObjectModel;
using Zafu.Disposing;

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

		public RunningTaskTable(IRunningContext? runningContext = null, string? name = null) : base(runningContext, null, CorrectWithDefault(name)) {
		}

		private static string CorrectWithDefault(string? name) {
			return name ?? DefaultName;
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

			// create a RunningTask object for the action and start its task
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				cancellationTokenSource: null,
				doNotDisposeCancellationTokenSource: false,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						action();
					} catch (Exception exception) {
						OnTaskException(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);
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
			if (cancellationTokenSource == null) {
				cancellationTokenSource = new CancellationTokenSource();
				doNotDisposeCancellationTokenSource = false;
			}

			// create a RunningTask object for the action and start its task
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				cancellationTokenSource: cancellationTokenSource,
				doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						action(rt.CancellationToken);
					} catch (Exception exception) {
						OnTaskException(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);
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
			if (task.IsCompleted) {
				// nothing to do
				if (doNotDisposeCancellationTokenSource == false) {
					DisposingUtil.DisposeLoggingException(cancellationTokenSource);
				}
				if (task.IsCompletedSuccessfully == false) {
					try {
						task.Sync();
					} catch (Exception exception) {
						OnTaskException(task, exception);
					}
				}
				return null;
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the task
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				cancellationTokenSource: cancellationTokenSource,
				doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						task.Sync();
					} catch (Exception exception) {
						OnTaskException(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);
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

		protected void Log(Task? task, LogLevel logLevel, string message, Exception? exception = null) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}

			// log
			Log<int>(logLevel, message, "task-id", task.Id, exception, default(EventId));
		}

		protected void Log(RunningTask? runningTask, LogLevel logLevel, string message, Exception? exception = null) {
			// check argument
			if (runningTask == null) {
				throw new ArgumentNullException(nameof(runningTask));
			}

			// log
			Log(runningTask.Task, logLevel, message, exception);
		}

		#endregion


		#region overridables

		protected virtual void OnTaskException(Task task, Exception exception) {
			if (this.LoggingLevel <= LogLevel.Error) {
				Log(task, LogLevel.Error, "The running task threw an exception.", exception);
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
				runningTask.Start();
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
