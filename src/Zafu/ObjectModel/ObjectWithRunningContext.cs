using System;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.ObjectModel {
	public class ObjectWithRunningContext {
		#region data

		protected readonly IRunningContext RunningContext;

		public string? NameForLogging { get; protected set; } = null;

		#endregion


		#region properties

		protected ILogger Logger {
			get {
				return this.RunningContext.Logger;
			}
		}

		#endregion


		#region creation

		public ObjectWithRunningContext(IRunningContext? runningContext) {
			// check argument
			if (runningContext == null) {
				// TODO: set default context
				throw new ArgumentNullException(nameof(runningContext));
			}

			// initialize member
			this.RunningContext = runningContext;
		}

		#endregion


		#region methods

		protected void LogDebug(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogDebug(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		protected void LogTrace(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogTrace(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		protected void LogInformation(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogInformation(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		protected void LogWarning(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogWarning(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		protected void LogError(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogError(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		protected void LogCritical(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			LoggingUtil.LogCritical(this.Logger, this.NameForLogging, message, exception, eventId);
		}

		#endregion
	}
}
