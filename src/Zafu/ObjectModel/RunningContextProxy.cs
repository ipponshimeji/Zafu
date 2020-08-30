using System;
using Microsoft.Extensions.Logging;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public class RunningContextProxy: IRunningContext {
		#region data

		protected readonly IRunningContext Target;

		#endregion


		#region creation

		public RunningContextProxy(IRunningContext? target = null) {
			// check arguments
			if (target == null) {
				target = ZafuEnvironment.DefaultRunningContext;
			}

			// initialize members
			this.Target = target;
		}

		#endregion


		#region IRunningContext

		public virtual ILogger Logger => this.Target.Logger;

		public virtual LogLevel LoggingLevel => this.Target.LoggingLevel;

		public virtual IRunningTaskMonitor RunningTaskMonitor => this.Target.RunningTaskMonitor;

		#endregion
	}
}
