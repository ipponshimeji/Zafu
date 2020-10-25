using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Tasks.Testing;
using Zafu.Testing.ObjectModel;
using Zafu.Testing.Logging;

namespace Zafu.Tasks.Tests {
	public class NullRunningTaskMonitorTest {
		#region IRunningTaskMonitor

		public class IRunningTaskMonitorTest: IRunningTaskMonitorTest<NullRunningTaskMonitor> {
			#region overrides

			protected override NullRunningTaskMonitor CreateTarget() {
				return NullRunningTaskMonitor.Instance;
			}

			#endregion
		}

		#endregion
	}
}
