using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public class NullRunningContext: IRunningContext {
		#region data

		public static readonly NullRunningContext Instance = new NullRunningContext();

		#endregion


		#region creation

		private NullRunningContext() {
		}

		#endregion


		#region IRunningContext

		public ILogger Logger => NullLogger.Instance;

		public LogLevel LoggingLevel => LogLevel.None;

		public IRunningTaskMonitor RunningTaskMonitor => NullRunningTaskMonitor.Instance;

		#endregion
	}
}
