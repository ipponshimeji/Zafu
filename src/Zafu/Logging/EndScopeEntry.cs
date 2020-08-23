using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Disposing;

namespace Zafu.Logging {
	public class EndScopeEntry: Entry, IEquatable<EndScopeEntry> {
		#region data

		public readonly object? Scope;

		#endregion


		#region creation

		public EndScopeEntry(object? scope): base() {
			// check argument
			// scope can be null

			// initialize members
			this.Scope = scope;
		}

		public EndScopeEntry(EndScopeEntry src): base(src) {
			// check argument
			Debug.Assert(src != null);

			// initialize members
			this.Scope = src.Scope;
		}

		#endregion


		#region operators

		public static bool operator ==(EndScopeEntry? x, EndScopeEntry? y) {
			if (object.ReferenceEquals(x, null)) {
				return object.ReferenceEquals(y, null);
			} else {
				if (object.ReferenceEquals(y, null)) {
					return false;
				} else {
					return x.Scope == y.Scope;
				}
			}
		}

		public static bool operator !=(EndScopeEntry? x, EndScopeEntry? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<EndScopeEntry>

		public bool Equals(EndScopeEntry? other) {
			return (this == other);
		}

		#endregion


		#region methods

		public virtual void EndScope(IDisposable scope) {
			DisposingUtil.DisposeIgnoringException(scope);
		}

		public virtual void EndScope(IEnumerable<IDisposable> scopes) {
			DisposingUtil.DisposeIgnoringException(scopes);
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			return (this == obj as EndScopeEntry);
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.Scope);
		}

		#endregion
	}
}
