using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Zafu.Disposing;
using Zafu.ObjectModel;

namespace Zafu.Logging {
	public class RelayingLogger: LockableObject, ILogger {
		#region types

		public new class Use: LockableObject.Use {
			public static readonly object Scope = new object();
		}

		#endregion


		#region data

		private List<ILogger> targetLoggers = new List<ILogger>();

		#endregion


		#region creation

		public RelayingLogger(string? name = null, IRunningContext? runningContext = null): base(null, name, runningContext) {
		}

		#endregion


		#region ILogger

		public virtual IDisposable BeginScope<TState>(TState state) {
			lock (this.InstanceLocker) {
				return BeginScopeNTS<TState>(this.targetLoggers, state);
			}
		}

		public virtual bool IsEnabled(LogLevel logLevel) {
			lock (this.InstanceLocker) {
				return IsEnabledNTS(this.targetLoggers, logLevel);
			}
		}

		public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			lock (this.InstanceLocker) {
				LogNTS<TState>(this.targetLoggers, logLevel, eventId, state, exception, formatter);
			}
		}

		#endregion


		#region methods

		public void AddTargetLogger(ILogger logger) {
			// check arguments
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			lock (this.InstanceLocker) {
				this.targetLoggers.Add(logger);
			}
		}

		public bool RemoveTargetLogger(ILogger logger) {
			// check arguments
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			lock (this.InstanceLocker) {
				return this.targetLoggers.Remove(logger);
			}
		}

		#endregion


		#region overrides

		protected override string? GetNameFor(object use) {
			if (use == Use.Scope) {
				return $"scope of {GetName(Use.Message)}";
			} else {
				return base.GetNameFor(use);
			}
		}

		#endregion


		#region overridables

		/// <remarks>
		/// This method is called in the scope of lock(this.InstanceLocker).
		/// </remarks>
		protected virtual IDisposable BeginScopeNTS<TState>(IEnumerable<ILogger> loggers, TState state) {
			// check arguments
			Debug.Assert(loggers != null);

			// create a redirecting scope
			IDisposable? beginScope(ILogger l, TState s) {
				try {
					return l.BeginScope<TState>(s);
				} catch {
					return null;
				}
			}

			IDisposable[] targetScopes = loggers.Select(l => beginScope(l, state)).Where(d => d != null).ToArray()!;
			return new DisposableCollection(targetScopes, GetName(Use.Scope), this.RunningContext);
		}

		/// <remarks>
		/// This method is called in the scope of lock(this.InstanceLocker).
		/// </remarks>
		protected virtual bool IsEnabledNTS(IEnumerable<ILogger> loggers, LogLevel logLevel) {
			// check arguments
			Debug.Assert(loggers != null);

			// redirect the log to the loggers
			foreach (ILogger logger in loggers) {
				Debug.Assert(logger != null);
				try {
					if (logger.IsEnabled(logLevel)) {
						return true;
					}
				} catch (Exception) {
					// continue
					// TODO: how to log the error in this situation?
				}
			}

			return false;
		}

		/// <remarks>
		/// This method is called in the scope of lock(this.InstanceLocker).
		/// </remarks>
		protected virtual void LogNTS<TState>(IEnumerable<ILogger> loggers, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			// check arguments
			Debug.Assert(loggers != null);

			// redirect the log to the loggers
			foreach (ILogger logger in loggers) {
				Debug.Assert(logger != null);
				try {
					logger.Log<TState>(logLevel, eventId, state, exception, formatter);
				} catch (Exception) {
					// continue
					// TODO: how to log the error in this situation?
				}
			}
		}

		#endregion
	}
}
