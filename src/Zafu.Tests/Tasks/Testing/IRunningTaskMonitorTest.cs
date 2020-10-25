using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Testing;
using Zafu.Testing.Tasks;
using Xunit;
using System.Reflection;

namespace Zafu.Tasks.Testing {
	public abstract class IRunningTaskMonitorTest<TTarget>: IRunningTaskMonitorTestBase<TTarget> where TTarget : IRunningTaskMonitor {
		#region overridables
		#endregion


		#region utilities

		protected void Test(Action<TTarget> test) {
			TTarget target = CreateTarget();
			try {
				test(target);
			} finally {
				DisposeTarget(target);
			}
		}

		#endregion


		#region tests

		[Fact(DisplayName = "simple action; successful")]
		public void SimpleAction_Successful() {
			Test((TTarget target) => {
				Test_SimpleAction_Successful(
					target: target,
					assert: (TestingActionState actionState, IRunningTask runningTask) => {
						// assert
						// All works should be done.
						Assert.Equal(TestingActionState.Works.All, actionState.Progress);
						Assert.True(runningTask.Task.IsCompletedSuccessfully);
						// run additional routine assertion
						AssertTarget(target);
					}
				);
			});
		}

		[Fact(DisplayName = "simple action; exception")]
		public void SimpleAction_Exception() {
			InvalidOperationException exception = new InvalidOperationException();
			Test((TTarget target) => {
				Test_SimpleAction_Exception(
					target: target,
					exception: exception,
					assert: (TestingActionState actionState, IRunningTask runningTask) => {
						// assert
						// Works.Worked should not be done due to the exception.
						Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
						AssertExceptionOnTask(exception, runningTask.Task);
						// run additional routine assertion
						AssertTarget(target);
					}
				);
			});
		}

		[Fact(DisplayName = "simple action; try to cancel")]
		public void SimpleAction_TryToCancel() {
			Test((TTarget target) => {
				Test_SimpleAction_TryToCancel(
					target: target,
					assert: (TestingActionState actionState, IRunningTask runningTask) => {
						// assert
						// All works should be done.
						// The runningTask.Cancel() does not work in this case.
						Assert.Equal(TestingActionState.Works.All, actionState.Progress);
						Assert.True(runningTask.Task.IsCompletedSuccessfully);
						// run additional routine assertion
						AssertTarget(target);
					}
				);
			});
		}

		[Fact(DisplayName = "simple action; action: null")]
		public void SimpleAction_action_null() {
			Test((TTarget target) => {
				// arrange
				Action action = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(action);
				});

				// assert
				Assert.Equal("action", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			});
		}


		[Theory(DisplayName = "cancellable action; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Successful(SyncCallingPattern pattern) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_CancellableAction_Successful(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// assert
							// All works should be done.
							Assert.Equal(TestingActionState.Works.All, actionState.Progress);
							Assert.True(runningTask.Task.IsCompletedSuccessfully);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "cancellable action; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Exception(SyncCallingPattern pattern) {
			InvalidOperationException exception = new InvalidOperationException();
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_CancellableAction_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// assert
							// Works.Worked should not be done due to the exception.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
							AssertExceptionOnTask(exception, runningTask.Task);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "cancellable action; canceled voluntarily")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_CanceledVoluntarily(SyncCallingPattern pattern) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_CancellableAction_CanceledVoluntarily(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// Works.Worked should not be done due to the cancellation.
							// Note that Cancel() works even if the cancellationTokenSource is null,
							// because a CancellationTokenSource is created in MonitorTask() in that case.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
							Assert.True(runningTask.Task.IsCompletedSuccessfully);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "cancellable action; canceled with exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_CanceledWithException(SyncCallingPattern pattern) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_CancellableAction_CanceledWithException(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// Works.Worked should not be done due to the cancellation.
							// Note that Cancel() works even if the cancellationTokenSource is null,
							// because a CancellationTokenSource is created in MonitorTask() in that case.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
							AssertCanceledExceptionOnTask(runningTask.Task);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Fact(DisplayName = "cancellable action; action: null")]
		public void CancellableAction_action_null() {
			Test((TTarget target) => {
				// arrange
				Action<CancellationToken> action = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(action);
				});

				// assert
				Assert.Equal("action", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			});
		}


