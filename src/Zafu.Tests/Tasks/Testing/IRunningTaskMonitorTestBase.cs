using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Zafu.Tasks.Testing {
	public abstract class IRunningTaskMonitorTestBase {
		#region types

		public class TestingCancellationTokenSource: CancellationTokenSource {
			#region data

			private int disposeCount = 0;

			#endregion


			#region properties

			public int DisposeCount => this.disposeCount;

			#endregion


			#region creation & disposal

			public TestingCancellationTokenSource(): base() {
			}

			protected override void Dispose(bool disposing) {
				if (disposing) {
					Interlocked.Increment(ref this.disposeCount);
				}
				base.Dispose(disposing);
			}

			#endregion
		}

		#endregion


		#region overridables

		protected abstract IRunningTaskMonitor CreateTarget();

		#endregion


		#region utilities

		protected void AssertExceptionOnTask(Exception expectedException, Task task, bool nested = false) {
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
			if (nested) {
				AggregateException? nestedException = aggregateException.InnerException as AggregateException;
				Assert.NotNull(nestedException);
				Debug.Assert(nestedException != null);
				Assert.Single(nestedException.InnerExceptions);
				Assert.Equal(expectedException, nestedException.InnerException);
			} else {
				Assert.Equal(expectedException, aggregateException.InnerException);
			}
		}

		#endregion


		#region tests

		[Fact(DisplayName = "simple action; successful")]
		public void simpleaction_successful() {
			// arrange
			TestAction sampleAction = new TestAction();
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// All works should be done.
			int expectedProgress = TestAction.Works.All;

			IRunningTaskMonitor target = CreateTarget();
			Action action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.Wait();

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "simple action; exception")]
		public void simpleaction_exception() {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestAction sampleAction = new TestAction(exception);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the exception.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask);
		}

		[Fact(DisplayName = "simple action; action: null")]
		public void simpleaction_action_null() {
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

		[Fact(DisplayName = "cancellable action; successful")]
		public void cancellableaction_successful() {
			// arrange
			TestAction sampleAction = new TestAction();
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// All works should be done.
			int expectedProgress = TestAction.Works.All;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			actualTask.Wait();

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "cancellable action; exception")]
		public void cancellableaction_exception() {
			// arrange
			InvalidOperationException exception = new InvalidOperationException();
			TestAction sampleAction = new TestAction(exception);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the exception.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.SimpleAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask);
		}

		[Fact(DisplayName = "cancellable action; cancel; cancellationTokenSource: null")]
		public void cancellableaction_cancel_cancellationTokenSource_null() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: false);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			actualTask.Wait();

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "cancellable action; cancel by exception; cancellationTokenSource: null")]
		public void cancellableaction_cancel_by_exception_cancellationTokenSource_null() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;

			// act
			IRunningTask runningTask = target.MonitorTask(action);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			AggregateException aggregateException = Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCanceled);
			Assert.IsAssignableFrom<TaskCanceledException>(aggregateException.InnerException);
		}

		[Fact(DisplayName = "cancellable action; cancel by exception; cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: false")]
		public void cancellableaction_cancel_by_exception_cancellationTokenSource_nonnull_doNotDisposeCancellationTokenSource_false() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;
			TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource();
			Debug.Assert(cancellationTokenSource.DisposeCount == 0);
			bool doNotDisposeCancellationTokenSource = false;

			// act
			IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			AggregateException aggregateException = Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCanceled);
			Assert.IsAssignableFrom<TaskCanceledException>(aggregateException.InnerException);
			Assert.Equal(1, cancellationTokenSource.DisposeCount);
		}

		[Fact(DisplayName = "cancellable action; cancel by exception; cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: true")]
		public void cancellableaction_cancel_by_exception_cancellationTokenSource_nonnull_doNotDisposeCancellationTokenSource_true() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Action<CancellationToken> action = sampleAction.PausableAction;
			using (TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource()) {
				Debug.Assert(cancellationTokenSource.DisposeCount == 0);
				bool doNotDisposeCancellationTokenSource = true;

				// act
				IRunningTask runningTask = target.MonitorTask(action, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Debug.Assert(runningTask != null);
				Task actualTask = runningTask.Task;

				sampleAction.WaitForPause();
				Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
				runningTask.Cancel();
				sampleAction.Resume();

				AggregateException aggregateException = Assert.Throws<AggregateException>(() => actualTask.Wait());

				// assert
				Assert.Equal(expectedProgress, sampleAction.Progress);
				Assert.True(actualTask.IsCanceled);
				Assert.IsAssignableFrom<TaskCanceledException>(aggregateException.InnerException);
				Assert.Equal(0, cancellationTokenSource.DisposeCount);
			}
		}

		[Fact(DisplayName = "cancellable action; action: null")]
		public void cancellableaction_action_null() {
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

		[Fact(DisplayName = "task; successful")]
		public void task_successful() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: false);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// All works should be done.
			int expectedProgress = TestAction.Works.All;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetPausableActionTask(cancellationTokenSource: null);
			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

			// act
			IRunningTask? runningTask = target.MonitorTask(task);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.Resume();
			actualTask.Wait();

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCompletedSuccessfully);
		}

		[Fact(DisplayName = "task; exception")]
		public void task_exception() {
			// arrange
			Exception exception = new InvalidOperationException();
			PausableTestAction sampleAction = new PausableTestAction(exception, throwOnCancellation: false);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the exception.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetPausableActionTask(cancellationTokenSource: null);
			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

			// act
			IRunningTask? runningTask = target.MonitorTask(task);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.Resume();
			Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask, nested: true);
		}

		[Fact(DisplayName = "task; cancel by exception; cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: false")]
		public void task_cancel_by_exception_cancellationTokenSource_nonnull_doNotDisposeCancellationTokenSource_false() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource();
			Debug.Assert(cancellationTokenSource.DisposeCount == 0);
			Task task = sampleAction.GetPausableActionTask(cancellationTokenSource);
			bool doNotDisposeCancellationTokenSource = false;

			// act
			IRunningTask? runningTask = target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource);
			Assert.NotNull(runningTask);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;

			sampleAction.WaitForPause();
			Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
			runningTask.Cancel();
			sampleAction.Resume();

			AggregateException aggregateException = Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			Assert.True(actualTask.IsCanceled);
			Assert.IsAssignableFrom<TaskCanceledException>(aggregateException.InnerException);
			Assert.Equal(1, cancellationTokenSource.DisposeCount);
		}

		[Fact(DisplayName = "task; cancel by exception; cancellationTokenSource: non-null, doNotDisposeCancellationTokenSource: true")]
		public void task_cancel_by_exception_cancellationTokenSource_nonnull_doNotDisposeCancellationTokenSource_true() {
			// arrange
			PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done due to the cancellation.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			using (TestingCancellationTokenSource cancellationTokenSource = new TestingCancellationTokenSource()) {
				Debug.Assert(cancellationTokenSource.DisposeCount == 0);
				Task task = sampleAction.GetPausableActionTask(cancellationTokenSource);
				bool doNotDisposeCancellationTokenSource = true;

				// act
				IRunningTask? runningTask = target.MonitorTask(task, cancellationTokenSource, doNotDisposeCancellationTokenSource);
				Assert.NotNull(runningTask);
				Debug.Assert(runningTask != null);
				Task actualTask = runningTask.Task;

				sampleAction.WaitForPause();
				Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);
				runningTask.Cancel();
				sampleAction.Resume();

				AggregateException aggregateException = Assert.Throws<AggregateException>(() => actualTask.Wait());

				// assert
				Assert.Equal(expectedProgress, sampleAction.Progress);
				Assert.True(actualTask.IsCanceled);
				Assert.IsAssignableFrom<TaskCanceledException>(aggregateException.InnerException);
				Assert.Equal(0, cancellationTokenSource.DisposeCount);
			}
		}







		[Fact(DisplayName = "task; done, successful")]
		public void task_done_successful() {
			// arrange
			TestAction sampleAction = new TestAction();
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// All works should be done.
			int expectedProgress = TestAction.Works.All;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetSimpleActionTask(cancellationTokenSource: null);
			task.Wait();

			// act
			IRunningTask? runningTask = target.MonitorTask(task);

			// assert
			Assert.Null(runningTask);
			Assert.Equal(expectedProgress, sampleAction.Progress);
		}

		[Fact(DisplayName = "task; done, exception")]
		public void task_done_exception() {
			// arrange
			NotSupportedException exception = new NotSupportedException();
			TestAction sampleAction = new TestAction(exception);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done because the exception occurs.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetSimpleActionTask(cancellationTokenSource: null);
			Assert.Throws<AggregateException>(() => task.Wait());

			// act
			IRunningTask? runningTask = target.MonitorTask(task);
			Assert.NotNull(runningTask);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			Assert.Throws<AggregateException>(() => task.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask, nested: true);
		}

		[Fact(DisplayName = "value task; done, successful")]
		public void valuetask_done_successful() {
			// arrange
			TestAction sampleAction = new TestAction();
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// All works should be done.
			int expectedProgress = TestAction.Works.All;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetSimpleActionTask(cancellationTokenSource: null);
			ValueTask valueTask = new ValueTask(task);
			task.Wait();

			// act
			IRunningTask? runningTask = target.MonitorTask(valueTask);

			// assert
			Assert.Null(runningTask);
			Assert.Equal(expectedProgress, sampleAction.Progress);
		}

		[Fact(DisplayName = "value task; done, exception")]
		public void valuetask_done_exception() {
			// arrange
			NotSupportedException exception = new NotSupportedException();
			TestAction sampleAction = new TestAction(exception);
			Debug.Assert(sampleAction.Progress == TestAction.Works.None);
			// Works.Worked should not be done because the exception occurs.
			int expectedProgress = TestAction.Works.Terminated;

			IRunningTaskMonitor target = CreateTarget();
			Task task = sampleAction.GetSimpleActionTask(cancellationTokenSource: null);
			ValueTask valueTask = new ValueTask(task);
			Assert.Throws<AggregateException>(() => task.Wait());

			// act
			IRunningTask? runningTask = target.MonitorTask(valueTask);
			Assert.NotNull(runningTask);
			Debug.Assert(runningTask != null);
			Task actualTask = runningTask.Task;
			Assert.Throws<AggregateException>(() => actualTask.Wait());

			// assert
			Assert.Equal(expectedProgress, sampleAction.Progress);
			AssertExceptionOnTask(exception, actualTask, nested: true);
		}

		#endregion
	}
}
