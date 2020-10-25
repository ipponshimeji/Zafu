using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Tasks.Testing;

namespace Zafu.Tasks.Tests {
	public class RunningTaskTableTest {
		#region IRunningTaskMonitor

		public class IRunningTaskMonitorTest: IRunningTaskMonitorTest<RunningTaskTable> {
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

				Assert.Equal(0, target.RunningTaskCount);
			}

			#endregion
		}

		#endregion


		#region IRunningTaskTable

		public class IRunningTaskTableTest: IRunningTaskTableTestBase<RunningTaskTable> {
			#region overrides

			protected override RunningTaskTable CreateTarget() {
				return new RunningTaskTable();
			}

			protected override void DisposeTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				bool disposed = target.IsDisposed;
				if (disposed == false) {
					target.Dispose();
				}
			}

			protected override void AssertTarget(RunningTaskTable target) {
				// check argument
				Debug.Assert(target != null);

				Assert.Equal(0, target.RunningTaskCount);
				Assert.True(target.IsDisposed);
			}

			#endregion
		}

		#endregion


		#region logging

		// TODO implement

		#endregion
	}
}
