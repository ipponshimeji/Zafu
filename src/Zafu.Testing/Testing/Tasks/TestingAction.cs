using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Testing.Tasks {
	public class TestingAction {
		#region types

		public class Works {
			#region constants

			public const int None = 0;

			public const int Started = 0x01;

			public const int Worked = 0x02;

			public const int Finished = 0x04;


			public const int All = (Started | Worked | Finished);

			// 'Worked' is not done due to an exception or cancellation.
			public const int Terminated = (Started | Finished);

			#endregion
		}

		#endregion


		#region data

		private static MethodInfo? exceptionSourceMethod = null;

		public readonly Exception? Exception;

		public readonly bool ThrowOnCancellation;

		public int Progress { get; private set; } = Works.None;

		#endregion


		#region properties

		public static MethodInfo ExceptionSourceMethod {
			get {
				MethodInfo? value = exceptionSourceMethod;
				if (value == null) {
					BindingFlags flags = (
						BindingFlags.DeclaredOnly |
						BindingFlags.Instance |
						BindingFlags.NonPublic
					);
					MethodInfo? temp = typeof(TestingAction).GetMethod("Work", flags);
					if (temp == null) {
						// unexpected state; the implementation was broken
						throw new NotSupportedException();
					}
					value = temp;
					// Multiple set operations may occur in very rare cases,
					// but it gives no actual harm except slight additional overhead.
					Interlocked.Exchange(ref exceptionSourceMethod, value);
				}
				return value;
			}
		}

		#endregion


		#region creation

		public TestingAction(Exception? exception = null, bool throwOnCancellation = false) {
			// check argument
			// exception can be null

			// initialize members
			this.Exception = exception;
			this.ThrowOnCancellation = throwOnCancellation;
		}

		#endregion


		#region overridables

		public virtual void Reset() {
			// reset its state
			this.Progress = Works.None;
		}

		#endregion


		#region methods

		protected void Done(int work) {
			this.Progress |= work;
		}

		protected bool Work(CancellationToken cancellationToken) {
			bool cancellationRequested = false;

			// do 'Work' according to its settings.
			if (this.Exception != null) {
				throw this.Exception;
			} else if (cancellationToken.CanBeCanceled) {
				if (this.ThrowOnCancellation) {
					cancellationToken.ThrowIfCancellationRequested();
				} else {
					cancellationRequested = cancellationToken.IsCancellationRequested;
				}
			}

			return cancellationRequested;
		}

		#endregion


		#region actions

		public void SimpleAction(CancellationToken cancellationToken) {
			// check state
			Debug.Assert(this.Progress == Works.None);

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

		public void SimpleAction() {
			SimpleAction(CancellationToken.None);
		}

		public Task GetSimpleActionTask(CancellationToken cancellationToken) {
			return Task.Run(() => this.SimpleAction(cancellationToken), cancellationToken);
		}

		public Task<T> GetSimpleActionTask<T>(CancellationToken cancellationToken, T result) {
			return Task<T>.Run(() => {
				this.SimpleAction(cancellationToken);
				return result;
			}, cancellationToken);
		}

		public ValueTask GetSimpleActionValueTask(CancellationToken cancellationToken) {
			return new ValueTask(GetSimpleActionTask(cancellationToken));
		}

		public ValueTask<T> GetSimpleActionValueTask<T>(CancellationToken cancellationToken, T result) {
			return new ValueTask<T>(GetSimpleActionTask<T>(cancellationToken, result));
		}

		#endregion
	}
}
