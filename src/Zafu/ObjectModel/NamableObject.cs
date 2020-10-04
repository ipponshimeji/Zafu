using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;


namespace Zafu.ObjectModel {
	public class NamableObject<TRunningContext>: LockableObject<TRunningContext> where TRunningContext: class, IRunningContext {
		#region data

		private string name;

		private string? nameForLoggingCache = null;

		#endregion


		#region properties

		public string Name {
			get {
				return this.name;
			}
			protected set {
				// check argument
				if (value == null) {
					throw new ArgumentNullException(nameof(value));
				}

				lock (this.InstanceLocker) {
					this.name = value;
					// clear all name caches
					ClearNameCacheFor(null);
				}
			}
		}

		protected string NameForLogging {
			get {
				string? value = this.nameForLoggingCache;
				lock (this.InstanceLocker) {
					// There is low possibility that this.nameForLoggingCache is set 
					// by another thread during acquiring the lock.
					value = this.nameForLoggingCache;
					if (value == null) {
						value = GetName(StandardNameUse.Logging);
						Interlocked.Exchange(ref this.nameForLoggingCache, value);
					}
				}

				return value;
			}
		}

		#endregion


		#region creation

		public NamableObject(TRunningContext runningContext, object? instanceLocker, string? name): base(runningContext, instanceLocker) {
			// check argument
			if (name == null) {
				Type type = GetType();
				name = type.FullName ?? type.Name;
			}

			// initialize member
			this.name = name;
		}

		#endregion


		#region methods

		public string GetName(object use) {
			string? name = GetNameFor(use);
			// use name as the default name
			return name ?? this.Name;
		}

		#endregion


		#region overrides

		protected override string GetNameForLogging() {
			return this.NameForLogging;
		}

		protected override void Log(LogLevel logLevel, string? message, Exception? exception, EventId eventId) {
			// use this.NameForLogging directly for source parameter for efficiency
			LoggingUtil.Log(this.Logger, logLevel, this.NameForLogging, message, exception, eventId);
		}

		protected override void Log<T>(LogLevel logLevel, string? message, string extraPropName, T extraPropValue, Exception? exception, EventId eventId) {
			// use this.NameForLogging directly for source parameter for efficiency
			LoggingUtil.Log<T>(this.Logger, logLevel, this.NameForLogging, message, extraPropName, extraPropValue, exception, eventId);
		}

		#endregion


		#region overridables

		protected virtual string? GetNameFor(object use) {
			// check argument
			Debug.Assert(use != null);

			return null;
		}

		protected virtual void ClearNameCacheFor(object? use) {
			if (use == null) {
				// clear all name caches
				Interlocked.Exchange(ref this.nameForLoggingCache, null);
			} else if (use == StandardNameUse.Logging) {
				Interlocked.Exchange(ref this.nameForLoggingCache, null);
			}
		}

		#endregion
	}


	public class NamableObject: NamableObject<IRunningContext> {
		#region creation

		public NamableObject(IRunningContext? runningContext = null, object? instanceLocker = null, string? name = null) : base(IRunningContext.CorrectWithDefault(runningContext), instanceLocker, name) {
		}

		public NamableObject(IRunningContext? runningContext, string name) : this(runningContext, null, name) {
		}

		#endregion
	}
}
