using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public class RunningContext: IRunningContext {
		#region data

		public LogLevel LoggingLevel { get; set; } = IRunningContext.DefaultLogLevel;

		// the logger should not be changed.
		private readonly ILogger logger;

		// the running task monitor should not be changed.
		private readonly IRunningTaskMonitor runningTaskMonitor;

		#endregion


		#region creation

		public RunningContext(ILogger logger, IRunningTaskMonitor runningTaskMonitor) {
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

		public RunningContext(ILogger logger, Func<IRunningContext, IRunningTaskMonitor> runningTaskMonitorCreator) {
			// check arguments
			if (logger == null) {
				logger = NullLogger.Instance;
			}
			if (runningTaskMonitorCreator == null) {
				throw new ArgumentNullException(nameof(runningTaskMonitor));
			}

			// initialize members
			this.logger = logger;
			this.runningTaskMonitor = runningTaskMonitorCreator(this);
			if (this.runningTaskMonitor == null) {
				throw new ArgumentException("It returns null.", nameof(runningTaskMonitor));
			}
		}

		#endregion


		#region IRunningContext

		public ILogger Logger => this.logger;

		public IRunningTaskMonitor RunningTaskMonitor => this.runningTaskMonitor;

		// LoggingLevel is defined as a property with get/set accessor.

		#endregion
	}
}
