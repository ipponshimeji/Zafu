using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Tasks.Testing;

namespace Zafu.Tasks.Tests {
	public class RunningTaskTableTest {
		#region IRunningTaskMonitor

		public class IRunningTaskMonitorTest: IRunningTaskMonitorTestBase<RunningTaskTable> {
			#region overrides

			protected override RunningTaskTable CreateTarget() {
				return new RunningTaskTable();
			}

			protected override void DisposeTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				target.Dispose();
			}

			protected override void AssertTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				Assert.Equal(0, target.Count);
			}

			#endregion
		}

		#endregion
	}
}
