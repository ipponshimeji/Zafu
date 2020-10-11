using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Testing;
using Zafu.Testing.Tasks;
using Xunit;

namespace Zafu.Tasks.Testing {
	public class IRunningTaskMonitorTestBase {
		#region samples

		public class SyncCallingPattern {
			#region data

			public readonly bool? DoNotDisposeCancellationTokenSource;

			#endregion


			#region creation

			public SyncCallingPattern(bool? doNotDisposeCancellationTokenSource) {
				// initialize member
				this.DoNotDisposeCancellationTokenSource = doNotDisposeCancellationTokenSource;
			}

			#endregion


			#region overrides

			public override string ToString() {
				if (this.DoNotDisposeCancellationTokenSource.HasValue) {
					if (this.DoNotDisposeCancellationTokenSource.Value) {
						return "cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: true";
					} else {
						return "cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: false";
					}
				} else {
					return "cancellationTokenSource: null";
				}
			}

			#endregion


			#region methods

			public void Test(Action<CancellationTokenSource?, bool> test) {
				// check arbument
				if (test == null) {
					throw new ArgumentNullException(nameof(test));
				}

				if (this.DoNotDisposeCancellationTokenSource.HasValue) {
					if (this.DoNotDisposeCancellationTokenSource.Value) {
						// In this pattern,
						//   * A CancellationTokenSource should be given to the test.
						//   * But it should not be disposed by the test.
						using (TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource()) {
							Debug.Assert(cancellationTokenSource.DisposeCount == 0);

							// test
							test(cancellationTokenSource, true);

							// assert additionally
							// cancellationTokenSource should not be disposed
							Assert.Equal(0, cancellationTokenSource.DisposeCount);
						}
					} else {
						// In this pattern,
						//   * A CancellationTokenSource should be given to the test.
						//   * And it should be disposed by the test.
						TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource();
						Debug.Assert(cancellationTokenSource.DisposeCount == 0);

						// test
						test(cancellationTokenSource, false);

						// assert additionally
						// cancellationTokenSource should be disposed
						Assert.Equal(1, cancellationTokenSource.DisposeCount);
					}
				} else {
					// In this pattern,
					//   * No CancellationTokenSource should be given to the test.
					test(null, false);
				}
			}

			#endregion
		}

		public static IEnumerable<SyncCallingPattern> GetSyncCallingPatterns() {
			return new SyncCallingPattern[] {
				// cancellationTokenSource: null
				new SyncCallingPattern(null),
				// cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: false
				new SyncCallingPattern(false),
				// cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: true
				new SyncCallingPattern(true)
			};
		}

		public static IEnumerable<object[]> GetSyncCallingPatternData() {
			return GetSyncCallingPatterns().ToTestData();
		}

		#endregion


		#region utilities

		protected void AssertExceptionOnTask(Exception expectedException, Task task) {
			// check argument
			if (expectedException == null) {
				throw new ArgumentNullException(nameof(expectedException));
			}
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}

			// assert the exception info in the task
			Assert.True(task.IsFaulted);
			AggregateException? aggregateException = task.Exception;
			Assert.NotNull(aggregateException);
			Debug.Assert(aggregateException != null);
			Assert.Single(aggregateException.InnerExceptions);
			Assert.Equal(expectedException, aggregateException.InnerException);
		}

