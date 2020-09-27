﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Tasks {
	public class RunningTask: IRunningTask {
		#region data

		private readonly object instanceLocker = new object();

		private readonly Task task;

		private CancellationTokenSource? cancellationTokenSource;

		/// <summary>
		/// Whether this object can dispose <see cref="cancellationTokenSource"/> after the running task finishes.
		/// </summary>
		private readonly bool doNotDisposeCancellationTokenSource;

		/// <summary>
		/// The value which <see cref="IsCancellationRequested"/> returns.
		/// After <see cref="cancellationTokenSource"/> is disposed,
		/// this value is used to keep the <see cref="IsCancellationRequested"/> value.
		/// </summary>
		private bool isCancellationRequestedEmulator = false;

		#endregion


		#region creation & disposal

		public RunningTask(Task task, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			// cancellationTokenSource can be null

			// initialize members
			this.task = task;
			this.cancellationTokenSource = cancellationTokenSource;
			this.doNotDisposeCancellationTokenSource = doNotDisposeCancellationTokenSource;
			Debug.Assert(this.isCancellationRequestedEmulator == false);
		}

		public static RunningTask Create(Action<RunningTask, CancellationToken> action, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// check arguments
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			// cancellationTokenSource can be null

			// create a RunningTask instance
			if (cancellationTokenSource != null) {
				RunningTask? runningTask = null;
				CancellationToken cancellationToken = cancellationTokenSource.Token;
				Task task = new Task(() => {
					Debug.Assert(runningTask != null);
					try {
						action(runningTask, cancellationToken);
					} finally {
						runningTask.DisposeCancellationTokenSource();
					}
				}, cancellationToken);

				// Note that the runningTask variable is referenced in the lambda above.
				runningTask = new RunningTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				return runningTask;
			} else {
				// create a CancellationTokenSource automatically
				CancellationTokenSource cts = new CancellationTokenSource();
				try {
					return Create(action, cts, false);
				} catch {
					cts.Dispose();
					throw;
				}
			}
		}

		public static RunningTask Create(Action<RunningTask> action, CancellationTokenSource? cancellationTokenSource = null, bool doNotDisposeCancellationTokenSource = false) {
			// check arguments
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			// cancellationTokenSource can be null

			RunningTask? runningTask = null;
			Task task = new Task(() => {
				Debug.Assert(runningTask != null);
				try {
					action(runningTask);
				} finally {
					runningTask.DisposeCancellationTokenSource();
				}
			});

			// Note that the runningTask variable is referenced in the lambda above.
			runningTask = new RunningTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			return runningTask;
		}


		public void DisposeCancellationTokenSource() {
			// replace the this.cancellationTokenSource with null
			CancellationTokenSource? cts;
			lock (this.instanceLocker) {
				cts = this.cancellationTokenSource;
				this.cancellationTokenSource = null;
			}

			// dispose the CancellationTokenSource if necessary
			if (cts != null) {
				// back up the cts.IsCancellationRequested property value before it is disposed
				this.isCancellationRequestedEmulator = cts.IsCancellationRequested;
				if (this.doNotDisposeCancellationTokenSource == false) {
					cts.Dispose();
				}
			}
		}

		#endregion


		#region IRunningTask

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
}
