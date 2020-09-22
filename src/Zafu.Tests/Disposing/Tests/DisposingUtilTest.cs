using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.ObjectModel;
using Zafu.Testing.Disposing;
using Zafu.Testing.Logging;

namespace Zafu.Disposing.Tests {
	public class DisposingUtilTest {
		#region utilities

		protected static (RunningContext, TestingLogger) CreateRunningContext() {
			TestingLogger logger = new TestingLogger();
			return (new RunningContext(logger), logger);
		}

		protected static LogData GetErrorLogData(Exception? exception, int index = -1) {
			string source = "DisposableUtil";
			string message = "An exception is thrown during a Dispose() call.";
			LogLevel logLevel = LogLevel.Error;
			if (0 <= index) {
				return LogData.CreateWithSimpleState<int>(source, message, "index", index, logLevel, exception);
			} else {
				return LogData.CreateWithSimpleState(source, message, logLevel, exception);
			}
		}

		#endregion


		#region DisposeLoggingException

		public class DisposeLoggingException {
			#region tests

			[Fact(DisplayName = "single; without exception")]
			public void single_without_exception() {
				// arrange
				TestingDisposable disposable = new TestingDisposable();
				Debug.Assert(disposable.DisposeCount == 0);
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposable, runningContext);

				// assert
				// The disposable should be disposed once.
				Assert.Equal(1, disposable.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "single; with exception")]
			public void single_with_exception() {
				// arrange
				NotSupportedException exception = new NotSupportedException();
				TestingDisposable disposable = new TestingDisposable(exception);
				Debug.Assert(disposable.DisposeCount == 0);
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposable, runningContext);

				// assert
				// The disposable should be disposed once.
				Assert.Equal(1, disposable.DisposeCount);
				// An error should be logged.
				Assert.Single(logger);
				LogData data = logger.GetLogData(0);
				Assert.Equal(GetErrorLogData(exception), data);
			}

			[Fact(DisplayName = "single; disposable: null")]
			public void single_disposable_null() {
				// arrange
				TestingDisposable? disposable = null;
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposable, runningContext);

