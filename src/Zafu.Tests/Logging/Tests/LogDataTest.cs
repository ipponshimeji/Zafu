using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Testing.Logging;

namespace Zafu.Logging.Tests {
	public class LogDataTest {
		#region types

		// The derived class from LogEntry to expose GetLogMethodInfo() and GetLogArguments() as public.
		class TestingLogEntry: LogData {
			#region creation

			public TestingLogEntry(LogData src) : base(src) {
			}

			#endregion


			#region methods

			public new MethodInfo GetLogMethodInfo() {
				return base.GetLogMethodInfo();
			}

			public new object?[] GetLogArguments() {
				return base.GetLogArguments();
			}

			#endregion
		}

		#endregion


		#region samples

		public static LogData Sample = new LogData(typeof(Version), LogLevel.Information, new EventId(51, "test"), new Version(1, 2), new ApplicationException(), (Func<Version?, Exception?, string>)VersionFormatter);

		private static string VersionFormatter(Version? state, Exception? exception) {
			return (state != null) ? state.ToString() : string.Empty;
		}

		private static string UriFormatter(Uri? state, Exception? exception) {
			return (state != null) ? state.ToString() : string.Empty;
		}

		#endregion


		#region constructor

		public class Constructor {
			[Fact(DisplayName = "general constructor; general")]
			public void General_general() {
				// arrange
				Type stateType = typeof(Uri);
				LogLevel logLevel = LogLevel.Warning;
				EventId eventId = new EventId(123, "warning event");
				object? state = new Uri("http://example.org/");
				Exception? exception = new SystemException();
				Delegate formatter = (Func<Uri?, Exception?, string>)UriFormatter;

				// act
				LogData actual = new LogData(stateType, logLevel, eventId, state, exception, formatter);

				// assert
				Assert.Equal(stateType, actual.StateType);
				Assert.Equal(logLevel, actual.LogLevel);
				Assert.Equal(eventId, actual.EventId);
				Assert.Equal(state, actual.State);
				Assert.Equal(exception, actual.Exception);
				Assert.Equal(formatter, actual.Formatter);
			}

			[Fact(DisplayName = "general constructor; stateType: null")]
			public void General_stateType_null() {
				// arrange
				Type stateType = null!;
				LogLevel logLevel = LogLevel.Warning;
				EventId eventId = new EventId(123, "warning event");
				object? state = new Uri("http://example.org/");
				Exception? exception = new SystemException();
				Delegate formatter = (Func<Uri?, Exception?, string>)UriFormatter;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new LogData(stateType, logLevel, eventId, state, exception, formatter);
				});

				// assert
				Assert.Equal("stateType", actual.ParamName);
			}

			[Fact(DisplayName = "copy constructor; general")]
			public void Copy_general() {
				// arrange
				LogData src = Sample;

				// act
				LogData actual = new LogData(src);

				// assert
				Assert.Equal(src.StateType, actual.StateType);
				Assert.Equal(src.LogLevel, actual.LogLevel);
				Assert.Equal(src.EventId, actual.EventId);
				Assert.Equal(src.State, actual.State);
				Assert.Equal(src.Exception, actual.Exception);
				Assert.Equal(src.Formatter, actual.Formatter);
			}

