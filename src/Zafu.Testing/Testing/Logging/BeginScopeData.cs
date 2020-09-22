using System;
using System.Diagnostics;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The class to represent data for ILogger.BeginScope() operation.
	/// An instance of this class is immutable.
	/// </summary>
	public class BeginScopeData: LoggingData, IEquatable<BeginScopeData> {
		#region data

		public readonly Type StateType;

		public readonly object? State;

		public readonly IDisposable? Scope;

		#endregion


		#region creation

		public BeginScopeData(Type stateType, object? state, IDisposable? scope): base() {
			// check argument
			if (stateType == null) {
				throw new ArgumentNullException(nameof(stateType));
			}
			// state and scope can be null
			if (state != null && stateType.IsInstanceOfType(state) == false) {
				throw new ArgumentException($"It is not an instance of {stateType.FullName}", nameof(state));
			}

			// initialize members
			this.StateType = stateType;
			this.State = state;
			this.Scope = scope;
		}

		public BeginScopeData(BeginScopeData src): base(src) {
			// check argument
			Debug.Assert(src != null);

			// initialize members
			this.StateType = src.StateType;
			this.State = src.State;
			this.Scope = src.Scope;
		}


		public static BeginScopeData Create<TState>(TState state, IDisposable? scope) {
			return new BeginScopeData(typeof(TState), state, scope);

		}

		#endregion


		#region operators

		public static bool operator ==(BeginScopeData? x, BeginScopeData? y) {
			if (object.ReferenceEquals(x, null)) {
				return object.ReferenceEquals(y, null);
			} else {
				if (object.ReferenceEquals(y, null)) {
					return false;
				} else {
					return (
						x.StateType == y.StateType &&
						// Do not compare by (x.State == y.State). That is a reference comparison.
						object.Equals(x.State, y.State) &&
						x.Scope == y.Scope
					);
				}
			}
		}

		public static bool operator !=(BeginScopeData? x, BeginScopeData? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<BeginScopeData>

		public bool Equals(BeginScopeData? other) {
			return (this == other);
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			return (this == obj as BeginScopeData);
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.StateType, this.State, this.Scope);
		}

		#endregion
	}
}
