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

			// create a RunningTask object for the action
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						action();
						OnTaskFinishing(rt.Task, null);
					} catch (Exception exception) {
						OnTaskFinishing(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);

			// register the task to the running task table and start the task
			return RegisterRunningTaskAndStart(runningTask, cancellationTokenSource: null, doNotDisposeCancellationTokenSource: false);
		}

		public virtual IRunningTask MonitorTask(Action<CancellationToken> action, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check arguments
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the action
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						action(rt.CancellationToken);
						OnTaskFinishing(rt.Task, null);
					} catch (Exception exception) {
						OnTaskFinishing(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);

			// register the task to the running task table and start the task
			CancellationTokenSource? createdCancellationTokenSource = null;
			if (cancellationTokenSource == null) {
				createdCancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource = createdCancellationTokenSource;
				doNotDisposeCancellationTokenSource = false;
			}
			try {
				return RegisterRunningTaskAndStart(runningTask, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			} catch {
				if (createdCancellationTokenSource != null) {
					createdCancellationTokenSource.Dispose();
				}
				throw;
			}
		}

		public virtual IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			if (task.IsCompleted) {
				// The task has already finished.
				if (doNotDisposeCancellationTokenSource == false) {
					DisposingUtil.DisposeLoggingException(cancellationTokenSource);
				}
				OnTaskFinished(task);
				return null;
			}
			// cancellationTokenSource can be null

			// create a RunningTask object for the task
			RunningTask runningTask = new RunningTask(
				runningContext: this.RunningContext,
				action: (RunningTask rt) => {
					Debug.Assert(rt != null);
					try {
						task.Sync();
						OnTaskFinishing(rt.Task, null);
					} catch (Exception exception) {
						OnTaskFinishing(rt.Task, exception);
						throw;
					} finally {
						UnregisterRunningTask(rt);
					}
				}
			);

			// register the task to the running task table and start the task
			return RegisterRunningTaskAndStart(runningTask, cancellationTokenSource, doNotDisposeCancellationTokenSource);
		}

		public virtual IRunningTask? MonitorTask(ValueTask valueTask, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check argument
			if (valueTask.IsCompleted) {
				// nothing to do
				if (doNotDisposeCancellationTokenSource == false) {
					DisposingUtil.DisposeLoggingException(cancellationTokenSource);
				}
				// TODO: logging
				return null;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, doNotDisposeCancellationTokenSource);
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

		protected virtual void OnTaskFinished(Task task) {
			// check argument
			Debug.Assert(task != null);
			Debug.Assert(task.IsCompleted);

			Exception? exception = null;
			if (task.IsFaulted) {
				AggregateException? aggregateException = task.Exception;
				Debug.Assert(aggregateException != null);
				exception = aggregateException.InnerException;
			} else if (task.IsCanceled) {
				exception = new TaskCanceledException();
			}
			OnTaskFinishing(task, exception);
		}

		#endregion


		#region overridables

		protected virtual void OnTaskFinishing(Task task, Exception? exception) {
			// Note that task is not completed at this point.
			// That is, task.IsCompleted is false and it does not have final result.
			// So you cannot use task.IsFaulted, task.IsCanceled, and task.IsCompletedSuccessfully here.

			// check argument
			Debug.Assert(task != null);

			if (exception == null) {
				// task is finishing successfully
				if (this.LoggingLevel <= LogLevel.Debug) {
					Log(task, LogLevel.Debug, "The running task finished successfully.", exception);
				}
			} else {
				// task throws an exception
				if (this.LoggingLevel <= LogLevel.Error) {
					Log(task, LogLevel.Error, "The running task ended with an exception.", exception);
				}
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

		private IRunningTask RegisterRunningTaskAndStart(RunningTask runningTask, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check arguments
			Debug.Assert(runningTask != null);

			// register the task to the running task table
			RegisterRunningTask(runningTask);
			try {
				// start the task
				// Make sure that the task starts after it is registered to the running task table.
				// Otherwise, if the task finishes immediately, RegisterRunningTask() may run
				// after UnregisterRunningTask() and the entry would remain.
				runningTask.Start(cancellationTokenSource, doNotDisposeCancellationTokenSource);
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
