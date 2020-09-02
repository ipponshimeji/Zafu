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

			public readonly string? Source;

			public readonly string? Message;

			public readonly Exception? Exception;

			public readonly EventId EventId;

			public readonly string? ExpectedMessage;

			#endregion


			#region constructor

			public LogSample(string? source, string? message, Exception? exception, EventId eventId, string? expectedMessage) {
				// initialize members
				this.Source = source;
				this.Message = message;
				this.Exception = exception;
				this.EventId = eventId;
				this.ExpectedMessage = expectedMessage;
			}

			#endregion


			#region overrides

			public override string ToString() {
				string source = TestingUtil.GetDisplayText(this.Source, quote: true);
				string message = TestingUtil.GetDisplayText(this.Message, quote: true);
				string exception = TestingUtil.GetNullOrNonNullText(this.Exception);

				return $"{{source: {source}, message: {message}, exception: {exception}, eventId: {this.EventId}}}";
			}

			#endregion


			#region methods

			public void AssertLog(LogLevel expectedLogLevel, object? expectedFormatter, SingleLogLogger logger) {
				// check argument
				if (logger == null) {
					throw new ArgumentNullException(nameof(logger));
				}

				// assert
				string expectedState = LoggingUtil.GetSimpleState(this.Source, this.Message);
				LogData? actual = logger.Data;
				
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(string), actual.StateType);
				Assert.Equal(expectedLogLevel, actual.LogLevel);
				Assert.Equal(this.EventId, actual.EventId);
				Assert.Equal(expectedState, actual.State);
				Assert.Equal(this.Exception, actual.Exception);
				Assert.Equal(expectedFormatter, actual.Formatter);
			}

			public void AssertLog(LogLevel expectedLogLevel, SingleLogLogger actualLog) {
				AssertLog(expectedLogLevel, (Func<string, Exception?, string>)LoggingUtil.DefaultFormatter<string>, actualLog);
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
				SingleLogLogger actualLog = new SingleLogLogger();
				CallTarget(actualLog, sample.Source, sample.Message, sample.Exception, sample.EventId);

				// assert
				sample.AssertLog(this.LogLevel, actualLog);
			}

			[Fact(DisplayName = "default arguments")]
			public void DefaultArguments() {
				// arrange

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTargetOmittingArguments(logger, "name", "content");

				// assert
				LogData? actual = logger.Data;
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
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

			protected abstract void CallTarget(ILogger ?logger, string? source, string? message, Exception? exception, EventId eventId);

			protected abstract void CallTargetOmittingArguments(ILogger? logger, string? source, string? message);

			#endregion
		}

		#endregion


		#region samples

		public static IEnumerable<object[]> GetLogSamples() {
			return new LogSample[] {
				//                  (source, message, exception, eventId, expectedMessage)
				new LogSample("name", "log content", null, default(EventId), "{\"source\": \"name\", \"message\": \"log conent\"}"),
				new LogSample(null, "log content", null, default(EventId), "{\"source\": \"\", \"message\": \"log conent\"}"),
				new LogSample("name", null, null, default(EventId), "{\"source\": \"\", \"message\": \"\"}"),
				new LogSample("name", "log content", new NotSupportedException(), default(EventId), "{\"source\": \"name\", \"message\": \"log conent\"}"),
				new LogSample("name", "log content", null, new EventId(32, "test event"), "{\"source\": \"name\", \"message\": \"log conent\"}")
			}.ToTestData();
		}

		#endregion


		#region DefaultLogger, AddToDefaultLogger/RemoveFromDefaultLogger

		// TODO: implement

		#endregion


		#region GetSimpleState

		public class GetSimpleState {
			#region types

			public class LogMessageSample {
				#region data

				public readonly string? Source;

				public readonly string? Message;

				public readonly string? ExpectedMessage;

				#endregion


				#region constructor

				public LogMessageSample(string? source, string? message, string? expectedMessage) {
					// initialize members
					this.Source = source;
					this.Message = message;
					this.ExpectedMessage = expectedMessage;
				}

				#endregion


				#region overrides

				public override string ToString() {
					string source = TestingUtil.GetDisplayText(this.Source, quote: true);
					string message = TestingUtil.GetDisplayText(this.Message, quote: true);

					return $"{{source: {source}, message: {message}}}";
				}

				#endregion
			}

			#endregion


			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return new LogMessageSample[] {
					//                  (source, message, expectedMessage)
					new LogMessageSample("name", "log content", "{\"source\": \"name\", \"message\": \"log content\"}"),
					new LogMessageSample("name", "", "{\"source\": \"name\", \"message\": \"\"}"),
					new LogMessageSample("name", null, "{\"source\": \"name\", \"message\": \"\"}"),
					new LogMessageSample("", "log content", "{\"source\": \"\", \"message\": \"log content\"}"),
					new LogMessageSample("", "", "{\"source\": \"\", \"message\": \"\"}"),
					new LogMessageSample("", null, "{\"source\": \"\", \"message\": \"\"}"),
					new LogMessageSample(null, "log content", "{\"source\": \"\", \"message\": \"log content\"}"),
					new LogMessageSample(null, "", "{\"source\": \"\", \"message\": \"\"}"),
					new LogMessageSample(null, null, "{\"source\": \"\", \"message\": \"\"}")
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
				string actual = LoggingUtil.GetSimpleState(sample.Source, sample.Message);

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
				Exception exception = new InvalidOperationException("something wrong.");

				// act
				string actual = LoggingUtil.DefaultFormatter<string>(message, exception);

				// assert
				Assert.Equal(message, actual);
			}

			[Fact(DisplayName = "exception: null")]
			public void exception_null() {
				// arrange
				string message = "error";
				Exception? exception = null;

				// act
				string actual = LoggingUtil.DefaultFormatter<string>(message, exception);

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

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogTrace(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogTrace(logger, source, message);
			}

			#endregion
		}

		#endregion


		#region LogDebug

		public class LogDebug: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Debug;

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogDebug(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogDebug(logger, source, message);
			}

			#endregion
		}

		#endregion


		#region LogInformation

		public class LogInformation: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Information;

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogInformation(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogInformation(logger, source, message);
			}

			#endregion
		}

		#endregion


		#region LogWarning

		public class LogWarning: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Warning;

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogWarning(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogWarning(logger, source, message);
			}

			#endregion
		}

		#endregion


		#region LogError

		public class LogError: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Error;

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogError(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogError(logger, source, message);
			}

			#endregion
		}

		#endregion


		#region LogCritical

		public class LogCritical: LogTestBase {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Critical;

			protected override void CallTarget(ILogger? logger, string? source, string? message, Exception? exception, EventId eventId) {
				LoggingUtil.LogCritical(logger, source, message, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message) {
				LoggingUtil.LogCritical(logger, source, message);
			}

			#endregion
		}

		#endregion
	}
}
