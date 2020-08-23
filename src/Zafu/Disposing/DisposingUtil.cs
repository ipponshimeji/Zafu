using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zafu.Logging;

namespace Zafu.Disposing {
	public static class DisposingUtil {
		#region constants

		public const string NameForLogging = "DisposableUtil";

		private const int NoIndex = -1;

		#endregion


		#region methods

		public static void DisposeIgnoringException(IDisposable? disposable, ILogger? logger = null, Func<string>? getLocation = null) {
			// check argument
			if (disposable == null) {
				return;
			}
			if (logger == null) {
				logger = LoggingUtil.DefaultLogger;
			}

			// dispose the object
			DisposeInternal(disposable, logger, getLocation, NoIndex);
		}

		public static void DisposeIgnoringException(IEnumerable<IDisposable?>? disposables, ILogger? logger = null, Func<string>? getLocation = null) {
			// check argument
			if (disposables == null) {
				return;
			}
			if (logger == null) {
				logger = LoggingUtil.DefaultLogger;
			}

			// dispose the objects
			int index = 0;
			foreach (IDisposable? disposable in disposables) {
				if (disposable != null) {
					DisposeInternal(disposable, logger, getLocation, index);
				}
				++index;
			}
		}

		public static void ClearDisposableIgnoringException<T>(ref T? disposable, ILogger? logger = null, Func<string>? getLocation = null) where T: class, IDisposable {
			T? value = Interlocked.Exchange(ref disposable, null);
			if (value != null) {
				DisposeIgnoringException(value, logger, getLocation);
			}
		}

		public static void ClearDisposablesIgnoringException<T>(ref T? disposables, ILogger? logger = null, Func<string>? getLocation = null) where T : class, IEnumerable<IDisposable> {
			T? value = Interlocked.Exchange(ref disposables, null);
			if (value != null) {
				DisposeIgnoringException(value, logger, getLocation);
			}
		}

		#endregion


		#region privates

		private static void DisposeInternal(IDisposable disposable, ILogger logger, Func<string>? getLocation, int index) {
			// check argument
			Debug.Assert(disposable != null);
			Debug.Assert(logger != null);

			// call Dispose()
			try {
				disposable.Dispose();
			} catch (Exception exception) {
				// if an exception is thrown, log it
				if (logger != NullLogger.Instance) {
					string? location = GetLocation(getLocation);
					string indexInfo = (0 <= index) ? $" index: {index}," : string.Empty;
					string message = $"An exception is thrown during a Dispose() call.{indexInfo} call from: {location}";
					LoggingUtil.LogError(logger, NameForLogging, message, exception);
				}
			}
		}

		private static string GetLocation(Func<string>? getLocation) {
			try {
				if (getLocation != null) {
					// get the location which is specified by the caller
					return getLocation() ?? string.Empty;
				} else {
					// get the name of the caller method
					Type thisType = typeof(DisposingUtil);
					StackTrace stackTrace = new StackTrace(true);
					for (int i = stackTrace.FrameCount - 1; 0 <= i; --i) {
						StackFrame? frame = stackTrace.GetFrame(i);
						if (frame != null) {
							MethodBase? method = frame.GetMethod();
							// Note that methods in DisposableUtil class are skipped
							if (method != null && method.DeclaringType != thisType) {
								return method.ToString() ?? string.Empty;
							}
						}
					}
				}
			} catch {
				// continue
			}

			return string.Empty;
		}

		#endregion
	}
}
