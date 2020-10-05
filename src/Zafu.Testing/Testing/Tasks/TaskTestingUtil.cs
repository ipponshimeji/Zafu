using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Zafu.Testing.Tasks {
	public static class TaskTestingUtil {
		#region methods

		public static Task WaitForCompletion(this Task task) {
			// check argument
			if (task == null) {
				throw new ArgumentNullException(nameof(task));
			}

			// wait for completion of the task
			try {
				task.Wait();
			} catch (AggregateException) {
				;   // continue
			}
			Debug.Assert(task.IsCompleted);

			return task;
		}

		#endregion
	}
}
