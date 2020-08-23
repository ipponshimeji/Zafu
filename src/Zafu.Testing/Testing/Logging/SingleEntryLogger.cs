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

		public LogEntry? Entry { get; protected set; } = null;

		public string? Message { get; protected set; } = null;

		#endregion


		#region properties

		public bool Logged {
			get {
				return this.Entry != null;
			}
		}

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
			if (this.Entry != null) {
				throw new InvalidOperationException("This logger can log only one entry.");
			}

			// store the log entry
			this.Entry = new LogEntry(typeof(TState), logLevel, eventId, state, exception, formatter);
			this.Message = (formatter == null) ? null : formatter(state, exception);
		}

		#endregion


		#region methods

		public void Clear() {
			// clear its state
			this.Entry = null;
			this.Message = null;
		}

		#endregion
	}
}
