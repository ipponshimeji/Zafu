using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	public class BeginScopeData: LoggingData, IEquatable<BeginScopeData> {
		#region data

		protected static readonly MethodInfo GenericBeginScopeMethodInfo;


		public readonly Type StateType;

		public readonly object? State;

		public readonly object? Scope;

		#endregion


		#region creation

		static BeginScopeData() {
			MethodInfo? genericBeginScopeMethodInfo = typeof(ILogger).GetMethod("BeginScope");
			Debug.Assert(genericBeginScopeMethodInfo != null);
			GenericBeginScopeMethodInfo = genericBeginScopeMethodInfo;
		}


		public BeginScopeData(Type stateType, object? state, object? scope): base() {
			// check argument
			if (stateType == null) {
				throw new ArgumentNullException(nameof(stateType));
			}
			// state and scope can be null

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
						x.State == y.State &&
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


		#region methods

		public virtual IDisposable? BeginScopeOn(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// call logger.BeginScope<TState>()
			IDisposable? scope = null;
			MethodInfo beginScopeMethod = GetBeginScopeMethodInfo();
			object?[] args = GetBeginScopeArguments();
			try {
				scope = beginScopeMethod.Invoke(logger, args) as IDisposable;
			} catch {
				// continue
				// TODO: should throw exception like Microsoft.Extensions.Logging.Logger.BeginScope() implementation?
			}

			return scope;
		}

		public virtual IEnumerable<IDisposable> BeginScopeOn(IEnumerable<ILogger?> loggers) {
			// check argument
			if (loggers == null) {
				throw new ArgumentNullException(nameof(loggers));
			}

			// call logger.BeginScope<TState>()
			MethodInfo beginScopeMethod = GetBeginScopeMethodInfo();
			object?[] args = GetBeginScopeArguments();
			IDisposable? beginScope(ILogger? logger) {
				IDisposable? scope = null;
				if (logger != null) {
					try {
						scope = beginScopeMethod.Invoke(logger, args) as IDisposable;
					} catch {
						// continue
						// TODO: should throw exception like Microsoft.Extensions.Logging.Logger.BeginScope() implementation?
					}
				}
				return scope;
			}

			return loggers.Select(l => beginScope(l)).Where(s => s != null).ToArray()!;
		}

		protected MethodInfo GetBeginScopeMethodInfo() {
			return GenericBeginScopeMethodInfo.MakeGenericMethod(this.StateType);
		}

		protected object?[] GetBeginScopeArguments() {
			return new object?[] { this.State };
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
