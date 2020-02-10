using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zafu.Utilities;
using Zafu.Utilities.Testing;
using Xunit;

namespace Zafu.Utilities.Test {
	public class DictionaryExtensionsTest {
		#region utilities

		protected static string GetDefaultInvalidCastExceptionMessage(Type from, Type to) {
			// argument checks
			Debug.Assert(from != null);
			Debug.Assert(to != null);

			return $"Unable to cast object of type '{from.FullName}' to type '{to.FullName}'.";
		}

		protected static Action<InvalidCastException> GetInvalidCastExceptionTest<TFrom, TTo>() {
			string message = GetDefaultInvalidCastExceptionMessage(typeof(TFrom), typeof(TTo));
			return new ExceptionTester<InvalidCastException>(message).Test;
		}

		protected static string GetNullValueExceptionMessage(Type to) {
			return "Its value is a null.";
		}

		protected static Action<InvalidCastException> GetNullValueExceptionTest<TTo>() {
			string message = GetNullValueExceptionMessage(typeof(TTo));
			return new ExceptionTester<InvalidCastException>(message).Test;
		}

		protected static string GetMissingKeyExceptionMessage<TKey>(TKey key) {
			return $"The indispensable key '{key}' is missing in the dictionary.";
		}

		protected static Action<KeyNotFoundException> GetNullValueExceptionTest<TKey>(TKey key) {
			string message = GetMissingKeyExceptionMessage<TKey>(key);
			return new ExceptionTester<KeyNotFoundException>(message).Test;
		}

