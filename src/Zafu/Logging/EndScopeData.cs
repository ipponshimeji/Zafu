using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Disposing;

namespace Zafu.Logging {
	public class EndScopeData: LoggingData, IEquatable<EndScopeData> {
		#region data

		public readonly object? Scope;

		#endregion


		#region creation

		public EndScopeData(object? scope): base() {
			// check argument
			// scope can be null

			// initialize members
			this.Scope = scope;
		}

		public EndScopeData(EndScopeData src): base(src) {
			// check argument
			Debug.Assert(src != null);

			// initialize members
			this.Scope = src.Scope;
		}

		#endregion


		#region operators

		public static bool operator ==(EndScopeData? x, EndScopeData? y) {
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

		public static bool operator !=(EndScopeData? x, EndScopeData? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<EndScopeData>

		public bool Equals(EndScopeData? other) {
			return (this == other);
		}

		#endregion


		#region methods

		public virtual void EndScope(IDisposable scope) {
			DisposingUtil.DisposeIgnoringException(scope);
		}

		public virtual void EndScope(IEnumerable<IDisposable?> scopes) {
			DisposingUtil.DisposeIgnoringException(scopes);
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			return (this == obj as EndScopeData);
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.Scope);
		}

		#endregion
	}
}
