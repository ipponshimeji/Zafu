using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Logging {
	public static class LoggingUtil {
		#region types

		public class LogPropertyNames {
			public const string Message = "message";
			public const string Source = "source";
		}

		#endregion


		#region properties

		public static ILogger DefaultLogger => ZafuEnvironment.DefaultRunningContext.Logger;

		public static ILogger NullLogger => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

		#endregion


		#region methods

		public static string GetSimpleState(string? source, string? message) {
			// TODO: JSON escape
			return $"{{\"{LogPropertyNames.Source}\": \"{source}\", \"{LogPropertyNames.Message}\": \"{message}\"}}";
		}

		/// <summary>
		/// The default method which can be specified to 'formatter' argument of ILogger.Log().
		/// </summary>
		/// <param name="state"></param>
		/// <param name="exception"></param>
		/// <returns></returns>
		public static string DefaultFormatter<TState>(TState state, Exception? exception) {
			if (state is null) {
				return string.Empty;
			} else {
				return state.ToString() ?? string.Empty;
			}
		}

		public static void Log(ILogger? logger, LogLevel logLevel, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (logger == null) {
				// nothing to do
				return;
			}

			// TODO: support structured logging
			try {
				logger.Log<string>(logLevel, eventId, GetSimpleState(source, message), exception, DefaultFormatter<string>);
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
