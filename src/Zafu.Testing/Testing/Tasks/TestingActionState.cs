using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Testing.Tasks {
	/// <summary>
	/// The base class for classes which provide usable actions for test.
	/// </summary>
	public class TestingActionState {
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
						BindingFlags.Static |
						BindingFlags.NonPublic
					);
					MethodInfo? temp = typeof(TestingActionState).GetMethod("ThrowException", flags);
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

		protected TestingActionState(Exception? exception, bool throwOnCancellation) {
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

		protected void EnsureNoneProgress() {
			// check state
			if (this.Progress != Works.None) {
				throw new InvalidOperationException("Another action has been called on this state.");
			}
		}

		protected bool Work(CancellationToken cancellationToken) {
			bool cancellationRequested = false;

			// do 'Work' according to its settings.
			if (this.Exception != null) {
				ThrowException(this.Exception);
			} else if (cancellationToken.CanBeCanceled) {
				if (this.ThrowOnCancellation) {
					cancellationToken.ThrowIfCancellationRequested();
				} else {
					cancellationRequested = cancellationToken.IsCancellationRequested;
				}
			}

			return cancellationRequested;
		}

		/// <summary>
		/// Throws the given exception.
		/// This method is used to set TargetSite property of the thrown exception to this method.
		/// </summary>
		/// <param name="exception"></param>
		/// <seealso cref="ExceptionSourceMethod"/>
		protected static void ThrowException(Exception exception) {
			// check argument
			if (exception == null) {
				throw new ArgumentNullException(nameof(exception));
			}

			throw exception;
		}

		#endregion
	}
}
