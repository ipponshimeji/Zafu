using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Testing.Tasks {
	public class SimpleActionState: TestingActionState {
		#region creation

		public SimpleActionState(Exception? exception = null, bool throwOnCancellation = false): base(exception, throwOnCancellation) {
		}

		#endregion


		#region actions

		public void Action(CancellationToken cancellationToken) {
			// check state
			EnsureNoneProgress();

			// do work
			Done(Works.Started);
			try {
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
