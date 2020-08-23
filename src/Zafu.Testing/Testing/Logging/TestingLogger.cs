using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.Testing.Logging {
	public class TestingLogger: ILogger, IReadOnlyList<LoggingData> {
		#region types

		protected class Scope: IDisposable {
			#region data

			private TestingLogger? owner;

			#endregion


			#region creation & disposal

			public Scope(TestingLogger owner) {
				// check argument
				Debug.Assert(owner != null);

				// initialize members
				this.owner = owner;
			}


			public void Dispose() {
				TestingLogger? owner = Interlocked.Exchange(ref this.owner, null);
				if (owner != null) {
					owner.EndScope(this);
				}
			}

			#endregion
		}

		#endregion


		#region data

		private readonly object instanceLocker = new object();


		private List<LoggingData> logs = new List<LoggingData>();

		private LogLevel loggingLevel = LogLevel.Debug;

		#endregion


		#region properties

		public LogLevel LoggingLevel {
			get {
				return this.loggingLevel;
			}
			set {
				lock (this.instanceLocker) {
					this.loggingLevel = value;
				}
			}
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			return this.logs.GetEnumerator();
		}

		#endregion


		#region IEnumerable<LoggingData>

		public IEnumerator<LoggingData> GetEnumerator() {
			return this.logs.GetEnumerator();
		}

		#endregion


		#region IReadOnlyCollection<LoggingData>

		public int Count => this.logs.Count;

		#endregion


		#region IReadOnlyList<LoggingData>

		public LoggingData this[int index] => this.logs[index];

		#endregion


		#region ILogger

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			lock (this.instanceLocker) {
				if (this.loggingLevel <= logLevel) {
					// add a logging data
					LoggingData data = new LogData(typeof(TState), logLevel, eventId, state, exception, formatter);
					this.logs.Add(data);
				}
			}
		}

		public bool IsEnabled(LogLevel logLevel) {
			return this.LoggingLevel <= logLevel;
		}

		public IDisposable BeginScope<TState>(TState state) {
			// create a scope
			Scope scope = new Scope(this);

			// add a logging data
			BeginScopeData data = new BeginScopeData(typeof(TState), state, scope);
			lock (this.instanceLocker) {
				this.logs.Add(data);
			}

			return scope;
		}

		#endregion


		#region methods

		public void Clear() {
			lock (this.instanceLocker) {
				this.logs.Clear();
			}
		}

		#endregion


		#region privates - for Scope class

		private void EndScope(Scope scope) {
			// check argument
			Debug.Assert(scope != null);

			// add a logging data
			EndScopeData data = new EndScopeData(scope);
			lock (this.instanceLocker) {
				this.logs.Add(data);
			}
		}

		#endregion
	}
}
