using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The logger which can store only one log.
	/// This object can be used to test logging output quickly. 
	/// </summary>
	/// <remarks>
	/// Any instance members of this type is not thread safe.
	/// </remarks>
	public class SingleLogLogger: ILogger {
		#region data

		public LogData? Data { get; protected set; } = null;

		public string? Message { get; protected set; } = null;

		#endregion


		#region properties

		public bool Logged {
			get {
				return this.Data != null;
			}
		}

		#endregion


		#region ILogger

		public IDisposable BeginScope<TState>(TState state) {
			throw new NotSupportedException();
		}

		public bool IsEnabled(LogLevel logLevel) {
			// A log of all level is logged.
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			// check state
			if (this.Data != null) {
				throw new InvalidOperationException("This logger has already accepted a log. It can accept only one log.");
			}

			// store the log data
			this.Data = LogData.Create<TState>(state, logLevel, exception, eventId, formatter);
			this.Message = (formatter == null) ? null : formatter(state, exception);
		}

		#endregion


		#region methods

		public void Clear() {
			// clear its state
			this.Data = null;
			this.Message = null;
		}

		#endregion
	}
}