		protected void AssertCanceledExceptionOnTask(Task task) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}

			// assert the exception info in the task
			Assert.True(task.IsFaulted);
			AggregateException? aggregateException = task.Exception;
			Assert.NotNull(aggregateException);
			Debug.Assert(aggregateException != null);
			Assert.Single(aggregateException.InnerExceptions);
			Assert.IsAssignableFrom<OperationCanceledException>(aggregateException.InnerException);
		}

		#endregion
	}

	public abstract class IRunningTaskMonitorTestBase<TTarget>: IRunningTaskMonitorTestBase where TTarget: IRunningTaskMonitor {
		#region overridables

		protected abstract TTarget CreateTarget();

		protected virtual void DisposeTarget(TTarget target) {
			// do nothing by default
		}

		/// <summary>
		/// Runs additional routine assertion on the target.
		/// </summary>
		/// <param name="target"></param>
		protected virtual void AssertTarget(TTarget target) {
			// do nothing by default
		}

		#endregion


		#region tests

		[Fact(DisplayName = "simple action; successful")]
		public void SimpleAction_Successful() {
			// arrange
			TestingActionState sampleAction = new TestingActionState();
			Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

			TTarget target = CreateTarget();
			try {
				Action action = sampleAction.SimpleAction;

				// act
				IRunningTask runningTask = target.MonitorTask(action);
				Debug.Assert(runningTask != null);
				runningTask.WaitForCompletion();

				// assert
				// All works should be done.
				Assert.Equal(TestingActionState.Works.All, sampleAction.Progress);
				Assert.True(runningTask.Task.IsCompletedSuccessfully);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Fact(DisplayName = "simple action; exception")]
		public void SimpleAction_Exception() {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestingActionState sampleAction = new TestingActionState(exception);
			Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

			TTarget target = CreateTarget();
			try {
				Action action = sampleAction.SimpleAction;

				// act
				IRunningTask runningTask = target.MonitorTask(action);
				Debug.Assert(runningTask != null);
				runningTask.WaitForCompletion();

				// assert
				// Works.Worked should not be done due to the exception.
				Assert.Equal(TestingActionState.Works.Terminated, sampleAction.Progress);
				AssertExceptionOnTask(exception, runningTask.Task);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Fact(DisplayName = "simple action; try to cancel")]
		public void SimpleAction_TryToCancel() {
			// arrange
			using (PausableActionState sampleAction = new PausableActionState()) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					Action action = () => sampleAction.PausableAction(CancellationToken.None);

					// act
					IRunningTask runningTask = target.MonitorTask(action);
					Debug.Assert(runningTask != null);

					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);
					runningTask.Cancel();
					sampleAction.Resume();

					runningTask.WaitForCompletion();

					// assert
					// All works should be done.
					// The runningTask.Cancel() does not work in this case.
					Assert.Equal(TestingActionState.Works.All, sampleAction.Progress);
					Assert.True(runningTask.Task.IsCompletedSuccessfully);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Fact(DisplayName = "simple action; action: null")]
		public void SimpleAction_action_null() {
			// arrange
			TTarget target = CreateTarget();
			try {
				Action action = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(action);
				});

				// assert
				Assert.Equal("action", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		protected void Test_CancellableAction_Successful(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			TestingActionState sampleAction = new TestingActionState();
			Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

			TTarget target = CreateTarget();
			try {
				Action<CancellationToken> action = sampleAction.SimpleAction;

				// act
				IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Debug.Assert(runningTask != null);
				runningTask.WaitForCompletion();

				// assert
				// All works should be done.
				Assert.Equal(TestingActionState.Works.All, sampleAction.Progress);
				Assert.True(runningTask.Task.IsCompletedSuccessfully);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "cancellable action; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Successful(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_Successful);
		}

		public void Test_CancellableAction_Exception(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestingActionState sampleAction = new TestingActionState(exception);
			Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

			TTarget target = CreateTarget();
			try {
				Action<CancellationToken> action = sampleAction.SimpleAction;

				// act
				IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Debug.Assert(runningTask != null);
				runningTask.WaitForCompletion();

				// assert
				// Works.Worked should not be done due to the exception.
				Assert.Equal(TestingActionState.Works.Terminated, sampleAction.Progress);
				AssertExceptionOnTask(exception, runningTask.Task);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "cancellable action; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Exception(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_Exception);
		}

		protected void Test_CancellableAction_CanceledVoluntarily(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			using (PausableActionState sampleAction = new PausableActionState(throwOnCancellation: false)) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					Action<CancellationToken> action = sampleAction.PausableAction;

					// act
					IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
					Debug.Assert(runningTask != null);

					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);
					runningTask.Cancel();
					sampleAction.Resume();

					runningTask.WaitForCompletion();

					// assert
					// Works.Worked should not be done due to the cancellation.
					// Note that Cancel() works even if the cancellationTokenSource is null,
					// because a CancellationTokenSource is created in MonitorTask() in that case.
					Assert.Equal(TestingActionState.Works.Terminated, sampleAction.Progress);
					Assert.True(runningTask.Task.IsCompletedSuccessfully);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "cancellable action; canceled voluntarily")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_CanceledVoluntarily(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_CanceledVoluntarily);
		}

		protected void Test_CancellableAction_CanceledWithException(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			using (PausableActionState sampleAction = new PausableActionState(throwOnCancellation: true)) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					Action<CancellationToken> action = sampleAction.PausableAction;

					// act
					IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
					Debug.Assert(runningTask != null);

					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);
					runningTask.Cancel();
					sampleAction.Resume();

					runningTask.WaitForCompletion();

					// assert
					// Works.Worked should not be done due to the cancellation.
					// Note that Cancel() works even if the cancellationTokenSource is null,
					// because a CancellationTokenSource is created in MonitorTask() in that case.
					Assert.Equal(TestingActionState.Works.Terminated, sampleAction.Progress);
					AssertCanceledExceptionOnTask(runningTask.Task);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "cancellable action; canceled with exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_CanceledWithException(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_CanceledWithException);
		}

		[Fact(DisplayName = "cancellable action; action: null")]
		public void CancellableAction_action_null() {
			// arrange
			TTarget target = CreateTarget();
			try {
				Action<CancellationToken> action = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(action);
				});

				// assert
				Assert.Equal("action", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		protected void Test_Task_Done_Successful(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			TTarget target = CreateTarget();
			try {
				Task task = Task.FromResult(0);
				Debug.Assert(task.IsCompletedSuccessfully);

				// act
				IRunningTask? runningTask = targetValueTask switch {
					true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
					_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
				};

				// assert
				// No need to create a RunningTask because the task finished.
				Assert.Null(runningTask);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "Task; done; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Successful(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Successful(cts, dndcts, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; done; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Successful(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Successful(cts, dndcts, targetValueTask: true));
		}

		protected void Test_Task_Done_Exception(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			TTarget target = CreateTarget();
			try {
				Exception exception = new NotImplementedException();
				Task task = Task.FromException(exception);
				Debug.Assert(task.IsFaulted);

				// act
				IRunningTask? runningTask = targetValueTask switch {
					true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
					_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
				};

				// assert
				// No need to create a RunningTask because the task finished.
				Assert.Null(runningTask);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "Task; done; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Exception(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Exception(cts, dndcts, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; done; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Exception(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Exception(cts, dndcts, targetValueTask: true));
		}

		protected void Test_Task_Done_Canceled(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			TTarget target = CreateTarget();
			try {
				Task task;
				using (CancellationTokenSource cts = new CancellationTokenSource()) {
					cts.Cancel();
					task = Task.FromCanceled(cts.Token);
				}
				Debug.Assert(task.IsCanceled);

				// act
				IRunningTask? runningTask = targetValueTask switch {
					true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
					_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
				};

				// assert
				// No need to create a RunningTask because the task finished.
				Assert.Null(runningTask);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "Task; done; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Done_Canceled(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Canceled(cts, dndcts, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; done; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Done_Canceled(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Done_Canceled(cts, dndcts, targetValueTask: true));
		}

		public void Test_Task_Running_Successful(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			using (PausableActionState sampleAction = new PausableActionState()) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
					Task task = sampleAction.GetPausableActionTask(cancellationToken);
					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);

					// act
					IRunningTask? runningTask = targetValueTask switch {
						true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
						_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
					};
					Debug.Assert(runningTask != null);

					sampleAction.Resume();
					runningTask.WaitForCompletion();

					// assert
					// All works should be done.
					Assert.Equal(TestingActionState.Works.All, sampleAction.Progress);
					Assert.True(task.IsCompletedSuccessfully);
					Assert.True(runningTask.Task.IsCompletedSuccessfully);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "Task; running; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_Successful(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Running_Successful(cts, dndcts, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; running; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_Successful(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Running_Successful(cts, dndcts, targetValueTask: true));
		}

		protected void Test_Task_Running_Exception(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			Exception exception = new InvalidOperationException();
			using (PausableActionState sampleAction = new PausableActionState(exception)) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
					Task task = sampleAction.GetPausableActionTask(cancellationToken);
					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);

					// act
					IRunningTask? runningTask = targetValueTask switch {
						true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
						_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
					};
					Debug.Assert(runningTask != null);

					sampleAction.Resume();
					runningTask.WaitForCompletion();

					// assert
					// Works.Worked should not be done due to the exception.
					Assert.Equal(TestingActionState.Works.Terminated, sampleAction.Progress);
					Assert.True(task.IsFaulted);
					AssertExceptionOnTask(exception, runningTask.Task);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "Task; running; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_Exception(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Running_Exception(cts, dndcts, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; running; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_Exception(SyncCallingPattern pattern) {
			pattern.Test((cts, dndcts) => this.Test_Task_Running_Exception(cts, dndcts, targetValueTask: true));
		}

		protected void Test_Task_Running_CanceledWithException(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			using (PausableActionState sampleAction = new PausableActionState(throwOnCancellation: true)) {
				Debug.Assert(sampleAction.Progress == TestingActionState.Works.None);

				TTarget target = CreateTarget();
				try {
					CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
					Task task = sampleAction.GetPausableActionTask(cancellationToken);
					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableActionState.Works.Started);

					// act
					IRunningTask? runningTask = targetValueTask switch {
						true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
						_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
					};
					Debug.Assert(runningTask != null);

					runningTask.Cancel();   // cancel the task
					sampleAction.Resume();
					runningTask.WaitForCompletion();

					// assert
					if (cancellationTokenSource == null) {
						// All works should be done, because runningTask.Cancel() gave no effect.
						Assert.Equal(TestingActionState.Works.All, sampleAction.Progress);
						Assert.True(runningTask.Task.IsCompletedSuccessfully);
					} else {
						// Works.Worked should not be done due to the exception.
						// Note that Works.Finished is not marked if whole the task is canceled.
						Assert.Equal(0, sampleAction.Progress & TestingActionState.Works.Worked);
						AssertCanceledExceptionOnTask(runningTask.Task);
					}
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "Task; running; canceled with exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void Task_Running_CanceledWithException(SyncCallingPattern pattern) {
			pattern.Test((cts, dnd) => Test_Task_Running_CanceledWithException(cts, dnd, targetValueTask: false));
		}

		[Theory(DisplayName = "ValueTask; running; canceled with exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void ValueTask_Running_CanceledWithException(SyncCallingPattern pattern) {
			pattern.Test((cts, dnd) => Test_Task_Running_CanceledWithException(cts, dnd, targetValueTask: true));
		}

		[Fact(DisplayName = "Task; task: null")]
		public void Task_task_null() {
			// arrange
			TTarget target = CreateTarget();
			try {
				Task task = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					target.MonitorTask(task, null, false);
				});

				// assert
				Assert.Equal("task", actual.ParamName);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		#endregion
	}
}
