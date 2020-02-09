using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zafu.Utilities;
using Xunit;

namespace Zafu.Utilities.Test {
	public class DictionaryExtensionsTest {
		#region ClearDisposable

		public class TryGetValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, bool expectedResult, T expectedValue) where T: TValue where TKey: notnull where TValue: class {
				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(key != null);

				// Act
				T actualValue1;
				T actualValue2;
				T actualValue3;
				// overload 1: bool TryGetValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult1 = readOnlyDictionarySample.TryGetValue<T, TKey, TValue>(key, out actualValue1);
				// overload 2: bool TryGetValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult2 = dictionarySample.TryGetValue<T, TKey, TValue>(key, out actualValue2);
				// overload 3: bool TryGetValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult3 = sample.TryGetValue<T, TKey, TValue>(key, out actualValue3);

				// Assert
				Assert.Equal(expectedResult, actualResult1);
				Assert.Equal(expectedResult, actualResult2);
				Assert.Equal(expectedResult, actualResult3);
				Assert.Equal(expectedValue, actualValue1);
				Assert.Equal(expectedValue, actualValue2);
				Assert.Equal(expectedValue, actualValue3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, bool expectedResult, T expectedValue) where T : notnull {
				// test TryGetValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, expectedResult, expectedValue);

				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(key != null);

				// Act
				T actualValue1;
				T actualValue2;
				T actualValue3;
				// overload 1: bool TryGetValue<T>(this IReadOnlyDictionary<string, object?>, string, out T)
				bool actualResult1 = readOnlyDictionarySample.TryGetValue<T>(key, out actualValue1);
				// overload 2: bool TryGetValue<T>(this IDictionary<string, object?>, string, out T)
				bool actualResult2 = dictionarySample.TryGetValue<T>(key, out actualValue2);
				// overload 3: bool TryGetValue<T>(this IDictionary<string, object?>, string, out T)
				bool actualResult3 = sample.TryGetValue<T>(key, out actualValue3);

				// Assert
				Assert.Equal(expectedResult, actualResult1);
				Assert.Equal(expectedResult, actualResult2);
				Assert.Equal(expectedResult, actualResult3);
				Assert.Equal(expectedValue, actualValue1);
				Assert.Equal(expectedValue, actualValue2);
				Assert.Equal(expectedValue, actualValue3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, string expectedMessage) where T: TValue where TKey: notnull where TValue: class where TException : Exception {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				T dummy;
				// overload 1: bool TryGetValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, out T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.TryGetValue<T, TKey, TValue>(key, out dummy); });
				// overload 2: bool TryGetValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.TryGetValue<T, TKey, TValue>(key, out dummy); });
				// overload 3: bool TryGetValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.TryGetValue<T, TKey, TValue>(key, out dummy); });

				// Assert
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, string expectedMessage) where T : notnull where TException : Exception {
				// test TryGetValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, expectedMessage);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				T dummy;
				// overload 1: bool TryGetValue<T>(this IReadOnlyDictionary<string, object>, string, out T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.TryGetValue<T>(key, out dummy); });
				// overload 2: bool TryGetValue<T>(this IDictionary<string, object>, string, out T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.TryGetValue<T>(key, out dummy); });
				// overload 3: bool TryGetValue<T>(this IDictionary<string, object>, string, out T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.TryGetValue<T>(key, out dummy); });

				// Assert
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected static string GetDefaultInvalidCastExceptionMessage(Type from, Type to) {
				return $"Unable to cast object of type '{from.FullName}' to type '{to.FullName}'.";
			}

			protected static string GetNullValueMessage(Type to) {
				return "Its value is a null.";
			}

			#endregion


			#region tests

			[Fact(DisplayName = "T: Reference Type, conformable")]
			public void ReferenceType_Conformable() {
				string key = "key";
				string expectedValue = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};
				bool expectedResult = true;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "T: Value Type, conformable")]
			public void ValueType_Conformable() {
				string key = "key";
				int expectedValue = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};
				bool expectedResult = true;

				TestNormal<int>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "T: Reference Type, unconformable")]
			public void ReferenceType_Unconformable() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, new Version(1, 2, 3) }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(Version), typeof(string));

				TestError<string, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, unconformable")]
			public void ValueType_Unconformable() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, true }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(bool), typeof(int));

				TestError<int, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Reference Type, null")]
			public void ReferenceType_Null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				string expectedMessage = GetNullValueMessage(typeof(string));

				TestError<string, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, null")]
			public void ValueType_Null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				string expectedMessage = GetNullValueMessage(typeof(int));

				TestError<int, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Reference Type, not found")]
			public void ReferenceType_NotFound() {
				string key = "key";
				string expectedValue = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				bool expectedResult = false;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "T: Value Type, not found")]
			public void ValueType_NotFound() {
				string key = "key";
				int expectedValue = 0;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};
				bool expectedResult = false;

				TestNormal<int>(sample, key, expectedResult, expectedValue);
			}

			#endregion
		}

		#endregion
	}
}
