using System;
using Microsoft.Extensions.Logging;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public abstract class RunningContextBase: IRunningContext {
		#region data

		private LogLevel loggingLevel = IRunningContext.DefaultLogLevel;

		#endregion


		#region properties

		public LogLevel LoggingLevel {
			get {
				return this.loggingLevel;
			}
			set {
				this.loggingLevel = value;
			}
		}

		#endregion


		#region IRunningContext

		public abstract ILogger Logger { get; }

		public abstract IRunningTaskMonitor RunningTaskMonitor { get; }

		// LoggingLevel is defined as a property with get/set accessor.
	
		#endregion
	}
}
