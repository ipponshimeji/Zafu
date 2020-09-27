using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Tasks.Testing {
	public class PausableTestAction: TestAction, IDisposable {
		#region data

		private AutoResetEvent? pausedEvent = new AutoResetEvent(false);

		private AutoResetEvent? resumeEvent = new AutoResetEvent(false);

		#endregion


		#region creation & disposal

		public PausableTestAction(Exception? exception = null, bool throwOnCancellation = false): base(exception, throwOnCancellation) {
		}

		public void Dispose() {
			// dispose the events
			static void dispose(ref AutoResetEvent? evt) {
				AutoResetEvent? oldValue = Interlocked.Exchange(ref evt, null);
				if (oldValue != null) {
					oldValue.Dispose();
				}
			}

			dispose(ref this.resumeEvent);
			dispose(ref this.pausedEvent);
		}

		#endregion


		#region overridables

		public override void Reset() {
			// check state
			(AutoResetEvent pausedEvent, AutoResetEvent resumeEvent) = EnsureNotDisposed();

			// reset the events
			pausedEvent.Reset();
			resumeEvent.Reset();

			// reset the base class level
			base.Reset();
		}

		#endregion


		#region methods

		protected (AutoResetEvent, AutoResetEvent) EnsureNotDisposed() {
			// check state
			AutoResetEvent? pausedEvent = this.pausedEvent;
			AutoResetEvent? resumeEvent = this.resumeEvent;
			if (pausedEvent == null || resumeEvent == null) {
				throw new ObjectDisposedException(null);
			}

			return (pausedEvent, resumeEvent);
		}

		public void WaitForPause() {
			// check state
			(AutoResetEvent pausedEvent, AutoResetEvent resumeEvent) = EnsureNotDisposed();

			// wait for the pause
			pausedEvent.WaitOne();
		}

		public void Resume() {
			// check state
			(AutoResetEvent pausedEvent, AutoResetEvent resumeEvent) = EnsureNotDisposed();

			// signal to resume
			resumeEvent.Set();
		}

		#endregion


		#region actions

		public void PausableAction(CancellationToken cancellationToken) {
			// check state
			(AutoResetEvent pausedEvent, AutoResetEvent resumeEvent) = EnsureNotDisposed();
			Debug.Assert(this.Progress == Works.None);

			// start
			Done(Works.Started);
			try {
				// pause/resume
				pausedEvent.Set();
				resumeEvent.WaitOne();

				// work
				// Note that Works.Worked is not done 
				// if the execution is terminated by an execption or cancellation.
				if (Work(cancellationToken)) {
					return;
				}
				Done(Works.Worked);
			} finally {
				Done(Works.Finished);
			}
		}

		public Task GetPausableActionTask(CancellationToken cancellationToken) {
			return Task.Run(() => this.PausableAction(cancellationToken), cancellationToken);
		}

		public Task<T> GetPausableActionTask<T>(CancellationToken cancellationToken, T result) {
			return Task<T>.Run(() => {
				this.PausableAction(cancellationToken);
				return result;
			}, cancellationToken);
		}

		public ValueTask GetPausableActionValueTask(CancellationToken cancellationToken) {
			return new ValueTask(GetPausableActionTask(cancellationToken));
		}

		public ValueTask<T> GetPausableActionValueTask<T>(CancellationToken cancellationToken, T result) {
			return new ValueTask<T>(GetPausableActionTask<T>(cancellationToken, result));
		}

		#endregion


		#region obsoletes

		public Task GetPausableActionTask(CancellationTokenSource? cancellationTokenSource) {
			return GetActionTask(cancellationTokenSource, this.PausableAction);
		}

		public ValueTask GetPausableActionValueTask(CancellationTokenSource? cancellationTokenSource) {
			return GetActionValueTask(cancellationTokenSource, this.PausableAction);
		}

		#endregion
	}
}
