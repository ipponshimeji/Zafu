using System;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public interface ITaskCanceler {
		Task? Task { get; }
		bool IsCancellationRequested { get; }
		void Cancel();
		void Cancel(bool throwOnFirstException);
		void CancelAfter(int millisecondsDelay);
		void CancelAfter(TimeSpan delay);
	}
}
