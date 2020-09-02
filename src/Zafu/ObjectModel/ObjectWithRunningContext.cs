using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.ObjectModel {
	public class ObjectWithRunningContext {
		#region types

		public class Use {
			public static readonly object Message = new object();

			public static readonly object Logging = new object();
		}

		public class LogPropertyNames: LoggingUtil.LogPropertyNames {
		}

		#endregion


		#region data

		protected readonly IRunningContext RunningContext;

		private string name;

		private string? nameForLoggingCache = null;

		#endregion


		#region properties

		protected ILogger Logger => this.RunningContext.Logger;

		public string Name {
			get {
				return this.name;
			}
			set {
				// check argument
				if (value == null) {
					throw new ArgumentNullException(nameof(value));
				}

				this.name = value;
			}
		}

		protected string NameForLogging {
			get {
				string? value = this.nameForLoggingCache;
				if (value == null) {
					// There is low possibility to set in parallel, 
					// but it gives no actual harm.
					value = GetName(Use.Logging);
					Interlocked.Exchange(ref this.nameForLoggingCache, value);
				}

				return value;
			}
		}

		#endregion


		#region creation

		public ObjectWithRunningContext(string? name = null, IRunningContext? runningContext = null) {
			// check argument
			if (name == null) {
				name = (this.GetType().FullName ?? string.Empty);
			}
			if (runningContext == null) {
				runningContext = ZafuEnvironment.DefaultRunningContext;
			}

			// initialize member
			this.RunningContext = runningContext;
			this.name = name;
		}

		#endregion


		#region methods

		public string GetName(object use) {
			string? name = GetNameFor(use);
			// use name as the default name
			return name ?? this.Name;
		}


		protected void LogDebug(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Debug, message, exception, eventId);
		}

		protected void LogTrace(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Trace, message, exception, eventId);
		}

		protected void LogInformation(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Information, message, exception, eventId);
		}

		protected void LogWarning(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Warning, message, exception, eventId);
		}

		protected void LogError(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Error, message, exception, eventId);
		}

		protected void LogCritical(string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(LogLevel.Critical, message, exception, eventId);
		}

		#endregion


		#region overridables

		protected virtual string? GetNameFor(object use) {
			return null;
		}

		protected virtual void ClearNameCacheFor(object use) {
			if (use == Use.Logging) {
				Interlocked.Exchange(ref this.nameForLoggingCache, null);
			}
		}

		protected virtual void Log(LogLevel logLevel, string? message, Exception? exception, EventId eventId) {
			LoggingUtil.Log(this.Logger, logLevel, this.NameForLogging, message, exception, eventId);
		}

		#endregion
	}
}
