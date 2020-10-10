using System;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	/// <summary>
	/// The interface to bridge some metods of <see cref="System.Threading.CancellationTokenSource"/> object.
	/// This interface is used in <see cref="RunningTaskTable"/>.
	/// </summary>
	public interface IRunningTask {
		Task Task { get; }
		bool IsCancellationRequested { get; }
		void Cancel();
		void WaitForCompletion();
	}
}
