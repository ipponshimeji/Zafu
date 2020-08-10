using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Utilities;
using Zafu.Utilities.Testing;
using Xunit;

namespace Zafu.Utilities.Test {
	public class DictionaryExtensionsTest {
		#region utilities

		protected static string GetDefaultInvalidCastExceptionMessage<TFrom, TTo>() {
			return $"Unable to cast object of type '{typeof(TFrom).FullName}' to type '{typeof(TTo).FullName}'.";
		}

		protected static Action<InvalidCastException> GetInvalidCastExceptionTest<TFrom, TTo>() {
			string message = GetDefaultInvalidCastExceptionMessage<TFrom, TTo>();
			return ExceptionTester<InvalidCastException>.GetTest(message);
		}

		protected static string GetNullValueExceptionMessage<TTo>() {
			return "Its value is a null.";
		}

		protected static Action<InvalidCastException> GetNullValueExceptionTest<TTo>() {
			string message = GetNullValueExceptionMessage<TTo>();
			return ExceptionTester<InvalidCastException>.GetTest(message);
		}

		protected static string GetMissingKeyExceptionMessage<TKey>(TKey key) {
			return $"The indispensable key '{key}' is missing in the dictionary.";
		}

		protected static Action<KeyNotFoundException> GetMissingKeyExceptionTest<TKey>(TKey key) {
			string message = GetMissingKeyExceptionMessage<TKey>(key);
			return ExceptionTester<KeyNotFoundException>.GetTest(message);
		}

		#endregion


		#region TryGetValue

