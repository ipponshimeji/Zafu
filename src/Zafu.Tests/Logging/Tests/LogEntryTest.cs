﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Testing.Logging;

namespace Zafu.Logging.Tests {
	public class LogEntryTest {
		#region samples

		public static LogEntry Sample = new LogEntry(typeof(Version), LogLevel.Information, new EventId(51, "test"), new Version(1, 2), new ApplicationException(), (Func<Version?, Exception?, string>)VersionFormatter);

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
				LogEntry actual = new LogEntry(stateType, logLevel, eventId, state, exception, formatter);

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
					new LogEntry(stateType, logLevel, eventId, state, exception, formatter);
				});

				// assert
				Assert.Equal("stateType", actual.ParamName);
			}

			[Fact(DisplayName = "copy constructor; general")]
			public void Copy_general() {
				// arrange
				LogEntry src = Sample;

				// act
				LogEntry actual = new LogEntry(src);

				// assert
				Assert.Equal(src.StateType, actual.StateType);
				Assert.Equal(src.LogLevel, actual.LogLevel);
				Assert.Equal(src.EventId, actual.EventId);
				Assert.Equal(src.State, actual.State);
				Assert.Equal(src.Exception, actual.Exception);
				Assert.Equal(src.Formatter, actual.Formatter);
			}


			[Fact(DisplayName = "copy constructor; src: null")]
			public void Copy_stateType_null() {
				// arrange
				LogEntry src = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new LogEntry(src);
				});

				// assert
				Assert.Equal("src", actual.ParamName);
			}
		}

		#endregion


		#region comparison

		public class Comparison {
			protected void Test(LogEntry? x, LogEntry? y, bool expected) {
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

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				LogEntry x = Sample;
				// create a clone not to equal to x as a reference 
				LogEntry y = new LogEntry(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "same; null")]
			public void same_null() {
				// arrange
				LogEntry? x = null;
				LogEntry? y = null;

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
				LogEntry x = Sample;
				// different from x only at StateType
				Type differentValue = typeof(Uri);
				Debug.Assert(differentValue != x.StateType);
				LogEntry y = new LogEntry(differentValue, x.LogLevel, x.EventId, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; LogLevel")]
			public void different_LogLevel() {
				// arrange
				LogEntry x = Sample;
				// different from x only at LogLevel
				LogLevel differentValue = LogLevel.Trace;
				Debug.Assert(differentValue != x.LogLevel);
				LogEntry y = new LogEntry(x.StateType, differentValue, x.EventId, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; EventId")]
			public void different_EventId() {
				// arrange
				LogEntry x = Sample;
				// different from x only at EventId
				EventId differentValue = new EventId(99);
				Debug.Assert(differentValue != x.EventId);
				LogEntry y = new LogEntry(x.StateType, x.LogLevel, differentValue, x.State, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; State")]
			public void different_State() {
				// arrange
				LogEntry x = Sample;
				// different from x only at State
				object differentValue = new Version(9, 5);
				Debug.Assert(differentValue != x.State);
				LogEntry y = new LogEntry(x.StateType, x.LogLevel, x.EventId, differentValue, x.Exception, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; Exception")]
			public void different_Exception() {
				// arrange
				LogEntry x = Sample;
				// different from x only at Exception
				Exception differentValue = new NotImplementedException();
				Debug.Assert(differentValue != x.Exception);
				LogEntry y = new LogEntry(x.StateType, x.LogLevel, x.EventId, x.State, differentValue, x.Formatter);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; Formatter")]
			public void different_Formatter() {
				// arrange
				LogEntry x = Sample;
				// different from x only at Formatter
				Delegate differentValue = (Func<Uri?, Exception?, string>)UriFormatter;
				Debug.Assert(differentValue != x.Formatter);
				LogEntry y = new LogEntry(x.StateType, x.LogLevel, x.EventId, x.State, x.Exception, differentValue);

				// act & assert
				Test(x, y, expected: false);
			}
		}

		#endregion


		#region GetHashCode

		public class HashCode {
			[Fact(DisplayName = "same")]
			public void same() {
				// arrange
				LogEntry x = Sample;
				// create a clone not to equal to x as a reference 
				LogEntry y = new LogEntry(x);
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
				LogEntry x = Sample;
				LogEntry y = new LogEntry(typeof(Uri), LogLevel.Critical, default(EventId), null, null, null);

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
			/// <summary>
			/// Returns a LogEntry instance which is different from src only at Formatter.
			/// </summary>
			/// <param name="formatter"></param>
			/// <returns></returns>
			protected static LogEntry GetSample(LogEntry src, Delegate formatter) {
				return new LogEntry(src.StateType, src.LogLevel, src.EventId, src.State, src.Exception, formatter);
			}


			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				LogEntry sample = Sample;

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Equal("1.2", actual);
			}

			[Fact(DisplayName = "Formatter: null")]
			public void Formatter_null() {
				// arrange
				LogEntry sample = GetSample(Sample, null);

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Null(actual);
			}

			[Fact(DisplayName = "Formatter: custom")]
			public void Formatter_custom() {
				// arrange
				Func<Version?, Exception?, string> formatter = (var, e) => "OK?";
				LogEntry sample = GetSample(Sample, formatter);

				// act
				string? actual = sample.GetMessage();

				// assert
				Assert.Equal("OK?", actual);
			}
		}

		#endregion


		#region LogTo

		public class LogTo {
			[Fact(DisplayName = "single; general")]
			public void single_general() {
				// arrange
				LogEntry sample = Sample;
				SingleEntryLogger actual = new SingleEntryLogger();
				Debug.Assert(actual.Logged == false);

				// act
				sample.LogTo(actual);

				// assert
				Assert.True(actual.Logged);
				Assert.Equal(sample.StateType, actual.StateType);
				Assert.Equal(sample.LogLevel, actual.LogLevel);
				Assert.Equal(sample.EventId, actual.EventId);
				Assert.Equal(sample.State, actual.State);
				Assert.Equal(sample.Exception, actual.Exception);
				Assert.Equal(sample.Formatter, actual.Formatter);
			}

			[Fact(DisplayName = "single; logger: null")]
			public void single_logger_null() {
				// arrange
				LogEntry sample = Sample;
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
				LogEntry sample = Sample;
				SingleEntryLogger actual1 = new SingleEntryLogger();
				Debug.Assert(actual1.Logged == false);
				SingleEntryLogger actual2 = new SingleEntryLogger();
				Debug.Assert(actual2.Logged == false);
				// Note that null logger should be skipped without any exceptioin.
				ILogger[] loggers = new ILogger[] { actual1, null!, actual2 };

				// act
				sample.LogTo(loggers);

				// assert
				Assert.True(actual1.Logged);
				Assert.Equal(sample.StateType, actual1.StateType);
				Assert.Equal(sample.LogLevel, actual1.LogLevel);
				Assert.Equal(sample.EventId, actual1.EventId);
				Assert.Equal(sample.State, actual1.State);
				Assert.Equal(sample.Exception, actual1.Exception);
				Assert.Equal(sample.Formatter, actual1.Formatter);

				Assert.True(actual2.Logged);
				Assert.Equal(sample.StateType, actual2.StateType);
				Assert.Equal(sample.LogLevel, actual2.LogLevel);
				Assert.Equal(sample.EventId, actual2.EventId);
				Assert.Equal(sample.State, actual2.State);
				Assert.Equal(sample.Exception, actual2.Exception);
				Assert.Equal(sample.Formatter, actual2.Formatter);
			}

			[Fact(DisplayName = "multiple; loggers: null")]
			public void multiple_loggers_null() {
				// arrange
				LogEntry sample = Sample;
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
	}
}
