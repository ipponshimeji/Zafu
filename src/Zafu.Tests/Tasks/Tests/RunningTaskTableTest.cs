using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Tasks.Testing;
using Zafu.Testing.Logging;
using Zafu.Testing.ObjectModel;
using Zafu.Testing.Tasks;
using Zafu.ObjectModel;
using System.Threading.Tasks;
using System.Threading;

namespace Zafu.Tasks.Tests {
	public class RunningTaskTableTest {
		#region IRunningTaskMonitor

		public class IRunningTaskMonitorTest: IRunningTaskMonitorTest<RunningTaskTable> {
			#region overrides

			protected override RunningTaskTable CreateTarget() {
				return new RunningTaskTable();
			}

			protected override void DisposeTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				target.Dispose();
			}

			protected override void AssertTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				Assert.Equal(0, target.RunningTaskCount);
			}

			#endregion
		}

		#endregion


		#region IRunningTaskTable

		public class IRunningTaskTableTest: IRunningTaskTableTestBase<RunningTaskTable> {
			#region overrides

			protected override RunningTaskTable CreateTarget() {
				return new RunningTaskTable();
			}

			protected override void DisposeTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				bool disposed = target.IsDisposed;
				if (disposed == false) {
					target.Dispose();
				}
			}

			protected override void AssertTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				Assert.Equal(0, target.RunningTaskCount);
				Assert.True(target.IsDisposed);
			}

			#endregion
		}

		#endregion


		#region logging

		public class Logging: IRunningTaskMonitorTestBase<RunningTaskTable> {
			#region utilities

			protected RunningTaskTable CreateTarget(IRunningContext runningContext) {
				return new RunningTaskTable(runningContext);
			}

			protected void DisposeTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				target.Dispose();
			}

			/// <summary>
			/// Runs additional routine assertion on the target.
			/// </summary>
			/// <param name="target"></param>
			protected void AssertTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				Assert.Equal(0, target.RunningTaskCount);
			}

			protected void Test(Action<TestingRunningEnvironment, RunningTaskTable> test) {
				using (TestingRunningEnvironment runningEnvironment = TestingRunningEnvironment.Create()) {
					RunningTaskTable target = CreateTarget(runningEnvironment);
					try {
						test(runningEnvironment, target);
					} finally {
						DisposeTarget(target);
					}
				}
			}

			protected LogData CreateLogData(LogLevel logLevel, string message, Task? task = null, Exception? exception = null, EventId eventId = default(EventId)) {
				int taskId = (task == null) ? 0 : task.Id;
				return LogData.CreateWithSimpleState<int>("RunningTaskMonitor", message, "task-id", taskId, logLevel, exception, eventId);
			}

			protected LogData CreateErrorLogData(Task? task = null, Exception? exception = null, EventId eventId = default(EventId)) {
				return CreateLogData(LogLevel.Error, "The running task finished with an exception.", task, exception, eventId);
			}

			#endregion


			#region tests

			private void Test_SimpleAction_Successful(LogLevel loggingLevel, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_SimpleAction_Successful(
						target: target,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "simple action; successful; logging level: Error")]
			public void SimpleAction_Successful_Error() {
				Test_SimpleAction_Successful(
					loggingLevel: LogLevel.Error,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_SimpleAction_Exception(LogLevel loggingLevel, Exception exception, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (exception == null) {
					throw new ArgumentNullException(nameof(exception));
				}
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_SimpleAction_Exception(
						target: target,
						exception: exception,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "simple action; exception; logging level: Error")]
			public void SimpleAction_Exception_Error() {
				Exception exception = new InvalidOperationException();

				Test_SimpleAction_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(task, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_SimpleAction_TryToCancel(LogLevel loggingLevel, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_SimpleAction_TryToCancel(
						target: target,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "simple action; try to cancel; logging level: Error")]
			public void SimpleAction_TryToCancel_Error() {
				Test_SimpleAction_TryToCancel(
					loggingLevel: LogLevel.Error,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_CancellableAction_Successful(LogLevel loggingLevel, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_CancellableAction_Successful(
						target: target,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "cancellable action; successful; logging level: Error")]
			public void CancellableAction_Successful_Error() {
				Test_CancellableAction_Successful(
					loggingLevel: LogLevel.Error,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_CancellableAction_Exception(LogLevel loggingLevel, Exception exception, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (exception == null) {
					throw new ArgumentNullException(nameof(exception));
				}
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_CancellableAction_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "cancellable action; exception; logging level: Error")]
			public void CancellableAction_Exception_Error() {
				Exception exception = new NotSupportedException();

				Test_CancellableAction_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(task, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_CancellableAction_CanceledVoluntarily(LogLevel loggingLevel, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						Test_CancellableAction_CanceledVoluntarily(
							target: target,
							cancellationTokenSource: cancellationTokenSource,
							doNotDisposeCancellationTokenSource: true,
							assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
						);
					}
				});
			}

			[Fact(DisplayName = "cancellable action; canceled voluntarily; logging level: Error")]
			public void CancellableAction_CanceledVoluntarily_Error() {
				Test_CancellableAction_CanceledVoluntarily(
					loggingLevel: LogLevel.Error,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_CancellableAction_CanceledWithException(LogLevel loggingLevel, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						Test_CancellableAction_CanceledWithException(
							target: target,
							cancellationTokenSource: cancellationTokenSource,
							doNotDisposeCancellationTokenSource: true,
							assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
						);
					}
				});
			}

			[Fact(DisplayName = "cancellable action; canceled with exception; logging level: Error")]
			public void CancellableAction_CanceledWithException_Error() {
				Test_CancellableAction_CanceledWithException(
					loggingLevel: LogLevel.Error,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							new ExpectedLogData(
								logData: CreateErrorLogData(task),
								exceptionChecker: e => e is OperationCanceledException
							)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Done_Successful(LogLevel loggingLevel, bool targetValueTask, Action<Task, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_Task_Done_Successful(
						target: target,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						targetValueTask: targetValueTask,
						assert: (Task task, IRunningTask? runningTask) => assert(task, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "Task; done; successful; logging level: Error")]
			public void Task_Done_Successful_Error() {
				Test_Task_Done_Successful(
					loggingLevel: LogLevel.Error,
					targetValueTask: false,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompletedSuccessfully);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; done; successful; logging level: Error")]
			public void ValueTask_Done_Successful_Error() {
				Test_Task_Done_Successful(
					loggingLevel: LogLevel.Error,
					targetValueTask: true,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompletedSuccessfully);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Done_Exception(LogLevel loggingLevel, Exception exception, bool targetValueTask, Action<Task, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}
				if (exception == null) {
					throw new ArgumentNullException(nameof(exception));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_Task_Done_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						targetValueTask: targetValueTask,
						assert: (Task task, IRunningTask? runningTask) => assert(task, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "Task; done; exception; logging level: Error")]
			public void Task_Done_Exception_Error() {
				Exception exception = new ObjectDisposedException(null);
				Test_Task_Done_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					targetValueTask: false,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsFaulted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(task, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; done; exception; logging level: Error")]
			public void ValueTask_Done_Exception_Error() {
				Exception exception = new NotSupportedException();
				Test_Task_Done_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					targetValueTask: true,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsFaulted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(null, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Done_Canceled(LogLevel loggingLevel, bool targetValueTask, Action<Task, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_Task_Done_Canceled(
						target: target,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						targetValueTask: targetValueTask,
						assert: (Task task, IRunningTask? runningTask) => assert(task, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "Task; done; canceled; logging level: Error")]
			public void Task_Done_Canceled_Error() {
				Test_Task_Done_Canceled(
					loggingLevel: LogLevel.Error,
					targetValueTask: false,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsCanceled);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							new ExpectedLogData(
								logData: CreateErrorLogData(task),
								exceptionChecker: e => e is OperationCanceledException
							)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; done; canceled; logging level: Error")]
			public void ValueTask_Done_Canceled_Error() {
				Test_Task_Done_Canceled(
					loggingLevel: LogLevel.Error,
					targetValueTask: true,
					assert: (Task task, IEnumerable<LoggingData> actualLogs) => {
						// check argument
						Debug.Assert(task != null);
						Debug.Assert(task.IsCanceled);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							new ExpectedLogData(
								logData: CreateErrorLogData(null),
								exceptionChecker: e => e is OperationCanceledException
							)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Running_Successful(LogLevel loggingLevel, bool targetValueTask, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_Task_Running_Successful(
						target: target,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						targetValueTask: targetValueTask,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "Task; running; successful; logging level: Error")]
			public void Task_Running_Successful_Error() {
				Test_Task_Running_Successful(
					loggingLevel: LogLevel.Error,
					targetValueTask: false,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; running; successful; logging level: Error")]
			public void ValueTask_Running_Successful_Error() {
				Test_Task_Running_Successful(
					loggingLevel: LogLevel.Error,
					targetValueTask: true,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Debug.Assert(runningTask.Task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = Array.Empty<LoggingData>();

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Running_Exception(LogLevel loggingLevel, Exception exception, bool targetValueTask, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					Test_Task_Running_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: null,
						doNotDisposeCancellationTokenSource: false,
						targetValueTask: targetValueTask,
						assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
					);
				});
			}

			[Fact(DisplayName = "Task; running; exception; logging level: Error")]
			public void Task_Running_Exception_Error() {
				Exception exception = new NotImplementedException();
				Test_Task_Running_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					targetValueTask: false,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(task, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; running; exception; logging level: Error")]
			public void ValueTask_Running_Exception_Error() {
				Exception exception = new ArgumentNullException();
				Test_Task_Running_Exception(
					loggingLevel: LogLevel.Error,
					exception: exception,
					targetValueTask: true,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							CreateErrorLogData(task, exception)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			private void Test_Task_Running_Canceled(LogLevel loggingLevel, bool targetValueTask, Action<IRunningTask, IEnumerable<LoggingData>> assert) {
				// check argument
				if (assert == null) {
					throw new ArgumentNullException(nameof(assert));
				}

				// test
				Test((TestingRunningEnvironment runningEnvironment, RunningTaskTable target) => {
					runningEnvironment.LoggingLevel = loggingLevel;
					// Note that you need cancellationTokenSource to actually cancel the task
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						Test_Task_Running_Canceled(
							target: target,
							cancellationTokenSource: cancellationTokenSource,
							doNotDisposeCancellationTokenSource: true,
							targetValueTask: targetValueTask,
							assert: (TestingActionState actionState, IRunningTask runningTask) => assert(runningTask, runningEnvironment.Logs)
						);
					}
				});
			}

			[Fact(DisplayName = "Task; running; canceled; logging level: Error")]
			public void Task_Running_Canceled_Error() {
				Test_Task_Running_Canceled(
					loggingLevel: LogLevel.Error,
					targetValueTask: false,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							new ExpectedLogData(
								logData: CreateErrorLogData(task),
								exceptionChecker: e => e is OperationCanceledException
							)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			[Fact(DisplayName = "ValueTask; running; canceled; logging level: Error")]
			public void ValueTask_Running_Canceled_Error() {
				Test_Task_Running_Canceled(
					loggingLevel: LogLevel.Error,
					targetValueTask: true,
					assert: (IRunningTask runningTask, IEnumerable<LoggingData> actualLogs) => {
						// check state
						Debug.Assert(runningTask != null);
						Task task = runningTask.Task;
						Debug.Assert(task != null);
						Debug.Assert(task.IsCompleted);

						// get expected logs
						IEnumerable<LoggingData> expectedLogs = new LoggingData[] {
							new ExpectedLogData(
								logData: CreateErrorLogData(task),
								exceptionChecker: e => e is OperationCanceledException
							)
						};

						// assert
						Assert.Equal(expectedLogs, actualLogs);
					}
				);
			}

			#endregion
		}

		#endregion
	}
}
