using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTask: ObjectWithRunningContext, IRunningTask {
		#region data

		private readonly object instanceLocker = new object();

		private readonly Action<RunningTask> action;

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


		#region properties

		public CancellationToken CancellationToken {
			get {
				CancellationTokenSource? cts = this.cancellationTokenSource;
				return (cts != null) ? cts.Token : CancellationToken.None;
			}
		}

		#endregion


		#region creation & disposal

		public RunningTask(IRunningContext? runningContext, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, Action<RunningTask> action) : base(IRunningContext.CorrectWithDefault(runningContext)) {
			// check argument
			// runningContext can be null
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}
			// cancellationTokenSource can be null

			// initialize members
			this.action = action;
			this.cancellationTokenSource = cancellationTokenSource;
			this.doNotDisposeCancellationTokenSource = doNotDisposeCancellationTokenSource;
			Debug.Assert(this.isCancellationRequestedEmulator == false);

			static void proc(object? state) {
				RunningTask? runningTask = state as RunningTask;
				Debug.Assert(runningTask != null);
				try {
					runningTask.action(runningTask);
				} finally {
					runningTask.DisposeCancellationTokenSource();
				}
			}
			CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
			this.task = new Task(proc, this, cancellationToken);
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


		#region methods

		public void Start() {
			this.task.Start();
		}

		#endregion
	}
}
