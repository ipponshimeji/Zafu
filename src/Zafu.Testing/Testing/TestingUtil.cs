using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Zafu.Testing {
	public static class TestingUtil {
		#region constants

		public const string NullLabel = "(null)";

		public const string NonNullLabel = "(non-null)";

		#endregion


		#region utilities

		public static IEnumerable<object[]> ToTestData<T>(this IEnumerable<T> data) where T : class {
			// check argument
			if (data == null) {
				throw new ArgumentNullException(nameof(data));
			}

			return data.Select(datum => new object[] { datum }).ToArray();
		}

		public static string GetDisplayText(object? value) {
			if (value == null) {
				return NullLabel;
			} else {
				string? valueText = value.ToString();
				return valueText ?? string.Empty;
			}
		}

		public static string GetDisplayText(string? value, bool quote = true) {
			if (value == null) {
				return NullLabel;
			} else {
				return quote ? string.Concat("\"", value, "\"") : value;
			}
		}

		public static string GetNullOrNonNullText(object? value) {
			return (value == null) ? NullLabel : NonNullLabel;
		}

		#endregion
	}
}
