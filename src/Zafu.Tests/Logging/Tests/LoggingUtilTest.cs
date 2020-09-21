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

		public class LogSample: SampleBase {
			#region data

			public readonly string? Source;

			public readonly string? Message;

			public readonly Exception? Exception;

			public readonly EventId EventId;

			public readonly string? ExpectedMessage;

			#endregion


			#region constructor

			public LogSample(string description, string? source, string? message, Exception? exception, EventId eventId, string? expectedMessage): base(description) {
				// initialize members
				this.Source = source;
				this.Message = message;
				this.Exception = exception;
				this.EventId = eventId;
				this.ExpectedMessage = expectedMessage;
			}

			#endregion
		}

		public class LogSample<T>: LogSample {
			#region data

			public readonly string ExtraPropertyName;

			public readonly T ExtraPropertyValue;

			#endregion


			#region constructor

			public LogSample(string description, string? source, string? message, Exception? exception, EventId eventId, string? expectedMessage, string extraPropName, T extraPropValue):
			base(description, source, message, exception, eventId, expectedMessage) {
				// initialize members
				this.ExtraPropertyName = extraPropName;
				this.ExtraPropertyValue = extraPropValue;
			}

			#endregion
		}


		public abstract class LogTestBase {
			#region samples

			public static SimpleState GeneralSampleValue => SimpleStateTest.GeneralSampleValue;

			public static IEnumerable<LogSample> GetSamples() {
				return new LogSample[] {
					new LogSample(
						description: "general",
						source: "name",
						message: "log content",
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"message\": \"log conent\"}"
					),
					new LogSample(
						description: "source: null",
						source: null,
						message: "log content",
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"\", \"message\": \"log conent\"}"
					),
					new LogSample(
						description: "message: null",
						source: "name",
						message: null,
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"\", \"message\": \"\"}"
					),
					new LogSample(
						description: "exception: non-null",
						source: "name",
						message: "log content",
						exception: new NotSupportedException(),
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"message\": \"log conent\"}"
					),
					new LogSample(
						description: "eventId: non-default",
						source: "name",
						message: "log content",
						exception: null,
						eventId: new EventId(32, "test event"),
						expectedMessage: "{\"source\": \"name\", \"message\": \"log conent\"}"
					)
				};
			}

			public static IEnumerable<object[]> GetSampleData() {
				return GetSamples().ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general arguments")]
			[MemberData(nameof(GetSampleData))]
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
				SimpleState sample = GeneralSampleValue;

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTargetOmittingArguments(logger, sample.Source, sample.Message);
				LogData? actual = logger.Data;

				// assert
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(SimpleState), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				Assert.Equal(default(EventId), actual.EventId);		// should be the default value
				Assert.Equal(sample, actual.State);
				Assert.Null(actual.Exception);                      // should be the default value
				Assert.Equal((Func<SimpleState, Exception?, string>)LoggingUtil.JsonFormatter, actual.Formatter);
			}

			[Fact(DisplayName = "logger: null")]
			public void logger_null() {
				// arrange
				ILogger? logger = null;
				SimpleState sample = GeneralSampleValue;

				// act
				CallTarget(logger, sample.Source, sample.Message, null, default(EventId));

				// assert
				// Nothing should happen, no ArgumentNullException should be thrown.
				// LoggingUtil.LogX() methods do nothing if the logger argument is null. 
			}

			#endregion


			#region overridables

			protected abstract LogLevel LogLevel { get; }

			protected abstract void CallTarget(ILogger ?logger, string? source, string? message, Exception? exception, EventId eventId);

			protected abstract void CallTargetOmittingArguments(ILogger? logger, string? source, string? message);

			#endregion
		}

		public abstract class LogTestBase<T> {
			#region utilities

			protected void Test_GeneralArguments(LogSample<T> sample) {
				// arrange
				Debug.Assert(sample != null);
				SimpleState<T> expectedState = new SimpleState<T>(sample.Source, sample.Message, sample.ExtraPropertyName, sample.ExtraPropertyValue);

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTarget(logger, sample.Source, sample.Message, sample.ExtraPropertyName, sample.ExtraPropertyValue, sample.Exception, sample.EventId);
				LogData? actual = logger.Data;

				// assert
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(SimpleState<T>), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				Assert.Equal(sample.EventId, actual.EventId);
				Assert.Equal(expectedState, actual.State);
				Assert.Equal(sample.Exception, actual.Exception);
				Assert.Equal((Func<SimpleState<T>, Exception?, string>)LoggingUtil.JsonFormatter<T>, actual.Formatter);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "default arguments")]
			public void DefaultArguments() {
				// arrange
				SimpleState<T> sample = this.GeneralSampleValue;

				// act
				SingleLogLogger logger = new SingleLogLogger();
				CallTargetOmittingArguments(logger, sample.Source, sample.Message, sample.ExtraPropertyName, sample.ExtraPropertyValue);
				LogData? actual = logger.Data;

				// assert
				Assert.NotNull(actual); // actually logged?
				Debug.Assert(actual != null);
				Assert.Equal(typeof(SimpleState<T>), actual.StateType);
				Assert.Equal(this.LogLevel, actual.LogLevel);
				Assert.Equal(default(EventId), actual.EventId);     // should be the default value
				Assert.Equal(sample, actual.State);
				Assert.Null(actual.Exception);                      // should be the default value
				Assert.Equal((Func<SimpleState<T>, Exception?, string>)LoggingUtil.JsonFormatter<T>, actual.Formatter);
			}

			[Fact(DisplayName = "logger: null")]
			public void logger_null() {
				// arrange
				ILogger? logger = null;
				SimpleState<T> sample = this.GeneralSampleValue;

				// act
				CallTarget(logger, sample.Source, sample.Message, sample.ExtraPropertyName, sample.ExtraPropertyValue, null, default(EventId));

				// assert
				// Nothing should happen, no ArgumentNullException should be thrown.
				// LoggingUtil.LogX() methods do nothing if the logger argument is null. 
			}

			#endregion


			#region overridables

			protected abstract LogLevel LogLevel { get; }

			protected abstract SimpleState<T> GeneralSampleValue { get; }

			protected abstract void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, T extraPropValue, Exception? exception, EventId eventId);

			protected abstract void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, T extraPropValue);

			#endregion
		}

		public abstract class LogTestBase_Int32: LogTestBase<int> {
			#region samples

			public static IEnumerable<LogSample<int>> GetSamples() {
				return new LogSample<int>[] {
					new LogSample<int>(
						description: "general",
						source: "name",
						message: "log content",
						extraPropName: "index",
						extraPropValue: 45,
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"index\": 45, \"message\": \"log conent\"}"
					),
					new LogSample<int>(
						description: "exception: non-null",
						source: "name",
						message: "log content",
						extraPropName: "index",
						extraPropValue: 0,
						exception: new NotSupportedException(),
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"index\": 0, \"message\": \"log conent\"}"
					),
					new LogSample<int>(
						description: "eventId: non-default",
						source: "name",
						message: "log content",
						extraPropName: "index",
						extraPropValue: -1,
						exception: null,
						eventId: new EventId(32, "test event"),
						expectedMessage: "{\"source\": \"name\", \"index\": -1, \"message\": \"log conent\"}"
					)
				};
			}

			public static IEnumerable<object[]> GetSampleData() {
				return GetSamples().ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general arguments")]
			[MemberData(nameof(GetSampleData))]
			public void GeneralArguments(LogSample<int> sample) {
				Test_GeneralArguments(sample);
			}

			#endregion


			#region overrides

			protected override SimpleState<int> GeneralSampleValue => SimpleStateTTest.GeneralInt32SampleValue;

			#endregion
		}

		public abstract class LogTestBase_String: LogTestBase<string> {
			#region samples

			public static IEnumerable<LogSample<string>> GetSamples() {
				return new LogSample<string>[] {
					new LogSample<string>(
						description: "general",
						source: "name",
						message: "log content",
						extraPropName: "description",
						extraPropValue: "general",
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"description\": \"general\", \"message\": \"log conent\"}"
					),
					new LogSample<string>(
						description: "ExtraPropertyValue: null",
						source: "name",
						message: "log content",
						extraPropName: "description",
						extraPropValue: null!,
						exception: null,
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"description\": null, \"message\": \"log conent\"}"
					),
					new LogSample<string>(
						description: "exception: non-null",
						source: "name",
						message: "log content",
						extraPropName: "description",
						extraPropValue: "general",
						exception: new NotSupportedException(),
						eventId: default(EventId),
						expectedMessage: "{\"source\": \"name\", \"description\": \"general\", \"message\": \"log conent\"}"
					),
					new LogSample<string>(
						description: "eventId: non-default",
						source: "name",
						message: "log content",
						extraPropName: "description",
						extraPropValue: "general",
						exception: null,
						eventId: new EventId(32, "test event"),
						expectedMessage: "{\"source\": \"name\", \"description\": \"general\", \"message\": \"log conent\"}"
					)
				};
			}

			public static IEnumerable<object[]> GetSampleData() {
				return GetSamples().ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general arguments")]
			[MemberData(nameof(GetSampleData))]
			public void GeneralArguments(LogSample<string> sample) {
				Test_GeneralArguments(sample);
			}

			#endregion


			#region overrides

			protected override SimpleState<string> GeneralSampleValue => SimpleStateTTest.GeneralStringSampleValue;

			#endregion
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
				// The exception argument does not affects the result in this implementation.

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

		public class JsonFormatterTestBase<T> {
			#region tests

			protected void Test_General(SimpleStateTTest.Sample<T> sample) {
				// The exception argument does not affects the result in this implementation.

				// arrange
				SimpleState<T> state = sample.Value;
				Exception exception = new InvalidOperationException("something wrong.");

				// act
				string actual = LoggingUtil.JsonFormatter<T>(state, exception);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			protected void Test_exception_null(SimpleStateTTest.Sample<T> sample) {
				// The exception argument does not affects the result in this implementation.

				// arrange
				SimpleState<T> state = sample.Value;
				Exception? exception = null;

				// act
				string actual = LoggingUtil.JsonFormatter<T>(state, exception);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			#endregion
		}

		public class JsonFormatter {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTest.GetSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void general(SimpleStateTest.Sample sample) {
				// The exception argument does not affects the result in this implementation.

				// arrange
				SimpleState state = sample.Value;
				Exception exception = new InvalidOperationException("something wrong.");

				// act
				string actual = LoggingUtil.JsonFormatter(state, exception);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "exception: null")]
			[MemberData(nameof(GetSampleData))]
			public void exception_null(SimpleStateTest.Sample sample) {
				// The exception argument does not affects the result in this implementation.

				// arrange
				SimpleState state = sample.Value;
				Exception? exception = null;

				// act
				string actual = LoggingUtil.JsonFormatter(state, exception);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			#endregion
		}

		public class JsonFormatter_Int32: JsonFormatterTestBase<int> {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetInt32SampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void General(SimpleStateTTest.Sample<int> sample) {
				Test_General(sample);
			}

			[Theory(DisplayName = "exception: null")]
			[MemberData(nameof(GetSampleData))]
			public void exception_null(SimpleStateTTest.Sample<int> sample) {
				Test_exception_null(sample);
			}

			#endregion
		}

		public class JsonFormatter_String: JsonFormatterTestBase<string> {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetStringSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void General(SimpleStateTTest.Sample<string> sample) {
				Test_General(sample);
			}

			[Theory(DisplayName = "exception: null")]
			[MemberData(nameof(GetSampleData))]
			public void exception_null(SimpleStateTTest.Sample<string> sample) {
				Test_exception_null(sample);
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

		public class LogTrace_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Trace;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogTrace(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogTrace(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogTrace_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Trace;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogTrace(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogTrace(logger, source, message, extraPropName, extraPropValue);
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

		public class LogDebug_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Debug;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogDebug(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogDebug(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogDebug_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Debug;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogDebug(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogDebug(logger, source, message, extraPropName, extraPropValue);
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

		public class LogInformation_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Information;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogInformation(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogInformation(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogInformation_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Information;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogInformation(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogInformation(logger, source, message, extraPropName, extraPropValue);
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

		public class LogWarning_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Warning;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogWarning(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogWarning(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogWarning_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Warning;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogWarning(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogWarning(logger, source, message, extraPropName, extraPropValue);
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

		public class LogError_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Error;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogError(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogError(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogError_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Error;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogError(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogError(logger, source, message, extraPropName, extraPropValue);
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

		public class LogCritical_Int32: LogTestBase_Int32 {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Critical;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogCritical(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, int extraPropValue) {
				LoggingUtil.LogCritical(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		public class LogCritical_String: LogTestBase_String {
			#region overridables

			protected override LogLevel LogLevel => LogLevel.Critical;

			protected override void CallTarget(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue, Exception? exception, EventId eventId) {
				LoggingUtil.LogCritical(logger, source, message, extraPropName, extraPropValue, exception, eventId);
			}

			protected override void CallTargetOmittingArguments(ILogger? logger, string? source, string? message, string extraPropName, string extraPropValue) {
				LoggingUtil.LogCritical(logger, source, message, extraPropName, extraPropValue);
			}

			#endregion
		}

		#endregion
	}
}
