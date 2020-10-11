using System;

namespace Zafu.Tasks {
	public interface IRunningTaskTable: IDisposable {
		public static readonly TimeSpan DefaultDisposeWaitingTimeout = TimeSpan.FromSeconds(2);

		public static readonly TimeSpan DefaultDisposeCancelingTimeout = TimeSpan.FromSeconds(3);


		IRunningTaskMonitor RunningTaskMonitor { get; }

		int RunningTaskCount { get; }

		bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut);
	}
}
