using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zafu.Testing;
using Zafu.Testing.Tasks;
using Xunit;

namespace Zafu.Tasks.Testing {
	public class IRunningTaskTableTestBase {
		#region constants

		public static readonly TimeSpan LongTimeout = TimeSpan.FromSeconds(3);

		public static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);

		#endregion


		#region samples

		public class DisposeSample {
			#region data

			public readonly TimeSpan WaitingTimeout;

			public readonly TimeSpan CancelingTimeOut;

			#endregion


			#region properties

			public bool IsNoTimeout => (this.WaitingTimeout == TimeSpan.Zero && this.CancelingTimeOut == TimeSpan.Zero);

			#endregion


			#region creation

			public DisposeSample(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
				// initialize members
				this.WaitingTimeout = waitingTimeout;
				this.CancelingTimeOut = cancelingTimeOut;
			}

			#endregion


			#region overrides

			public override string ToString() {
				static string toDisplayText(TimeSpan val) {
					if (val == TimeSpan.Zero) {
						return "0";
					} else if (val == LongTimeout) {
						return "long";
					} else if (val == ShortTimeout) {
						return "short";
					} else {
						return val.ToString();
					}
				}

				return $"waitingTimeout: {toDisplayText(this.WaitingTimeout)}, cancelingTimeOut: {toDisplayText(this.CancelingTimeOut)}";
			}

