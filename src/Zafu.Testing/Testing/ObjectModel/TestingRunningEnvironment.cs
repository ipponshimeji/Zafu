using System;
using Zafu.ObjectModel;
using Zafu.Tasks;
using Zafu.Testing.Logging;


namespace Zafu.Testing.ObjectModel {
	public class TestingRunningEnvironment: RunningEnvironment {
		#region data

		public readonly TestingLogger Logs;

		#endregion


		#region creation

		protected TestingRunningEnvironment(TestingLogger logger, Func<IRunningContext, IRunningTaskTable>? runningTaskTableCreator) : base(logger, runningTaskTableCreator) {
			// check arguments
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// initialize members
			this.Logs = logger;
		}

		public static TestingRunningEnvironment Create(Func<IRunningContext, IRunningTaskTable>? runningTaskTableCreator = null) {
			return new TestingRunningEnvironment(new TestingLogger(), runningTaskTableCreator);
		}

		#endregion
	}
}
