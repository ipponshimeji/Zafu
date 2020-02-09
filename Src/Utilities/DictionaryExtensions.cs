using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zafu.Utilities {
	public static class DictionaryExtensions {
		#region methods

		private static bool CheckValue<T, TOriginalValue>(bool valueExists, TOriginalValue? originalValue, [MaybeNullWhen(false)] out T value) where T : notnull, TOriginalValue where TOriginalValue: class {
			if (valueExists == false) {
				// the value does not exist
				value = default(T)!;
				return false;
			} else if (originalValue == null) {
				// the value exists, but it is null
				throw new InvalidCastException("Its value is a null.");
			} else {
				// the value exists and it is not null
				// The cast below may throw an InvalidCastException if originalValue is incompatible with TValue.
				value = (T)originalValue;
				return true;
			}
		}

		private static bool CheckNullableValue<T, TOriginalValue>(bool valueExists, TOriginalValue? originalValue, out T? value) where T : class, TOriginalValue where TOriginalValue : class {
			if (valueExists == false) {
				// the value does not exist
				value = null;
				return false;
			} else {
				// the value exists
				// The cast below may throw an InvalidCastException if originalValue is incompatible with TValue.
				value = (T?)originalValue;   // may throw an InvalidCastException
				return true;
			}
		}

		public static bool TryGetValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?> dictionary, TKey key, [MaybeNullWhen(false)] out T value) where T: notnull, TValue where TKey: notnull where TValue: class {
			TValue? originalValue;
			return CheckValue<T, TValue>(dictionary.TryGetValue(key, out originalValue), originalValue, out value);
		}

		public static bool TryGetValue<T, TKey, TValue>(this IDictionary<TKey, TValue?> dictionary, TKey key, [MaybeNullWhen(false)] out T value) where T : notnull, TValue where TKey : notnull where TValue : class {
			TValue? originalValue;
			return CheckValue<T, TValue>(dictionary.TryGetValue(key, out originalValue), originalValue, out value);
		}

		public static bool TryGetValue<T, TKey, TValue>(this Dictionary<TKey, TValue?> dictionary, TKey key, [MaybeNullWhen(false)] out T value) where T : notnull, TValue where TKey : notnull where TValue : class {
			return TryGetValue<T, TKey, TValue>((IDictionary<TKey, TValue?>)dictionary, key, out value);
		}

		public static bool TryGetValue<T>(this IReadOnlyDictionary<string, object?> dictionary, string key, [MaybeNullWhen(false)] out T value) where T: notnull {
			return TryGetValue<T, string, object>(dictionary, key, out value);
		}

		public static bool TryGetValue<T>(this IDictionary<string, object?> dictionary, string key, [MaybeNullWhen(false)] out T value) where T : notnull {
			return TryGetValue<T, string, object>(dictionary, key, out value);
		}

		public static bool TryGetValue<T>(this Dictionary<string, object?> dictionary, string key, [MaybeNullWhen(false)] out T value) where T : notnull {
			return TryGetValue<T, string, object>(dictionary, key, out value);
		}

		public static bool TryGetNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?> dictionary, TKey key, out T? value) where T: class, TValue where TKey: notnull where TValue: class {
			TValue? originalValue;
			return CheckValue<T, TValue>(dictionary.TryGetValue(key, out originalValue), originalValue, out value);
		}

		public static bool TryGetNullableValue<T>(this IDictionary<string, object?> dictionary, string key, out T? value) where T: class {
			return TryGetNullableValue<T, string, object>(dictionary, key, out value);
		}


		public static T GetOptionalValue<T, TKey, TValue>(this IDictionary<TKey, TValue?> dictionary, TKey key, T defaultValue) where T : notnull, TValue where TKey: notnull where TValue: class {
			T value;
			return TryGetValue<T, TKey, TValue>(dictionary, key, out value) ? value : defaultValue;
		}

		public static T GetOptionalValue<T>(this IDictionary<string, object?> dictionary, string key, T defaultValue) where T: notnull {
			return GetOptionalValue<T, string, object>(dictionary, key, defaultValue);
		}

		public static T? GetOptionalNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?> dictionary, TKey key, T? defaultValue) where T : class, TValue where TKey : notnull where TValue : class {
			T? value;
			return TryGetNullableValue<T, TKey, TValue>(dictionary, key, out value) ? value : defaultValue;
		}

		public static T? GetOptionalNullableValue<T>(this IDictionary<string, object?> dictionary, string key, T? defaultValue) where T: class {
			return GetOptionalNullableValue<T, string, object>(dictionary, key, defaultValue);
		}

		#endregion
	}
}
