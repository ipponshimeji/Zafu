using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Zafu.Testing;
using Zafu.Text;

namespace Zafu.Logging.Tests {
	public class SimpleStateTest {
		#region samples

		public class SampleBase<T>: Zafu.Testing.SampleBase<T> {
			#region data

			public readonly string Text;

			public readonly string JsonText;

			#endregion


			#region constructor

			public SampleBase(string description, T value, string text, string jsonText): base(description, value) {
				// initialize members
				this.Text = text;
				this.JsonText = jsonText;
			}

			#endregion
		}

		public class Sample: SampleBase<SimpleState> {
			#region constructor

			public Sample(string description, string? source, string? message, string text, string jsonText): base(description, new SimpleState(source, message), text, jsonText) {
			}

			public Sample(string description, SimpleState value, string text, string jsonText): base(description, value, text, jsonText) {
			}

			#endregion
		}


		public static SimpleState GeneralSampleValue => new SimpleState("method1", "hello!");

		public static IEnumerable<Sample> GetSamples() {
			return new Sample[] {
				new Sample(
					description: "general",
					source: "method1",
					message: "hello!",
					text: "source: \"method1\", message: \"hello!\"",
					jsonText: "{ \"source\": \"method1\", \"message\": \"hello!\" }"
				),
				new Sample(
					description: "property values: empty",
					source: "",
					message: "",
					text: "source: \"\", message: \"\"",
					jsonText: "{ \"source\": \"\", \"message\": \"\" }"
				),
				new Sample(
					description: "property values: contains special characters",
					source: "a\\b",
					message: "hello \"world!\"",
					text: "source: \"a\\\\b\", message: \"hello \\\"world!\\\"\"",
					jsonText: "{ \"source\": \"a\\\\b\", \"message\": \"hello \\\"world!\\\"\" }"
				),
			};
		}

		public static IEnumerable<object[]> GetSampleData() {
			return GetSamples().ToTestData();
		}

		#endregion


		#region constructor

		public class Constructor {
			#region tests

			[Fact(DisplayName = "general")]
			public void General() {
				// arrange
				string source = "method1";
				string message = "hello!";

				// act
				SimpleState target = new SimpleState(source, message);

				// assert
				Assert.Equal(source, target.Source);
				Assert.Equal(message, target.Message);
			}

			[Fact(DisplayName = "default")]
			public void Default() {
				// arrange

				// act
				SimpleState target = new SimpleState();

				// assert
				Assert.Equal(string.Empty, target.Source);
				Assert.Equal(string.Empty, target.Message);
			}

			[Fact(DisplayName = "source: null, message: null")]
			public void source_null_message_null() {
				// null values should be converted to empty strings.
	
				// arrange
				string? source = null;
				string? message = null;

				// act
				SimpleState target = new SimpleState(source, message);

				// assert
				Assert.Equal(string.Empty, target.Source);
				Assert.Equal(string.Empty, target.Message);
			}

			#endregion
		}

		#endregion


		#region IReadOnlyDictionary<string, object>

		public class AsReadOnlyDictionary {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTest.GetSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void General(Sample sample) {
				// arrange
				SimpleState target = sample.Value;
				KeyValuePair<string, object?>[] expected = new KeyValuePair<string, object?>[] {
					new KeyValuePair<string, object?>(SimpleState.SourcePropertyName, target.Source),
					new KeyValuePair<string, object?>(SimpleState.MessagePropertyName, target.Message)
				};
				const string invalidKey = "NotAKey";

				// assert

				// as IEnumerable
				Assert.Equal(expected, (IEnumerable)target);

				// as IEnumerable<KeyValuePair<string, object>>
				Assert.Equal(expected, (IEnumerable<KeyValuePair<string, object?>>)target);

				// as IReadOnlyCollection<KeyValuePair<string, object?>>
				Assert.Equal(2, target.Count);

				// IReadOnlyDictionary<string, object?>
				// item
				Assert.Equal(target.Source, target[SimpleState.SourcePropertyName]);
				Assert.Equal(target.Message, target[SimpleState.MessagePropertyName]);
				Assert.Throws<KeyNotFoundException>(() => {
					object? dummy = target[invalidKey];
				});
				// Keys
				Assert.Equal(new string[] { SimpleState.SourcePropertyName, SimpleState.MessagePropertyName }, target.Keys);
				// Values
				Assert.Equal(new object?[] { target.Source, target.Message }, target.Values);
				// Contains
				Assert.True(target.ContainsKey(SimpleState.SourcePropertyName));
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
				testTryGetValue(target, SimpleState.MessagePropertyName, true, target.Message);
				testTryGetValue(target, invalidKey, false, null);
			}

			#endregion
		}

		#endregion


		#region stringizing (ToString and ToJson)

		public class Stringizing {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return SimpleStateTest.GetSampleData();
			}

			#endregion


			#region utilities

			protected void TestFormatter(SimpleState target, IJsonFormatter formatter) {
				// arrange
				string expected = JsonUtil.GetJsonObject<SimpleState>(target, formatter);

				// act
				string actual = target.ToJson(formatter);

				// assert
				Assert.Equal(expected, actual);
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void General(Sample sample) {
				// arrange
				SimpleState target = sample.Value;
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

			[Fact(DisplayName = "format: invalid")]
			public void format_invalid() {
				// arrange
				SimpleState target;
				string format = "invalid";
				IFormatProvider? formatProvider = null;

				// act
				FormatException actual = Assert.Throws<FormatException>(() => {
					target.ToString(format, formatProvider);
				});

				// assert
				Assert.Equal("The invalid format string is not supported.", actual.Message);
			}

			[Fact(DisplayName = "formatter: CompactJsonFormatter")]
			public void formatter_CompactJsonFormatter() {
				// arrange
				SimpleState target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.CompactFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			[Fact(DisplayName = "formatter: LineJsonFormatter")]
			public void formatter_LineJsonFormatter() {
				// arrange
				SimpleState target = GeneralSampleValue;
				IJsonFormatter formatter = JsonUtil.LineFormatter;

				// act & assert
				TestFormatter(target, formatter);
			}

			#endregion
		}

		#endregion


		#region comparison (including GetHashCode())

		public class Comparison {
			#region tests

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("source", "message");

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
				SimpleState x = new SimpleState(string.Empty, string.Empty);
				SimpleState y;

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
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("different", "message");

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
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("source", "different");

				// act
				bool actual = x.Equals(y);
				int hash_x = x.GetHashCode();
				int hash_y = y.GetHashCode();

				// assert
				Assert.False(actual);
				Assert.NotEqual(hash_x, hash_y);
			}

			[Fact(DisplayName = "different; type")]
			public void different_type() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				object y = "abc";

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			[Fact(DisplayName = "different; null")]
			public void different_null() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				object? y = null;

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			#endregion
		}

		#endregion
	}
}
