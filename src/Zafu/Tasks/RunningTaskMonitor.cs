using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTaskMonitor: DisposableObject, IRunningTaskTable, IRunningTaskMonitor {
		#region types

		public class RunningTask: IRunningTask {
			#region data

			private static int lastId = 0;


			private readonly object instanceLocker = new object();

			private readonly int id;

			private readonly Task task;

			private CancellationTokenSource? cancellationTokenSource;

			/// <summary>
			/// Whether this object can dispose <see cref="cancellationTokenSource"/> after the running task finishes.
			/// </summary>
			private readonly bool dontDisposeCancellationTokenSource;

			/// <summary>
			/// The value which <see cref="IsCancellationRequested"/> returns.
			/// After <see cref="cancellationTokenSource"/> is disposed,
			/// this value is used to keep the <see cref="IsCancellationRequested"/> value.
			/// </summary>
			private bool isCancellationRequestedEmulator = false;

			#endregion


			#region creation & disposal

			public RunningTask(Task task, CancellationTokenSource? cancellationTokenSource, bool dontDisposeCancellationTokenSource) {
				// check argument
				Debug.Assert(task != null);
				// cancellationTokenSource can be null

				// initialize members
				this.id = Interlocked.Increment(ref lastId);
				this.task = task;
				this.cancellationTokenSource = cancellationTokenSource;
				this.dontDisposeCancellationTokenSource = dontDisposeCancellationTokenSource;
				Debug.Assert(this.isCancellationRequestedEmulator == false);
			}

			public void DisposeCancellationTokenSource() {
				// replace the this.cancellationTokenSource with null
				CancellationTokenSource? cts;
				lock (this.instanceLocker) {
					cts = this.cancellationTokenSource;
					this.cancellationTokenSource = null;
				}

				// dispose the CancellationTokenSource if necessary
				if (cts != null && this.dontDisposeCancellationTokenSource == false) {
					// back up the cts.IsCancellationRequested property value before it is disposed
					this.isCancellationRequestedEmulator = cts.IsCancellationRequested;
					cts.Dispose();
				}
			}

			#endregion


			#region IRunningTask

			public int Id => this.id;

			public Task Task => this.task;

			public bool IsCancellationRequested {
				get {
					lock (this.instanceLocker) {
						CancellationTokenSource? cts = this.cancellationTokenSource;
						// return the emulation value after the cancellationTokenSource is disposed
						return (cts == null) ? this.isCancellationRequestedEmulator: cts.IsCancellationRequested;
					}
				}
			}

			public void Cancel() {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.Cancel();
					} else {
						this.isCancellationRequestedEmulator = true;
					}
				}
			}

			public void Cancel(bool throwOnFirstException) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.Cancel(throwOnFirstException);
					} else {
						this.isCancellationRequestedEmulator = true;
					}
				}
			}

			public void CancelAfter(int millisecondsDelay) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.CancelAfter(millisecondsDelay);
					} else {
						this.isCancellationRequestedEmulator = true;
					}
				}
			}

			public void CancelAfter(TimeSpan delay) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.CancelAfter(delay);
					} else {
						this.isCancellationRequestedEmulator = true;
					}
				}
			}

			#endregion
		}

		#endregion


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

		public RunningTaskMonitor(string? name, IRunningContext? runningContext = null): base(null, name, runningContext) {
		}

		public RunningTaskMonitor(IRunningContext? runningContext = null) : base(null, DefaultName, runningContext) {
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				Dispose(IRunningTaskTable.DefaultDisposeWaitingTimeout, IRunningTaskTable.DefaultDisposeCancelingTimeout);
			}
		}

		#endregion


		#region IRunningTaskTable

		public virtual IRunningTaskMonitor Monitor => this;

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

		public virtual IRunningTask MonitorTask(Action<CancellationToken> action) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			CancellationTokenSource cts = new CancellationTokenSource();
			try {
				// create task related resources
				RunningTask? runningTask = null;
				CancellationToken cancellationToken = cts.Token;
				Task task = new Task(() => {
					Debug.Assert(runningTask != null);
					try {
						action(cancellationToken);
					} catch (Exception exception) {
						OnTaskException(runningTask, exception);
						throw;
					} finally {
						RemoveRunningTask(runningTask);
						runningTask.DisposeCancellationTokenSource();
					}
				}, cancellationToken);

				// register the task to the running task table and start it
				return AddRunningTaskAndStart(task, cancellationTokenSource: cts, dontDisposeCancellationTokenSource: false);
			} catch {
				cts.Dispose();
				throw;
			}
		}

		public virtual IRunningTask MonitorTask(Action action) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			// create task resources
			RunningTask? runningTask = null;
			Task task = new Task(() => {
				Debug.Assert(runningTask != null);
				try {
					action();
				} catch (Exception exception) {
					OnTaskException(runningTask, exception);
					throw;
				} finally {
					RemoveRunningTask(runningTask);
				}
			});

			// register the task to the running task table and start it
			return AddRunningTaskAndStart(task, cancellationTokenSource: null, dontDisposeCancellationTokenSource: false);
		}

		public virtual IRunningTask? MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			if (task.IsCompletedSuccessfully) {
				// nothing to do
				return null;
			}
			// cancellationTokenSource can be null

			// create task resources
			RunningTask? runningTask = null;
			CancellationToken cancellationToken = (cancellationTokenSource != null)? cancellationTokenSource.Token: CancellationToken.None;
			Task outerTask = new Task(async () => {
				Debug.Assert(runningTask != null);
				try {
					await task;
				} catch (Exception exception) {
					OnTaskException(runningTask, exception);
					throw;
				} finally {
					RemoveRunningTask(runningTask);
					runningTask.DisposeCancellationTokenSource();
				}
			}, cancellationToken);

			// register the task to the running task table and start it
			return AddRunningTaskAndStart(outerTask, cancellationTokenSource, dontDisposeCancellationTokenSource);
		}

		public virtual IRunningTask? MonitorTask(ValueTask valueTask, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			// check argument
			if (valueTask.IsCompletedSuccessfully) {
				// nothing to do
				return null;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, dontDisposeCancellationTokenSource);
		}

		#endregion


		#region methods

		protected void Log(RunningTask? runningTask, LogLevel logLevel, string message, Exception? exception = null) {
			// check argument
			if (runningTask == null) {
				throw new ArgumentNullException(nameof(runningTask));
			}

			// log
			Log<int>(logLevel, message, "id", runningTask.Id, exception, default(EventId));
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

		private void AddRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			lock (this.InstanceLocker) {
				// check state
				EnsureNotDisposed();

				// add the running task to the running task table
				Debug.Assert(this.runningTasks.Contains(runningTask) == false);
				this.runningTasks.Add(runningTask);
			}

			// log
			LogLevel logLevel = LogLevel.Debug;
			if (this.LoggingLevel <= logLevel) {
				Log(runningTask, logLevel, "A running task was registered.");
			}
		}

		private void RemoveRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			// remove the running task from the running task table
			lock (this.InstanceLocker) {
				this.runningTasks.Remove(runningTask);
			}

			// log
			LogLevel logLevel = LogLevel.Debug;
			if (this.LoggingLevel <= logLevel) {
				Log(runningTask, logLevel, "The running task was unregistered.");
			}
		}

		private IRunningTask AddRunningTaskAndStart(Task task, CancellationTokenSource? cancellationTokenSource, bool dontDisposeCancellationTokenSource) {
			// check arguments
			Debug.Assert(task != null);
			// cancellationTokenSource can be null

			// register the task to the running task table
			RunningTask runningTask = new RunningTask(task, cancellationTokenSource, dontDisposeCancellationTokenSource);
			AddRunningTask(runningTask);
			try {
				// start the task
				// Make sure that the task starts after it is registered to the running task table.
				// Otherwise, AddRunningTask() may run after RemoveRunningTask() and
				// the entry would remain forever if the task finishes immediately.
				task.Start();
			} catch (Exception exception) {
				RemoveRunningTask(runningTask);
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
