using System;

namespace Zafu.ObjectModel {
	public class LockableObject: ObjectWithRunningContext {
		#region data

		private readonly object instanceLocker;

		#endregion


		#region properties

		protected object InstanceLocker {
			get {
				return this.instanceLocker;
			}
		}

		#endregion


		#region creation

		public LockableObject(object? instanceLocker = null, string? name = null, IRunningContext? runningContext = null): base(name, runningContext) {
			// check argument
			if (instanceLocker == null) {
				instanceLocker = new object();
			}

			// initialize members
			this.instanceLocker = instanceLocker;
		}

		#endregion


		#region methods

		protected void RunUnderInstanceLock(Action action) {
			lock (this.instanceLocker) {
				action();
			}
		}

		protected T RunUnderInstanceLock<T>(Func<T> func) {
			lock (this.instanceLocker) {
				return func();
			}
		}

		#endregion
	}
}