		private void TaskOrValueTask_Done_Successful(SyncCallingPattern pattern, bool targetValueTask) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Done_Successful(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (IRunningTask? runningTask) => {
							// No need to create a RunningTask because the task finished.
							Assert.Null(runningTask);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; done; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Successful(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Successful(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; done; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Successful(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Successful(pattern: pattern, targetValueTask: true);
		}

		private void TaskOrValueTask_Done_Exception(SyncCallingPattern pattern, bool targetValueTask) {
			Exception exception = new NotImplementedException();
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Done_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (IRunningTask? runningTask) => {
							// No need to create a RunningTask because the task finished.
							Assert.Null(runningTask);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; done; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Exception(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Exception(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; done; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Exception(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Exception(pattern: pattern, targetValueTask: true);
		}

		private void TaskOrValueTask_Done_Canceled(SyncCallingPattern pattern, bool targetValueTask) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Done_Canceled(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (IRunningTask? runningTask) => {
							// No need to create a RunningTask because the task finished.
							Assert.Null(runningTask);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; done; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Canceled(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Canceled(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; done; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Canceled(SyncCallingPattern pattern) {
			TaskOrValueTask_Done_Canceled(pattern: pattern, targetValueTask: true);
		}

		private void TaskOrValueTask_Running_Successful(SyncCallingPattern pattern, bool targetValueTask) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Running_Successful(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// All works should be done.
							Assert.Equal(TestingActionState.Works.All, actionState.Progress);
							Assert.True(runningTask.Task.IsCompletedSuccessfully);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; running; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_Successful(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Successful(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; running; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_Successful(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Successful(pattern: pattern, targetValueTask: true);
		}

		private void TaskOrValueTask_Running_Exception(SyncCallingPattern pattern, bool targetValueTask) {
			Exception exception = new InvalidOperationException();
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Running_Exception(
						target: target,
						exception: exception,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							// Works.Worked should not be done due to the exception.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
							AssertExceptionOnTask(exception, runningTask.Task);
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; running; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_Exception(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Exception(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; running; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_Exception(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Exception(pattern: pattern, targetValueTask: true);
		}

		private void TaskOrValueTask_Running_Canceled(SyncCallingPattern pattern, bool targetValueTask) {
			Test((TTarget target) => {
				pattern.Test((CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) => {
					Test_Task_Running_Canceled(
						target: target,
						cancellationTokenSource: cancellationTokenSource,
						doNotDisposeCancellationTokenSource: doNotDisposeCancellationTokenSource,
						targetValueTask: targetValueTask,
						assert: (TestingActionState actionState, IRunningTask runningTask) => {
							if (cancellationTokenSource == null) {
								// All works should be done, because runningTask.Cancel() gave no effect.
								Assert.Equal(TestingActionState.Works.All, actionState.Progress);
								Assert.True(runningTask.Task.IsCompletedSuccessfully);
							} else {
								// Works.Worked should not be done due to the cancellation.
								Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
								AssertCanceledExceptionOnTask(runningTask.Task);
							}
							// run additional routine assertion
							AssertTarget(target);
						}
					);
				});
			});
		}

		[Theory(DisplayName = "Task; running; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_Canceled(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Canceled(pattern: pattern, targetValueTask: false);
		}

		[Theory(DisplayName = "ValueTask; running; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_CanceledWithException(SyncCallingPattern pattern) {
			TaskOrValueTask_Running_Canceled(pattern: pattern, targetValueTask: true);
		}

		[Fact(DisplayName = "Task; task: null")]
		public void Task_task_null() {
			Test((TTarget target) => {
				// arrange
				Task task = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(task, null, false);
				});

				// assert
				Assert.Equal("task", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			});
		}

		#endregion
	}
}
