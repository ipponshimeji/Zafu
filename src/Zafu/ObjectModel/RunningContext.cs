using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public class RunningContext: RunningContextBase {
		#region data

		// the logger should not be changed.
		private readonly ILogger logger;

		// the running task monitor should not be changed.
		private readonly IRunningTaskMonitor runningTaskMonitor;

		#endregion


		#region creation

		public RunningContext(ILogger? logger, IRunningTaskMonitor? runningTaskMonitor = null): base() {
			// check arguments
			if (logger == null) {
				logger = NullLogger.Instance;
			}
			if (runningTaskMonitor == null) {
				runningTaskMonitor = NullRunningTaskMonitor.Instance;
			}

			// initialize members
			this.logger = logger;
			this.runningTaskMonitor = runningTaskMonitor;
		}

		#endregion


		#region IRunningContext

		public override ILogger Logger => this.logger;

		public override IRunningTaskMonitor RunningTaskMonitor => this.runningTaskMonitor;

		#endregion
	}
}
