using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Disposing;

namespace Zafu.Testing.Tasks {
	public class PausableActionState: TestingActionState, IDisposable {
		#region data

		private AutoResetEvent? pausedEvent = new AutoResetEvent(false);

		private AutoResetEvent? resumeEvent = new AutoResetEvent(false);

		#endregion


		#region creation & disposal

		public PausableActionState(Exception? exception = null, bool throwOnCancellation = false): base(exception, throwOnCancellation) {
		}

		public void Dispose() {
			// dispose the events
			DisposingUtil.ClearDisposableLoggingException(ref this.resumeEvent);
			DisposingUtil.ClearDisposableLoggingException(ref this.pausedEvent);
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

		public void Action(CancellationToken cancellationToken) {
			// check state
			EnsureNoneProgress();
			(AutoResetEvent pausedEvent, AutoResetEvent resumeEvent) = EnsureNotDisposed();

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

		public void UncancellableAction() {
			Action(CancellationToken.None);
		}

		public Task GetActionTask(CancellationToken cancellationToken) {
			return Task.Run(() => this.Action(cancellationToken), cancellationToken);
		}

		public Task<T> GetActionTask<T>(CancellationToken cancellationToken, T result) {
			return Task<T>.Run(() => { this.Action(cancellationToken); return result; }, cancellationToken);
		}

		#endregion
	}
}
