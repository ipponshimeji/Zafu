using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.ObjectModel {
	public class ObjectWithRunningContext<TRunningContext> where TRunningContext: class, IRunningContext {
		#region data

		protected readonly TRunningContext RunningContext;

		#endregion


		#region properties

		protected ILogger Logger => this.RunningContext.Logger;

		protected LogLevel LoggingLevel => this.RunningContext.LoggingLevel;

		#endregion


		#region creation

		public ObjectWithRunningContext(TRunningContext runningContext) {
			// check argument
			if (runningContext == null) {
				throw new ArgumentNullException(nameof(runningContext));
			}

			// initialize member
			this.RunningContext = runningContext;
		}

		#endregion


		#region methods

		protected void LogTrace(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Trace, message, exception, eventId);
		}

		protected void LogTrace<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Trace, message, extraPropName, extraPropValue, exception, eventId);
		}

		protected void LogDebug(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Debug, message, exception, eventId);
		}

		protected void LogDebug<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Debug, message, extraPropName, extraPropValue, exception, eventId);
		}

		protected void LogInformation(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Information, message, exception, eventId);
		}

		protected void LogInformation<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Information, message, extraPropName, extraPropValue, exception, eventId);
		}

		protected void LogWarning(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Warning, message, exception, eventId);
		}

		protected void LogWarning<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Warning, message, extraPropName, extraPropValue, exception, eventId);
		}

		protected void LogError(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Error, message, exception, eventId);
		}

		protected void LogError<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Error, message, extraPropName, extraPropValue, exception, eventId);
		}

		protected void LogCritical(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Critical, message, exception, eventId);
		}

		protected void LogCritical<T>(string? message, string extraPropName, T extraPropValue, Exception? exception = null, EventId eventId = default(EventId)) {
			Log<T>(LogLevel.Critical, message, extraPropName, extraPropValue, exception, eventId);
		}

		#endregion


		#region overridables

		protected virtual string GetNameForLogging() {
			Type type = GetType();
			return type.FullName ?? type.Name;
		}

		protected virtual void Log(LogLevel logLevel, string? message, Exception? exception, EventId eventId) {
			LoggingUtil.Log(this.Logger, logLevel, GetNameForLogging(), message, exception, eventId);
		}

		protected virtual void Log<T>(LogLevel logLevel, string? message, string extraPropName, T extraPropValue, Exception? exception, EventId eventId) {
			LoggingUtil.Log<T>(this.Logger, logLevel, GetNameForLogging(), message, extraPropName, extraPropValue, exception, eventId);
		}

		#endregion
	}


	public class ObjectWithRunningContext: ObjectWithRunningContext<IRunningContext> {
		#region creation

		public ObjectWithRunningContext(IRunningContext? runningContext) : base(IRunningContext.CorrectWithDefault(runningContext)) {
		}

		#endregion
	}
}
