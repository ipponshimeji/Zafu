using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Zafu.Tasks.Testing;

namespace Zafu.Tasks.Tests {
	public class TaskUtilTest {
		#region Sync

		public class Sync {
			#region types

			public abstract class SyncTestBase<TTarget, TTask, T> where TTask : Task {
				#region overridables

				protected abstract T GetResult();

				protected abstract TTask GetSimpleActionTask(TestAction sampleAction, CancellationToken cancellationToken, T result);

				protected abstract TTask GetPausableActionTask(PausableTestAction sampleAction, CancellationToken cancellationToken, T result);

				protected abstract TTarget GetTarget(TTask task);

				protected abstract T CallSync(TTarget target, bool passThroughAggregateException);

				#endregion


				#region methods

				protected static void WaitForCompletion(Task task) {
					// check arguments
					if (task == null) {
						throw new ArgumentNullException(nameof(task));
					}

					// wait for the task's completion suppressing AggregateException exception
					try {
						task.Wait();
					} catch (AggregateException) {
						// ignore the exception
					}
				}

				/// <summary>
				///  Calls <see cref="CallSync(TTarget, bool)"/> method on another thread and resume the task which the method is executing.
				///  This method is used to run <see cref="CallSync(TTarget, bool)"/> to execute a pausing pausable action.
				///  Note that you have no way to resume the action if <see cref="CallSync(TTarget, bool)"/> method is run on the current thread.
				/// </summary>
				/// <param name="target"></param>
				/// <param name="passThroughAggregateException"></param>
				/// <param name="sampleAction"></param>
				/// <returns></returns>
				protected (T, Exception?) CallSyncOnAnotherThread(TTarget target, bool passThroughAggregateException, PausableTestAction sampleAction) {
					// check argument
					if (sampleAction == null) {
						throw new ArgumentNullException(nameof(sampleAction));
					}
					if (sampleAction.Progress != PausableTestAction.Works.Started) {
						throw new ArgumentException("It should be at 'Started' progress.", nameof(sampleAction));
					}

					// call CallSync method on another thread
					// Note that CallSync won't return until the sampleAction is resumed.
					Exception? capturedException = null;
					T actualResult = default(T);
					Thread? workingThread = null;
					Task workingTask = Task.Run(() => {
						workingThread = Thread.CurrentThread;
						try {
							actualResult = CallSync(target, passThroughAggregateException);
						} catch (Exception exception) {
							capturedException = exception;
							// continue
						}
					});

					// wait for the working thread to be waiting state
					while (workingThread == null || (workingThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0) {
						Thread.Sleep(100);
					}

					// resume the sampleAction
					sampleAction.Resume();
					workingTask.Wait();

					return (actualResult, capturedException);
				}

				#endregion


				#region tests

				protected void Test_Done_Successful(Action<TTarget>? additionalAsserts = null) {
					// arrange
					TestAction sampleAction = new TestAction();
					Debug.Assert(sampleAction.Progress == TestAction.Works.None);

					T result = GetResult();
					TTask task = GetSimpleActionTask(sampleAction, CancellationToken.None, result);
					task.Wait();
					Debug.Assert(task.IsCompletedSuccessfully); // task has already finished

					TTarget target = GetTarget(task);
					bool passThroughAggregateException = false;

					// act
					T actualResult = CallSync(target, passThroughAggregateException);

					// assert
					// All works should be done.
					Assert.Equal(TestAction.Works.All, sampleAction.Progress);
					Assert.Equal(result, actualResult);
					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Done_Exception(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					// arrange
					Exception exception = new InvalidOperationException();
					TestAction sampleAction = new TestAction(exception);
					Debug.Assert(sampleAction.Progress == TestAction.Works.None);

					T result = GetResult();
					TTask task = GetSimpleActionTask(sampleAction, CancellationToken.None, result);
					WaitForCompletion(task);
					Debug.Assert(task.IsFaulted);   // task has already finished

					TTarget target = GetTarget(task);

					// act
					Exception capturedException = Assert.ThrowsAny<Exception>(() => {
						CallSync(target, passThroughAggregateException);
					});

					// assert
					// Works.Worked should not be done due to the exception.
					Assert.Equal(TestAction.Works.Terminated, sampleAction.Progress);

					Exception? actualException;
					if (passThroughAggregateException) {
						// capturedException should be an AggregateException which wraps the original exception
						Assert.IsType<AggregateException>(capturedException);
						Debug.Assert(capturedException != null);
						actualException = ((AggregateException)capturedException).InnerException;
					} else {
						// capturedException should be the unwrapped exception
						actualException = capturedException;
					}
					Assert.Equal(exception, actualException);
					Debug.Assert(actualException != null);
					// The source of the exception should not be one when it was rethrown but the original one. 
					Assert.Equal(TestAction.ExceptionSourceMethod, actualException.TargetSite);

					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Done_CanceledWithException(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						TestAction sampleAction = new TestAction(throwOnCancellation: true);
						Debug.Assert(sampleAction.Progress == TestAction.Works.None);

						cancellationTokenSource.Cancel();
						T result = GetResult();
						TTask task = GetSimpleActionTask(sampleAction, cancellationTokenSource.Token, result);
						WaitForCompletion(task);
						Debug.Assert(task.IsCanceled);   // task was canceled before it started

						TTarget target = GetTarget(task);

						// act
						Exception capturedException = Assert.ThrowsAny<Exception>(() => {
							CallSync(target, passThroughAggregateException);
						});

						// assert
						// The task was canceled before it started.
						Assert.Equal(TestAction.Works.None, sampleAction.Progress);

						Exception? actualException;
						if (passThroughAggregateException) {
							// capturedException should be an AggregateException which wraps the original exception
							Assert.IsType<AggregateException>(capturedException);
							Debug.Assert(capturedException != null);
							actualException = ((AggregateException)capturedException).InnerException;
						} else {
							// capturedException should be the unwrapped exception
							actualException = capturedException;
						}
						Assert.IsType<TaskCanceledException>(actualException);

						if (additionalAsserts != null) {
							additionalAsserts(target);
						}
					}
				}

				protected void Test_Running_Successful(Action<TTarget>? additionalAsserts = null) {
					// arrange
					PausableTestAction sampleAction = new PausableTestAction();
					Debug.Assert(sampleAction.Progress == TestAction.Works.None);

					T result = GetResult();
					TTask task = GetPausableActionTask(sampleAction, CancellationToken.None, result);
					// The task should not finish at this point.
					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

					TTarget target = GetTarget(task);
					bool passThroughAggregateException = false;

					// act
					(T actualResult, Exception? actualException) = CallSyncOnAnotherThread(target, passThroughAggregateException, sampleAction);
					WaitForCompletion(task);
					Debug.Assert(task.IsCompleted);

					// assert
					// All works should be done.
					Assert.Equal(TestAction.Works.All, sampleAction.Progress);
					Assert.Equal(result, actualResult);
					Assert.Null(actualException);
					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Running_Exception(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					// arrange
					Exception exception = new NotSupportedException();
					PausableTestAction sampleAction = new PausableTestAction(exception);
					Debug.Assert(sampleAction.Progress == TestAction.Works.None);

					T result = GetResult();
					TTask task = GetPausableActionTask(sampleAction, CancellationToken.None, result);
					// The task should not finish at this point.
					sampleAction.WaitForPause();
					Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

					TTarget target = GetTarget(task);

					// act
					(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, passThroughAggregateException, sampleAction);
					WaitForCompletion(task);
					Debug.Assert(task.IsCompleted);

					// assert
					// Works.Worked should not be done due to the exception.
					Assert.Equal(TestAction.Works.Terminated, sampleAction.Progress);

					Exception? actualException;
					if (passThroughAggregateException) {
						// capturedException should be an AggregateException which wraps the original exception
						Assert.IsType<AggregateException>(capturedException);
						Debug.Assert(capturedException != null);
						actualException = ((AggregateException)capturedException).InnerException;
					} else {
						// capturedException should be an unwrapped exception
						actualException = capturedException;
					}
					Assert.Equal(exception, actualException);
					Debug.Assert(actualException != null);
					// The source of the exception should not be one when it was rethrown but the original one. 
					Assert.Equal(TestAction.ExceptionSourceMethod, actualException.TargetSite);

					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Running_CanceledWithException(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: true);
						Debug.Assert(sampleAction.Progress == TestAction.Works.None);

						T result = GetResult();
						TTask task = GetPausableActionTask(sampleAction, cancellationTokenSource.Token, result);
						// The task should not finish at this point.
						sampleAction.WaitForPause();
						Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

						TTarget target = GetTarget(task);

						// act
						cancellationTokenSource.Cancel();
						(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, passThroughAggregateException, sampleAction);
						WaitForCompletion(task);
						Debug.Assert(task.IsCompleted);

						// assert
						// Works.Worked should not be done due to the cancellation.
						Assert.Equal(TestAction.Works.Terminated, sampleAction.Progress);

						Exception? actualException;
						if (passThroughAggregateException) {
							// capturedException should be an AggregateException which wraps the original exception
							Assert.IsType<AggregateException>(capturedException);
							Debug.Assert(capturedException != null);
							actualException = ((AggregateException)capturedException).InnerException;
						} else {
							// capturedException should be an unwrapped exception
							actualException = capturedException;
						}
						Assert.IsType<TaskCanceledException>(actualException);

						if (additionalAsserts != null) {
							additionalAsserts(target);
						}
					}
				}

				protected void Test_Running_CanceledVoluntary(Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						PausableTestAction sampleAction = new PausableTestAction(throwOnCancellation: false);
						Debug.Assert(sampleAction.Progress == TestAction.Works.None);

						T result = GetResult();
						TTask task = GetPausableActionTask(sampleAction, cancellationTokenSource.Token, result);
						// The task should not finish at this point.
						sampleAction.WaitForPause();
						Debug.Assert(sampleAction.Progress == PausableTestAction.Works.Started);

						TTarget target = GetTarget(task);

						// act
						cancellationTokenSource.Cancel();
						(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, false, sampleAction);
						WaitForCompletion(task);
						Debug.Assert(task.IsCompleted);

						// assert
						// Works.Worked should not be done due to the cancellation.
						Assert.Equal(TestAction.Works.Terminated, sampleAction.Progress);
						Assert.Equal(result, actualResult);
						if (additionalAsserts != null) {
							additionalAsserts(target);
						}
					}
				}

				#endregion
			}

