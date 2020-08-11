using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	public static class LoggingUtil {
		#region constants

		public const string LogCategoryName = "Zafu";

		#endregion


		#region data

		private static readonly RedirectingLogger defaultLogger = new RedirectingLogger();

		#endregion


		#region properties

		public static ILogger DefaultLogger => defaultLogger;

		#endregion


		#region methods

		public static void AddToDefaultLogger(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// add the logger to the default logger
			defaultLogger.AddTargetLogger(logger);
		}

		public static ILogger AddToDefaultLogger(ILoggerFactory loggerFactory) {
			// check argument
			if (loggerFactory == null) {
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			// create a logger and add it to the default logger
			ILogger logger = loggerFactory.CreateLogger(LogCategoryName);
			AddToDefaultLogger(logger);
			return logger;
		}

		public static bool RemoveFromDefaultLogger(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// remove the logger from the default logger
			return defaultLogger.RemoveTargetLogger(logger);
		}


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

		public static void Log(ILogger logger, LogLevel logLevel, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			logger.Log(logLevel, eventId, FormatLogMessage(header, message), exception, FormatLog);
		}

		public static void LogTrace(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Trace, header, message, exception, eventId);
		}

		public static void LogDebug(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Debug, header, message, exception, eventId);
		}

		public static void LogInformation(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Information, header, message, exception, eventId);
		}

		public static void LogWarning(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Warning, header, message, exception, eventId);
		}

		public static void LogError(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Error, header, message, exception, eventId);
		}

		public static void LogCritical(ILogger logger, string? header, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Critical, header, message, exception, eventId);
		}

		#endregion
	}
}
