using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Zafu.Disposing {

	public class DisposableCollection: IDisposable, ICollection<IDisposable> {
		#region data

		private readonly object instanceLocker = new object();

		public readonly string? Name;

		private readonly ILogger? logger;

		private List<IDisposable>? disposables;

		#endregion


		#region creation & disposal

		private DisposableCollection(List<IDisposable> disposables, string? name, ILogger? logger) {
			// check argument
			Debug.Assert(disposables != null);

			// initialize member
			this.Name = name;
			this.logger = logger;
			this.disposables = disposables;
		}

		public DisposableCollection(IEnumerable<IDisposable> disposables, string? name = null, ILogger? logger = null) : this(new List<IDisposable>(disposables), name, logger) {
		}

		public DisposableCollection(int capacity, string? name = null, ILogger? logger = null): this(new List<IDisposable>(capacity), name, logger) {
		}

		public DisposableCollection(string? name = null, ILogger? logger = null): this(new List<IDisposable>(), name, logger) {
		}

		public void Dispose() {
			List<IDisposable>? value = Interlocked.Exchange(ref this.disposables, null);
			if (value != null) {
				Func<string>? getLocation = (this.Name == null) ? (Func<string>?)null : GetDisposeLocation;
				DisposingUtil.DisposeIgnoringException(value, this.logger, getLocation);
			}
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			lock (this.instanceLocker) {
				return EnsureNotDisposedNTS().GetEnumerator();
			}
		}

		#endregion


		#region IEnumerable<IDisposable>

		public IEnumerator<IDisposable> GetEnumerator() {
			lock (this.instanceLocker) {
				return EnsureNotDisposedNTS().GetEnumerator();
			}
		}

		#endregion


		#region IReadOnlyCollection<IDisposable>

		public int Count {
			get {
				lock (this.instanceLocker) {
					return EnsureNotDisposedNTS().Count;
				}
			}
		}

		#endregion


		#region ICollection<IDisposable>

		public bool IsReadOnly => false;

		public void Add(IDisposable item) {
			lock (this.instanceLocker) {
				EnsureNotDisposedNTS().Add(item);
			}
		}

		public void Clear() {
			lock (this.instanceLocker) {
				EnsureNotDisposedNTS().Clear();
			}
		}

		public bool Contains(IDisposable item) {
			lock (this.instanceLocker) {
				return EnsureNotDisposedNTS().Contains(item);
			}
		}

		public void CopyTo(IDisposable[] array, int arrayIndex) {
			lock (this.instanceLocker) {
				EnsureNotDisposedNTS().CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(IDisposable item) {
			lock (this.instanceLocker) {
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
				throw new ObjectDisposedException(this.Name);
			}

			return disposables;
		}

		#endregion


		#region privates

		public string GetDisposeLocation() {
			return $"{this.GetType().FullName}.Dispose() on '{this.Name}'";
		}

		#endregion
	}
}
