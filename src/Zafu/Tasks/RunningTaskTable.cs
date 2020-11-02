﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

		private bool taskTableDisposed = false;

		#endregion


		#region creation & disposable

		public RunningTaskTable(IRunningContext? runningContext = null, string? name = null) : base(runningContext, null, CorrectWithDefault(name)) {
		}

		private static string CorrectWithDefault(string? name) {
			return name ?? DefaultName;
		}

		public override void Dispose() {
			if (this.taskTableDisposed == false) {
				Dispose(IRunningTaskTable.DefaultDisposeWaitingTimeout, IRunningTaskTable.DefaultDisposeCancelingTimeout);
			}
			base.Dispose();
		}

		#endregion


		#region IRunningTaskTable

		public virtual IRunningTaskMonitor RunningTaskMonitor => this;

		public virtual int RunningTaskCount => this.runningTasks.Count;

		public virtual bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// check state
			lock (this.InstanceLocker) {
				if (this.taskTableDisposed) {
					Debug.Assert(this.RunningTaskCount == 0);
					return true;
				}
				this.taskTableDisposed = true;
			}

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
			if (waitingTimeout == TimeSpan.Zero) {
				completed = (this.RunningTaskCount == 0);
			} else {
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

			// dispose this object
			Dispose();

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
						try {
							action();
						} catch (Exception exception) {
							OnTaskFinishing(rt, exception);
							throw;
						}
						OnTaskFinishing(rt, null);
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
						try {
							action(rt.CancellationToken);
						} catch (Exception exception) {
							OnTaskFinishing(rt, exception);
							throw;
						}
						OnTaskFinishing(rt, null);
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
						try {
							task.Sync();
						} catch (Exception exception) {
							OnTaskFinishing(rt, exception);
							throw;
						}
						OnTaskFinishing(rt, null);
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
				OnValueTaskFinished(valueTask);
				return null;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, doNotDisposeCancellationTokenSource);
		}

		#endregion


		#region methods

		protected void Log(int taskId, LogLevel logLevel, string message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (message == null) {
				throw new ArgumentNullException(nameof(message));
			}
			// exception can be null

			// log
			Log<int>(logLevel, message, "task-id", taskId, exception, eventId);
		}

		protected void Log(RunningTask runningTask, LogLevel logLevel, string message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (runningTask == null) {
				throw new ArgumentNullException(nameof(runningTask));
			}

			// log
			Log(runningTask.Task.Id, logLevel, message, exception, eventId);
		}

		protected void OnTaskFinished(Task task) {
			// check argument
			Debug.Assert(task != null);
			Debug.Assert(task.IsCompleted);

			Exception? exception = null;
			try {
				task.Wait();
			} catch (AggregateException e) {
				exception = e.InnerException;
				Debug.Assert(exception != null);
			}
			LogTaskFinished(task.Id, exception);
		}

		protected void OnValueTaskFinished(ValueTask valueTask) {
			// check argument
			Debug.Assert(valueTask.IsCompleted);

			Exception? exception = null;
			try {
				valueTask.GetAwaiter().GetResult();
			} catch (Exception e) {
				exception = e;
				Debug.Assert(exception != null);
			}
			LogTaskFinished(0, exception);
		}

		protected void LogTaskFinished(int taskId, Exception? exception) {
			if (exception == null) {
				// task is finished successfully
				LogLevel logLevel = LogLevel.Information;
				if (this.LoggingLevel <= logLevel) {
					Log(taskId, logLevel, "The running task finished successfully.");
				}
			} else {
				// task throws an exception
				LogLevel logLevel = LogLevel.Error;
				if (this.LoggingLevel <= logLevel) {
					Log(taskId, logLevel, "The running task finished with an exception.", exception);
				}
			}
		}

		#endregion


		#region overridables

		protected virtual void OnTaskFinishing(IRunningTask runningTask, Exception? exception) {
			// check argument
			if (runningTask == null) {
				throw new ArgumentNullException(nameof(runningTask));
			}
			// exception can be null

			// Note that task may not be completed at this point.
			// That is, runningTask.Task.IsCompleted is false and it does not have final result.
			// So you cannot use Task.IsFaulted, Task.IsCanceled, and Task.IsCompletedSuccessfully here.

			// log the result
			LogTaskFinished(runningTask.Task.Id, exception);
		}

		#endregion


		#region privates

		private void RegisterRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			lock (this.InstanceLocker) {
				// check state
				if (this.taskTableDisposed) {
					throw CreateObjectDisposedException();
				}

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
