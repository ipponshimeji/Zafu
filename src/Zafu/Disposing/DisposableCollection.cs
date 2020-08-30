using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.ObjectModel;

namespace Zafu.Disposing {

	public class DisposableCollection: DisposableObject, ICollection<IDisposable> {
		#region data

		private List<IDisposable>? disposables;

		#endregion


		#region creation & disposal

		private DisposableCollection(List<IDisposable> disposables, string? name, IRunningContext? runningContext): base(null, name, runningContext) {
			// check argument
			Debug.Assert(disposables != null);

			// initialize member
			this.disposables = disposables;
		}

		public DisposableCollection(IEnumerable<IDisposable> disposables, string? name, IRunningContext? runningContext = null) : this(new List<IDisposable>(disposables), name, runningContext) {
		}

		public DisposableCollection(int capacity, string? name, IRunningContext? runningContext = null) : this(new List<IDisposable>(capacity), name, runningContext) {
		}

		public DisposableCollection(string? name, IRunningContext? runningContext = null) : this(new List<IDisposable>(), name, runningContext) {
		}

		protected override void Dispose(bool disposing) {
			List<IDisposable>? value = this.disposables;
			if (value != null) {
				DisposingUtil.DisposeLoggingException(value, this.RunningContext);
			}
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

		#endregion
	}
}
