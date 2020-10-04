using System;

namespace Zafu.ObjectModel {
	public class LockableObject<TRunningContext>: ObjectWithRunningContext<TRunningContext> where TRunningContext: class, IRunningContext {
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

		public LockableObject(TRunningContext runningContext, object? instanceLocker): base(runningContext) {
			// check argument
			if (instanceLocker == null) {
				instanceLocker = new object();
			}

			// initialize member
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


	public class LockableObject: LockableObject<IRunningContext> {
		#region creation

		public LockableObject(IRunningContext? runningContext, object? instanceLocker) : base(IRunningContext.CorrectWithDefault(runningContext), instanceLocker) {
		}

		#endregion
	}
}
