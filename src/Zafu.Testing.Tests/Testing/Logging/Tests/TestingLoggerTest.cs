using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Zafu.Logging;
using Xunit;

namespace Zafu.Testing.Logging.Tests {
	public class TestingLoggerTest {
		#region samples

		public static readonly LogData SampleLog1 = new LogData(typeof(Version), LogLevel.Information, new EventId(3), new Version(1, 2), null, (Func<Version?, Exception?, string>)VersionFormatter);

		public static readonly LogData SampleLog2 = new LogData(typeof(Uri), LogLevel.Critical, new EventId(51, "test"), new Uri("http://example.org/"), new InvalidOperationException(), (Func<Uri?, Exception?, string>)UriFormatter);

		public static readonly LogData SampleLog3 = new LogData(typeof(Version), LogLevel.Trace, new EventId(-2000), new Version(3, 4), null, (Func<Version?, Exception?, string>)VersionFormatter);

		public static readonly BeginScopeData SampleBeginScope1 = new BeginScopeData(typeof(Version), new Version(7, 6, 5, 4), null);

		public static readonly BeginScopeData SampleBeginScope2 = new BeginScopeData(typeof(Uri), new Uri("https://example.org"), null);


		private static string VersionFormatter(Version? state, Exception? exception) {
			return (state != null) ? state.ToString() : string.Empty;
		}

		private static string UriFormatter(Uri? state, Exception? exception) {
			return (state != null) ? state.ToString() : string.Empty;
		}

		#endregion


		#region utilities

		protected static void Log<TState>(TestingLogger target, LogData data) {
			// check argument
			Debug.Assert(target != null);
			Debug.Assert(data != null);
			if (data.StateType != typeof(TState)) {
				throw new ArgumentException($"It does not match to {nameof(TState)}.", $"{nameof(data)}.{nameof(data.StateType)}");
			}

			// Do not use data.LogTo().
			// In this test, we would rather test ILogger interface of the target directly.
			TState state = (TState)data.State!;
			Func<TState, Exception, string> formatter = (Func<TState, Exception, string>)data.Formatter!;
			target.Log<TState>(data.LogLevel, data.EventId, state, data.Exception!, formatter);
		}

		protected static IDisposable BeginScope<TState>(TestingLogger target, BeginScopeData data) {
			// check argument
			Debug.Assert(target != null);
			Debug.Assert(data != null);
			if (data.StateType != typeof(TState)) {
				throw new ArgumentException($"It does not match to {nameof(TState)}.", $"{nameof(data)}.{nameof(data.StateType)}");
			}

			// Do not use data.BeginScopeOn().
			// In this test, we would rather test ILogger interface of the target directly.
			TState state = (TState)data.State!;
			return target.BeginScope<TState>(state);
		}

		#endregion


		#region Scope

		public class Scope {
			#region tests

			[Fact(DisplayName = "multiple Dispose() call")]
			public void multiple_dispose() {
				// arrange
				BeginScopeData sample = SampleBeginScope1;
				TestingLogger target = new TestingLogger();

				// act
				IDisposable scope = BeginScope<Version>(target, sample);
				// scope.Dispose() can be called multiple time safely
				scope.Dispose();
				scope.Dispose();
				scope.Dispose();

				// assert
				// no exception should be thrown
			}

			#endregion
		}

		#endregion


		#region constructor

		public class Constructor {
			#region tests

			[Fact(DisplayName = "initial state")]
			public void initial_state() {
				// arrange

				// act
				TestingLogger actual = new TestingLogger();

				// assert
				// check initial state
				// LoggingLevel is LogLevel.Trace to record all logs by default.
				Assert.Equal(LogLevel.Trace, actual.LoggingLevel);
			}

			#endregion
		}

		#endregion


		#region logging

		public class Logging {
			#region utilities

			protected static void AssertLogs(IEnumerable<LoggingData> expected, IReadOnlyList<LoggingData> actual) {
				// check argument
				Debug.Assert(expected != null);
				Debug.Assert(actual != null);

				// assert as IEnumerable
				IEnumerable actualEnumerable = actual;
				Assert.Equal(expected, actualEnumerable);

				// assert as IEnumerable<LoggingData>
				IEnumerable<LoggingData> actualGenericEnumerable = actual;
				Assert.Equal(expected, actualGenericEnumerable);

				// assert as IReadOnlyList<LoggingData> (Count and indexer)
				LoggingData[] expectedArray = expected.ToArray();
				Assert.Equal(expectedArray.Length, actual.Count);
				for (int i = 0; i < expectedArray.Length; ++i) {
					Assert.Equal(expectedArray[i], actual[i]);
				}
			}

			#endregion


			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				LogData sample1 = SampleLog1;
				LogData sample2 = SampleLog2;
				LogData sample3 = SampleLog3;
				TestingLogger target = new TestingLogger();

				// act
				Log<Version>(target, sample1);
				Log<Uri>(target, sample2);
				Log<Version>(target, sample3);

				// assert
				LoggingData[] expected = new LoggingData[] { sample1, sample2, sample3 };
				AssertLogs(expected, target);
			}

			[Fact(DisplayName = "none")]
			public void none() {
				// arrange
				TestingLogger target = new TestingLogger();

				// act
				// no logging

				// assert
				LoggingData[] expected = new LoggingData[0];
				AssertLogs(expected, target);
			}

