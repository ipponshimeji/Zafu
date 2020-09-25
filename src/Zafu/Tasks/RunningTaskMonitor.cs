using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTaskMonitor: DisposableObject, IRunningTaskMonitor {
		#region types

		public class RunningTask: ITaskCanceler {
			#region data

			private readonly object instanceLocker = new object();

			public readonly Task task;

			private CancellationTokenSource? cancellationTokenSource;

			private readonly bool dontDisposeCancellationTokenSource = false;

			private bool isCancellationRequested = false;

			#endregion


			#region creation & disposal

			public RunningTask(Task task, CancellationTokenSource? cancellationTokenSource, bool dontDisposeCancellationTokenSource) {
				// check argument
				Debug.Assert(task != null);
				// cancellationTokenSource can be null

				// initialize members
				this.task = task;
				this.cancellationTokenSource = cancellationTokenSource;
				this.dontDisposeCancellationTokenSource = dontDisposeCancellationTokenSource;
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
					// back up the IsCancellationRequested property value before it is disposed
					this.isCancellationRequested = cts.IsCancellationRequested;
					cts.Dispose();
				}
			}

			#endregion


			#region ITaskCanceler

			public Task Task => this.task;

			public bool IsCancellationRequested {
				get {
					lock (this.instanceLocker) {
						CancellationTokenSource? cts = this.cancellationTokenSource;
						// return the value backed up if the CancellationTokenSource was disposed
						return (cts == null) ? this.isCancellationRequested: cts.IsCancellationRequested;
					}
				}
			}

			public void Cancel() {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.Cancel();
					}
				}
			}

			public void Cancel(bool throwOnFirstException) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.Cancel(throwOnFirstException);
					}
				}
			}

			public void CancelAfter(int millisecondsDelay) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.CancelAfter(millisecondsDelay);
					}
				}
			}

			public void CancelAfter(TimeSpan delay) {
				lock (this.instanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					if (cts != null) {
						cts.CancelAfter(delay);
					}
				}
			}

			#endregion
		}

		public class NullTaskCanceler: ITaskCanceler {
			#region data

			public static readonly NullTaskCanceler Instance = new NullTaskCanceler();

			#endregion


			#region creation

			private NullTaskCanceler() {
			}

			#endregion


			#region ITaskCanceler

			public Task? Task => null;

			public bool IsCancellationRequested => false;

			public void Cancel() {
			}

			public void Cancel(bool throwOnFirstException) {
			}

			public void CancelAfter(int millisecondsDelay) {
			}

			public void CancelAfter(TimeSpan delay) {
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
				Dispose(IRunningTaskMonitor.DefaultDisposeWaitingTimeout, IRunningTaskMonitor.DefaultDisposeCancelingTimeout);
			}
		}

		public virtual bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// Note that waitingTimeout and cancelingTimeOut may be -1 millisecond (infinite).
			bool completed = false;
			Task[] tasks;

			// try to wait for completion of the current running tasks
			if (waitingTimeout != TimeSpan.Zero) {
				// get the current running tasks
				lock (this.InstanceLocker) {
					tasks = this.runningTasks.Select(rt => rt.Task).ToArray();
				}

				// wait for completion of the tasks
				if (0 < tasks.Length) {
					completed = Task.WaitAll(tasks, waitingTimeout);
				} else {
					completed = true;
				}
			}

			// cancell the all running tasks and wait for completion of them
			if (completed == false && cancelingTimeOut != TimeSpan.Zero) {
				lock (this.InstanceLocker) {
					// cancel the all running tasks
					foreach (RunningTask runningTask in this.runningTasks) {
						runningTask.Cancel();
					}

					// get the remaining running tasks
					tasks = this.runningTasks.Select(rt => rt.Task).ToArray();
				}

				// wait for completion of the tasks
				if (0 < tasks.Length) {
					completed = Task.WaitAll(tasks, waitingTimeout);
				} else {
					completed = true;
				}
			}

			return completed;
		}

		#endregion


		#region IRunningTaskMonitor

		public virtual ITaskCanceler MonitorTask(Action<CancellationToken> action) {
			// check argument
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			CancellationTokenSource cts = new CancellationTokenSource();
			try {
				// create task resources
				RunningTask? runningTask = null;
				CancellationToken cancellationToken = cts.Token;
				Task task = new Task(() => {
					Debug.Assert(runningTask != null);
					try {
						action(cancellationToken);
					} finally {
						RemoveRunningTask(runningTask);
						runningTask.DisposeCancellationTokenSource();
					}

					// ToDo: logging
				}, cancellationToken);

				// register the task to the running task table and start it
				return AddRunningTaskAndStart(task, cancellationTokenSource: cts, dontDisposeCancellationTokenSource: false);
			} catch {
				cts.Dispose();
				throw;
			}
		}

		public virtual void MonitorTask(Action action) {
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
				} finally {
					RemoveRunningTask(runningTask);
				}

				// ToDo: logging
			});

			// register the task to the running task table and start it
			AddRunningTaskAndStart(task, cancellationTokenSource: null, dontDisposeCancellationTokenSource: false);
		}

		public virtual ITaskCanceler MonitorTask(Task task, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			if (task.IsCompletedSuccessfully) {
				// nothing to do
				return NullTaskCanceler.Instance;
			}
			// cancellationTokenSource can be null

			// create task resources
			RunningTask? runningTask = null;
			CancellationToken cancellationToken = (cancellationTokenSource != null)? cancellationTokenSource.Token: CancellationToken.None;
			Task outerTask = new Task(async () => {
				Debug.Assert(runningTask != null);
				try {
					await task;
				} finally {
					RemoveRunningTask(runningTask);
					runningTask.DisposeCancellationTokenSource();
				}

				// ToDo: logging
			}, cancellationToken);

			// register the task to the running task table and start it
			return AddRunningTaskAndStart(outerTask, cancellationTokenSource, dontDisposeCancellationTokenSource);
		}

		public virtual ITaskCanceler MonitorTask(ValueTask valueTask, CancellationTokenSource? cancellationTokenSource = null, bool dontDisposeCancellationTokenSource = false) {
			// check argument
			if (valueTask.IsCompletedSuccessfully) {
				// nothing to do
				return NullTaskCanceler.Instance;
			}
			// cancellationTokenSource can be null

			return MonitorTask(valueTask.AsTask(), cancellationTokenSource, dontDisposeCancellationTokenSource);
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
		}

		private void RemoveRunningTask(RunningTask runningTask) {
			// check argument
			Debug.Assert(runningTask != null);

			// remove the running task from the running task table
			lock (this.InstanceLocker) {
				this.runningTasks.Remove(runningTask);
			}
		}

		private ITaskCanceler AddRunningTaskAndStart(Task task, CancellationTokenSource? cancellationTokenSource, bool dontDisposeCancellationTokenSource) {
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
				// the entry would remain forever if the task finish immediately.
				task.Start();
			} catch {
				RemoveRunningTask(runningTask);
				throw;
			}

			return runningTask;
		}

		#endregion
	}
}
