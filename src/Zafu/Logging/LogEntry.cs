using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	public class LogEntry: IEquatable<LogEntry> {
		#region data

		protected static readonly MethodInfo GenericLogMethodInfo;


		public readonly Type StateType;

		public readonly LogLevel LogLevel;

		public readonly EventId EventId;

		public readonly object? State;

		public readonly Exception? Exception;

		public readonly Delegate? Formatter;


		private MethodInfo? logMethodInfoCache = null;

		private object?[]? logArgumentsCache = null;

		#endregion


		#region properties

		protected MethodInfo LogMethodInfo {
			get {
				MethodInfo? logMethodInfo = this.logMethodInfoCache;
				if (logMethodInfo == null) {
					// Rarely this.logMethodInfoCache may be set at the same time from multiple threads.
					// But don't care because the setting values will be the same after all.
					logMethodInfo = GenericLogMethodInfo.MakeGenericMethod(this.StateType);
					Interlocked.Exchange(ref this.logMethodInfoCache, logMethodInfo);
				}
				return logMethodInfo;
			}
		}

		protected object?[] LogArguments {
			get {
				object?[]? logArguments = this.logArgumentsCache;
				if (logArguments == null) {
					// Rarely this.logArgumentsCache may be set at the same time from multiple threads.
					// But don't care because the setting values will be the same after all.
					logArguments = new object?[] {
						this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter
					};
					Interlocked.Exchange(ref this.logArgumentsCache, logArguments);
				}
				return logArguments;
			}
		}

		#endregion


		#region creation

		static LogEntry() {
			MethodInfo ? genericLogMethodInfo = typeof(ILogger).GetMethod("Log");
			Debug.Assert(genericLogMethodInfo != null);
			GenericLogMethodInfo = genericLogMethodInfo;
		}

		public LogEntry(Type stateType, LogLevel logLevel, EventId eventId, object? state, Exception? exception, Delegate? formatter) {
			// check argument
			if (stateType == null) {
				throw new ArgumentNullException(nameof(stateType));
			}
			// other parameters are directly stored

			// initialize members
			this.StateType = stateType;
			this.LogLevel = logLevel;
			this.EventId = eventId;
			this.State = state;
			this.Exception = exception;
			this.Formatter = formatter;
		}

		public LogEntry(LogEntry src) {
			// check argument
			if (src == null) {
				throw new ArgumentNullException(nameof(src));
			}

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

		public static bool operator == (LogEntry? x, LogEntry? y) {
			if (object.ReferenceEquals(x, null)) {
				return object.ReferenceEquals(y, null);
			} else {
				if (object.ReferenceEquals(y, null)) {
					return false;
				} else {
					return (
						x.LogLevel == y.LogLevel &&
						x.EventId == y.EventId &&
						x.State == y.State &&
						x.Exception == y.Exception &&
						x.Formatter == y.Formatter &&
						x.StateType == y.StateType
					);
				}				
			}
		}

		public static bool operator !=(LogEntry? x, LogEntry? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<LogEntry>

		public bool Equals(LogEntry? other) {
			return (this == other);
		}

		#endregion


		#region methods

		public string? GetMessage() {
			if (this.Formatter == null) {
				return null;
			} else {
				return this.Formatter.DynamicInvoke(this.State, this.Exception) as string;
			}
		}

		public void LogTo(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// call logger.Log<TState>()
			try {
				this.LogMethodInfo.Invoke(logger, this.LogArguments);
			} catch {
				// continue
			}
		}

		public void LogTo(IEnumerable<ILogger> loggers) {
			// check argument
			if (loggers == null) {
				throw new ArgumentNullException(nameof(loggers));
			}

			// call logger.Log<TState>()
			MethodInfo logMethodInfo = this.LogMethodInfo;
			object?[] logArguments = this.LogArguments;
			foreach (ILogger logger in loggers) {
				if (logger != null) {
					try {
						logMethodInfo.Invoke(logger, logArguments);
					} catch {
						// continue
					}
				}
			}
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			return (this == obj as LogEntry);
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
