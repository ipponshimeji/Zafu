using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
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

				return $"source: {source}, message: {message}, exception: {exception}, eventId: {this.EventId}";
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
			public void GeneralArguments(LogSample sample) {
				// arrange
				Debug.Assert(sample != null);
				SimpleState expectedState = new SimpleState(sample.Source, sample.Message);

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTarget(logger, sample.Source, sample.Message, sample.Exception, sample.EventId);
				LogData? actual = logger.Data;

				// assert
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(SimpleState), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				Assert.Equal(sample.EventId, actual.EventId);
				Assert.Equal(expectedState, actual.State);
				Assert.Equal(sample.Exception, actual.Exception);
				Assert.Equal((Func<SimpleState, Exception?, string>)LoggingUtil.JsonFormatter, actual.Formatter);
			}

			[Fact(DisplayName = "default arguments")]
			public void DefaultArguments() {
				// arrange
				string source = "source";
				string message = "message";
				SimpleState expectedState = new SimpleState(source, message);

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTargetOmittingArguments(logger, source, message);
				LogData? actual = logger.Data;

				// assert
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(SimpleState), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				Assert.Equal(default(EventId), actual.EventId);		// should be the default value
				Assert.Equal(expectedState, actual.State);
				Assert.Null(actual.Exception);                      // should be the default value
				Assert.Equal((Func<SimpleState, Exception?, string>)LoggingUtil.JsonFormatter, actual.Formatter);
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
				//  LogSample(source, message, exception, eventId, expectedMessage)
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


		#region DefaultFormatter

		public class DefaultFormatter {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				Version state = new Version(1, 2, 3);
				Exception exception = new InvalidOperationException("something wrong.");
				string expected = state.ToString();

				// act
				string actual = LoggingUtil.DefaultFormatter<Version>(state, exception);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "state: null")]
			public void state_null() {
				// arrange
				Uri state = null!;
				Exception exception = new InvalidOperationException("something wrong.");
				string expected = string.Empty;

				// act
				string actual = LoggingUtil.DefaultFormatter<Uri>(state, exception);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "exception: null")]
			public void exception_null() {
				// arrange
				int state = -345;
				Exception? exception = null;
				string expected = state.ToString();

				// act
				string actual = LoggingUtil.DefaultFormatter<int>(state, exception);

				// assert
				Assert.Equal(expected, actual);
			}

			#endregion
		}

		#endregion


		#region JsonFormatter

		public class JsonFormatter {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				SimpleState state = new SimpleState("my object", "OK?");
				Exception exception = new InvalidOperationException("something wrong.");
				string expected = "{ \"source\": \"my object\", \"message\": \"OK?\" }";

				// act
				string actual = LoggingUtil.JsonFormatter(state, exception);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "escaped")]
			public void escaped() {
				// arrange
				// The contents of the state includes characters to be escaped in the JSON string.
				SimpleState state = new SimpleState("my \"object\"", "a\\b");
				Exception exception = new InvalidOperationException("something wrong.");
				string expected = "{ \"source\": \"my \\\"object\\\"\", \"message\": \"a\\\\b\" }";

				// act
				string actual = LoggingUtil.JsonFormatter(state, exception);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "exception: null")]
			public void exception_null() {
				// arrange
				SimpleState state = new SimpleState("my object", "OK?");
				Exception? exception = null;
				string expected = "{ \"source\": \"my object\", \"message\": \"OK?\" }";

				// act
				string actual = LoggingUtil.JsonFormatter(state, exception);

				// assert
				Assert.Equal(expected, actual);
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
