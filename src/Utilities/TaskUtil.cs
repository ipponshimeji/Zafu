using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Zafu.Utilities {
	public static class TaskUtil {
		#region methods

		private static void EnsureOnTime(bool onTime) {
			if (!onTime) {
				throw new TimeoutException();
			}
		}


		public static void Sync(this Task task, int millisecondsTimeout, CancellationToken cancellationToken) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}

			// wait for completion of the task
			if (task.IsCompleted == false) {
				EnsureOnTime(task.Wait(millisecondsTimeout, cancellationToken));
			}
		}

		public static void Sync(this Task task, CancellationToken cancellationToken) {
			Sync(task, Timeout.Infinite, cancellationToken);
		}

		public static void Sync(this Task task, int millisecondsTimeout = Timeout.Infinite) {
			Sync(task, millisecondsTimeout, CancellationToken.None);
		}


		public static void Sync(this ValueTask valueTask, int millisecondsTimeout, CancellationToken cancellationToken) {
			// wait for completion of the task
			if (valueTask.IsCompleted == false) {
				valueTask.AsTask().Sync(millisecondsTimeout, cancellationToken);
			}
		}

		public static void Sync(this ValueTask valueTask, CancellationToken cancellationToken) {
			Sync(valueTask, Timeout.Infinite, cancellationToken);
		}

		public static void Sync(this ValueTask valueTask, int millisecondsTimeout = Timeout.Infinite) {
			Sync(valueTask, millisecondsTimeout, CancellationToken.None);
		}


		public static T Sync<T>(this Task<T> task, int millisecondsTimeout, CancellationToken cancellationToken) {
			// wait for completion of the task
			((Task)task).Sync(millisecondsTimeout, cancellationToken);

			return task.Result;
		}

		public static T Sync<T>(this Task<T> task, CancellationToken cancellationToken) {
			return Sync<T>(task, Timeout.Infinite, cancellationToken);
		}

		public static T Sync<T>(this Task<T> task, int millisecondsTimeout = Timeout.Infinite) {
			return Sync<T>(task, millisecondsTimeout, CancellationToken.None);
		}


		public static T Sync<T>(this ValueTask<T> valueTask, int millisecondsTimeout, CancellationToken cancellationToken) {
			return valueTask.IsCompleted ? valueTask.Result : valueTask.AsTask().Sync(millisecondsTimeout, cancellationToken);
		}

		public static T Sync<T>(this ValueTask<T> valueTask, CancellationToken cancellationToken) {
			return Sync<T>(valueTask, Timeout.Infinite, cancellationToken);
		}

		public static T Sync<T>(this ValueTask<T> valueTask, int millisecondsTimeout = Timeout.Infinite) {
			return Sync<T>(valueTask, millisecondsTimeout, CancellationToken.None);
		}

		#endregion
	}
}
