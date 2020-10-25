using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Zafu.Testing.Tasks;

namespace Zafu.Tasks.Tests {
	public class TaskUtilTest {
		#region Sync

		public class Sync {
			#region types

			public abstract class SyncTestBase<TTarget, TTask, T> where TTask : Task {
				#region overridables

				protected abstract T GetResult();

				protected abstract TTask GetSimpleActionTask(SimpleActionState actionState, CancellationToken cancellationToken, T result);

				protected abstract TTask GetPausableActionTask(PausableActionState actionState, CancellationToken cancellationToken, T result);

				protected abstract TTarget GetTarget(TTask task);

				protected abstract T CallSync(TTarget target, bool passThroughAggregateException);

				#endregion


				#region methods

				/// <summary>
				///  Calls <see cref="CallSync(TTarget, bool)"/> method on another thread and resume the task which the method is executing.
				///  This method is used to run <see cref="CallSync(TTarget, bool)"/> to execute a pausing pausable action.
				///  Note that you have no way to resume the action if <see cref="CallSync(TTarget, bool)"/> method is run on the current thread.
				/// </summary>
				/// <param name="target"></param>
				/// <param name="passThroughAggregateException"></param>
				/// <param name="actionState"></param>
				/// <returns></returns>
				protected (T, Exception?) CallSyncOnAnotherThread(TTarget target, bool passThroughAggregateException, PausableActionState actionState) {
					// check argument
					if (actionState == null) {
						throw new ArgumentNullException(nameof(actionState));
					}
					if (actionState.Progress != PausableActionState.Works.Started) {
						throw new ArgumentException("It should be at 'Started' progress.", nameof(actionState));
					}

					// call CallSync method on another thread
					// Note that CallSync won't return until the actionState is resumed.
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

					// resume the actionState
					actionState.Resume();
					workingTask.Wait();

					return (actualResult, capturedException);
				}

				#endregion


				#region tests

				protected void Test_Done_Successful(Action<TTarget>? additionalAsserts = null) {
					// arrange
					SimpleActionState actionState = new SimpleActionState();
					Debug.Assert(actionState.Progress == TestingActionState.Works.None);

					T result = GetResult();
					TTask task = GetSimpleActionTask(actionState, CancellationToken.None, result);
					task.WaitForCompletion();
					Debug.Assert(task.IsCompletedSuccessfully); // task has already finished

					TTarget target = GetTarget(task);
					bool passThroughAggregateException = false;

					// act
					T actualResult = CallSync(target, passThroughAggregateException);

					// assert
					// All works should be done.
					Assert.Equal(TestingActionState.Works.All, actionState.Progress);
					Assert.Equal(result, actualResult);
					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Done_Exception(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					// arrange
					Exception exception = new InvalidOperationException();
					SimpleActionState actionState = new SimpleActionState(exception);
					Debug.Assert(actionState.Progress == TestingActionState.Works.None);

					T result = GetResult();
					TTask task = GetSimpleActionTask(actionState, CancellationToken.None, result);
					task.WaitForCompletion();
					Debug.Assert(task.IsFaulted);   // task has already finished

					TTarget target = GetTarget(task);

					// act
					Exception capturedException = Assert.ThrowsAny<Exception>(() => {
						CallSync(target, passThroughAggregateException);
					});

					// assert
					// Works.Worked should not be done due to the exception.
					Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);

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
					Assert.Equal(TestingActionState.ExceptionSourceMethod, actualException.TargetSite);

					if (additionalAsserts != null) {
						additionalAsserts(target);
					}
				}

				protected void Test_Done_CanceledWithException(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						using (PausableActionState actionState = new PausableActionState(throwOnCancellation: true)) {
							Debug.Assert(actionState.Progress == TestingActionState.Works.None);

							T result = GetResult();
							TTask task = GetPausableActionTask(actionState, cancellationTokenSource.Token, result);

							// cancel the task
							actionState.WaitForPause();
							Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

							cancellationTokenSource.Cancel();
							actionState.Resume();
							task.WaitForCompletion();
							Debug.Assert(task.IsCanceled);

							TTarget target = GetTarget(task);

							// act
							Exception capturedException = Assert.ThrowsAny<Exception>(() => {
								CallSync(target, passThroughAggregateException);
							});

							// assert
							// Works.Worked should not be done due to the cancellation.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);

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
							Assert.IsAssignableFrom<OperationCanceledException>(actualException);

							if (additionalAsserts != null) {
								additionalAsserts(target);
							}
						}
					}
				}

				protected void Test_Running_Successful(Action<TTarget>? additionalAsserts = null) {
					// arrange
					using (PausableActionState actionState = new PausableActionState()) {
						Debug.Assert(actionState.Progress == TestingActionState.Works.None);

						T result = GetResult();
						TTask task = GetPausableActionTask(actionState, CancellationToken.None, result);
						// The task should not finish at this point.
						actionState.WaitForPause();
						Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

						TTarget target = GetTarget(task);
						bool passThroughAggregateException = false;

						// act
						(T actualResult, Exception? actualException) = CallSyncOnAnotherThread(target, passThroughAggregateException, actionState);
						task.WaitForCompletion();
						Debug.Assert(task.IsCompleted);

						// assert
						// All works should be done.
						Assert.Equal(TestingActionState.Works.All, actionState.Progress);
						Assert.Equal(result, actualResult);
						Assert.Null(actualException);
						if (additionalAsserts != null) {
							additionalAsserts(target);
						}
					}
				}

