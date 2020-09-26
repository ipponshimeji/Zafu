using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Logging;
using Zafu.ObjectModel;
using Zafu.Tasks;

namespace Zafu {
	public class ZafuEnvironment: RunningEnvironment {
		#region constants

		public const string LoggingCategoryName = "Zafu";

		#endregion


		#region data

		private static readonly object classLocker = new object();

		private static ZafuEnvironment? defaultEnvironment = null;

		private static int defaultEnvironmentRefCount = 0;


		private readonly object instanceLocker = new object();

		private RelayingLogger? relayingLogger = null;

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

		#endregion


		#region creation & disposal

		protected ZafuEnvironment(ILogger? logger, Func<IRunningContext, IRunningTaskTable> runningTaskTableCreator, IConfigurationSection? config): base(logger, runningTaskTableCreator) {
			// initialize members
			InitializeThisClassLevel(config);
		}

		protected ZafuEnvironment(RelayingLogger relayingLogger, Func<IRunningContext, IRunningTaskTable> runningTaskTableCreator, IConfigurationSection? config) : base(relayingLogger, runningTaskTableCreator) {
			// initialize members
			this.relayingLogger = relayingLogger;
			InitializeThisClassLevel(config);
		}


		public static ZafuEnvironment Create(ILogger? logger, IConfigurationSection? config = null) {
			// check arguments
			// logger can be null
			// config can be null

			// prepare parameters
			Func<IRunningContext, IRunningTaskTable> runningTaskTableCreator = GetRunningTaskTableCreator(config);

			return new ZafuEnvironment(logger, runningTaskTableCreator, config);
		}

		public static ZafuEnvironment Create(ILoggerFactory loggerFactory, string? categoryName = null, IConfigurationSection? config = null) {
			// check arguments
			if (loggerFactory == null) {
				throw new ArgumentNullException(nameof(loggerFactory));
			}
			if (categoryName == null) {
				categoryName = LoggingCategoryName;
			}
			// config can be null

			// prepare parameters
			ILogger logger = loggerFactory.CreateLogger(categoryName);
			Func<IRunningContext, IRunningTaskTable> runningTaskTableCreator = GetRunningTaskTableCreator(config);

			return new ZafuEnvironment(logger, runningTaskTableCreator, config);
		}

		public static ZafuEnvironment Create(IConfigurationSection? config = null) {
			// check arguments
			// config can be null

			// prepare parameters
			RelayingLogger relayingLogger = CreateRelayingLogger(config);
			Func<IRunningContext, IRunningTaskTable> runningTaskTableCreator = GetRunningTaskTableCreator(config);

			return new ZafuEnvironment(relayingLogger, runningTaskTableCreator, config);
		}

		private static Func<IRunningContext, IRunningTaskTable> GetRunningTaskTableCreator(IConfigurationSection? config) {
			return (runningContext) => {
				return new RunningTaskTable(runningContext);
			};
		}

		private static RelayingLogger CreateRelayingLogger(IConfigurationSection? config) {
			// TODO: support QueuedLogger
			return new RelayingLogger();
		}

		private void InitializeThisClassLevel(IConfigurationSection? config) {
			// check argument
			// config can be null

			// initialize members
			if (this.Logger is NullLogger) {
				// no need to log
				this.LoggingLevel = LogLevel.None;
			} else {
				// TODO: overwrite LoggingLevel by config
			}
		}


		protected override bool DisposeImpl(TimeSpan waitingTimeout, TimeSpan cancelingTimeOut) {
			bool completed = base.DisposeImpl(waitingTimeout, cancelingTimeOut);

			lock (this.instanceLocker) {
				// clear logger
				this.relayingLogger = null;
			}

			return completed;
		}

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
			return InitializeDefaultEnvironment(out newlyCreated, () => ZafuEnvironment.Create(logger, config));
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(ILogger logger, IConfigurationSection? config = null) {
			bool dummy;
			return InitializeDefaultEnvironment(out dummy, logger, config);
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, ILoggerFactory loggerFactory, IConfigurationSection? config = null) {
			return InitializeDefaultEnvironment(out newlyCreated, () => ZafuEnvironment.Create(loggerFactory, null, config));
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(ILoggerFactory loggerFactory, IConfigurationSection? config = null) {
			bool dummy;
			return InitializeDefaultEnvironment(out dummy, loggerFactory, config);
		}

		public static DefaultZafuEnvironmentScope InitializeDefaultEnvironment(out bool newlyCreated, IConfigurationSection? config = null) {
			return InitializeDefaultEnvironment(out newlyCreated, () => ZafuEnvironment.Create(config));
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
