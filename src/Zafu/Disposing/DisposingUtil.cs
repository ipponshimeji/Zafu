using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Zafu.Logging;
using Zafu.ObjectModel;

namespace Zafu.Disposing {
	public static class DisposingUtil {
		#region constants

		public const string NameForLogging = "DisposableUtil";

		private const int NoIndex = -1;

		#endregion


		#region methods

		public static void DisposeLoggingException(IDisposable? disposable, IRunningContext? runningContext = null) {
			// check argument
			if (disposable == null) {
				return;
			}
			if (runningContext == null) {
				runningContext = ZafuEnvironment.DefaultRunningContext;
			}

			// dispose the object
			DisposeInternal(disposable, runningContext, NoIndex);
		}

		public static void DisposeLoggingException(IEnumerable<IDisposable?>? disposables, IRunningContext? runningContext = null) {
			// check argument
			if (disposables == null) {
				return;
			}
			if (runningContext == null) {
				runningContext = ZafuEnvironment.DefaultRunningContext;
			}

			// dispose the objects
			int index = 0;
			foreach (IDisposable? disposable in disposables) {
				if (disposable != null) {
					DisposeInternal(disposable, runningContext, index);
				}
				++index;
			}
		}

		public static void ClearDisposableLoggingException<T>(ref T? disposable, IRunningContext? runningContext = null) where T: class, IDisposable {
			T? value = Interlocked.Exchange(ref disposable, null);
			if (value != null) {
				DisposeLoggingException(value, runningContext);
			}
		}

		public static void ClearDisposablesLoggingException<T>(ref T? disposables, IRunningContext? runningContext = null) where T : class, IEnumerable<IDisposable> {
			T? value = Interlocked.Exchange(ref disposables, null);
			if (value != null) {
				DisposeLoggingException(value, runningContext);
			}
		}

		#endregion


		#region privates

		private static void DisposeInternal(IDisposable disposable, IRunningContext runningContext, int index) {
			// check argument
			Debug.Assert(disposable != null);
			Debug.Assert(runningContext != null);

			// call Dispose()
			try {
				disposable.Dispose();
			} catch (Exception exception) {
				// if an exception is thrown, log it
				if (runningContext.LoggingLevel <= LogLevel.Error) {
					string indexInfo = (0 <= index) ? $" at index {index}," : string.Empty;
					string message = $"An exception is thrown during a Dispose() call{indexInfo}.";
					LoggingUtil.LogError(runningContext.Logger, NameForLogging, message, exception);
				}
			}
		}

		#endregion
	}
}
