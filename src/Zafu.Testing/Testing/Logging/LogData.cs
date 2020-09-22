using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Zafu.Logging;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The class to represent data for ILogger.Log() operation.
	/// An instance of this class is immutable.
	/// </summary>
	public class LogData: LoggingData, IEquatable<LogData> {
		#region data

		public readonly Type StateType;

		public readonly LogLevel LogLevel;

		public readonly EventId EventId;

		public readonly object? State;

		public readonly Exception? Exception;

		public readonly Delegate? Formatter;

		#endregion


		#region creation

		public LogData(Type stateType, LogLevel logLevel, EventId eventId, object? state, Exception? exception, Delegate? formatter): base() {
			// check argument
			if (stateType == null) {
				throw new ArgumentNullException(nameof(stateType));
			}
			// state, exception, and formatter can be null
			if (state != null && stateType.IsInstanceOfType(state) == false) {
				throw new ArgumentException($"It is not an instance of {stateType.FullName}", nameof(state));
			}

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


		public static LogData Create<TState>(TState state, LogLevel logLevel, Exception? exception = null, EventId eventId = default(EventId), Func<TState, Exception, string>? formatter = null) {
			// check argument
			if (formatter == null) {
				formatter = LoggingUtil.DefaultFormatter<TState>;
			}

			return new LogData(typeof(TState), logLevel, eventId, state, exception, formatter);
		}

		public static LogData CreateWithSimpleState(string? source, string? message, LogLevel logLevel, Exception? exception = null, EventId eventId = default(EventId)) {
			return Create<SimpleState>(new SimpleState(source, message), logLevel, exception, eventId, LoggingUtil.JsonFormatter);
		}

		public static LogData CreateWithSimpleState<T>(string? source, string? message, string extraPropName, T extraPropValue, LogLevel logLevel, Exception? exception = null, EventId eventId = default(EventId)) {
			return Create<SimpleState<T>>(
				new SimpleState<T>(source, message, extraPropName, extraPropValue),
				logLevel,
				exception,
				eventId,
				LoggingUtil.JsonFormatter
			);
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
