using System;
using Microsoft.Extensions.Logging;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public interface IRunningContext {
		public const LogLevel DefaultLogLevel = LogLevel.Warning;

		public static IRunningContext CorrectWithDefault(IRunningContext? runningContext) {
			if (runningContext == null) {
				runningContext = ZafuEnvironment.DefaultRunningContext;
			}
			return runningContext;
		}


		ILogger Logger { get; }

		LogLevel LoggingLevel { get; }

		IRunningTaskMonitor RunningTaskMonitor { get; }
	}
}
