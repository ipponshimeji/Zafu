using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging.Testing {
	/// <summary>
	/// The logger which can store only one log entry.
	/// This object is used in test of logging methods to capture its log output. 
	/// </summary>
	public class SingleEntryLogger: ILogger {
		#region types

		/// <remarks>
		/// This object is immutable.
		/// </remarks>
		public class EntryData {
			#region data

			public readonly LogLevel LogLevel;

			public readonly EventId EventId;

			public readonly object? State;

			public readonly Exception? Exception;

			public readonly object? Formatter;

			#endregion


			#region constructor

			public EntryData(LogLevel logLevel, EventId eventId, object? state, Exception? exception, object? formatter) {
				// initialize members
				this.LogLevel = logLevel;
				this.EventId = eventId;
				this.State = state;
				this.Exception = exception;
				this.Formatter = formatter;
			}

			#endregion
		}

		public class ScopeData: IDisposable {
			#region data

			public readonly object? State;

			public bool Disposed { get; private set; } = false;

			#endregion


			#region constructor

			public ScopeData(object? state) {
				// initialize members
				this.State = state;
			}

			#endregion


			#region IDisposable

			public void Dispose() {
				this.Disposed = true;
			}

			#endregion
		}

		#endregion


		#region data

		public LogLevel LoggingLevel { get; private set; } = LogLevel.Trace;

		public EntryData? Entry { get; private set; } = null;

		public ScopeData? Scope { get; private set; } = null;

		#endregion


		#region ILogger

		public IDisposable BeginScope<TState>(TState state) {
			// check state
			if (this.Scope != null) {
				throw new InvalidOperationException("This logger can has only one scope.");
			}

			ScopeData scope = new ScopeData(state);
			this.Scope = scope;
			return scope;
		}

		public bool IsEnabled(LogLevel logLevel) {
			return this.LoggingLevel <= logLevel;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			// check state
			if (this.Entry != null) {
				throw new InvalidOperationException("This logger can log only one entry.");
			}

			// save log information
			this.Entry = new EntryData(logLevel, eventId, state, exception, formatter);
		}

		#endregion
	}
}