		public class TryGetValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, bool expectedResult, T expectedValue) where T: TValue where TKey: notnull where TValue: class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

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
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

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

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, Action<TException> testException) where T: TValue where TKey: notnull where TValue: class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

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
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, Action<TException> testException) where T : notnull where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test TryGetValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

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
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable, Reference Type")]
			public void value_conformable_ReferenceType() {
				string key = "key";
				string value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				bool expectedResult = true;
				string expectedValue = value;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "value: conformable, Value Type")]
			public void value_conformable_ValueType() {
				string key = "key";
				int value = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				bool expectedResult = true;
				int expectedValue = value;

				TestNormal<int>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable, Reference Type")]
			public void value_unconformable_ReferenceType() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: unconformable, Value Type")]
			public void value_unconformable_ValueType() {
				string key = "key";
				bool value = true;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<bool, int>();

				TestError<int, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null, Reference Type")]
			public void value_null_ReferenceType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetNullValueExceptionTest<string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null, Value Type")]
			public void value_null_ValueType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetNullValueExceptionTest<int>();

				TestError<int, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: missing, Reference Type")]
			public void value_missing_ReferenceType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				bool expectedResult = false;
				string expectedValue = null!;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "value: missing, Value Type")]
			public void value_missing_ValueType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};

				bool expectedResult = false;
				int expectedValue = 0;

				TestNormal<int>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			#endregion
		}

		#endregion


		#region TryGetNullableValue

		public class TryGetNullableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, bool expectedResult, T? expectedValue) where T : class, TValue where TKey : notnull where TValue : class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				T? actualValue1;
				T? actualValue2;
				T? actualValue3;
				// overload 1: bool TryGetNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult1 = readOnlyDictionarySample.TryGetNullableValue<T, TKey, TValue>(key, out actualValue1);
				// overload 2: bool TryGetNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult2 = dictionarySample.TryGetNullableValue<T, TKey, TValue>(key, out actualValue2);
				// overload 3: bool TryGetNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				bool actualResult3 = sample.TryGetNullableValue<T, TKey, TValue>(key, out actualValue3);

				// Assert
				Assert.Equal(expectedResult, actualResult1);
				Assert.Equal(expectedResult, actualResult2);
				Assert.Equal(expectedResult, actualResult3);
				Assert.Equal(expectedValue, actualValue1);
				Assert.Equal(expectedValue, actualValue2);
				Assert.Equal(expectedValue, actualValue3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, bool expectedResult, T? expectedValue) where T : class {
				// test TryGetNullableValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, expectedResult, expectedValue);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				T? actualValue1;
				T? actualValue2;
				T? actualValue3;
				// overload 1: bool TryGetNullableValue<T>(this IReadOnlyDictionary<string, object?>, string, out T)
				bool actualResult1 = readOnlyDictionarySample.TryGetNullableValue<T>(key, out actualValue1);
				// overload 2: bool TryGetNullableValue<T>(this IDictionary<string, object?>, string, out T)
				bool actualResult2 = dictionarySample.TryGetNullableValue<T>(key, out actualValue2);
				// overload 3: bool TryGetNullableValue<T>(this IDictionary<string, object?>, string, out T)
				bool actualResult3 = sample.TryGetNullableValue<T>(key, out actualValue3);

				// Assert
				Assert.Equal(expectedResult, actualResult1);
				Assert.Equal(expectedResult, actualResult2);
				Assert.Equal(expectedResult, actualResult3);
				Assert.Equal(expectedValue, actualValue1);
				Assert.Equal(expectedValue, actualValue2);
				Assert.Equal(expectedValue, actualValue3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, Action<TException> testException) where T : class, TValue where TKey : notnull where TValue : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				T? dummy;
				// overload 1: bool TryGetNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, out T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.TryGetNullableValue<T, TKey, TValue>(key, out dummy); });
				// overload 2: bool TryGetNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.TryGetNullableValue<T, TKey, TValue>(key, out dummy); });
				// overload 3: bool TryGetNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, out T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.TryGetNullableValue<T, TKey, TValue>(key, out dummy); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, Action<TException> testException) where T : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test TryGetNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				T? dummy;
				// overload 1: bool TryGetNullableValue<T>(this IReadOnlyDictionary<string, object>, string, out T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.TryGetNullableValue<T>(key, out dummy); });
				// overload 2: bool TryGetNullableValue<T>(this IDictionary<string, object>, string, out T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.TryGetNullableValue<T>(key, out dummy); });
				// overload 3: bool TryGetNullableValue<T>(this IDictionary<string, object>, string, out T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.TryGetNullableValue<T>(key, out dummy); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable")]
			public void value_conformable() {
				string key = "key";
				string? value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				bool expectedResult = true;
				string? expectedValue = value;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable")]
			public void value_unconformable() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null")]
			public void value_null() {
				string key = "key";
				string? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				bool expectedResult = true;
				string? expectedValue = value;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "value: missing")]
			public void value_missing() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				bool expectedResult = false;
				string? expectedValue = null;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			#endregion
		}

		#endregion


		#region GetOptionalValue

		public class GetOptionalValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T defaultValue, T expectedValue) where T : TValue where TKey : notnull where TValue : class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T GetOptionalValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, T)
				T actual1 = readOnlyDictionarySample.GetOptionalValue<T, TKey, TValue>(key, defaultValue);
				// overload 2: T GetOptionalValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				T actual2 = dictionarySample.GetOptionalValue<T, TKey, TValue>(key, defaultValue);
				// overload 3: T GetOptionalValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				T actual3 = sample.GetOptionalValue<T, TKey, TValue>(key, defaultValue);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, T defaultValue, T expectedValue) where T : notnull {
				// test GetOptionalValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, defaultValue, expectedValue);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T GetOptionalValue<T>(this IReadOnlyDictionary<string, object?>, string, T)
				T actual1 = readOnlyDictionarySample.GetOptionalValue<T>(key, defaultValue);
				// overload 2: T GetOptionalValue<T>(this IDictionary<string, object?>, string, T)
				T actual2 = dictionarySample.GetOptionalValue<T>(key, defaultValue);
				// overload 3: T GetOptionalValue<T>(this IDictionary<string, object?>, string, T)
				T actual3 = sample.GetOptionalValue<T>(key, defaultValue);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, T defaultValue, Action<TException> testException) where T : TValue where TKey : notnull where TValue : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T GetOptionalValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetOptionalValue<T, TKey, TValue>(key, defaultValue); });
				// overload 2: T GetOptionalValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetOptionalValue<T, TKey, TValue>(key, defaultValue); });
				// overload 3: T GetOptionalValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetOptionalValue<T, TKey, TValue>(key, defaultValue); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, T defaultValue, Action<TException> testException) where T : notnull where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test TryGetValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, defaultValue, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T GetOptionalValue<T>(this IReadOnlyDictionary<string, object>, string, T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetOptionalValue<T>(key, defaultValue); });
				// overload 2: T GetOptionalValue<T>(this IDictionary<string, object>, string, T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetOptionalValue<T>(key, defaultValue); });
				// overload 3: T GetOptionalValue<T>(this IDictionary<string, object>, string, T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetOptionalValue<T>(key, defaultValue); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable, Reference Type")]
			public void value_conformable_ReferenceType() {
				string key = "key";
				string value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string defaultValue = "default";

				string expectedValue = value;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: conformable, Value Type")]
			public void value_conformable_ValueType() {
				string key = "key";
				int value = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				int defaultValue = -1;

				int expectedValue = value;

				TestNormal<int>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable, Reference Type")]
			public void value_unconformable_ReferenceType() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string defaultValue = "default";

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "value: unconformable, Value Type")]
			public void value_unconformable_ValueType() {
				string key = "key";
				bool value = false;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				int defaultValue = -1;

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<bool, int>();

				TestError<int, InvalidCastException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "value: null, Reference Type")]
			public void value_null_ReferenceType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string defaultValue = "default";

				Action<InvalidCastException> testException = GetNullValueExceptionTest<string>();

				TestError<string, InvalidCastException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "value: null, Value Type")]
			public void value_null_ValueType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				int defaultValue = -1;

				Action<InvalidCastException> testException = GetNullValueExceptionTest<int>();

				TestError<int, InvalidCastException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "value: missing, Reference Type")]
			public void value_missing_ReferenceType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				string defaultValue = "default";

				string expectedValue = defaultValue;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: missing, Value Type")]
			public void value_missing_ValueType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};
				int defaultValue = 3;

				int expectedValue = defaultValue;

				TestNormal<int>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;
				string defaultValue = "default";

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();
				string defaultValue = "default";

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, defaultValue, testException);
			}

			#endregion
		}

		#endregion


		#region GetOptionalNullableValue

		public class GetOptionalNullableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T? defaultValue, T? expectedValue) where T : class, TValue where TKey : notnull where TValue : class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T? GetOptionalNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, T)
				T? actual1 = readOnlyDictionarySample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue);
				// overload 2: T? GetOptionalNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				T? actual2 = dictionarySample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue);
				// overload 3: T? GetOptionalNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				T? actual3 = sample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, T? defaultValue, T? expectedValue) where T : class {
				// test TryGetNullableValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, defaultValue, expectedValue);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T? GetOptionalNullableValue<T>(this IReadOnlyDictionary<string, object?>, string, T)
				T? actual1 = readOnlyDictionarySample.GetOptionalNullableValue<T>(key, defaultValue);
				// overload 2: T? GetOptionalNullableValue<T>(this IDictionary<string, object?>, string, T)
				T? actual2 = dictionarySample.GetOptionalNullableValue<T>(key, defaultValue);
				// overload 3: T? GetOptionalNullableValue<T>(this IDictionary<string, object?>, string, T)
				T? actual3 = sample.GetOptionalNullableValue<T>(key, defaultValue);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, T? defaultValue, Action<TException> testException) where T : class, TValue where TKey : notnull where TValue : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T? GetOptionalNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey, T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue); });
				// overload 2: T? GetOptionalNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue); });
				// overload 3: T? GetOptionalNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey, T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetOptionalNullableValue<T, TKey, TValue>(key, defaultValue); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, T? defaultValue, Action<TException> testException) where T : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test GetOptionalNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, defaultValue, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T? GetOptionalNullableValue<T>(this IReadOnlyDictionary<string, object>, string, T)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetOptionalNullableValue<T>(key, defaultValue); });
				// overload 2: T? GetOptionalNullableValue<T>(this IDictionary<string, object>, string, T)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetOptionalNullableValue<T>(key, defaultValue); });
				// overload 3: T? GetOptionalNullableValue<T>(this IDictionary<string, object>, string, T)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetOptionalNullableValue<T>(key, defaultValue); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable")]
			public void value_conformable() {
				string key = "key";
				string? value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string? defaultValue = "default";

				string? expectedValue = value;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable")]
			public void value_unconformable() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string? defaultValue = "default";

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "value: null")]
			public void value_null() {
				string key = "key";
				string? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};
				string? defaultValue = "default";

				string? expectedValue = value;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: missing; defaultValue: non-null")]
			public void value_missing_defaultValue_nonnull() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				string? defaultValue = "default";

				string? expectedValue = defaultValue;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "value: missing; defaultValue: null")]
			public void value_missing_defaultValue_null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				string? defaultValue = null;

				string? expectedValue = defaultValue;

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;
				string defaultValue = "default";

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, defaultValue, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();
				string defaultValue = "default";

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, defaultValue, testException);
			}

			#endregion
		}

		#endregion


		#region GetIndispensableValue

		public class GetIndispensableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T expectedValue) where T : TValue where TKey : notnull where TValue : class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T GetIndispensableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey)
				T actual1 = readOnlyDictionarySample.GetIndispensableValue<T, TKey, TValue>(key);
				// overload 2: T GetIndispensableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				T actual2 = dictionarySample.GetIndispensableValue<T, TKey, TValue>(key);
				// overload 3: T GetIndispensableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				T actual3 = sample.GetIndispensableValue<T, TKey, TValue>(key);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, T expectedValue) where T : notnull {
				// test GetIndispensableValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, expectedValue);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T GetIndispensableValue<T>(this IReadOnlyDictionary<string, object?>, string)
				T actual1 = readOnlyDictionarySample.GetIndispensableValue<T>(key);
				// overload 2: T GetIndispensableValue<T>(this IDictionary<string, object?>, string)
				T actual2 = dictionarySample.GetIndispensableValue<T>(key);
				// overload 3: T GetIndispensableValue<T>(this IDictionary<string, object?>, string)
				T actual3 = sample.GetIndispensableValue<T>(key);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, Action<TException> testException) where T : TValue where TKey : notnull where TValue : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T GetIndispensableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetIndispensableValue<T, TKey, TValue>(key); });
				// overload 2: T GetIndispensableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetIndispensableValue<T, TKey, TValue>(key); });
				// overload 3: T GetIndispensableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetIndispensableValue<T, TKey, TValue>(key); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, Action<TException> testException) where T : notnull where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test GetIndispensableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T GetIndispensableValue<T>(this IReadOnlyDictionary<string, object>, string)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetIndispensableValue<T>(key); });
				// overload 2: T GetIndispensableValue<T>(this IDictionary<string, object>, string)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetIndispensableValue<T>(key); });
				// overload 3: T GetIndispensableValue<T>(this IDictionary<string, object>, string)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetIndispensableValue<T>(key); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable, Reference Type")]
			public void value_conformable_ReferenceType() {
				string key = "key";
				string value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				string expectedValue = value;

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "value: conformable, Value Type")]
			public void value_conformable_ValueType() {
				string key = "key";
				int value = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				int expectedValue = value;

				TestNormal<int>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable, Reference Type")]
			public void value_unconformable_ReferenceType() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: unconformable, Value Type")]
			public void value_unconformable_ValueType() {
				string key = "key";
				bool value = true;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<bool, int>();

				TestError<int, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null, Reference Type")]
			public void value_null_ReferenceType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetNullValueExceptionTest<string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null, Value Type")]
			public void value_null_ValueType() {
				string key = "key";
				object? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetNullValueExceptionTest<int>();

				TestError<int, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: missing, Reference Type")]
			public void value_missing_ReferenceType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				Action<KeyNotFoundException> testException = GetMissingKeyExceptionTest(key);

				TestError<string, KeyNotFoundException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: missing, Value Type")]
			public void value_missing_ValueType() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};

				Action<KeyNotFoundException> testException = GetMissingKeyExceptionTest(key);

				TestError<int, KeyNotFoundException>(sample, key, testException);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			#endregion
		}

		#endregion


		#region GetIndispensableNullableValue

		public class GetIndispensableNullableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T? expectedValue) where T : class, TValue where TKey : notnull where TValue : class {
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T? GetIndispensableNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey)
				T? actual1 = readOnlyDictionarySample.GetIndispensableNullableValue<T, TKey, TValue>(key);
				// overload 2: T? GetIndispensableNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				T? actual2 = dictionarySample.GetIndispensableNullableValue<T, TKey, TValue>(key);
				// overload 3: T? GetIndispensableNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				T? actual3 = sample.GetIndispensableNullableValue<T, TKey, TValue>(key);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestNormal<T>(Dictionary<string, object?> sample, string key, T? expectedValue) where T : class {
				// test GetIndispensableNullableValue<T, TKey, TValue> overloads
				TestNormal<T, string, object>(sample, key, expectedValue);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T? GetIndispensableNullableValue<T>(this IReadOnlyDictionary<string, object?>, string)
				T? actual1 = readOnlyDictionarySample.GetIndispensableNullableValue<T>(key);
				// overload 2: T? GetIndispensableNullableValue<T>(this IDictionary<string, object?>, string)
				T? actual2 = dictionarySample.GetIndispensableNullableValue<T>(key);
				// overload 3: T? GetIndispensableNullableValue<T>(this IDictionary<string, object?>, string)
				T? actual3 = sample.GetIndispensableNullableValue<T>(key);

				// Assert
				Assert.Equal(expectedValue, actual1);
				Assert.Equal(expectedValue, actual2);
				Assert.Equal(expectedValue, actual3);
			}

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, Action<TException> testException) where T : class, TValue where TKey : notnull where TValue : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;

				// Act
				// overload 1: T? GetIndispensableNullableValue<T, TKey, TValue>(this IReadOnlyDictionary<TKey, TValue?>, TKey)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetIndispensableNullableValue<T, TKey, TValue>(key); });
				// overload 2: T? GetIndispensableNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetIndispensableNullableValue<T, TKey, TValue>(key); });
				// overload 3: T? GetIndispensableNullableValue<T, TKey, TValue>(this IDictionary<TKey, TValue?>, TKey)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetIndispensableNullableValue<T, TKey, TValue>(key); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, Action<TException> testException) where T : class where TException : Exception {
				// argument checks
				Debug.Assert(testException != null);

				// test GetOptionalNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;

				// Act
				// overload 1: T? GetIndispensableNullableValue<T>(this IReadOnlyDictionary<string, object>, string)
				TException actualException1 = Assert.Throws<TException>(() => { readOnlyDictionarySample.GetIndispensableNullableValue<T>(key); });
				// overload 2: T? GetIndispensableNullableValue<T>(this IDictionary<string, object>, string)
				TException actualException2 = Assert.Throws<TException>(() => { dictionarySample.GetIndispensableNullableValue<T>(key); });
				// overload 3: T? GetIndispensableNullableValue<T>(this IDictionary<string, object>, string)
				TException actualException3 = Assert.Throws<TException>(() => { sample.GetIndispensableNullableValue<T>(key); });

				// Assert
				testException(actualException1);
				testException(actualException2);
				testException(actualException3);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "value: conformable")]
			public void value_conformable() {
				string key = "key";
				string? value = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				string? expectedValue = value;

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "value: unconformable")]
			public void value_unconformable() {
				string key = "key";
				Version value = new Version(1, 2, 3);
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "value: null")]
			public void value_null() {
				string key = "key";
				string? value = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, value }
				};

				string? expectedValue = value;

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "value: missing")]
			public void value_missing() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				Action<KeyNotFoundException> testException = GetMissingKeyExceptionTest(key);

				TestError<string, KeyNotFoundException>(sample, key, testException);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("dictionary");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();

				Action<ArgumentNullException> testException = ArgumentExceptionTester<ArgumentNullException>.GetTest("key");

				TestError<string, ArgumentNullException>(sample, key, testException);
			}

			#endregion
		}

		#endregion
	}
}