			[Fact(DisplayName = "copy constructor; src: null")]
			public void Copy_src_null() {
				// arrange
				LogData src = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new LogData(src);
				});

				// assert
				Assert.Equal("src", actual.ParamName);
			}
		}

		#endregion


		#region comparison

		public class Comparison {
			#region utilities

			protected void Test(LogData? x, LogData? y, bool expected) {
				// act (operators)
				bool actual_equality = (x == y);
				bool actual_inequality = (x != y);

				// assert (operators)
				Assert.Equal(expected, actual_equality);
				Assert.Equal(!expected, actual_inequality);

				if (object.ReferenceEquals(x, null) == false) {
					// act (Equal methods)
					bool actual_equal = x.Equals(y);
					bool actual_objectEqual = x.Equals((object?)y);

					// assert (Equal methods)
					Assert.Equal(expected, actual_equal);
					Assert.Equal(expected, actual_objectEqual);
				}
			}

			#endregion


			#region tests

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				LogData x = Sample;
				// create a clone not to equal to x as a reference 
				LogData y = new LogData(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "same; null")]
			public void same_null() {
				// arrange
				LogData? x = null;
				LogData? y = null;

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "different; null")]
			public void different_null() {
				// act & assert
				// test (Sample == null), (Sample != null), Sample.Equals((LogEntry?)null), and Sample.Equals((object?)null)
				Test(Sample, null, expected: false);
				// test (null == Sample) and (null != Sample)
				Test(null, Sample, expected: false);
			}

			[Fact(DisplayName = "different; StateType")]
			public void different_StateType() {
				// arrange
				LogData x = Sample;
				// different from x only at StateType
				Type differentValue = typeof(Uri);
				Debug.Assert(differentValue != x.StateType);
				LogData y = new LogData(differentValue, x.LogLevel, x.EventId, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; LogLevel")]
			public void different_LogLevel() {
				// arrange
				LogData x = Sample;
				// different from x only at LogLevel
				LogLevel differentValue = LogLevel.Trace;
				Debug.Assert(differentValue != x.LogLevel);
				LogData y = new LogData(x.StateType, differentValue, x.EventId, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; EventId")]
			public void different_EventId() {
				// arrange
				LogData x = Sample;
				// different from x only at EventId
				EventId differentValue = new EventId(99);
				Debug.Assert(differentValue != x.EventId);
				LogData y = new LogData(x.StateType, x.LogLevel, differentValue, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; State")]
			public void different_State() {
				// arrange
				LogData x = Sample;
				// different from x only at State
				object differentValue = new Version(9, 5);
				Debug.Assert(differentValue != x.State);
				LogData y = new LogData(x.StateType, x.LogLevel, x.EventId, differentValue, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; Exception")]
			public void different_Exception() {
				// arrange
				LogData x = Sample;
				// different from x only at Exception
				Exception differentValue = new NotImplementedException();
				Debug.Assert(differentValue != x.Exception);
				LogData y = new LogData(x.StateType, x.LogLevel, x.EventId, x.State, differentValue, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; Formatter")]
			public void different_Formatter() {
				// arrange
				LogData x = Sample;
				// different from x only at Formatter
				Delegate differentValue = (Func<Uri?, Exception?, string>)UriFormatter;
				Debug.Assert(differentValue != x.Formatter);
				LogData y = new LogData(x.StateType, x.LogLevel, x.EventId, x.State, x.Exception, differentValue);

				// act & assert
				Test(x, y, expected: false);
			}

			#endregion
		}

		#endregion


		#region GetHashCode

		public class HashCode {
			[Fact(DisplayName = "same")]
			public void same() {
				// arrange
				LogData x = Sample;
				// create a clone not to equal to x as a reference 
				LogData y = new LogData(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.Equal(actual_x, actual_y);
			}

			[Fact(DisplayName = "different")]
			public void different() {
				// arrange
				LogData x = Sample;
				LogData y = new LogData(typeof(Uri), LogLevel.Critical, default(EventId), null, null, null);

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.NotEqual(actual_x, actual_y);
			}
		}

		#endregion


		#region GetMessage

		public class GetMessage {
			#region utilities

			/// <summary>
			/// Returns a LogEntry instance which is different from src only at Formatter.
			/// </summary>
			/// <param name="formatter"></param>
			/// <returns></returns>
			protected static LogData GetSample(LogData src, Delegate? formatter) {
				return new LogData(src.StateType, src.LogLevel, src.EventId, src.State, src.Exception, formatter);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				LogData sample = Sample;

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Equal("1.2", actual);
			}

			[Fact(DisplayName = "Formatter: null")]
			public void Formatter_null() {
				// arrange
				LogData sample = GetSample(Sample, null);

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Null(actual);
			}

			[Fact(DisplayName = "Formatter: custom")]
			public void Formatter_custom() {
				// arrange
				Func<Version?, Exception?, string> formatter = (var, e) => "OK?";
				LogData sample = GetSample(Sample, formatter);

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Equal("OK?", actual);
			}

			#endregion
		}

		#endregion


		#region LogTo

		public class LogTo {
			[Fact(DisplayName = "single; general")]
			public void single_general() {
				// arrange
				LogData sample = Sample;
				SingleEntryLogger logger = new SingleEntryLogger();
				Debug.Assert(logger.Logged == false);

				// act
				sample.LogTo(logger);

				// assert
				Assert.Equal(sample, logger.Data);
			}

			[Fact(DisplayName = "single; logger: null")]
			public void single_logger_null() {
				// arrange
				LogData sample = Sample;
				ILogger logger = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					sample.LogTo(logger);
				});

				// assert
				Assert.Equal("logger", actual.ParamName);
			}

			[Fact(DisplayName = "multiple; general")]
			public void multiple_general() {
				// arrange
				LogData sample = Sample;
				SingleEntryLogger logger1 = new SingleEntryLogger();
				Debug.Assert(logger1.Logged == false);
				SingleEntryLogger logger2 = new SingleEntryLogger();
				Debug.Assert(logger2.Logged == false);
				// Note that null logger should be skipped without any exceptioin.
				ILogger?[] loggers = new ILogger?[] { logger1, null, logger2 };

				// act
				sample.LogTo(loggers);

				// assert
				Assert.Equal(sample, logger1.Data);
				Assert.Equal(sample, logger2.Data);
			}

			[Fact(DisplayName = "multiple; loggers: null")]
			public void multiple_loggers_null() {
				// arrange
				LogData sample = Sample;
				IEnumerable<ILogger> loggers = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					sample.LogTo(loggers);
				});

				// assert
				Assert.Equal("loggers", actual.ParamName);
			}
		}

		#endregion


		#region GetLogMethodInfo

		public class GetLogMethodInfo {
			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				// Use wrapper class (TestingLogEntry) to access protected methods of LogEntry. 
				TestingLogEntry sample = new TestingLogEntry(Sample);

				// act
				MethodInfo actual = sample.GetLogMethodInfo();
				string str = actual.ToString();

				// assert
				Assert.Equal("Void Log[Version](Microsoft.Extensions.Logging.LogLevel, Microsoft.Extensions.Logging.EventId, System.Version, System.Exception, System.Func`3[System.Version,System.Exception,System.String])", actual.ToString());
			}
		}

		#endregion


		#region GetLogArguments

		public class GetLogArguments {
			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				// Use wrapper class (TestingLogEntry) to access protected methods of LogEntry. 
				TestingLogEntry sample = new TestingLogEntry(Sample);
				object? expected = new object?[] {
					sample.LogLevel, sample.EventId, sample.State, sample.Exception, sample.Formatter
				};

				// act
				object?[] actual = sample.GetLogArguments();

				// assert
				Assert.Equal(expected, actual);
			}
		}

		#endregion
	}
}