				protected void Test_Running_Exception(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					// arrange
					Exception exception = new NotSupportedException();
					using (PausableActionState actionState = new PausableActionState(exception)) {
						Debug.Assert(actionState.Progress == TestingActionState.Works.None);

						T result = GetResult();
						TTask task = GetPausableActionTask(actionState, CancellationToken.None, result);
						// The task should not finish at this point.
						actionState.WaitForPause();
						Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

						TTarget target = GetTarget(task);

						// act
						(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, passThroughAggregateException, actionState);
						task.WaitForCompletion();
						Debug.Assert(task.IsCompleted);

						// assert
						// Works.Worked should not be done due to the exception.
						Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);

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
						Assert.Equal(TestingActionState.ExceptionSourceMethod, actualException.TargetSite);

						if (additionalAsserts != null) {
							additionalAsserts(target);
						}
					}
				}

				protected void Test_Running_CanceledWithException(bool passThroughAggregateException, Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						using (PausableActionState actionState = new PausableActionState(throwOnCancellation: true)) {
							Debug.Assert(actionState.Progress == TestingActionState.Works.None);

							T result = GetResult();
							TTask task = GetPausableActionTask(actionState, cancellationTokenSource.Token, result);
							// The task should not finish at this point.
							actionState.WaitForPause();
							Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

							TTarget target = GetTarget(task);

							// act
							cancellationTokenSource.Cancel();
							(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, passThroughAggregateException, actionState);
							task.WaitForCompletion();
							Debug.Assert(task.IsCompleted);

							// assert
							// Works.Worked should not be done due to the cancellation.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);

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
							Assert.IsAssignableFrom<OperationCanceledException>(actualException);

							if (additionalAsserts != null) {
								additionalAsserts(target);
							}
						}
					}
				}

				protected void Test_Running_CanceledVoluntarily(Action<TTarget>? additionalAsserts = null) {
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource()) {
						// arrange
						using (PausableActionState actionState = new PausableActionState(throwOnCancellation: false)) {
							Debug.Assert(actionState.Progress == TestingActionState.Works.None);

							T result = GetResult();
							TTask task = GetPausableActionTask(actionState, cancellationTokenSource.Token, result);
							// The task should not finish at this point.
							actionState.WaitForPause();
							Debug.Assert(actionState.Progress == PausableActionState.Works.Started);

							TTarget target = GetTarget(task);

							// act
							cancellationTokenSource.Cancel();
							(T actualResult, Exception? capturedException) = CallSyncOnAnotherThread(target, false, actionState);
							task.WaitForCompletion();
							Debug.Assert(task.IsCompleted);

							// assert
							// Works.Worked should not be done due to the cancellation.
							Assert.Equal(TestingActionState.Works.Terminated, actionState.Progress);
							Assert.Equal(result, actualResult);
							if (additionalAsserts != null) {
								additionalAsserts(target);
							}
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

				protected override Task GetSimpleActionTask(SimpleActionState actionState, CancellationToken cancellationToken, ValueTuple result) {
					// check argument
					Debug.Assert(actionState != null);
					// ignore the 'result' argument

					return actionState.GetActionTask(cancellationToken);
				}

				protected override Task GetPausableActionTask(PausableActionState actionState, CancellationToken cancellationToken, ValueTuple result) {
					// check argument
					Debug.Assert(actionState != null);
					// ignore the 'result' argument

					return actionState.GetActionTask(cancellationToken);
				}

				#endregion
			}

			public abstract class Sync_TaskT<T>: SyncTestBase<Task<T>, Task<T>, T> {
				#region overrides

				protected override Task<T> GetSimpleActionTask(SimpleActionState actionState, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(actionState != null);

					return actionState.GetActionTask<T>(cancellationToken, result);
				}

				protected override Task<T> GetPausableActionTask(PausableActionState actionState, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(actionState != null);

					return actionState.GetActionTask<T>(cancellationToken, result);
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

				[Fact(DisplayName = "running; canceled voluntarily")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntarily(additionalAsserts: (task) => {
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

				protected override Task<T> GetSimpleActionTask(SimpleActionState actionState, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(actionState != null);

					return actionState.GetActionTask<T>(cancellationToken, result);
				}

				protected override Task<T> GetPausableActionTask(PausableActionState actionState, CancellationToken cancellationToken, T result) {
					// check argument
					Debug.Assert(actionState != null);

					return actionState.GetActionTask<T>(cancellationToken, result);
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

				[Fact(DisplayName = "running; canceled voluntarily")]
				public void Running_CanceledVoluntary() {
					Test_Running_CanceledVoluntarily(additionalAsserts: (valueTask) => {
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

				[Fact(DisplayName = "running; canceled voluntarily")]
				public void Running_CanceledVoluntarily() {
					Test_Running_CanceledVoluntarily(additionalAsserts: (task) => {
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

				[Fact(DisplayName = "running; canceled voluntarily")]
				public void Running_CanceledVoluntarily() {
					Test_Running_CanceledVoluntarily(additionalAsserts: (valueTask) => {
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