				// assert
				// No exception should be thrown.
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "multiple; without exception")]
			public void multiple_without_exception() {
				// arrange
				TestingDisposable sample1 = new TestingDisposable();
				TestingDisposable sample2 = new TestingDisposable();
				TestingDisposable sample3 = new TestingDisposable();
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				Debug.Assert(sample3.DisposeCount == 0);
				TestingDisposable[] disposables = new TestingDisposable[] {
					sample1,
					sample2,
					sample3
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposables, runningContext);

				// assert
				// The samples should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				Assert.Equal(1, sample3.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "multiple; with exception")]
			public void multiple_with_exception() {
				// arrange
				NotSupportedException exception1 = new NotSupportedException();
				InvalidOperationException exception2 = new InvalidOperationException();
				TestingDisposable sample1 = new TestingDisposable(exception1);
				TestingDisposable sample2 = new TestingDisposable();
				TestingDisposable sample3 = new TestingDisposable(exception2);
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				Debug.Assert(sample3.DisposeCount == 0);
				TestingDisposable[]? disposables = new TestingDisposable[] {
					sample1,
					sample2,
					sample3
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposables, runningContext);

				// assert
				// The samples should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				Assert.Equal(1, sample3.DisposeCount);
				// Errors should be logged.
				Assert.Equal(2, logger.Count);
				Assert.Equal(GetErrorLogData(exception1, 0), logger.GetLogData(0));
				Assert.Equal(GetErrorLogData(exception2, 2), logger.GetLogData(1));
			}

			[Fact(DisplayName = "multiple; disposables: null")]
			public void multiple_disposables_null() {
				// arrange
				TestingDisposable?[]? disposables = null;
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposables, runningContext);

				// assert
				// No exception should be thrown.
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "multiple; disposables: contains null")]
			public void multiple_disposables_contains_null() {
				// arrange
				TestingDisposable sample1 = new TestingDisposable();
				TestingDisposable sample2 = new TestingDisposable();
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				TestingDisposable?[] disposables = new TestingDisposable?[] {
					sample1,
					null,
					sample2
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.DisposeLoggingException(disposables, runningContext);

				// assert
				// The disposable should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			#endregion
		}

		#endregion


		#region ClearDisposableLoggingException

		public class ClearDisposableLoggingException {
			#region tests

			[Fact(DisplayName = "without exception")]
			public void without_exception() {
				// arrange
				TestingDisposable sample = new TestingDisposable();
				Debug.Assert(sample.DisposeCount == 0);
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				TestingDisposable? disposable = sample;
				Debug.Assert(disposable != null);

				// act
				DisposingUtil.ClearDisposableLoggingException(ref disposable, runningContext);

				// assert
				// The disposable should be cleared.
				Assert.Null(disposable);
				// The sample should be disposed once.
				Assert.Equal(1, sample.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "with exception")]
			public void with_exception() {
				// arrange
				NotSupportedException exception = new NotSupportedException();
				TestingDisposable sample = new TestingDisposable(exception);
				Debug.Assert(sample.DisposeCount == 0);
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				TestingDisposable? disposable = sample;
				Debug.Assert(disposable != null);

				// act
				DisposingUtil.ClearDisposableLoggingException(ref disposable, runningContext);

				// assert
				// The disposable should be cleared.
				Assert.Null(disposable);
				// The sample should be disposed once.
				Assert.Equal(1, sample.DisposeCount);
				// An error should be logged.
				Assert.Single(logger);
				LogData data = logger.GetLogData(0);
				Assert.Equal(GetErrorLogData(exception), data);
			}

			[Fact(DisplayName = "disposable: null")]
			public void disposable_null() {
				// arrange
				TestingDisposable? disposable = null;
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.ClearDisposableLoggingException(ref disposable, runningContext);

				// assert
				// No exception should be thrown.
				// The disposable should be null.
				Assert.Null(disposable);
				// No error should be logged.
				Assert.Empty(logger);
			}

			#endregion
		}

		#endregion


		#region ClearDisposablesLoggingException

		public class ClearDisposablesLoggingException {
			#region tests

			[Fact(DisplayName = "without exception")]
			public void without_exception() {
				// arrange
				TestingDisposable sample1 = new TestingDisposable();
				TestingDisposable sample2 = new TestingDisposable();
				TestingDisposable sample3 = new TestingDisposable();
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				Debug.Assert(sample3.DisposeCount == 0);
				TestingDisposable[]? disposables = new TestingDisposable[] {
					sample1,
					sample2,
					sample3
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.ClearDisposablesLoggingException(ref disposables, runningContext);

				// assert
				// The disposables should be cleared.
				Assert.Null(disposables);
				// The samples should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				Assert.Equal(1, sample3.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "with exception")]
			public void with_exception() {
				// arrange
				NotSupportedException exception1 = new NotSupportedException();
				InvalidOperationException exception2 = new InvalidOperationException();
				TestingDisposable sample1 = new TestingDisposable();
				TestingDisposable sample2 = new TestingDisposable(exception1);
				TestingDisposable sample3 = new TestingDisposable(exception2);
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				Debug.Assert(sample3.DisposeCount == 0);
				TestingDisposable[]? disposables = new TestingDisposable[] {
					sample1,
					sample2,
					sample3
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.ClearDisposablesLoggingException(ref disposables, runningContext);

				// assert
				// The disposables should be cleared.
				Assert.Null(disposables);
				// The samples should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				Assert.Equal(1, sample3.DisposeCount);
				// Errors should be logged.
				Assert.Equal(2, logger.Count);
				Assert.Equal(GetErrorLogData(exception1, 1), logger.GetLogData(0));
				Assert.Equal(GetErrorLogData(exception2, 2), logger.GetLogData(1));
			}

			[Fact(DisplayName = "disposables: null")]
			public void disposables_null() {
				// arrange
				TestingDisposable?[]? disposables = null;
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.ClearDisposablesLoggingException(ref disposables, runningContext);

				// assert
				// No exception should be thrown.
				// The disposables should be cleared.
				Assert.Null(disposables);
				// No error should be logged.
				Assert.Empty(logger);
			}

			[Fact(DisplayName = "disposables: contains null")]
			public void disposables_contains_null() {
				// arrange
				TestingDisposable sample1 = new TestingDisposable();
				TestingDisposable sample2 = new TestingDisposable();
				Debug.Assert(sample1.DisposeCount == 0);
				Debug.Assert(sample2.DisposeCount == 0);
				TestingDisposable?[]? disposables = new TestingDisposable?[] {
					sample1,
					null,
					sample2
				};
				(RunningContext runningContext, TestingLogger logger) = CreateRunningContext();

				// act
				DisposingUtil.ClearDisposablesLoggingException(ref disposables, runningContext);

				// assert
				// The disposables should be cleared.
				Assert.Null(disposables);
				// The samples should be disposed once.
				Assert.Equal(1, sample1.DisposeCount);
				Assert.Equal(1, sample2.DisposeCount);
				// No error should be logged.
				Assert.Empty(logger);
			}

			#endregion
		}

		#endregion
	}
}
