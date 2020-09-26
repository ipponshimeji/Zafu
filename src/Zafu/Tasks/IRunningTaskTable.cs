using System;
using System.Threading.Tasks;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public interface IRunningTaskTable: IDisposable {
		public static readonly TimeSpan DefaultDisposeWaitingTimeout = TimeSpan.FromSeconds(2);

		public static readonly TimeSpan DefaultDisposeCancelingTimeout = TimeSpan.FromSeconds(3);


		IRunningTaskMonitor Monitor { get; }

		bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut);
	}
}
