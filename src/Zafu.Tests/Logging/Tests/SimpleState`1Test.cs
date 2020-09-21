using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Zafu.Testing;
using Zafu.Text;

namespace Zafu.Logging.Tests {
	public class SimpleStateTTest {
		#region samples

		public class Sample<T>: SimpleStateTest.SampleBase<SimpleState<T>> {
			#region constructor

			public Sample(string description, SimpleState<T> value, string text, string jsonText): base(description, value, text, jsonText) {
			}

			public Sample(string description, string? source, string? message, string? extraPropName, T extraPropValue, string text, string jsonText):
			base(description, new SimpleState<T>(source, message, extraPropName, extraPropValue), text, jsonText) {
			}

			#endregion
		}


		public static SimpleState<int> GeneralInt32SampleValue => new SimpleState<int>("method1", "hello!", "index", 5);

		public static IEnumerable<Sample<int>> GetInt32Samples() {
			return new Sample<int>[] {
				new Sample<int>(
					description: "general",
					source: "method1",
					message: "hello!",
					extraPropName: "index",
					extraPropValue: 5,
					text: "source: \"method1\", index: 5, message: \"hello!\"",
					jsonText: "{ \"source\": \"method1\", \"index\": 5, \"message\": \"hello!\" }"
				),
				new Sample<int>(
					description: "property values: contains special characters",
					source: "a\\b",
					message: "hello \"world!\"",
					extraPropName: "index",
					extraPropValue: -58,
					text: "source: \"a\\\\b\", index: -58, message: \"hello \\\"world!\\\"\"",
					jsonText: "{ \"source\": \"a\\\\b\", \"index\": -58, \"message\": \"hello \\\"world!\\\"\" }"
				),
				new Sample<int>(
					description: "ExtraPropertyName: empty",
					source: "",
					message: "",
					extraPropName: "",
					extraPropValue: 0,
					text: "source: \"\", : 0, message: \"\"",
					jsonText: "{ \"source\": \"\", \"\": 0, \"message\": \"\" }"
				),
			};
		}

		public static IEnumerable<object[]> GetInt32SampleData() {
			return GetInt32Samples().ToTestData();
		}


		public static SimpleState<string> GeneralStringSampleValue => new SimpleState<string>("method1", "hello!", "description", "general");

		public static IEnumerable<Sample<string>> GetStringSamples() {
			return new Sample<string>[] {
				new Sample<string>(
					description: "general",
					source: "method1",
					message: "hello!",
					extraPropName: "description",
					extraPropValue: "general",
					text: "source: \"method1\", description: \"general\", message: \"hello!\"",
					jsonText: "{ \"source\": \"method1\", \"description\": \"general\", \"message\": \"hello!\" }"
				),
				new Sample<string>(
					description: "property values: empty",
					source: "",
					message: "",
					extraPropName: "description",
					extraPropValue: "",
					text: "source: \"\", description: \"\", message: \"\"",
					jsonText: "{ \"source\": \"\", \"description\": \"\", \"message\": \"\" }"
				),
				new Sample<string>(
					description: "property values: contains special characters",
					source: "a\\b",
					message: "hello \"world!\"",
					extraPropName: "description",
					extraPropValue: "\0",
					text: "source: \"a\\\\b\", description: \"\\u0000\", message: \"hello \\\"world!\\\"\"",
					jsonText: "{ \"source\": \"a\\\\b\", \"description\": \"\\u0000\", \"message\": \"hello \\\"world!\\\"\" }"
				),
				new Sample<string>(
					description: "ExtraPropertyName: empty",
					source: "",
					message: "",
					extraPropName: "",
					extraPropValue: "empty",
					text: "source: \"\", : \"empty\", message: \"\"",
					jsonText: "{ \"source\": \"\", \"\": \"empty\", \"message\": \"\" }"
				),
			};
		}

		public static IEnumerable<object[]> GetStringSampleData() {
			return GetStringSamples().ToTestData();
		}

		#endregion


		#region constructor

		public abstract class ConstructorBase<T> {
			#region overridables

			/// <summary>
			/// The sample value which can be used as extra property value.
			/// </summary>
			protected abstract T Value { get; }

			#endregion


			#region tests

			[Fact(DisplayName = "general")]
			public void General() {
				// arrange
				string source = "method1";
				string message = "hello!";
				string extraPropName = "extra";
				T extraPropValue = this.Value;

				// act
				SimpleState<T> target = new SimpleState<T>(source, message, extraPropName, extraPropValue);

				// assert
				Assert.Equal(source, target.Source);
				Assert.Equal(message, target.Message);
				Assert.Equal(extraPropName, target.ExtraPropertyName);
				Assert.Equal(extraPropValue, target.ExtraPropertyValue);
			}

			[Fact(DisplayName = "default")]
			public void Default() {
				// arrange

				// act
				SimpleState<T> target = new SimpleState<T>();

				// assert
				Assert.Equal(string.Empty, target.Source);
				Assert.Equal(string.Empty, target.Message);
				Assert.Equal(string.Empty, target.ExtraPropertyName);
				Assert.Equal(default(T)!, target.ExtraPropertyValue);
			}

			[Fact(DisplayName = "source: null, message: null, extraPropName: null")]
			public void source_null_message_null_extraPropName_null() {
				// null values should be converted to empty strings.

				// arrange
				string? source = null;
				string? message = null;
				string? extraPropName = null;
				T extraPropValue = this.Value;

				// act
				SimpleState<T> target = new SimpleState<T>(source, message, extraPropName, extraPropValue);

				// assert
				Assert.Equal(string.Empty, target.Source);
				Assert.Equal(string.Empty, target.Message);
				Assert.Equal(string.Empty, target.ExtraPropertyName);
				Assert.Equal(extraPropValue, target.ExtraPropertyValue);
			}

			#endregion
		}

		public class Constructor_Int32: ConstructorBase<int> {
			#region overrides

			protected override int Value => 5678;

			#endregion
		}

		public class Constructor_String: ConstructorBase<string> {
			#region overrides

			protected override string Value => "abcde";

			#endregion
		}

		#endregion


		#region IReadOnlyDictionary<string, object?>

		public abstract class AsReadOnlyDictionaryBase<T> {
			#region utilities

			protected void TestGeneral(SimpleState<T> target) {
				// arrange
				KeyValuePair<string, object?>[] expected = new KeyValuePair<string, object?>[] {
					new KeyValuePair<string, object?>(SimpleState.SourcePropertyName, target.Source),
					new KeyValuePair<string, object?>(target.ExtraPropertyName, target.ExtraPropertyValue),
					new KeyValuePair<string, object?>(SimpleState.MessagePropertyName, target.Message)
				};
				const string invalidKey = "NotAKey";

				// assert

				// as IEnumerable
				Assert.Equal(expected, (IEnumerable)target);

				// as IEnumerable<KeyValuePair<string, object>>
				Assert.Equal(expected, (IEnumerable<KeyValuePair<string, object?>>)target);

				// as IReadOnlyCollection<KeyValuePair<string, object?>>
				Assert.Equal(3, target.Count);

				// IReadOnlyDictionary<string, object?>
				// item
				Assert.Equal(target.Source, target[SimpleState.SourcePropertyName]);
				Assert.Equal(target.Message, target[SimpleState.MessagePropertyName]);
				Assert.Equal(target.ExtraPropertyValue, target[target.ExtraPropertyName]);
				Assert.Throws<KeyNotFoundException>(() => {
					object? dummy = target[invalidKey];
				});
				// Keys
				Assert.Equal(new string[] { SimpleState.SourcePropertyName, target.ExtraPropertyName, SimpleState.MessagePropertyName }, target.Keys);
				// Values
				Assert.Equal(new object?[] { target.Source, target.ExtraPropertyValue, target.Message }, target.Values);
				// Contains
				Assert.True(target.ContainsKey(SimpleState.SourcePropertyName));
				Assert.True(target.ContainsKey(target.ExtraPropertyName));
				Assert.True(target.ContainsKey(SimpleState.MessagePropertyName));
				Assert.False(target.ContainsKey(invalidKey));
				// TryGetValue
				static void testTryGetValue(IReadOnlyDictionary<string, object?> t, string name, bool expectedResult, object? expectedValue) {
					object? actualValue;
					bool actualResult = t.TryGetValue(name, out actualValue);
					Assert.Equal(expectedResult, actualResult);
					Assert.Equal(expectedValue, actualValue);
				}
				testTryGetValue(target, SimpleState.SourcePropertyName, true, target.Source);
				testTryGetValue(target, target.ExtraPropertyName, true, target.ExtraPropertyValue);
				testTryGetValue(target, SimpleState.MessagePropertyName, true, target.Message);
				testTryGetValue(target, invalidKey, false, null);
			}

			#endregion
		}

		public class AsReadOnlyDictionary_Int32: AsReadOnlyDictionaryBase<int> {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetInt32SampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void general(Sample<int> sample) {
				TestGeneral(sample.Value);
			}

			#endregion
		}

		public class AsReadOnlyDictionary_String: AsReadOnlyDictionaryBase<string> {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetStringSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void general(Sample<string> sample) {
				TestGeneral(sample.Value);
			}

			#endregion
		}

		#endregion


		#region stringizing (ToString and ToJson)

		public abstract class StringizingBase<T> {
			#region utilities

			protected void TestGeneral(Sample<T> sample) {
				// arrange
				SimpleState<T> target = sample.Value;
				IFormatProvider? formatProvider = null;

				// act
				string actual_string = target.ToString();
				string actual_format_G = target.ToString("G", formatProvider);
				string actual_format_g = target.ToString("g", formatProvider);
				string actual_format_null = target.ToString(null, formatProvider);
				string actual_format_empty = target.ToString(string.Empty, formatProvider);
				string actual_json = target.ToJson();
				string actual_format_J = target.ToString("J", formatProvider);
				string actual_format_j = target.ToString("j", formatProvider);

				// assert
				Assert.Equal(sample.Text, actual_string);
				Assert.Equal(sample.Text, actual_format_G);
				Assert.Equal(sample.Text, actual_format_g);
				Assert.Equal(sample.Text, actual_format_null);
				Assert.Equal(sample.Text, actual_format_empty);
				Assert.Equal(sample.JsonText, actual_json);
				Assert.Equal(sample.JsonText, actual_format_J);
				Assert.Equal(sample.JsonText, actual_format_j);
			}

			protected void TestFormatter(SimpleState<T> target, IJsonFormatter formatter) {
				// arrange
				string expected = JsonUtil.GetJsonObject<SimpleState<T>>(target, formatter);

				// act
				string actual = target.ToJson(formatter);

				// assert
				Assert.Equal(expected, actual);
			}

			#endregion


			#region tests

			[Fact(DisplayName = "format: invalid")]
			public void format_invalid() {
				// arrange
				SimpleState<T> target = new SimpleState<T>();
				string format = "invalid";
				IFormatProvider? formatProvider = null;

				// act
				FormatException actual = Assert.Throws<FormatException>(() => {
					target.ToString(format, formatProvider);
				});

				// assert
				Assert.Equal("The invalid format string is not supported.", actual.Message);
			}

			#endregion
		}

		public class Stringizing_Int32: StringizingBase<int> {
			#region samples

			public static SimpleState<int> GeneralSampleValue => GeneralInt32SampleValue;

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetInt32SampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void general(Sample<int> sample) {
				TestGeneral(sample);
			}

			[Fact(DisplayName = "formatter: CompactJsonFormatter")]
			public void formatter_CompactJsonFormatter() {
				// arrange
				SimpleState<int> target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.CompactFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			[Fact(DisplayName = "formatter: LineJsonFormatter")]
			public void formatter_LineJsonFormatter() {
				// arrange
				SimpleState<int> target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.LineFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			#endregion
		}

		public class Stringizing_String: StringizingBase<string> {
			#region samples

			public static SimpleState<string> GeneralSampleValue => GeneralStringSampleValue;

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTTest.GetStringSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void general(Sample<string> sample) {
				TestGeneral(sample);
			}

			[Fact(DisplayName = "formatter: CompactJsonFormatter")]
			public void formatter_CompactJsonFormatter() {
				// arrange
				SimpleState<string> target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.CompactFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			[Fact(DisplayName = "formatter: LineJsonFormatter")]
			public void formatter_LineJsonFormatter() {
				// arrange
				SimpleState<string> target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.LineFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			#endregion
		}

		#endregion


		#region comparison (including GetHashCode())

		public abstract class ComparisonBase<T> {
			#region overridables

			protected abstract T Value1 { get; }

			protected abstract T Value2 { get; }

			#endregion


			#region tests

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				T value = this.Value1;
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", value);
				SimpleState<T> y = new SimpleState<T>("source", "message", "extra", value);

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.True(actual);
				Assert.Equal(hash_x, hash_y);
			}

			[Fact(DisplayName = "same; empty")]
			public void same_empty() {
				// arrange
				SimpleState<T> x = new SimpleState<T>(string.Empty, string.Empty, string.Empty, default(T)!);
				SimpleState<T> y = new SimpleState<T>();

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.True(actual);
				Assert.Equal(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; Source")]
			public void different_Source() {
				// arrange
				T value = this.Value1;
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", value);
				SimpleState<T> y = new SimpleState<T>("different", "message", "extra", value);

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.False(actual);
				Assert.NotEqual(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; Message")]
			public void different_Message() {
				// arrange
				T value = this.Value1;
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", value);
				SimpleState<T> y = new SimpleState<T>("source", "different", "extra", value);

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.False(actual);
				Assert.NotEqual(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; ExtraPropertyName")]
			public void different_ExtraPropertyName() {
				// arrange
				T value = this.Value1;
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", value);
				SimpleState<T> y = new SimpleState<T>("source", "message", "different", value);

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.False(actual);
				Assert.NotEqual(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; ExtraPropertyValue")]
			public void different_ExtraPropertyValue() {
				// arrange
				T value1 = this.Value1;
				T value2 = this.Value2;
				Debug.Assert(object.Equals(value1, value2) == false);
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", value1);
				SimpleState<T> y = new SimpleState<T>("source", "message", "extra", value2);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
				Assert.NotEqual(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; type")]
			public void different_type() {
				// arrange
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", this.Value1);
				string y = "other type";

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			[Fact(DisplayName = "different; null")]
			public void different_null() {
				// arrange
				SimpleState<T> x = new SimpleState<T>("source", "message", "extra", this.Value1);
				object? y = null;

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			#endregion
		}

		public class Comparison_Int32: ComparisonBase<int> {
			#region overrides

			protected override int Value1 => 30;

			protected override int Value2 => -57;

			#endregion


			#region tests

			[Fact(DisplayName = "different; integer type")]
			public void different_integer_type() {
				// arrange
				SimpleState<int> x = new SimpleState<int>("source", "message", "extra", 12);
				SimpleState<short> y = new SimpleState<short>("source", "message", "extra", 12);

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			#endregion
		}

		public class Comparison_String: ComparisonBase<string> {
			#region overrides

			protected override string Value1 => "abc";

			protected override string Value2 => "123";

			#endregion


			#region tests

			// no specific test

			#endregion
		}

		#endregion
	}
}
