using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;


namespace Zafu.Testing.Logging.Tests {
	public class SingleLogLoggerTest {
		#region Log

		public class Log {
			#region tests

			[Fact(DisplayName = "empty")]
			public void Empty() {
				// arrange
				SingleLogLogger target = new SingleLogLogger();

				// act
				// nothing to do

				// assert
				Assert.False(target.Logged);
				Assert.Null(target.Data);
				Assert.Null(target.Message);
			}

			[Fact(DisplayName = "single log")]
			public void SingleLog() {
				// arrange
				// Keep typed version of state and formatter, because those in LogData is not typed.
				// (i.e. LogData.State is object and LogData.Formatter is Delegate)
				Version state = new Version(1, 2, 3);
				Func<Version, Exception, string> formatter = ((Version v, Exception e) => v.ToString());
				LogData sample = LogData.Create<Version>(state, LogLevel.Trace, formatter: formatter);
				SingleLogLogger target = new SingleLogLogger();

				// act
				// The log should be recorded.
				target.Log<Version>(sample.LogLevel, sample.EventId, state, sample.Exception!, formatter);

				// assert
				Assert.True(target.Logged);
				Assert.Equal(sample, target.Data);
				Assert.Equal("1.2.3", target.Message);
			}

			[Fact(DisplayName = "multiple logs")]
			public void MultipleLogs() {
				// arrange
				// Keep typed version of state and formatter, because those in LogData is not typed.
				// (i.e. LogData.State is object and LogData.Formatter is Delegate)
				Version state1 = new Version(1, 2, 3);
				Func<Version, Exception, string> formatter1 = ((Version s, Exception e) => s.ToString());
				LogData sample1 = LogData.Create<Version>(state1, LogLevel.Trace, formatter: formatter1);

				Uri state2 = new Uri("http://example.org");
				Func<Uri, Exception, string> formatter2 = ((Uri s, Exception e) => s.ToString());
				LogData sample2 = LogData.Create<Uri>(state2, LogLevel.Critical, formatter: formatter2);

				SingleLogLogger target = new SingleLogLogger();

				// act
				// The target should log only one log.
				target.Log<Version>(sample1.LogLevel, sample1.EventId, state1, sample1.Exception!, formatter1);
				InvalidOperationException actualException = Assert.Throws<InvalidOperationException>(() => {
					target.Log<Uri>(sample2.LogLevel, sample2.EventId, state2, sample2.Exception!, formatter2);
				});

				// assert
				Assert.True(target.Logged);
				Assert.Equal(sample1, target.Data);
				Assert.Equal("1.2.3", target.Message);
				Assert.Equal("This logger has already accepted a log. It can accept only one log.", actualException.Message);
			}

			#endregion
		}

		#endregion


		#region BeginScope

		public class BeginScope {
			#region tests

			[Fact(DisplayName = "not supported")]
			public void NotSupported() {
				// arrange
				SingleLogLogger target = new SingleLogLogger();

				// act
				Assert.Throws<NotSupportedException>(() => {
					target.BeginScope<Uri>(new Uri("http://example.org"));
				});
			}

			#endregion
		}

		#endregion


		#region IsEnabled

		public class IsEnabled {
			#region tests

			[Fact(DisplayName = "enabled")]
			public void Enabled() {
				// arrange
				SingleLogLogger target = new SingleLogLogger();

				// act
				// It should return true except for LogLevel.None.
				bool allEnabled = true;
				foreach (LogLevel logLevel in Enum.GetValues(typeof(LogLevel)).OfType<LogLevel>()) {
					if (logLevel != LogLevel.None) {
						if (target.IsEnabled(logLevel) == false) {
							allEnabled = false;
							break;
						}
					}
				}

				// assert
				Assert.True(allEnabled);
			}

			#endregion
		}

		#endregion


		#region Log

		public class Clear {
			#region tests

			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				// Keep typed version of state and formatter, because those in LogData is not typed.
				// (i.e. LogData.State is object and LogData.Formatter is Delegate)
				Version state1 = new Version(1, 2, 3);
				Func<Version, Exception, string> formatter1 = ((Version s, Exception e) => s.ToString());
				LogData sample1 = LogData.Create<Version>(state1, LogLevel.Trace, formatter: formatter1);

				Uri state2 = new Uri("http://example.org/");
				Func<Uri, Exception, string> formatter2 = ((Uri s, Exception e) => s.ToString());
				LogData sample2 = LogData.Create<Uri>(state2, LogLevel.Critical, formatter: formatter2);

				SingleLogLogger target = new SingleLogLogger();

				// act & assert
				// Clear() should clear the current log.
				target.Log<Version>(sample1.LogLevel, sample1.EventId, state1, sample1.Exception!, formatter1);
				Assert.True(target.Logged);
				Assert.Equal(sample1, target.Data);
				Assert.Equal("1.2.3", target.Message);

				target.Clear();
				Assert.False(target.Logged);
				Assert.Null(target.Data);
				Assert.Null(target.Message);

				target.Log<Uri>(sample2.LogLevel, sample2.EventId, state2, sample2.Exception!, formatter2);
				Assert.True(target.Logged);
				Assert.Equal(sample2, target.Data);
				Assert.Equal("http://example.org/", target.Message);
			}

			[Fact(DisplayName = "clear empty")]
			public void empty() {
				// arrange
				SingleLogLogger target = new SingleLogLogger();

				// act & assert
				// Clear() can be called on empty state.
				Assert.False(target.Logged);
				Assert.Null(target.Data);
				Assert.Null(target.Message);

				target.Clear();
				Assert.False(target.Logged);
				Assert.Null(target.Data);
				Assert.Null(target.Message);
			}

			#endregion
		}

		#endregion
	}
}
