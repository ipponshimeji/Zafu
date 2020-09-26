using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Tasks;

namespace Zafu.ObjectModel {
	public class RunningEnvironment: RunningContextBase, IDisposable {
		#region data

		private readonly object instanceLocker = new object();

		private ILogger logger;

		// the running task table should not be changed.
		private readonly IRunningTaskTable runningTaskTable;

		private bool disposed = false;

		private TimeSpan disposeWaitingTimeout = IRunningTaskTable.DefaultDisposeWaitingTimeout;

		private TimeSpan disposeCancelingTimeout = IRunningTaskTable.DefaultDisposeCancelingTimeout;

		#endregion


		#region properties

		public TimeSpan DisposeWaitingTimeout {
			get {
				return this.disposeWaitingTimeout;
			}
			set {
				// check argument
				if (value.TotalMilliseconds < 0 && value != Timeout.InfiniteTimeSpan) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				lock (this.instanceLocker) {
					this.disposeWaitingTimeout = value;
				}
			}
		}

		public TimeSpan DisposeCancelingTimeout {
			get {
				return this.disposeCancelingTimeout;
			}
			set {
				// check argument
				if (value.TotalMilliseconds < 0 && value != Timeout.InfiniteTimeSpan) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				lock (this.instanceLocker) {
					this.disposeCancelingTimeout = value;
				}
			}
		}

		#endregion


		#region creation

		public RunningEnvironment(ILogger? logger, Func<IRunningContext, IRunningTaskTable>? runningTaskTableCreator = null): base() {
			// check arguments
			if (logger == null) {
				logger = NullLogger.Instance;
			}
			if (runningTaskTableCreator == null) {
				runningTaskTableCreator = CreateDefaultRunningTaskTable;
			}

			// initialize members
			this.logger = logger;
			this.runningTaskTable = runningTaskTableCreator(this);
			if (this.runningTaskTable == null) {
				throw new ArgumentException("It returns null.", nameof(runningTaskTableCreator));
			}
		}

		public static IRunningTaskTable CreateDefaultRunningTaskTable(IRunningContext context) {
			return new RunningTaskTable(context);
		}

		public void Dispose() {
			Dispose(this.DisposeWaitingTimeout, this.DisposeCancelingTimeout);
		}

		public bool Dispose(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// check state
			lock (this.instanceLocker) {
				if (this.disposed) {
					return true;
				} else {
					this.disposed = true;
				}
			}

			return DisposeImpl(waitingTimeout, cancelingTimeOut);
		}

		protected virtual bool DisposeImpl(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			// wait for tasks in the task monitor
			bool completed = this.runningTaskTable.Dispose(waitingTimeout, cancelingTimeOut);

			lock (this.instanceLocker) {
				// clear logger
				this.LoggingLevel = LogLevel.None;
				this.logger = NullLogger.Instance;
			}

			return completed;
		}

		#endregion


		#region IRunningContext

		public override ILogger Logger => this.logger;

		public override IRunningTaskMonitor RunningTaskMonitor => this.runningTaskTable.RunningTaskMonitor;

		#endregion
	}
}
