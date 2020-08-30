using System;
using System.Diagnostics;
using System.Threading;

namespace Zafu {
	public sealed class DefaultZafuEnvironmentScope: IDisposable {
		#region types

		public enum DisposeResult {
			/// <summary>
			/// This scope is already disposed.
			/// </summary>
			ScopeDisposed = -2,

			/// <summary>
			/// The Dispose operation of the default ZafuEnvironment is timed out.
			/// </summary>
			Timeout = -1,

			/// <summary>
			/// The Dispose operation of the default ZafuEnvironment is completed.
			/// </summary>
			Completed = 0,

			/// <summary>
			/// The default ZafuEnvironment is not disposed because its reference count is not zero.
			/// </summary>
			InUse = 1
		}

		#endregion


		#region data

		private ZafuEnvironment? defaultZafuEnvironment;

		#endregion


		#region properties

		public ZafuEnvironment DefaultZafuEnvironment {
			get {
				// check state
				ZafuEnvironment? value = this.defaultZafuEnvironment;
				if (value == null) {
					throw new ObjectDisposedException(null);
				}

				return value;
			}
		}

		public bool IsDisposed {
			get {
				return this.defaultZafuEnvironment == null;
			}
		}

		#endregion


		#region creation & disposal

		internal DefaultZafuEnvironmentScope(ZafuEnvironment defaultZafuEnvironment) {
			// check argument
			if (defaultZafuEnvironment == null) {
				throw new ArgumentNullException(nameof(defaultZafuEnvironment));
			}

			// initialize members
			this.defaultZafuEnvironment = defaultZafuEnvironment;
		}

		public void Dispose() {
			ZafuEnvironment? env = this.defaultZafuEnvironment;
			if (env != null) {
				Dispose(env.DisposeWaitingTimeout, env.DisposeCancelingTimeout);
			}
		}

		public DisposeResult Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			ZafuEnvironment? env = Interlocked.Exchange(ref this.defaultZafuEnvironment, null);
			return (env == null)? DisposeResult.ScopeDisposed: ZafuEnvironment.UninitializeDefaultEnvironment(waitingTimeout, cancelingTimeOut);
		}

		#endregion
	}
}