			// ValueTuple is used as 'Unit'.
			public abstract class Sync_NonGeneric<TTarget>: SyncTestBase<TTarget, Task, ValueTuple> {
				#region overrides

				protected override ValueTuple GetResult() {
					// return 'Unit' as a dummy result
					return default(ValueTuple);
				}

				protected override Task GetSimpleActionTask(TestAction sampleAction, CancellationToken cancellationToken, ValueTuple result) {
					// check argument
					Debug.Assert(sampleAction != null);
					// ignore the 'result' argument

					return sampleAction.GetSimpleActionTask(cancellationToken);
				}

				protected override Task GetPausableActionTask(PausableTestAction sampleAction, CancellationToken cancellationToken, ValueTuple result) {
					// check argument
					Debug.Assert(sampleAction != null);
					// ignore the 'result' argument

					return sampleAction.GetPausableActionTask(cancellationToken);
				}

				#endregion
			}

			public abstract class Sync_TaskT<T>: SyncTestBase<Task<T>, Task<T>, T> {
				#region overrides

				protected override Task<T> GetSimpleActionTask(TestAction sampleAction, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(sampleAction != null);

					return sampleAction.GetSimpleActionTask<T>(cancellationToken, result);
				}

				protected override Task<T> GetPausableActionTask(PausableTestAction sampleAction, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(sampleAction != null);

					return sampleAction.GetPausableActionTask<T>(cancellationToken, result);
				}

