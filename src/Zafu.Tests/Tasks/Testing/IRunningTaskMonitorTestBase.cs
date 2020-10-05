using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Testing;
using Zafu.Testing.Tasks;
using Xunit;

namespace Zafu.Tasks.Testing {
	public abstract class IRunningTaskMonitorTestBase {
		#region overridables

		protected abstract IRunningTaskMonitor CreateTarget();

		#endregion


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

		#endregion


		#region tests

		[Fact(DisplayName = "simple action; successful")]
		public void SimpleAction_Successful() {
			// arrange
			TestingAction sampleAction = new TestingAction();
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.WaitForCompletion();

			// assert
			// All works should be done.
			Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "simple action; exception")]
		public void SimpleAction_Exception() {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestingAction sampleAction = new TestingAction(exception);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.WaitForCompletion();

			// assert
			// Works.Worked should not be done due to the exception.
			Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask);
		}

		[Fact(DisplayName = "simple action; try to cancel")]
		public void SimpleAction_Cancel() {
			// arrange
			TestingAction sampleAction = new TestingAction();
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			// try to cancel, but it gives no effect
			runningTask.Cancel();

			Task actualTask = runningTask.Task;
			actualTask.WaitForCompletion();

			// assert
			// All works should be done.
			Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "simple action; action: null")]
		public void SimpleAction_action_null() {
			// arrange
			IRunningTaskMonitor target = CreateTarget();
			Action action = null!;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				target.MonitorTask(action);
			});

			// assert
			Assert.Equal("action", actual.ParamName);
		}

		protected void Test_CancellableAction_Successful(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			TestingAction sampleAction = new TestingAction();
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.WaitForCompletion();

			// assert
			// All works should be done.
			Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Theory(DisplayName = "cancellable action; successful")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Successful(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_Successful);
		}

		public void Test_CancellableAction_Exception(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestingAction sampleAction = new TestingAction(exception);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.WaitForCompletion();

			// assert
			// Works.Worked should not be done due to the exception.
			Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask);
		}

		[Theory(DisplayName = "cancellable action; exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Exception(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_Exception);
		}

		protected void Test_CancellableAction_Canceled(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			PausableTestingAction sampleAction = new PausableTestingAction(throwOnCancellation: false);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestingAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			actualTask.WaitForCompletion();

			// assert
			// Works.Worked should not be done due to the cancellation.
			// Note that Cancel() works even if the cancellationTokenSource is null,
			// because a CancellationTokenSource is created in Cancel() in that case.
			Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Theory(DisplayName = "cancellable action; canceled")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_Canceled(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_Canceled);
		}

		protected void Test_CancellableAction_CanceledWithException(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource) {
			// arrange
			PausableTestingAction sampleAction = new PausableTestingAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestingAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			actualTask.WaitForCompletion();

			// assert
			// Works.Worked should not be done due to the cancellation.
			// Note that Cancel() works even if the cancellationTokenSource is null,
			// because a CancellationTokenSource is created in Cancel() in that case.
			Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
			Assert.True(actualTask.IsCanceled);
		}

		[Theory(DisplayName = "cancellable action; canceled with exception")]
		[MemberData(nameof(GetSyncCallingPatternData))]
		public void CancellableAction_CanceledWithException(SyncCallingPattern pattern) {
			pattern.Test(this.Test_CancellableAction_CanceledWithException);
		}

		[Fact(DisplayName = "cancellable action; action: null")]
		public void CancellableAction_action_null() {
			// arrange
			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = null!;

			// act
			ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
				target.MonitorTask(action);
			});

			// assert
			Assert.Equal("action", actual.ParamName);
		}

		protected void Test_Task_Done_Successful(CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// arrange
			IRunningTaskMonitor target = CreateTarget();
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
			IRunningTaskMonitor target = CreateTarget();
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
			IRunningTaskMonitor target = CreateTarget();
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
			PausableTestingAction sampleAction = new PausableTestingAction();
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None; 
			Task task = sampleAction.GetPausableActionTask(cancellationToken);
			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestingAction.Works.Started);

			// act
			IRunningTask? runningTask = targetValueTask switch {
				true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
				_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
			};
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.Resume();
			actualTask.WaitForCompletion();

			// assert
			// All works should be done.
			Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
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
			PausableTestingAction sampleAction = new PausableTestingAction(exception);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
			Task task = sampleAction.GetPausableActionTask(cancellationToken);
			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestingAction.Works.Started);

			// act
			IRunningTask? runningTask = targetValueTask switch {
				true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
				_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
			};
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.Resume();
			actualTask.WaitForCompletion();

			// assert
			// Works.Worked should not be done due to the exception.
			Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask);
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
			PausableTestingAction sampleAction = new PausableTestingAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestingAction.Works.None);

			IRunningTaskMonitor target = CreateTarget();
			CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
			Task task = sampleAction.GetPausableActionTask(cancellationToken);
			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestingAction.Works.Started);

			// act
			IRunningTask? runningTask = targetValueTask switch {
				true => target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource),
				_ => target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource)
			};
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			runningTask.Cancel();	// cancel the task
			sampleAction.Resume();
			actualTask.WaitForCompletion();

			// assert
			if (cancellationTokenSource == null) {
				// All works should be done, because runningTask.Cancel() gave no effect.
				Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
				Assert.True(actualTask.IsCompletedSuccessfully);
			} else {
				// Works.Worked should not be done due to the exception.
				Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
				Assert.True(actualTask.IsCanceled);
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

		#endregion
	}
}
