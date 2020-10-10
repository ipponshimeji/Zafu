using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public class RunningTask: LockableObject, IRunningTask {
		#region data

		private readonly Action<RunningTask> action;

		private readonly Task task;

		private CancellationTokenSource? cancellationTokenSource = null;

		/// <summary>
		/// Whether this object can actually dispose <see cref="cancellationTokenSource"/> after the running task finishes.
		/// </summary>
		private bool doNotDisposeCancellationTokenSource = false;

		/// <summary>
		/// The value which <see cref="IsCancellationRequested"/> returns in the following cases:
		/// <list type="bullet">
		///   <item><see cref="cancellationTokenSource"/> is null (it was not given)</item>
		///   <item>after <see cref="cancellationTokenSource"/> is disposed.</item>
		/// </list>
		/// In that case, this value is used to keep the <see cref="IsCancellationRequested"/> value.
		/// </summary>
		private bool isCancellationRequestedEmulator = false;

		private bool isStarted = false;

		#endregion


		#region properties

		public CancellationToken CancellationToken {
			get {
				CancellationTokenSource? cts;
				lock (this.InstanceLocker) {
					// check state
					EnsureStartedNTS();

					// get CancellationTokenSource
					cts = this.cancellationTokenSource;
				}

				return (cts != null) ? cts.Token : CancellationToken.None;
			}
		}

		#endregion


		#region creation & disposal

		public RunningTask(IRunningContext? runningContext, Action<RunningTask> action): base(IRunningContext.CorrectWithDefault(runningContext), null) {
			// check arguments
			// runningContext can be null
			if (action == null) {
				throw new ArgumentNullException(nameof(action));
			}

			// initialize members
			this.action = action;
			static void proc(object? state) {
				RunningTask? runningTask = state as RunningTask;
				Debug.Assert(runningTask != null);
				try {
					runningTask.action(runningTask);
				} finally {
					runningTask.DisposeCancellationTokenSource();
				}
			}
			// The task is created without a cancellation token,
			// because the finally block in the proc may not be executed
			// if the task is created with a cancellation token and canceled by it.
			// It is assumed that cancellation is supported by the action itself.
			this.task = new Task(proc, this);

			Debug.Assert(this.cancellationTokenSource == null);
			Debug.Assert(this.doNotDisposeCancellationTokenSource == false);
			Debug.Assert(this.isCancellationRequestedEmulator == false);
			Debug.Assert(this.isStarted == false);
		}


		public void DisposeCancellationTokenSource() {
			// replace the this.cancellationTokenSource with null
			CancellationTokenSource? cts;
			lock (this.InstanceLocker) {
				cts = this.cancellationTokenSource;
				this.cancellationTokenSource = null;
			}

			// dispose the CancellationTokenSource if necessary
			if (cts != null) {
				// back up the cts.IsCancellationRequested property value before it is disposed
				this.isCancellationRequestedEmulator = cts.IsCancellationRequested;
				if (this.doNotDisposeCancellationTokenSource == false) {
					// Note that this object does not have the ownership of cts
					// if Start() method is not called even though doNotDisposeCancellationTokenSource is false.
					cts.Dispose();
				}
			}
		}

		#endregion


		#region IRunningTask

		public Task Task => this.task;

		public bool IsCancellationRequested {
			get {
				lock (this.InstanceLocker) {
					CancellationTokenSource? cts = this.cancellationTokenSource;
					// return the emulation value after the cancellationTokenSource is disposed
					return (cts == null) ? this.isCancellationRequestedEmulator: cts.IsCancellationRequested;
				}
			}
		}

		public void Cancel() {
			lock (this.InstanceLocker) {
				// check state
				EnsureStartedNTS();

				// cancel the task
				CancellationTokenSource? cts = this.cancellationTokenSource;
				if (cts != null) {
					cts.Cancel();
				} else {
					this.isCancellationRequestedEmulator = true;
				}
			}
		}

		public void WaitForCompletion() {
			// check state
			Task task = this.task;
			if (task.IsCompleted) {
				return;
			}

			// wait for completion of the task
			try {
				task.Wait();
			} catch (AggregateException) {
				;    // continue;
			}
		}

		#endregion


		#region methods

		/// <summary>
		/// Checks whether the task has been started or not.
		/// </summary>
		/// <remarks>
		/// This method must be called in the scope where this.instanceLocker is locked.
		/// </remarks>
		protected void EnsureStartedNTS() {
			if (this.isStarted == false) {
				throw new InvalidOperationException("It is not started yet.");
			}
		}

		public void Start(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			lock (this.InstanceLocker) {
				// check state
				if (this.isStarted) {
					throw new InvalidOperationException("It has been already started.");
				}
				this.isStarted = true;

				// set cancellationTokenSource
				Debug.Assert(this.cancellationTokenSource == null);
				this.cancellationTokenSource = cancellationTokenSource;
				this.doNotDisposeCancellationTokenSource = doNotDisposeCancellationTokenSource;
				try {
					this.task.Start();
				} catch {
					this.doNotDisposeCancellationTokenSource = false;
					this.cancellationTokenSource = null;
					throw;
				}
			}
		}

		#endregion
	}
}
