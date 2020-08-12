using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Disposing;

namespace Zafu.Logging {
	public class RedirectingLogger: ILogger {
		#region data

		private readonly object instanceLocker = new object();

		private List<ILogger> targetLoggers = new List<ILogger>();

		#endregion


		#region creation

		public RedirectingLogger() {
		}

		#endregion


		#region ILogger

		public IDisposable BeginScope<TState>(TState state) {
			lock (this.instanceLocker) {
				return BeginScope<TState>(this.targetLoggers, state);
			}
		}

		public bool IsEnabled(LogLevel logLevel) {
			lock (this.instanceLocker) {
				return IsEnabled(this.targetLoggers, logLevel);
			}
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			lock (this.instanceLocker) {
				Log<TState>(this.targetLoggers, logLevel, eventId, state, exception, formatter);
			}
		}

		#endregion


		#region methods

		public void AddTargetLogger(ILogger logger) {
			// check arguments
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			lock (this.instanceLocker) {
				this.targetLoggers.Add(logger);
			}
		}

		public bool RemoveTargetLogger(ILogger logger) {
			// check arguments
			if (logger == null) {
				throw new ArgumentNullException(nameof(logger));
			}

			lock (this.instanceLocker) {
				return this.targetLoggers.Remove(logger);
			}
		}

		#endregion


		#region overridables

		/// <remarks>
		/// This method is called in the scope of lock(this.instanceLocker).
		/// </remarks>
		protected virtual IDisposable BeginScope<TState>(IEnumerable<ILogger> loggers, TState state) {
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

			// TODO: create NotNull extension?
			IDisposable[] targetScopes = loggers.Select(l => beginScope(l, state)).Where(d => d != null).ToArray()!;
			return new DisposableCollection(targetScopes, "Scope of the Default Logger", this);
		}

		/// <remarks>
		/// This method is called in the scope of lock(this.instanceLocker).
		/// </remarks>
		protected virtual bool IsEnabled(IEnumerable<ILogger> loggers, LogLevel logLevel) {
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
		/// This method is called in the scope of lock(this.instanceLocker).
		/// </remarks>
		protected virtual void Log<TState>(IEnumerable<ILogger> loggers, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
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
