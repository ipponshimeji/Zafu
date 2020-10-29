using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The class to check LogData whehter it is expected one or not.
	/// </summary>
	public class ExpectedLogData: LoggingData, IEquatable<LogData> {
		#region data

		public readonly LogData? LogData;

		public readonly Predicate<Type>? StateTypeChecker;

		public readonly Predicate<LogLevel>? LogLevelChecker;

		public readonly Predicate<EventId>? EventIdChecker;

		public readonly Predicate<object?>? StateChecker;

		public readonly Predicate<Exception?>? ExceptionChecker;

		public readonly Predicate<Delegate?>? FormatterChecker;

		#endregion


		#region creation

		public ExpectedLogData(LogData logData, Predicate<Type>? stateTypeChecker = null, Predicate<LogLevel>? logLevelChecker = null, Predicate<EventId>? eventIdChecker = null, Predicate<object?>? stateChecker = null, Predicate<Exception?>? exceptionChecker = null, Predicate<Delegate?>? formatterChecker = null): base() {
			// check argument
			if (logData == null) {
				if (
					stateTypeChecker == null ||
					logLevelChecker == null ||
					eventIdChecker == null ||
					stateChecker == null ||
					exceptionChecker == null ||
					formatterChecker == null
				) {
					throw new ArgumentNullException(nameof(logData), "It must not be null when one of other checkers is null.");
				}

			}

			// initialize members
			this.LogData = logData;
			this.StateTypeChecker = stateTypeChecker;
			this.LogLevelChecker = logLevelChecker;
			this.EventIdChecker = eventIdChecker;
			this.StateChecker = stateChecker;
			this.ExceptionChecker = exceptionChecker;
			this.FormatterChecker = formatterChecker;
		}

		public ExpectedLogData(ExpectedLogData src): base(src) {
			// check argument
			Debug.Assert(src != null);

			// initialize members
			this.LogData = src.LogData;
			this.StateTypeChecker = src.StateTypeChecker;
			this.LogLevelChecker = src.LogLevelChecker;
			this.EventIdChecker = src.EventIdChecker;
			this.StateChecker = src.StateChecker;
			this.ExceptionChecker = src.ExceptionChecker;
			this.FormatterChecker = src.FormatterChecker;
		}

		#endregion


		#region operators

		public static bool operator == (ExpectedLogData? x, LogData? y) {
			if (object.ReferenceEquals(x, null)) {
				return object.ReferenceEquals(y, null);
			} else {
				if (object.ReferenceEquals(y, null)) {
					return false;
				} else {
					static bool checkStateType(ExpectedLogData e, Type a) {
						if (e.StateTypeChecker != null) {
							return e.StateTypeChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							return e.LogData.StateType == a;
						}
					}
					static bool checkLogLevel(ExpectedLogData e, LogLevel a) {
						if (e.LogLevelChecker != null) {
							return e.LogLevelChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							return e.LogData.LogLevel == a;
						}
					}
					static bool checkEventId(ExpectedLogData e, EventId a) {
						if (e.EventIdChecker != null) {
							return e.EventIdChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							return e.LogData.EventId == a;
						}
					}
					static bool checkState(ExpectedLogData e, object? a) {
						if (e.StateChecker != null) {
							return e.StateChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							// Do not compare by (e.LogData.State == a.State). That is a reference comparison.
							return object.Equals(e.LogData.State, a);
						}
					}
					static bool checkException(ExpectedLogData e, Exception? a) {
						if (e.ExceptionChecker != null) {
							return e.ExceptionChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							return object.Equals(e.LogData.Exception, a);
						}
					}
					static bool checkFormatter(ExpectedLogData e, Delegate? a) {
						if (e.FormatterChecker != null) {
							return e.FormatterChecker(a);
						} else {
							Debug.Assert(e.LogData != null);
							return e.LogData.Formatter == a;
						}
					}

					return (
						checkLogLevel(x, y.LogLevel) &&
						checkEventId(x, y.EventId) &&
						checkStateType(x, y.StateType) &&
						checkState(x, y.State) &&
						checkException(x, y.Exception) &&
						checkFormatter(x, y.Formatter)
					);
				}
			}
		}

		public static bool operator !=(ExpectedLogData? x, LogData? y) {
			return !(x == y);
		}

		#endregion


		#region IEquatable<LogData>

		public bool Equals(LogData? other) {
			return (this == other);
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			switch (obj) {
				case LogData logData:
					return this == logData;
				case ExpectedLogData that:
					return (
						this.LogData == that.LogData &&
						this.StateTypeChecker == that.StateTypeChecker &&
						this.LogLevelChecker == that.LogLevelChecker &&
						this.EventIdChecker == that.EventIdChecker &&
						this.StateChecker == that.StateChecker &&
						this.ExceptionChecker == that.ExceptionChecker &&
						this.FormatterChecker == that.FormatterChecker
					);
				default:
					// include null
					return false;
			}
		}

		public override int GetHashCode() {
			return HashCode.Combine(
				this.LogData,
				this.StateTypeChecker,
				this.LogLevelChecker,
				this.EventIdChecker,
				this.StateChecker,
				this.ExceptionChecker,
				this.FormatterChecker
			);
		}

		#endregion
	}
}
