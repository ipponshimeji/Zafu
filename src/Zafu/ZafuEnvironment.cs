using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Logging;
using Zafu.ObjectModel;
using Zafu.Tasks;

namespace Zafu {
	public class ZafuEnvironment: IRunningContext, IDisposable {
		#region constants

		public const string LoggingCategoryName = "Zafu";

		#endregion


		#region data

		private static readonly object classLocker = new object();

		private static ZafuEnvironment? defaultEnvironment = null;

		private static int defaultEnvironmentRefCount = 0;


		private readonly object instanceLocker = new object();

		public LogLevel LoggingLevel { get; set; } = IRunningContext.DefaultLogLevel;

		private ILogger logger;

		private readonly RunningTaskTable runningTaskMonitor;

		private RelayingLogger? relayingLogger = null;

		private TimeSpan disposeWaitingTimeout = IRunningTaskTable.DefaultDisposeWaitingTimeout;

		private TimeSpan disposeCancelingTimeout = IRunningTaskTable.DefaultDisposeCancelingTimeout;

		private bool disposed = false;

		#endregion


		#region properties

		public static ZafuEnvironment? Default {
			get {
				return defaultEnvironment;
			}
		}

		public static IRunningContext DefaultRunningContext {
			get {
				ZafuEnvironment? env = defaultEnvironment;
				return env ?? (IRunningContext)NullRunningContext.Instance;
			}
		}


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


		#region creation & disposal

		public ZafuEnvironment(ILogger logger, IConfigurationSection? config = null) {
			// check arguments
			if (logger == null) {
				logger = NullLogger.Instance;
			}

			// initialize members
			this.logger = logger;
			this.runningTaskMonitor = CreateRunningTaskMonitor(config);
			InitializeThisClassLevel(config);
		}

		public ZafuEnvironment(ILoggerFactory loggerFactory, string? categoryName = null, IConfigurationSection? config = null) {
			// check arguments
			if (loggerFactory == null) {
				throw new ArgumentNullException(nameof(loggerFactory));
			}
			if (categoryName == null) {
				categoryName = LoggingCategoryName;
			}

			// initialize members
			this.logger = loggerFactory.CreateLogger(categoryName);
			this.runningTaskMonitor = CreateRunningTaskMonitor(config);
			InitializeThisClassLevel(config);
		}

		public ZafuEnvironment(IConfigurationSection? config = null) {
			// initialize members
			this.relayingLogger = CreateRelayingLogger(config);
			this.logger = this.relayingLogger;
			this.runningTaskMonitor = CreateRunningTaskMonitor(config);
			InitializeThisClassLevel(config);
		}

		private RunningTaskTable CreateRunningTaskMonitor(IConfigurationSection? config) {
			return new RunningTaskTable(this);
		}

		private RelayingLogger CreateRelayingLogger(IConfigurationSection? config) {
			// TODO: support QueuedLogger
			return new RelayingLogger();
		}

		private void InitializeThisClassLevel(IConfigurationSection? config) {
			// check argument
			// config can be null

			// initialize members
			if (this.logger is NullLogger) {
				// no need to log
				this.LoggingLevel = LogLevel.None;
			} else {
				// TODO: overwrite LoggingLevel by config
			}
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
			bool completed = this.runningTaskMonitor.Dispose(waitingTimeout, cancelingTimeOut);

			lock (this.instanceLocker) {
				// clear logger
				this.LoggingLevel = LogLevel.None;
				this.logger = NullLogger.Instance;
				this.relayingLogger = null;
			}

			return completed;
		}

		#endregion


		#region IRunningContext

		public ILogger Logger => this.logger;

		public IRunningTaskMonitor RunningTaskMonitor => this.runningTaskMonitor;

		// LoggingLevel is defined as a property with get/set accessor.

		#endregion


		#region methods
		#endregion


		#region methods - default context

