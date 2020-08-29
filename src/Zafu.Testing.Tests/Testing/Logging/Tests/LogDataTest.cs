using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Zafu.Testing.Logging.Tests {
	public class LogDataTest {
		#region samples

		public static readonly LogData Sample = LogData.Create<Version>(new Version(1, 2), LogLevel.Information, new EventId(51, "test"), new NotImplementedException());

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
				Delegate formatter = (Func<Uri, Exception?, string>)LogData.DefaultFormatter<Uri>;

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
				Delegate formatter = (Func<Uri, Exception?, string>)LogData.DefaultFormatter<Uri>;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new LogData(stateType, logLevel, eventId, state, exception, formatter);
				});

				// assert
				Assert.Equal("stateType", actual.ParamName);
			}

			[Fact(DisplayName = "general constructor; state: type mismatch")]
			public void General_state_typeMismatch() {
				// arrange
				Type stateType = typeof(Uri);
				LogLevel logLevel = LogLevel.Warning;
				EventId eventId = new EventId(123, "warning event");
				object? state = new Version(1, 2);
				Exception? exception = new SystemException();
				Delegate formatter = (Func<Uri, Exception?, string>)LogData.DefaultFormatter<Uri>;

				// act
				ArgumentException actual = Assert.Throws<ArgumentException>(() => {
					new LogData(stateType, logLevel, eventId, state, exception, formatter);
				});

				// assert
				Assert.Equal("state", actual.ParamName);
				Assert.Equal("It is not an instance of System.Uri (Parameter 'state')", actual.Message);
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

			[Fact(DisplayName = "Create")]
			public void Create() {
				// arrange
				LogLevel logLevel = LogLevel.Warning;
				EventId eventId = new EventId(123, "warning event");
				Uri state = new Uri("http://example.org/");
				Exception? exception = new SystemException();
				Func<Uri, Exception, string> formatter = LogData.DefaultFormatter<Uri>;

				// act
				LogData actual = LogData.Create<Uri>(state, logLevel, eventId, exception, formatter);

				// assert
				Assert.Equal(typeof(Uri), actual.StateType);
				Assert.Equal(logLevel, actual.LogLevel);
				Assert.Equal(eventId, actual.EventId);
				Assert.Equal(state, actual.State);
				Assert.Equal(exception, actual.Exception);
				Assert.Equal(formatter, actual.Formatter);
			}
		}

		#endregion


		#region comparison

		public class Comparison {
			#region utilities

			protected void Test(LogData? x, LogData? y, bool expected) {
				// test operators
				// act
				bool actual_equality = (x == y);
				bool actual_inequality = (x != y);

				// assert
				Assert.Equal(expected, actual_equality);
				Assert.Equal(!expected, actual_inequality);

				// test Equals methods
				if (object.ReferenceEquals(x, null) == false) {
					// act
					bool actual_equal = x.Equals(y);
					bool actual_objectEqual = x.Equals((object?)y);

					// assert
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
				Type differentValue = typeof(object);
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
				Delegate differentValue = (Func<Version, Exception?, string>)((Version v, Exception? e) => "Hi!");
				Debug.Assert(differentValue != x.Formatter);
				LogData y = new LogData(x.StateType, x.LogLevel, x.EventId, x.State, x.Exception, differentValue);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; incompatible type")]
			public void different_incompatible_type() {
				// arrange
				LogData x = Sample;
				// object of incompatible type (string)
				object y = "abc";

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
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
	}
}
