using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Testing;
using Zafu.Testing.Logging;

namespace Zafu.Logging.Tests {
	public class LoggingUtilTest {
		#region types

		public class LogSample {
			#region data

			public readonly string? Header;

			public readonly string? Message;

			public readonly Exception? Exception;

			public readonly EventId EventId;

			public readonly string? ExpectedMessage;

			#endregion


			#region constructor

			public LogSample(string? header, string? message, Exception? exception, EventId eventId, string? expectedMessage) {
				// initialize members
				this.Header = header;
				this.Message = message;
				this.Exception = exception;
				this.EventId = eventId;
				this.ExpectedMessage = expectedMessage;
			}

			#endregion


			#region overrides

			public override string ToString() {
				string header = TestingUtil.GetDisplayText(this.Header, quote: true);
				string message = TestingUtil.GetDisplayText(this.Message, quote: true);
				string exception = TestingUtil.GetNullOrNonNullText(this.Exception);

				return $"{{header: {header}, message: {message}, exception: {exception}, eventId: {this.EventId}}}";
			}

			#endregion


			#region methods

			public void AssertLog(LogLevel expectedLogLevel, object? expectedFormatter, SingleEntryLogger actual) {
				// check argument
				if (actual == null) {
					throw new ArgumentNullException(nameof(actual));
				}

				// assert
				string expectedState = LoggingUtil.FormatLogMessage(this.Header, this.Message);

				Assert.True(actual.Logged); // actually logged?
				Assert.Equal(typeof(string), actual.StateType);
				Assert.Equal(expectedLogLevel, actual.LogLevel);
				Assert.Equal(this.EventId, actual.EventId);
				Assert.Equal(expectedState, actual.State);
				Assert.Equal(this.Exception, actual.Exception);
				Assert.Equal(expectedFormatter, actual.Formatter);
			}

			public void AssertLog(LogLevel expectedLogLevel, SingleEntryLogger actualLog) {
				AssertLog(expectedLogLevel, (Func<string, Exception?, string>)LoggingUtil.FormatLog, actualLog);
			}

			#endregion
		}


		public abstract class LogTestBase {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return GetLogSamples();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general arguments")]
			[MemberData(nameof(GetSamples))]
			public void Arguments(LogSample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				SingleEntryLogger actualLog = new SingleEntryLogger();
				CallTarget(actualLog, sample.Header, sample.Message, sample.Exception, sample.EventId);

				// assert
				sample.AssertLog(this.LogLevel, actualLog);
			}

			[Fact(DisplayName = "default arguments")]
			public void DefaultArguments() {
				// arrange

				// act
				SingleEntryLogger actual = new SingleEntryLogger();
				CallTargetOmittingArguments(actual, "name", "content");

				// assert
				Assert.True(actual.Logged); // actually logged?
				Assert.Equal(typeof(string), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				// logged the default values?
				Assert.Equal(default(EventId), actual.EventId);
				Assert.Null(actual.Exception);
			}

			[Fact(DisplayName = "logger: null")]
			public void logger_null() {
				// arrange
				ILogger? logger = null;

				// act
				CallTarget(logger, "name", "content", null, default(EventId));

				// assert
				// nothing should happen, no ArgumentNullException should be thrown
				// LoggingUtil.LogX() methods do nothing if the logger argument is null. 
			}

			#endregion


			#region overridables

			protected abstract LogLevel LogLevel { get; }

			protected abstract void CallTarget(ILogger ?logger, string? header, string? message, Exception? exception, EventId eventId);

			protected abstract void CallTargetOmittingArguments(ILogger? logger, string? header, string? message);

			#endregion
		}

		#endregion


		#region samples

		public static IEnumerable<object[]> GetLogSamples() {
			return new LogSample[] {
				//                  (header, message, exception, eventId, expectedMessage)
				new LogSample("name", "log content", null, default(EventId), "[name] log content"),
				new LogSample(null, "log content", null, default(EventId), "log content"),
				new LogSample("name", null, null, default(EventId), "[name] "),
				new LogSample("name", "log content", new ApplicationException(), default(EventId), "[name] log content"),
				new LogSample("name", "log content", null, new EventId(32, "test event"), "[name] log content")
			}.ToTestData();
		}

		#endregion


		#region DefaultLogger, AddToDefaultLogger/RemoveFromDefaultLogger

		// TODO: implement

		#endregion


		#region FormatLogMessage

		public class FormatLogMessage {
			#region types

			public class LogMessageSample {
				#region data

				public readonly string? Header;

				public readonly string? Message;

				public readonly string? ExpectedMessage;

				#endregion


				#region constructor

				public LogMessageSample(string? header, string? message, string? expectedMessage) {
					// initialize members
					this.Header = header;
					this.Message = message;
					this.ExpectedMessage = expectedMessage;
				}

				#endregion


				#region overrides

				public override string ToString() {
					string header = TestingUtil.GetDisplayText(this.Header, quote: true);
					string message = TestingUtil.GetDisplayText(this.Message, quote: true);

					return $"{{header: {header}, message: {message}}}";
				}

				#endregion
			}

			#endregion


			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return new LogMessageSample[] {
					//                  (header, message, expectedMessage)
					new LogMessageSample("name", "log content", "[name] log content"),
					new LogMessageSample("name", "", "[name] "),
					new LogMessageSample("name", null, "[name] "),
					new LogMessageSample("", "log content", "log content"),
					new LogMessageSample("", "", ""),
					new LogMessageSample("", null, ""),
					new LogMessageSample(null, "log content", "log content"),
					new LogMessageSample(null, "", ""),
					new LogMessageSample(null, null, "")
				}.ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general arguments")]
			[MemberData(nameof(GetSamples))]
			public void Arguments(LogMessageSample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				string actual = LoggingUtil.FormatLogMessage(sample.Header, sample.Message);

				// assert
				Assert.Equal(sample.ExpectedMessage, actual);
			}

			#endregion
		}

		#endregion


		#region FormatLog

		public class FormatLog {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				string message = "error";
				Exception exception = new ApplicationException("something wrong.");

				// act
				string actual = LoggingUtil.FormatLog(message, exception);

				// assert
				Assert.Equal(message, actual);
			}

			[Fact(DisplayName = "exception: null")]
			public void exception_null() {
				// arrange
				string message = "error";
				Exception? exception = null;

				// act
				string actual = LoggingUtil.FormatLog(message, exception);

				// assert
				Assert.Equal(message, actual);
			}

			#endregion
		}

		#endregion


		#region Log

		// LoggingUtil.Log() is tested by tests for LogTrace, LogDebug, etc.

		#endregion


		#region LogTrace

		public class LogTrace: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Trace;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogTrace(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogTrace(logger, header, message);
			}

			#endregion
		}

		#endregion


		#region LogDebug

		public class LogDebug: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Debug;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogDebug(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogDebug(logger, header, message);
			}

			#endregion
		}

		#endregion


		#region LogInformation

		public class LogInformation: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Information;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogInformation(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogInformation(logger, header, message);
			}

			#endregion
		}

		#endregion


		#region LogWarning

		public class LogWarning: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Warning;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogWarning(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogWarning(logger, header, message);
			}

			#endregion
		}

		#endregion


		#region LogError

		public class LogError: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Error;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogError(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogError(logger, header, message);
			}

			#endregion
		}

		#endregion


		#region LogCritical

		public class LogCritical: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Critical;

			protected override void CallTarget(ILogger? logger, string? header, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogCritical(logger, header, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? header, string? message) {
				LoggingUtil.LogCritical(logger, header, message);
			}

			#endregion
		}

		#endregion
	}
}
