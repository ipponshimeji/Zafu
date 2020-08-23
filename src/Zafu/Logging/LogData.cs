using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	/// <summary>
	/// The class to represent data for ILogger.Log operation.
	/// An instance of this class is immutable.
	/// </summary>
	public class LogData: LoggingData, IEquatable<LogData> {
		#region data

		protected static readonly MethodInfo GenericLogMethodInfo;


		public readonly Type StateType;

		public readonly LogLevel LogLevel;

		public readonly EventId EventId;

		public readonly object? State;

		public readonly Exception? Exception;

		public readonly Delegate? Formatter;

		#endregion


		#region creation

		static LogData() {
			MethodInfo ? genericLogMethodInfo = typeof(ILogger).GetMethod("Log");
			Debug.Assert(genericLogMethodInfo != null);
			GenericLogMethodInfo = genericLogMethodInfo;
		}

		public LogData(Type stateType, LogLevel logLevel, EventId eventId, object? state, Exception? exception, Delegate? formatter): base() {
			// check argument
			if (stateType == null) {
				throw new ArgumentNullException(nameof(stateType));
			}
			// state, exception, and formatter can be null

			// initialize members
			this.StateType = stateType;
			this.LogLevel = logLevel;
			this.EventId = eventId;
			this.State = state;
			this.Exception = exception;
			this.Formatter = formatter;
		}

		public LogData(LogData src): base(src) {
			// check argument
			Debug.Assert(src != null);

			// initialize members
			this.StateType = src.StateType;
			this.LogLevel = src.LogLevel;
			this.EventId = src.EventId;
			this.State = src.State;
			this.Exception = src.Exception;
			this.Formatter = src.Formatter;
		}

		#endregion


		#region operators

		public static bool operator == (LogData? x, LogData? y) {
			if (object.ReferenceEquals(x, null)) {
				return object.ReferenceEquals(y, null);
			} else {
				if (object.ReferenceEquals(y, null)) {
					return false;
				} else {
					return (
						x.LogLevel == y.LogLevel &&
						x.EventId == y.EventId &&
						x.StateType == y.StateType &&
						x.State == y.State &&
						x.Exception == y.Exception &&
						x.Formatter == y.Formatter
					);
				}				
			}
		}

		public static bool operator !=(LogData? x, LogData? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<LogData>

		public bool Equals(LogData? other) {
			return (this == other);
		}

		#endregion


		#region methods

		public virtual string? GetMessage() {
			if (this.Formatter == null) {
				return null;
			} else {
				return this.Formatter.DynamicInvoke(this.State, this.Exception) as string;
			}
		}

		public virtual void LogTo(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// call logger.Log<TState>()
			MethodInfo logMethod = GetLogMethodInfo();
			object?[] args = GetLogArguments();
			try {
				logMethod.Invoke(logger, args);
			} catch {
				// continue
				// TODO: should throw exception like Microsoft.Extensions.Logging.Logger.Log() implementation?
			}
		}

		public virtual void LogTo(IEnumerable<ILogger?> loggers) {
			// check argument
			if (loggers == null) {
				throw new ArgumentNullException(nameof(loggers));
			}

			// call logger.Log<TState>()
			MethodInfo logMethod = GetLogMethodInfo();
			object?[] args = GetLogArguments();
			foreach (ILogger? logger in loggers) {
				if (logger != null) {
					try {
						logMethod.Invoke(logger, args);
					} catch {
						// continue
						// TODO: should throw exception like Microsoft.Extensions.Logging.Logger.Log() implementation?
					}
				}
			}
		}

		protected MethodInfo GetLogMethodInfo() {
			return GenericLogMethodInfo.MakeGenericMethod(this.StateType);
		}

		protected object?[] GetLogArguments() {
			return new object?[] {
				this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter
			};
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			return (this == obj as LogData);
		}

		public override int GetHashCode() {
			return HashCode.Combine(
				this.StateType,
				this.LogLevel,
				this.EventId,
				this.State,
				this.Exception,
				this.Formatter
			);
		}

		#endregion
	}
}
