using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.Testing.Logging {
	public class TestingLogger: ILogger, IReadOnlyList<Entry> {
		#region data

		private readonly object instanceLocker = new object();


		private List<Entry> logs = new List<Entry>();

		private LogLevel logLevel = LogLevel.Debug;

		#endregion


		#region properties

		public LogLevel LogLevel {
			get {
				return this.logLevel;
			}
			set {
				lock (this.instanceLocker) {
					this.logLevel = value;
				}
			}
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			return this.logs.GetEnumerator();
		}

		#endregion


		#region IEnumerable<Entry>

		public IEnumerator<Entry> GetEnumerator() {
			return this.logs.GetEnumerator();
		}

		#endregion


		#region IReadOnlyCollection<Entry>

		public int Count => this.logs.Count;

		#endregion


		#region IReadOnlyList<Entry>

		public Entry this[int index] => this.logs[index];

		#endregion


		#region ILogger

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
			// check argument
			if (formatter == null) {
				throw new ArgumentNullException(nameof(formatter));
			}

			lock (this.instanceLocker) {
				if (this.logLevel <= logLevel) {
					Entry entry = new LogEntry(typeof(TState), logLevel, eventId, state, exception, formatter);
					this.logs.Add(entry);
				}
			}
		}

		public bool IsEnabled(LogLevel logLevel) {
			return this.LogLevel <= logLevel;
		}

		public IDisposable BeginScope<TState>(TState state) {
			throw new NotImplementedException();
		}

		#endregion


		#region methods

		public void Clear() {
			lock (this.instanceLocker) {
				this.logs.Clear();
			}
		}

		#endregion
	}
}
