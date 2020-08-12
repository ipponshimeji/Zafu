using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Zafu.Logging;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The logger which can store only one log entry.
	/// This object can be used to test logging output quickly. 
	/// </summary>
	/// <remarks>
	/// Any instance members of this type is not thread safe.
	/// </remarks>
	public class SingleEntryLogger: ILogger {
		#region data

		public bool Logged { get; protected set; } = false;

		public Type? StateType { get; protected set; } = null;

		public LogLevel LogLevel { get; protected set; } = LogLevel.None;

		public EventId EventId { get; protected set; } = default(EventId);

		public object? State { get; protected set; } = null;

		public Exception? Exception { get; protected set; } = null;

		public Delegate? Formatter { get; protected set; } = null;

		public string? Message { get; protected set; } = null;

		#endregion


		#region ILogger

		public IDisposable BeginScope<TState>(TState state) {
			throw new NotSupportedException();
		}

		public bool IsEnabled(LogLevel logLevel) {
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			// check state
			if (this.Logged) {
				throw new InvalidOperationException("This logger can log only one entry.");
			} else {
				this.Logged = true;
			}

			// store the log entry
			this.StateType = typeof(TState);
			this.LogLevel = logLevel;
			this.EventId = eventId;
			this.State = state;
			this.Exception = exception;
			this.Formatter = formatter;
			if (formatter != null) {
				this.Message = formatter(state, exception);
			} else {
				this.Message = null;
			}
		}

		#endregion


		#region methods

		public void Clear() {
			// clear its state
			this.Logged = false;
			this.StateType = null;
			this.LogLevel = LogLevel.None;
			this.EventId = default(EventId);
			this.State = null;
			this.Exception = null;
			this.Formatter = null;
			this.Message = null;
		}

		public LogEntry? GetLogEntry() {
			// check state
			if (this.Logged == false) {
				return null;
			}

			Debug.Assert(this.StateType != null);
			return new LogEntry(this.StateType, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter);
		}

		#endregion
	}
}
