using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Disposing;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The class to represent data for end scope operation of a scope which is returned from ILogger.BeginScope().
	/// An instance of this class is immutable.
	/// </summary>
	public class EndScopeData: LoggingData, IEquatable<EndScopeData> {
		#region data

		public readonly IDisposable? Scope;

		#endregion


		#region creation

		public EndScopeData(IDisposable? scope): base() {
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
					// Do not compare by (x.Scope == y.Scope). That is a reference comparison.
					return object.Equals(x.Scope, y.Scope);
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
