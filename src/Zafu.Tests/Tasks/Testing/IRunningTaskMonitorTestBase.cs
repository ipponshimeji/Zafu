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

		protected IRunningTask? CallMonitorTask(IRunningTaskMonitor target, Task task, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask) {
			// check arguments
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}
			// cancellationTokenSource can be null

			// call IRunningTaskMonitor.MonitorTask(Task or ValueTask, CancellationTokenSource?, bool)
			if (targetValueTask) {
				return target.MonitorTask(new ValueTask(task), cancellationTokenSource, doNotDisposeCancellationTokenSource);
			} else {
				return target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			}
		}

		#endregion
	}

	public abstract class IRunningTaskMonitorTestBase<TTarget>: IRunningTaskMonitorTestBase where TTarget: IRunningTaskMonitor {
		#region tests

		protected void Test_SimpleAction_Successful(TTarget target, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			SimpleActionState actionState = new SimpleActionState();
			Debug.Assert(actionState.Progress == TestingActionState.Works.None);

			Action action = actionState.UncancellableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			runningTask.WaitForCompletion();

			// assert
			assert(actionState, runningTask);
		}

		protected void Test_SimpleAction_Exception(TTarget target, Exception exception, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			if (exception == null) {
				throw new ArgumentNullException(nameof(exception));
			}
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			SimpleActionState actionState = new SimpleActionState(exception);
			Debug.Assert(actionState.Progress == TestingActionState.Works.None);

			Action action = actionState.UncancellableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			runningTask.WaitForCompletion();

			// assert
			assert(actionState, runningTask);
		}

		protected void Test_SimpleAction_TryToCancel(TTarget target, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			using (PausableActionState actionState = new PausableActionState()) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				// in this case, there is no CancellationTokenSource for the action
				Action action = actionState.UncancellableAction;

				// act
				IRunningTask runningTask = target.MonitorTask(action);
				Debug.Assert(runningTask != null);

				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);
				runningTask.Cancel();	// try to cancel, but it should give no effect
				actionState.Resume();

				runningTask.WaitForCompletion();

				// assert
				assert(actionState, runningTask);
			}
		}

		protected void Test_CancellableAction_Successful(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			// cancellationTokenSource can be null
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			SimpleActionState actionState = new SimpleActionState();
			Debug.Assert(actionState.Progress == TestingActionState.Works.None);

			Action<CancellationToken> action = actionState.Action;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			runningTask.WaitForCompletion();

			// assert
			assert(actionState, runningTask);
		}

		protected void Test_CancellableAction_Exception(TTarget target, Exception exception, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			if (exception == null) {
				throw new ArgumentNullException(nameof(exception));
			}
			// cancellationTokenSource can be null
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			SimpleActionState actionState = new SimpleActionState(exception);
			Debug.Assert(actionState.Progress == TestingActionState.Works.None);

			Action<CancellationToken> action = actionState.Action;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			runningTask.WaitForCompletion();

			// assert
			assert(actionState, runningTask);
		}

		protected void Test_CancellableAction_CanceledVoluntarily(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			// cancellationTokenSource can be null
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			using (PausableActionState actionState = new PausableActionState(throwOnCancellation: false)) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				Action<CancellationToken> action = actionState.Action;

				// act
				IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Debug.Assert(runningTask != null);

				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);
				runningTask.Cancel();
				actionState.Resume();

				runningTask.WaitForCompletion();

				// assert
				// Note that Cancel() works even if the cancellationTokenSource is null,
				// because a CancellationTokenSource is created in MonitorTask() in that case.
				assert(actionState, runningTask);
			}
		}

		protected void Test_CancellableAction_CanceledWithException(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, Action<TestingActionState, IRunningTask> assert) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}
			// cancellationTokenSource can be null
			if (assert == null) {
				throw new ArgumentNullException(nameof(assert));
			}

			// arrange
			using (PausableActionState actionState = new PausableActionState(throwOnCancellation: true)) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				Action<CancellationToken> action = actionState.Action;

				// act
				IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Debug.Assert(runningTask != null);

				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);
				runningTask.Cancel();
				actionState.Resume();

				runningTask.WaitForCompletion();

				// assert
				// Note that Cancel() works even if the cancellationTokenSource is null,
				// because a CancellationTokenSource is created in MonitorTask() in that case.
				assert(actionState, runningTask);
			}
		}

		protected void Test_Task_Done_Successful(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<Task, IRunningTask?> assert) {
			// arrange
			Task task = Task.FromResult(0);
			Debug.Assert(task.IsCompletedSuccessfully);

			// act
			IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);

			// assert
			assert(task, runningTask);
		}

		protected void Test_Task_Done_Exception(TTarget target, Exception exception, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<Task, IRunningTask?> assert) {
			// arrange
			Task task = Task.FromException(exception);
			Debug.Assert(task.IsFaulted);

			// act
			IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);

			// assert
			assert(task, runningTask);
		}

		protected void Test_Task_Done_Canceled(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<Task, IRunningTask?> assert) {
			// arrange
			Task task;
			using (CancellationTokenSource cts = new CancellationTokenSource()) {
				cts.Cancel();
				task = Task.FromCanceled(cts.Token);
			}
			Debug.Assert(task.IsCanceled);

			// act
			IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);

			// assert
			assert(task, runningTask);
		}

		protected void Test_Task_Running_Successful(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<TestingActionState, IRunningTask> assert) {
			// arrange
			using (PausableActionState actionState = new PausableActionState()) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
				Task task = Task.Run(() => actionState.Action(cancellationToken));
				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

				// act
				IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);
				Debug.Assert(runningTask != null);

				actionState.Resume();
				runningTask.WaitForCompletion();
				Debug.Assert(task.IsCompletedSuccessfully);

				// assert
				assert(actionState, runningTask);
			}
		}

		protected void Test_Task_Running_Exception(TTarget target, Exception exception, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<TestingActionState, IRunningTask> assert) {
			// arrange
			using (PausableActionState actionState = new PausableActionState(exception)) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
				Task task = Task.Run(() => actionState.Action(cancellationToken));
				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

				// act
				IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);
				Debug.Assert(runningTask != null);

				actionState.Resume();
				runningTask.WaitForCompletion();
				Debug.Assert(task.IsFaulted);

				// assert
				assert(actionState, runningTask);
			}
		}

		protected void Test_Task_Running_Canceled(TTarget target, CancellationTokenSource? cancellationTokenSource, bool doNotDisposeCancellationTokenSource, bool targetValueTask, Action<TestingActionState, IRunningTask> assert) {
			// arrange
			using (PausableActionState actionState = new PausableActionState(throwOnCancellation: true)) {
				Debug.Assert(actionState.Progress == TestingActionState.Works.None);

				CancellationToken cancellationToken = (cancellationTokenSource != null) ? cancellationTokenSource.Token : CancellationToken.None;
				Task task = actionState.GetActionTask(cancellationToken);
				actionState.WaitForPause();
				Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

				// act
				IRunningTask? runningTask = CallMonitorTask(target, task, cancellationTokenSource, doNotDisposeCancellationTokenSource, targetValueTask);
				Debug.Assert(runningTask != null);

				runningTask.Cancel();
				actionState.Resume();
				runningTask.WaitForCompletion();
				Assert.True(
					(cancellationTokenSource == null && task.IsCompletedSuccessfully) ||
					(cancellationTokenSource != null && task.IsCanceled)
				);

				// assert
				assert(actionState, runningTask);
			}
		}

		#endregion
	}
}
