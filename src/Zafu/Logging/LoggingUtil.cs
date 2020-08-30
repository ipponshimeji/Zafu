using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	public static class LoggingUtil {
		#region properties

		public static ILogger DefaultLogger => ZafuEnvironment.DefaultRunningContext.Logger;

		public static ILogger NullLogger => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

		#endregion


		#region methods

		public static string FormatLogMessage(string? header, string? message) {
			if (string.IsNullOrEmpty(header)) {
				return message ?? string.Empty;
			} else {
				return $"[{header}] {message}";
			}
		}

		/// <summary>
		/// The default method which can be specified to 'formatter' argument of ILogger.Log().
		/// </summary>
		/// <param name="message"></param>
		/// <param name="exception"></param>
		/// <returns></returns>
		public static string FormatLog(string message, Exception? exception) {
			return message;
		}

		public static void Log(ILogger? logger, LogLevel logLevel, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (logger == null) {
				// nothing to do
				return;
			}

			try {
				logger.Log(logLevel, eventId, FormatLogMessage(header, message), exception, FormatLog);
			} catch (Exception) {
				// continue
			}
		}

		public static void LogTrace(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Trace, header, message, exception, eventId);
		}

		public static void LogDebug(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Debug, header, message, exception, eventId);
		}

		public static void LogInformation(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Information, header, message, exception, eventId);
		}

		public static void LogWarning(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Warning, header, message, exception, eventId);
		}

		public static void LogError(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Error, header, message, exception, eventId);
		}

		public static void LogCritical(ILogger? logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Critical, header, message, exception, eventId);
		}

		#endregion
	}
}