			[Fact(DisplayName = "scope: simple")]
			public void scope_simple() {
				// arrange
				LogData sampleLog1 = SampleLog1;
				BeginScopeData sampleScope = SampleBeginScope1;
				LogData sampleLog2 = SampleLog2;
				LogData sampleLog3 = SampleLog3;
				TestingLogger target = new TestingLogger();

				// act
				Log<Version>(target, sampleLog1);
				IDisposable scope = BeginScope<Version>(target, sampleScope);
				Log<Uri>(target, sampleLog2);
				scope.Dispose();
				Log<Version>(target, sampleLog3);

				// assert
				LoggingData[] expected = new LoggingData[] {
					sampleLog1,
					new BeginScopeData(sampleScope.StateType, sampleScope.State, scope),
					sampleLog2,
					new EndScopeData(scope),
					sampleLog3
				};
				AssertLogs(expected, target);
			}

			[Fact(DisplayName = "scope: nested")]
			public void scope_nested() {
				// arrange
				LogData sampleLog1 = SampleLog1;
				BeginScopeData sampleScope1 = SampleBeginScope1;
				LogData sampleLog2 = SampleLog2;
				BeginScopeData sampleScope2 = SampleBeginScope2;
				LogData sampleLog3 = SampleLog3;
				TestingLogger target = new TestingLogger();

				// act
				Log<Version>(target, sampleLog1);
				IDisposable scope1 = BeginScope<Version>(target, sampleScope1);
				Log<Uri>(target, sampleLog2);
				IDisposable scope2 = BeginScope<Uri>(target, sampleScope2);
				Log<Version>(target, sampleLog3);
				scope2.Dispose();
				scope1.Dispose();

				// assert
				LoggingData[] expected = new LoggingData[] {
					sampleLog1,
					new BeginScopeData(sampleScope1.StateType, sampleScope1.State, scope1),
					sampleLog2,
					new BeginScopeData(sampleScope2.StateType, sampleScope2.State, scope2),
					sampleLog3,
					new EndScopeData(scope2),
					new EndScopeData(scope1)
				};
				AssertLogs(expected, target);
			}

			// TestingLogger should be able to record overlapped scopes,
			// though it is not sure that overlapped scopes are valid for ILogger,
			// but 
			[Fact(DisplayName = "scope: overlapped")]
			public void scope_overlapped() {
				// arrange
				LogData sampleLog1 = SampleLog1;
				BeginScopeData sampleScope1 = SampleBeginScope1;
				LogData sampleLog2 = SampleLog2;
				BeginScopeData sampleScope2 = SampleBeginScope2;
				LogData sampleLog3 = SampleLog3;
				TestingLogger target = new TestingLogger();

				// act
				Log<Version>(target, sampleLog1);
				IDisposable scope1 = BeginScope<Version>(target, sampleScope1);
				IDisposable scope2 = BeginScope<Uri>(target, sampleScope2);
				Log<Uri>(target, sampleLog2);
				scope1.Dispose();
				Log<Version>(target, sampleLog3);
				scope2.Dispose();

				// assert
				LoggingData[] expected = new LoggingData[] {
					sampleLog1,
					new BeginScopeData(sampleScope1.StateType, sampleScope1.State, scope1),
					new BeginScopeData(sampleScope2.StateType, sampleScope2.State, scope2),
					sampleLog2,
					new EndScopeData(scope1),
					sampleLog3,
					new EndScopeData(scope2)
				};
				AssertLogs(expected, target);
			}

			// double dispose

			#endregion
		}

		#endregion


		#region LoggingLevel

		public class LoggingLevel {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				static string formatter(string state, Exception exception) {
					return state;
				}
				static LogData createSample(LogLevel logLevel) {
					return new LogData(typeof(string), logLevel, default(EventId), logLevel.ToString(), null, (Func<string, Exception, string>)formatter);
				}
				LogData sampleT = createSample(LogLevel.Trace);
				LogData sampleD = createSample(LogLevel.Debug);
				LogData sampleI = createSample(LogLevel.Information);
				LogData sampleW = createSample(LogLevel.Warning);
				LogData sampleE = createSample(LogLevel.Error);
				LogData sampleC = createSample(LogLevel.Critical);
				TestingLogger target = new TestingLogger();

				// act
				target.LoggingLevel = LogLevel.Warning;
				Log<string>(target, sampleT);
				Log<string>(target, sampleD);
				Log<string>(target, sampleI);
				Log<string>(target, sampleW);
				Log<string>(target, sampleE);
				Log<string>(target, sampleC);

				// assert
				// The target should record only logs equal to or severer than LogLevel.Warning.
				LoggingData[] expected = new LoggingData[] { sampleW, sampleE, sampleC };
				Assert.Equal(expected, target);
			}

			#endregion
		}

		#endregion


		#region Clear

		public class Clear {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				LogData sample1 = SampleLog1;
				LogData sample2 = SampleLog2;
				LogData sample3 = SampleLog3;

				TestingLogger target = new TestingLogger();
				Log<Version>(target, sample1);
				Log<Uri>(target, sample2);
				Log<Version>(target, sample3);
				Debug.Assert(target.Count == 3);

				// act
				target.Clear();

				// assert
				// The contents of the target should be cleared.
				Assert.Empty(target);
			}

			#endregion
		}

		#endregion
	}
}
