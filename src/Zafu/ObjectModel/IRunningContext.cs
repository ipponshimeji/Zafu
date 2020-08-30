using System;
using Microsoft.Extensions.Logging;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public interface IRunningContext {
		public const LogLevel DefaultLogLevel = LogLevel.Warning;

		ILogger Logger { get; }

		LogLevel LoggingLevel { get; }

		IRunningTaskMonitor RunningTaskMonitor { get; }
	}
}