			#endregion
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose(TimeSpan timeout) {
			// check argument
			if (timeout == TimeSpan.Zero) {
				throw new ArgumentException("It must not be zero.", nameof(timeout));
			}

			return new DisposeSample[] {
				// waitingTimeout: 0, cancelingTimeOut: 0
				new DisposeSample(TimeSpan.Zero, TimeSpan.Zero),
				// waitingTimeout: timeout, cancelingTimeOut: 0
				new DisposeSample(timeout, TimeSpan.Zero),
				// waitingTimeout: 0, cancelingTimeOut: timeout
				new DisposeSample(TimeSpan.Zero, timeout),
				// waitingTimeout: timeout, cancelingTimeOut: timeout
				new DisposeSample(timeout, timeout)
			}.ToTestData();
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose_NoTask() {
			return GetSamplesFor_Dispose(LongTimeout);
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose_WaitingTask() {
			return GetSamplesFor_Dispose(LongTimeout);
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose_CancellingTask() {
			return new DisposeSample[] {
				// waitingTimeout: 0, cancelingTimeOut: 0
				new DisposeSample(TimeSpan.Zero, TimeSpan.Zero),
				// waitingTimeout: short, cancelingTimeOut: 0
				new DisposeSample(ShortTimeout, TimeSpan.Zero),
				// waitingTimeout: 0, cancelingTimeOut: short
				new DisposeSample(TimeSpan.Zero, LongTimeout),
				// waitingTimeout: short, cancelingTimeOut: long
				new DisposeSample(ShortTimeout, LongTimeout),
			}.ToTestData();
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose_UncancellableTask() {
			return GetSamplesFor_Dispose(ShortTimeout);
		}

		public static IEnumerable<object[]> GetSamplesFor_Dispose_Mixed() {
			return GetSamplesFor_Dispose(TimeSpan.FromSeconds(1));
		}

		#endregion
	}


	public abstract class IRunningTaskTableTestBase<TTarget>: IRunningTaskTableTestBase where TTarget: IRunningTaskTable {
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


		#region methods

		protected static Task<(bool, int)> CallDisposeOnAnotherThread(TTarget target, TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// check argument
			if (target == null) {
				throw new ArgumentNullException(nameof(target));
			}

			// call Dispose(TimeSpan, TimeSpan) method on another thread
			Thread? workingThread = null;
			bool finished = false;
			Task<(bool, int)> workingTask = Task.Run(() => {
				try {
					workingThread = Thread.CurrentThread;
					bool completed = target.Dispose(waitingTimeout, cancelingTimeOut);
					int count = target.RunningTaskCount;
					return (completed, count);
				} finally {
					finished = true;
				}
			});

			// wait for the working thread to be waiting state
			// Note that the working thread is not blocked if both waitingTimeout and cancelingTimeOut are 0.
			while (finished == false && (workingThread == null || (workingThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0)) {
				Thread.Sleep(100);
			}

			// resume the sampleAction
			return workingTask;
		}

		protected static Task<(bool, int)> CallDisposeOnAnotherThread(TTarget target, DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			return CallDisposeOnAnotherThread(target, sample.WaitingTimeout, sample.CancelingTimeOut);
		}

		#endregion


		#region tests

		[Fact(DisplayName = "Count")]
		public void Count() {
			// arrange
			using (PausableTestingAction sampleAction1 = new PausableTestingAction()) {
				using (PausableTestingAction sampleAction2 = new PausableTestingAction()) {
					using (PausableTestingAction sampleAction3 = new PausableTestingAction()) {
						TTarget target = CreateTarget();
						try {
							IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;

							// act & assert

							// run task1
							IRunningTask runningTask1 = taskMonitor.MonitorTask(sampleAction1.UncancellablePausableAction);
							sampleAction1.WaitForPause();
							Assert.Equal(1, target.RunningTaskCount);

							// run task2
							IRunningTask runningTask2 = taskMonitor.MonitorTask(sampleAction2.UncancellablePausableAction);
							sampleAction2.WaitForPause();
							Assert.Equal(2, target.RunningTaskCount);

							// complete task2
							sampleAction2.Resume();
							runningTask2.WaitForCompletion();
							Assert.Equal(1, target.RunningTaskCount);

							// run task3
							IRunningTask runningTask3 = taskMonitor.MonitorTask(sampleAction3.UncancellablePausableAction);
							sampleAction3.WaitForPause();
							Assert.Equal(2, target.RunningTaskCount);

							// complete task1
							sampleAction1.Resume();
							runningTask1.WaitForCompletion();
							Assert.Equal(1, target.RunningTaskCount);

							// complete task3
							sampleAction3.Resume();
							runningTask3.WaitForCompletion();
							Assert.Equal(0, target.RunningTaskCount);

							// dispose the target
							target.Dispose();

							// run additional routine assertion
							AssertTarget(target);
						} finally {
							DisposeTarget(target);
						}
					}
				}
			}
		}

		[Theory(DisplayName = "Dispose; no task")]
		[MemberData(nameof(GetSamplesFor_Dispose_NoTask))]
		public void  Dispose_NoTask(DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			// arrange
			TestingAction sampleAction = new TestingAction();
			TTarget target = CreateTarget();
			try {
				// run a task
				IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
				IRunningTask runningTask = taskMonitor.MonitorTask(sampleAction.SimpleAction);
				runningTask.WaitForCompletion();
				Debug.Assert(target.RunningTaskCount == 0);

				// act
				bool actualCompleted = target.Dispose(sample.WaitingTimeout, sample.CancelingTimeOut);
				int actualCount = target.RunningTaskCount;

				// assert
				Assert.True(actualCompleted);
				Assert.Equal(0, actualCount);
				Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "Dispose; waiting task")]
		[MemberData(nameof(GetSamplesFor_Dispose_WaitingTask))]
		public void Dispose_WaitingTasks(DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			// arrange
			using (PausableTestingAction sampleAction = new PausableTestingAction()) {
				TTarget target = CreateTarget();
				try {
					// run a task
					IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
					IRunningTask runningTask = taskMonitor.MonitorTask(sampleAction.UncancellablePausableAction);

					sampleAction.WaitForPause();
					Debug.Assert(target.RunningTaskCount == 1);

					// act
					Task<(bool, int)> disposeTask = CallDisposeOnAnotherThread(target, sample);
					sampleAction.Resume();
					runningTask.WaitForCompletion();
					(bool actualCompleted, int actualCount) = disposeTask.Sync();

					// assert
					if (sample.IsNoTimeout) {
						Assert.False(actualCompleted);
						Assert.Equal(1, actualCount);
					} else {
						Assert.True(actualCompleted);
						Assert.Equal(0, actualCount);
					}
					Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "Dispose; cancelling task")]
		[MemberData(nameof(GetSamplesFor_Dispose_CancellingTask))]
		public void Dispose_CancellingTask(DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			// arrange
			TestingAction sampleAction = new TestingAction();
			TTarget target = CreateTarget();
			try {
				// run a task
				IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
				IRunningTask runningTask = taskMonitor.MonitorTask(sampleAction.CancellableAction);
				Debug.Assert(target.RunningTaskCount == 1);

				// act
				bool actualCompleted = target.Dispose(sample.WaitingTimeout, sample.CancelingTimeOut);
				int actualCount = target.RunningTaskCount;
				if (actualCompleted == false) {
					runningTask.Cancel();
				}
				runningTask.WaitForCompletion();

				// assert
				if (sample.CancelingTimeOut == TimeSpan.Zero) {
					Assert.False(actualCompleted);
					Assert.Equal(1, actualCount);
				} else {
					Assert.True(actualCompleted);
					Assert.Equal(0, actualCount);
				}
				// Works.Worked should not be done due to the cancellation.
				Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Theory(DisplayName = "Dispose; uncancellable task")]
		[MemberData(nameof(GetSamplesFor_Dispose_UncancellableTask))]
		public void Dispose_UncancellableTask(DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			// arrange
			using (PausableTestingAction sampleAction = new PausableTestingAction(throwOnCancellation: false)) {
				TTarget target = CreateTarget();
				try {
					// run a task
					IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
					IRunningTask runningTask = taskMonitor.MonitorTask(sampleAction.UncancellablePausableAction);

					sampleAction.WaitForPause();
					Debug.Assert(target.RunningTaskCount == 1);

					// act
					Task<(bool, int)> workingTask = CallDisposeOnAnotherThread(target, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
					(bool actualCompleted, int actualCount) = workingTask.Sync();

					sampleAction.Resume();
					runningTask.WaitForCompletion();

					// assert
					Assert.False(actualCompleted);  // should be timeouted
					Assert.Equal(1, actualCount);
					Assert.Equal(TestingAction.Works.All, sampleAction.Progress);
					// run additional routine assertion
					AssertTarget(target);
				} finally {
					DisposeTarget(target);
				}
			}
		}

		[Theory(DisplayName = "Dispose; mixed")]
		[MemberData(nameof(GetSamplesFor_Dispose_Mixed))]
		public void Dispose_Mixed(DisposeSample sample) {
			// check argument
			if (sample == null) {
				throw new ArgumentNullException(nameof(sample));
			}

			// arrange
			int expectedCount;
			if (sample.CancelingTimeOut == TimeSpan.Zero) {
				if (sample.WaitingTimeout == TimeSpan.Zero) {
					expectedCount = 3;
				} else {
					expectedCount = 2;
				}
			} else {
				expectedCount = 1;
			}

			TestingAction sampleAction_cancelling = new TestingAction();
			using (PausableTestingAction sampleAction_waiting = new PausableTestingAction()) {
				using (PausableTestingAction sampleAction_uncancellable = new PausableTestingAction()) {
					TTarget target = CreateTarget();
					try {
						// run three tasks
						IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
						IRunningTask runningTask_waiting = taskMonitor.MonitorTask(sampleAction_waiting.UncancellablePausableAction);
						IRunningTask runningTask_cancelling = taskMonitor.MonitorTask(sampleAction_cancelling.CancellableAction);
						IRunningTask runningTask_uncancellable = taskMonitor.MonitorTask(sampleAction_uncancellable.UncancellablePausableAction);

						sampleAction_waiting.WaitForPause();
						sampleAction_uncancellable.WaitForPause();
						Debug.Assert(target.RunningTaskCount == 3);

						// act
						Task<(bool, int)> workingTask = CallDisposeOnAnotherThread(target, sample);
						sampleAction_waiting.Resume();
						(bool actualCompleted, int actualCount) = workingTask.Sync();
						if (runningTask_cancelling.IsCancellationRequested == false) {
							runningTask_cancelling.Cancel();
						}
						sampleAction_uncancellable.Resume();

						runningTask_waiting.WaitForCompletion();
						runningTask_cancelling.WaitForCompletion();
						runningTask_uncancellable.WaitForCompletion();

						// assert
						Assert.False(actualCompleted);
						Assert.Equal(expectedCount, actualCount);
						Assert.Equal(TestingAction.Works.All, sampleAction_waiting.Progress);
						Assert.Equal(TestingAction.Works.Terminated, sampleAction_cancelling.Progress);
						Assert.Equal(TestingAction.Works.All, sampleAction_uncancellable.Progress);
						// run additional routine assertion
						AssertTarget(target);
					} finally {
						DisposeTarget(target);
					}
				}
			}
		}

		[Fact(DisplayName = "Dispose; IDisposable.Dispose")]
		public void Dispose_IDisposable() {
			// arrange
			TestingAction sampleAction = new TestingAction();
			TTarget target = CreateTarget();
			try {
				// run a task
				IRunningTaskMonitor taskMonitor = target.RunningTaskMonitor;
				IRunningTask runningTask = taskMonitor.MonitorTask(sampleAction.CancellableAction);
				Debug.Assert(target.RunningTaskCount == 1);

				// act
				target.Dispose();
				int actualCount = target.RunningTaskCount;
				if (0 < actualCount) {
					runningTask.Cancel();
				}
				runningTask.WaitForCompletion();

				// assert
				Assert.Equal(0, actualCount);
				// Works.Worked should not be done due to the cancellation.
				Assert.Equal(TestingAction.Works.Terminated, sampleAction.Progress);
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		[Fact(DisplayName = "Dispose; adds task after dispose")]
		public void Dispose_AddTaskAfterDispose() {
			// arrange
			TTarget target = CreateTarget();
			try {
				bool result = target.Dispose(TimeSpan.Zero, TimeSpan.Zero);
				Debug.Assert(result);

				// act
				Assert.Throws<ObjectDisposedException>(() => {
					target.RunningTaskMonitor.MonitorTask(() => { });
				});

				// assert
				// run additional routine assertion
				AssertTarget(target);
			} finally {
				DisposeTarget(target);
			}
		}

		#endregion
	}
}