		private static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, Func<ZafuEnvironment> creator) {
			// check argument
			Debug.Assert(creator != null);

			DefaultZafuEnvironmentScope scope;
			lock (classLocker) {
				// check state
				int count = defaultEnvironmentRefCount;
				if (int.MaxValue <= count) {
					throw new OverflowException();
				}
				if (count < 0) {
					throw new InvalidOperationException();
				}

				// create the default ZafuEnvironment if necessary
				if (count == 0) {
					Debug.Assert(defaultEnvironment == null);
					ZafuEnvironment env = creator();
					scope = new DefaultZafuEnvironmentScope(env);
					defaultEnvironment = env;
					newlyCreated = true;
				} else {
					Debug.Assert(defaultEnvironment != null);
					scope = new DefaultZafuEnvironmentScope(defaultEnvironment);
					newlyCreated = false;
				}

				// increment the reference count
				++defaultEnvironmentRefCount;
			}

			return scope;
		}


		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, ILogger logger, IConfigurationSection? config = null) {
			return InitializeDefaultEnvironment(out newlyCreated, () => new ZafuEnvironment(logger, config));
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(ILogger logger, IConfigurationSection? config = null) {
			bool dummy;
			return InitializeDefaultEnvironment(out dummy, logger, config);
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, ILoggerFactory loggerFactory, IConfigurationSection? config = null) {
			return InitializeDefaultEnvironment(out newlyCreated, () => new ZafuEnvironment(loggerFactory, null, config));
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(ILoggerFactory loggerFactory, IConfigurationSection? config = null) {
			bool dummy;
			return InitializeDefaultEnvironment(out dummy, loggerFactory, config);
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, IConfigurationSection? config = null) {
			return InitializeDefaultEnvironment(out newlyCreated, () => new ZafuEnvironment(config));
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(IConfigurationSection? config = null) {
			bool dummy;
			return InitializeDefaultEnvironment(out dummy, config);
		}

		internal static DefaultZafuEnvironmentScope.DisposeResult UninitializeDefaultEnvironment(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			ZafuEnvironment? envToBeDisposed = null;
			lock (classLocker) {
				// check state
				if (defaultEnvironmentRefCount <= 0) {
					throw new InvalidOperationException();
				}

				// decrement the reference count
				--defaultEnvironmentRefCount;
				if (defaultEnvironmentRefCount <= 0) {
					// the default ZafuEnvironment must be disposed
					envToBeDisposed = defaultEnvironment;
					defaultEnvironment = null;
					Debug.Assert(envToBeDisposed != null);
				}
			}

			// dispose the default ZafuEnvironment if necessary
			DefaultZafuEnvironmentScope.DisposeResult result;
			if (envToBeDisposed == null) {
				// The default ZafuEnvironment is still in use.
				result = DefaultZafuEnvironmentScope.DisposeResult.InUse;
			} else {
				if (envToBeDisposed.Dispose(waitingTimeout, cancelingTimeOut)) {
					result = DefaultZafuEnvironmentScope.DisposeResult.Completed;
				} else {
					result = DefaultZafuEnvironmentScope.DisposeResult.Timeout;
				}
			}

			return result;
		}

		#endregion


		// TODO: add/remove logger to relayingLogger
#if false

		public static void AddToDefaultLogger(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// add the logger to the default logger
			DefaultLogger.AddTargetLogger(logger);
		}

		public static ILogger AddToDefaultLogger(ILoggerFactory loggerFactory) {
			// check argument
			if (loggerFactory == null) {
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			// create a logger and add it to the default logger
			ILogger logger = loggerFactory.CreateLogger(LogCategoryName);
			AddToDefaultLogger(logger);
			return logger;
		}

		public static bool RemoveFromDefaultLogger(ILogger logger) {
			// check argument
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			// remove the logger from the default logger
			return defaultLogger.RemoveTargetLogger(logger);
		}

#endif
	}
}
