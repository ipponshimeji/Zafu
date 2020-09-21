using System;
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

		/// <summary>
		/// The default method which can be specified to 'formatter' argument of <see cref="ILogger.Log{TState}"/>.
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

		/// <summary>
		/// The default method which can be specified to 'formatter' argument of <see cref="ILogger.Log{SimpleState}"/>.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="exception"></param>
		/// <returns></returns>
		public static string JsonFormatter(SimpleState state, Exception? exception) {
			// format to JSON representation
			return state.ToJson();
		}

		public static string JsonFormatter<T>(SimpleState<T> state, Exception? exception) {
			// format to JSON representation
			return state.ToJson();
		}

		public static void Log(ILogger? logger, LogLevel logLevel, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			// check argument
			if (logger == null) {
				// nothing to do
				return;
			}

			try {
				logger.Log<SimpleState>(logLevel, eventId, new SimpleState(source, message), exception, JsonFormatter);
			} catch (Exception) {
				// continue
			}
		}

		public static void LogTrace(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Trace, source, message, exception, eventId);
		}

		public static void LogDebug(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Debug, source, message, exception, eventId);
		}

		public static void LogInformation(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Information, source, message, exception, eventId);
		}

		public static void LogWarning(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Warning, source, message, exception, eventId);
		}

		public static void LogError(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Error, source, message, exception, eventId);
		}

		public static void LogCritical(ILogger? logger, string? source, string? message, Exception? exception = null, EventId eventId = default(EventId)) {
			Log(logger, LogLevel.Critical, source, message, exception, eventId);
		}

		#endregion
	}
}
