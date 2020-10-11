using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Testing.Tasks {
	public class CancellableActionState: TestingActionState {
		#region data

		public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);


		public readonly TimeSpan Timeout;

		#endregion


		#region creation

		public CancellableActionState(TimeSpan timeout, Exception? exception = null, bool throwOnCancellation = false) : base(exception, throwOnCancellation) {
			// initialize member
			this.Timeout = timeout;
		}

		public CancellableActionState(Exception? exception = null, bool throwOnCancellation = false) : this(DefaultTimeout, exception, throwOnCancellation) {
		}

		#endregion


		#region actions

		public void Action(CancellationToken cancellationToken) {
			// check argument
			if (cancellationToken.CanBeCanceled == false) {
				// cancellationToken must be cancellable,
				// because cancellation with it is the only way to return this method successfully.
				throw new ArgumentException("It must be cancellable.", nameof(cancellationToken));
			}

			// check state
			EnsureNoneProgress();

			// do work
			Done(Works.Started);
			try {
				// wait for cancellation
				bool canceled = cancellationToken.WaitHandle.WaitOne(this.Timeout);
				if (canceled) {
					if (this.ThrowOnCancellation) {
						cancellationToken.ThrowIfCancellationRequested();
					}
					return;
				}

				if (this.Exception != null) {
					ThrowException(this.Exception);
				}
				Done(Works.Worked);
			} finally {
				Done(Works.Finished);
			}
		}

		#endregion
	}
}