		protected static Action<TException> GetArgumentExceptionTest<TException>(string paramName) where TException : ArgumentException {
			return new ArgumentExceptionTester<TException>(paramName).Test;
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
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(testException != null);

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
				// test TryGetValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(testException != null);

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
				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "T: Value Type, unconformable")]
			public void ValueType_Unconformable() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, true }
				};
				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<bool, int>();

				TestError<int, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "T: Reference Type, null")]
			public void ReferenceType_Null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				Action<InvalidCastException> testException = GetNullValueExceptionTest<string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "T: Value Type, null")]
			public void ValueType_Null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				Action<InvalidCastException> testException = GetNullValueExceptionTest<int>();

				TestError<int, InvalidCastException>(sample, key, testException);
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

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;
				Action<ArgumentNullException> exceptionTest = GetArgumentExceptionTest<ArgumentNullException>("dictionary");

				TestError<string, ArgumentNullException>(sample, key, exceptionTest);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();
				Action<ArgumentNullException> exceptionTest = GetArgumentExceptionTest<ArgumentNullException>("key");

				TestError<string, ArgumentNullException>(sample, key, exceptionTest);
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
				// Arrange
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(testException != null);

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
				// test TryGetNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, testException);

				// Arrange
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(testException != null);

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

			[Fact(DisplayName = "conformable")]
			public void Conformable() {
				string key = "key";
				string? expectedValue = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};
				bool expectedResult = true;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "unconformable")]
			public void Unconformable() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, new Version(1, 2, 3) }
				};
				Action<InvalidCastException> testException = GetInvalidCastExceptionTest<Version, string>();

				TestError<string, InvalidCastException>(sample, key, testException);
			}

			[Fact(DisplayName = "null")]
			public void Null() {
				string key = "key";
				string? expectedValue = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};
				bool expectedResult = true;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "not found")]
			public void NotFound() {
				string key = "key";
				string? expectedValue = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				bool expectedResult = false;

				TestNormal<string>(sample, key, expectedResult, expectedValue);
			}

			[Fact(DisplayName = "dictionary: null")]
			public void dictionary_null() {
				string key = "key";
				Dictionary<string, object?> sample = null!;
				Action<ArgumentNullException> exceptionTest = GetArgumentExceptionTest<ArgumentNullException>("dictionary");

				TestError<string, ArgumentNullException>(sample, key, exceptionTest);
			}

			[Fact(DisplayName = "key: null")]
			public void key_null() {
				string key = null!;
				Dictionary<string, object?> sample = new Dictionary<string, object?>();
				Action<ArgumentNullException> exceptionTest = GetArgumentExceptionTest<ArgumentNullException>("key");

				TestError<string, ArgumentNullException>(sample, key, exceptionTest);
			}

			#endregion
		}

		#endregion


		#region GetOptionalValue

		public class GetOptionalValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T defaultValue, T expectedValue) where T : TValue where TKey : notnull where TValue : class {
				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(key != null);

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
				Debug.Assert(sample != null);
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(key != null);

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

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, T defaultValue, string expectedMessage) where T : TValue where TKey : notnull where TValue : class where TException : Exception {
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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, T defaultValue, string expectedMessage) where T : notnull where TException : Exception {
				// test TryGetValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, defaultValue, expectedMessage);

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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "T: Reference Type, conformable")]
			public void ReferenceType_Conformable() {
				string key = "key";
				string defaultValue = "default";
				string expectedValue = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "T: Value Type, conformable")]
			public void ValueType_Conformable() {
				string key = "key";
				int defaultValue = -1;
				int expectedValue = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<int>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "T: Reference Type, unconformable")]
			public void ReferenceType_Unconformable() {
				string key = "key";
				string defaultValue = "default";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, new Version(1, 2, 3) }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(Version), typeof(string));

				TestError<string, InvalidCastException>(sample, key, defaultValue, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, unconformable")]
			public void ValueType_Unconformable() {
				string key = "key";
				int defaultValue = -1;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, true }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(bool), typeof(int));

				TestError<int, InvalidCastException>(sample, key, defaultValue, expectedMessage);
			}

			[Fact(DisplayName = "T: Reference Type, null")]
			public void ReferenceType_Null() {
				string key = "key";
				string defaultValue = "default";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				string expectedMessage = GetNullValueExceptionMessage(typeof(string));

				TestError<string, InvalidCastException>(sample, key, defaultValue, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, null")]
			public void ValueType_Null() {
				string key = "key";
				int defaultValue = -1;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				string expectedMessage = GetNullValueExceptionMessage(typeof(int));

				TestError<int, InvalidCastException>(sample, key, defaultValue, expectedMessage);
			}

			[Fact(DisplayName = "T: Reference Type, not found")]
			public void ReferenceType_NotFound() {
				string key = "key";
				string defaultValue = "default";
				string expectedValue = defaultValue;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "T: Value Type, not found")]
			public void ValueType_NotFound() {
				string key = "key";
				int defaultValue = 3;
				int expectedValue = defaultValue;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};

				TestNormal<int>(sample, key, defaultValue, expectedValue);
			}

			#endregion
		}

		#endregion


		#region GetOptionalNullableValue

		public class GetOptionalNullableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T? defaultValue, T? expectedValue) where T : class, TValue where TKey : notnull where TValue : class {
				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(key != null);

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
				Debug.Assert(sample != null);
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(key != null);

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

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, T? defaultValue, string expectedMessage) where T : class, TValue where TKey : notnull where TValue : class where TException : Exception {
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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, T? defaultValue, string expectedMessage) where T : class where TException : Exception {
				// test GetOptionalNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, defaultValue, expectedMessage);

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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "conformable")]
			public void Conformable() {
				string key = "key";
				string? defaultValue = "default";
				string? expectedValue = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "unconformable")]
			public void Unconformable() {
				string key = "key";
				string? defaultValue = "default";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, new Version(1, 2, 3) }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(Version), typeof(string));

				TestError<string, InvalidCastException>(sample, key, defaultValue, expectedMessage);
			}

			[Fact(DisplayName = "null")]
			public void Null() {
				string key = "key";
				string? defaultValue = "default";
				string? expectedValue = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "not found, defaultValue: non-null")]
			public void NotFound_defaultValue_NonNull() {
				string key = "key";
				string? defaultValue = "default";
				string? expectedValue = defaultValue;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			[Fact(DisplayName = "not found, defaultValue: null")]
			public void NotFound_defaultValue_Null() {
				string key = "key";
				string? defaultValue = null;
				string? expectedValue = defaultValue;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};

				TestNormal<string>(sample, key, defaultValue, expectedValue);
			}

			#endregion
		}

		#endregion


		#region GetIndispensableValue

		public class GetIndispensableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T expectedValue) where T : TValue where TKey : notnull where TValue : class {
				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(key != null);

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
				Debug.Assert(sample != null);
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(key != null);

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

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, string expectedMessage) where T : TValue where TKey : notnull where TValue : class where TException : Exception {
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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, string expectedMessage) where T : notnull where TException : Exception {
				// test GetIndispensableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, expectedMessage);

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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
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

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "T: Value Type, conformable")]
			public void ValueType_Conformable() {
				string key = "key";
				int expectedValue = 7;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<int>(sample, key, expectedValue);
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
				string expectedMessage = GetNullValueExceptionMessage(typeof(string));

				TestError<string, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, null")]
			public void ValueType_Null() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, null }
				};
				string expectedMessage = GetNullValueExceptionMessage(typeof(int));

				TestError<int, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Reference Type, not found")]
			public void ReferenceType_NotFound() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				string expectedMessage = GetMissingKeyExceptionMessage(key);

				TestError<string, KeyNotFoundException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "T: Value Type, not found")]
			public void ValueType_NotFound() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", 5 }
				};
				string expectedMessage = GetMissingKeyExceptionMessage(key);

				TestError<int, KeyNotFoundException>(sample, key, expectedMessage);
			}

			#endregion
		}

		#endregion


		#region GetIndispensableNullableValue

		public class GetIndispensableNullableValue {
			#region utilities

			protected void TestNormal<T, TKey, TValue>(Dictionary<TKey, TValue?> sample, TKey key, T? expectedValue) where T : class, TValue where TKey : notnull where TValue : class {
				// Arrange
				Debug.Assert(sample != null);
				IReadOnlyDictionary<TKey, TValue?> readOnlyDictionarySample = sample;
				IDictionary<TKey, TValue?> dictionarySample = sample;
				Debug.Assert(key != null);

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
				Debug.Assert(sample != null);
				IReadOnlyDictionary<string, object?> readOnlyDictionarySample = sample;
				IDictionary<string, object?> dictionarySample = sample;
				Debug.Assert(key != null);

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

			protected void TestError<T, TKey, TValue, TException>(Dictionary<TKey, TValue?> sample, TKey key, string expectedMessage) where T : class, TValue where TKey : notnull where TValue : class where TException : Exception {
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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			protected void TestError<T, TException>(Dictionary<string, object?> sample, string key, string expectedMessage) where T : class where TException : Exception {
				// test GetOptionalNullableValue<T, TKey, TValue> overloads
				TestError<T, string, object, TException>(sample, key, expectedMessage);

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
				Assert.Equal(expectedMessage, actualException1.Message);
				Assert.Equal(expectedMessage, actualException2.Message);
				Assert.Equal(expectedMessage, actualException3.Message);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "conformable")]
			public void Conformable() {
				string key = "key";
				string? expectedValue = "value";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "unconformable")]
			public void Unconformable() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, new Version(1, 2, 3) }
				};
				string expectedMessage = GetDefaultInvalidCastExceptionMessage(typeof(Version), typeof(string));

				TestError<string, InvalidCastException>(sample, key, expectedMessage);
			}

			[Fact(DisplayName = "null")]
			public void Null() {
				string key = "key";
				string? expectedValue = null;
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ key, expectedValue }
				};

				TestNormal<string>(sample, key, expectedValue);
			}

			[Fact(DisplayName = "not found")]
			public void NotFound() {
				string key = "key";
				Dictionary<string, object?> sample = new Dictionary<string, object?>() {
					{ "anotherKey", "abc" }
				};
				string expectedMessage = GetMissingKeyExceptionMessage(key);

				TestError<string, KeyNotFoundException>(sample, key, expectedMessage);
			}

			#endregion
		}

		#endregion
	}
}
