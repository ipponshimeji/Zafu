using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.ObjectModel;

namespace Zafu.Disposing {

	public class DisposableCollection<TRunningContext>: DisposableObject<TRunningContext>, ICollection<IDisposable> where TRunningContext: class, IRunningContext {
		#region data

		private List<IDisposable>? disposables;

		#endregion


		#region creation & disposal

		private DisposableCollection(TRunningContext runningContext, List<IDisposable> disposables, string? name): base(runningContext, null, name) {
			// check argument
			Debug.Assert(disposables != null);

			// initialize member
			this.disposables = disposables;
		}

		public DisposableCollection(TRunningContext runningContext, IEnumerable<IDisposable> disposables, string? name = null) : this(runningContext, new List<IDisposable>(disposables), name) {
		}

		public DisposableCollection(TRunningContext runningContext, int capacity, string? name = null) : this(runningContext, new List<IDisposable>(capacity), name) {
		}

		public DisposableCollection(TRunningContext runningContext, string? name = null) : this(runningContext, new List<IDisposable>(), name) {
		}

		protected override void Dispose(bool disposing) {
			DisposingUtil.ClearDisposablesLoggingException(ref this.disposables, this.RunningContext);
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			lock (this.InstanceLocker) {
				return EnsureNotDisposedNTS().GetEnumerator();
			}
		}

		#endregion


		#region IEnumerable<IDisposable>

		public IEnumerator<IDisposable> GetEnumerator() {
			lock (this.InstanceLocker) {
				return EnsureNotDisposedNTS().GetEnumerator();
			}
		}

		#endregion


		#region IReadOnlyCollection<IDisposable>

		public int Count {
			get {
				lock (this.InstanceLocker) {
					return EnsureNotDisposedNTS().Count;
				}
			}
		}

		#endregion


		#region ICollection<IDisposable>

		public bool IsReadOnly => false;

		public void Add(IDisposable item) {
			lock (this.InstanceLocker) {
				EnsureNotDisposedNTS().Add(item);
			}
		}

		public void Clear() {
			lock (this.InstanceLocker) {
				EnsureNotDisposedNTS().Clear();
			}
		}

		public bool Contains(IDisposable item) {
			lock (this.InstanceLocker) {
				return EnsureNotDisposedNTS().Contains(item);
			}
		}

		public void CopyTo(IDisposable[] array, int arrayIndex) {
			lock (this.InstanceLocker) {
				EnsureNotDisposedNTS().CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(IDisposable item) {
			lock (this.InstanceLocker) {
				return EnsureNotDisposedNTS().Remove(item);
			}
		}

		#endregion


		#region methods

		/// <remarks>
		/// This method is not thread safe.
		/// </remarks>
		protected List<IDisposable> EnsureNotDisposedNTS() {
			// check state
			List<IDisposable>? disposables = this.disposables;
			if (disposables == null) {
				throw CreateObjectDisposedException();
			}

			return disposables;
		}


		// expose RunUnderInstanceLock() methods as public

		public new void RunUnderInstanceLock(Action action) {
			base.RunUnderInstanceLock(action);
		}

		public new T RunUnderInstanceLock<T>(Func<T> func) {
			return base.RunUnderInstanceLock<T>(func);
		}

		#endregion
	}

	public class DisposableCollection: DisposableCollection<IRunningContext> {
		#region creation & disposal

		public DisposableCollection(IRunningContext? runningContext, IEnumerable<IDisposable> disposables, string? name = null) : base(IRunningContext.CorrectWithDefault(runningContext), disposables, name) {
		}

		public DisposableCollection(IRunningContext? runningContext, int capacity, string? name = null) : base(IRunningContext.CorrectWithDefault(runningContext), capacity, name) {
		}

		public DisposableCollection(IRunningContext? runningContext, string? name = null) : base(IRunningContext.CorrectWithDefault(runningContext), name) {
		}

		#endregion
	}
}