				protected override Task<T> GetTarget(Task<T> task) {
					return task;
				}

				protected override T CallSync(Task<T> target, bool passThroughAggregateException) {
					return TaskUtil.Sync<T>(target, passThroughAggregateException);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "done; successful")]
				public void Done_Successful() {
					Test_Done_Successful();
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: false")]
				public void Done_Exception_passThroughAggregateException_false() {
					Test_Done_Exception(passThroughAggregateException: false);
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: true")]
				public void Done_Exception_passThroughAggregateException_true() {
					Test_Done_Exception(passThroughAggregateException: true);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: false")]
				protected void Done_CanceledWithException_passThroughAggregateException_false() {
					Test_Done_CanceledWithException(passThroughAggregateException: false);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: true")]
				protected void Done_CanceledWithException_passThroughAggregateException_true() {
					Test_Done_CanceledWithException(passThroughAggregateException: true);
				}

				[Fact(DisplayName = "running; successful")]
				public void Running_Successful() {
					Test_Running_Successful(additionalAsserts: (task) => {
						Assert.True(task.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: false")]
				public void Running_Exception_passThroughAggregateException_false() {
					Test_Running_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (task) => {
							Assert.True(task.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: true")]
				public void Running_Exception_passThroughAggregateException_true() {
					Test_Running_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (task) => {
							Assert.True(task.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: false")]
				public void Running_CanceledWithException_passThroughAggregateException_false() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (task) => {
							Assert.True(task.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: true")]
				public void Running_CanceledWithException_passThroughAggregateException_true() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (task) => {
							Assert.True(task.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled voluntary")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntary(additionalAsserts: (task) => {
						Assert.True(task.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "task: null")]
				public void task_null() {
					// arrange
					Task<T> task = null!;

					// act
					ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
						TaskUtil.Sync<T>(task);
					});

					// assert
					Assert.Equal("task", actual.ParamName);
				}

				#endregion
			}

			public abstract class Sync_ValueTaskT<T>: SyncTestBase<ValueTask<T>, Task<T>, T> {
				#region overrides

				protected override Task<T> GetSimpleActionTask(TestAction sampleAction, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(sampleAction != null);

					return sampleAction.GetSimpleActionTask<T>(cancellationToken, result);
				}

				protected override Task<T> GetPausableActionTask(PausableTestAction sampleAction, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(sampleAction != null);

					return sampleAction.GetPausableActionTask<T>(cancellationToken, result);
				}

				protected override ValueTask<T> GetTarget(Task<T> task) {
					return new ValueTask<T>(task);
				}

				protected override T CallSync(ValueTask<T> target, bool passThroughAggregateException) {
					return TaskUtil.Sync<T>(target, passThroughAggregateException);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "done; successful")]
				public void Done_Successful() {
					Test_Done_Successful(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: false")]
				public void Done_Exception_passThroughAggregateException_false() {
					Test_Done_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: true")]
				public void Done_Exception_passThroughAggregateException_true() {
					Test_Done_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: false")]
				protected void Done_CanceledWithException_passThroughAggregateException_false() {
					Test_Done_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: true")]
				protected void Done_CanceledWithException_passThroughAggregateException_true() {
					Test_Done_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; successful")]
				public void Running_Successful() {
					Test_Running_Successful(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: false")]
				public void Running_Exception_passThroughAggregateException_false() {
					Test_Running_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: true")]
				public void Running_Exception_passThroughAggregateException_true() {
					Test_Running_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: false")]
				public void Running_CanceledWithException_passThroughAggregateException_false() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: true")]
				public void Running_CanceledWithException_passThroughAggregateException_true() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled voluntary")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntary(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				#endregion
			}

			#endregion


			#region tests

			public class Sync_Task: Sync_NonGeneric<Task> {
				#region overrides

				protected override Task GetTarget(Task task) {
					return task;
				}

				protected override ValueTuple CallSync(Task target, bool passThroughAggregateException) {
					TaskUtil.Sync(target, passThroughAggregateException);

					// return 'Unit' as a dummy result
					return default(ValueTuple);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "done; successful")]
				public void Done_Successful() {
					Test_Done_Successful();
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: false")]
				public void Done_Exception_passThroughAggregateException_false() {
					Test_Done_Exception(passThroughAggregateException: false);
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: true")]
				public void Done_Exception_passThroughAggregateException_true() {
					Test_Done_Exception(passThroughAggregateException: true);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: false")]
				protected void Done_CanceledWithException_passThroughAggregateException_false() {
					Test_Done_CanceledWithException(passThroughAggregateException: false);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: true")]
				protected void Done_CanceledWithException_passThroughAggregateException_true() {
					Test_Done_CanceledWithException(passThroughAggregateException: true);
				}

				[Fact(DisplayName = "running; successful")]
				public void Running_Successful() {
					Test_Running_Successful(additionalAsserts: (task) => {
						Assert.True(task.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: false")]
				public void Running_Exception_passThroughAggregateException_false() {
					Test_Running_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (task) => {
							Assert.True(task.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: true")]
				public void Running_Exception_passThroughAggregateException_true() {
					Test_Running_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (task) => {
							Assert.True(task.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: false")]
				public void Running_CanceledWithException_passThroughAggregateException_false() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (task) => {
							Assert.True(task.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: true")]
				public void Running_CanceledWithException_passThroughAggregateException_true() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (task) => {
							Assert.True(task.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled voluntary")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntary(additionalAsserts: (task) => {
						Assert.True(task.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "task: null")]
				public void task_null() {
					// arrange
					Task task = null!;

					// act
					ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
						TaskUtil.Sync(task);
					});

					// assert
					Assert.Equal("task", actual.ParamName);
				}

				#endregion
			}

			public class Sync_ValueTask: Sync_NonGeneric<ValueTask> {
				#region overrides

				protected override ValueTask GetTarget(Task task) {
					return new ValueTask(task);
				}

				protected override ValueTuple CallSync(ValueTask target, bool passThroughAggregateException) {
					TaskUtil.Sync(target, passThroughAggregateException);

					// return 'Unit' as a dummy result
					return default(ValueTuple);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "done; successful")]
				public void Done_Successful() {
					Test_Done_Successful(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: false")]
				public void Done_Exception_passThroughAggregateException_false() {
					Test_Done_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "done; exception; passThroughAggregateException: true")]
				public void Done_Exception_passThroughAggregateException_true() {
					Test_Done_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: false")]
				protected void Done_CanceledWithException_passThroughAggregateException_false() {
					Test_Done_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "done; canceled with exception; passThroughAggregateException: true")]
				protected void Done_CanceledWithException_passThroughAggregateException_true() {
					Test_Done_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; successful")]
				public void Running_Successful() {
					Test_Running_Successful(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: false")]
				public void Running_Exception_passThroughAggregateException_false() {
					Test_Running_Exception(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; exception; passThroughAggregateException: true")]
				public void Running_Exception_passThroughAggregateException_true() {
					Test_Running_Exception(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsFaulted);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: false")]
				public void Running_CanceledWithException_passThroughAggregateException_false() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: false,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled with exception; passThroughAggregateException: true")]
				public void Running_CanceledWithException_passThroughAggregateException_true() {
					Test_Running_CanceledWithException(
						passThroughAggregateException: true,
						additionalAsserts: (valueTask) => {
							Assert.True(valueTask.IsCanceled);
						}
					);
				}

				[Fact(DisplayName = "running; canceled voluntary")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntary(additionalAsserts: (valueTask) => {
						Assert.True(valueTask.IsCompletedSuccessfully);
					});
				}

				#endregion
			}

			public class Sync_Task_String: Sync_TaskT<string> {
				#region overrides
	
				protected override string GetResult() {
					return "abc";
				}

				#endregion
			}

			public class Sync_ValueTask_Int32: Sync_ValueTaskT<int> {
				#region overrides

				protected override int GetResult() {
					return 31;
				}

				#endregion
			}

			#endregion
		}

		#endregion
	}
}
