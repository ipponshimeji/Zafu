using System;
using Zafu.ObjectModel;

namespace Zafu.Tasks {
	public interface IRunningTaskMonitor {
		public static readonly TimeSpan DefaultDisposeWaitingTimeout = TimeSpan.FromSeconds(2);

		public static readonly TimeSpan DefaultDisposeCancelingTimeout = TimeSpan.FromSeconds(3);
	}
}
