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

		public class Sample {
			#region data

			public readonly SimpleState Value;

			public readonly string Text;

			public readonly string JsonText;

			#endregion


			#region constructor

			public Sample(string? source, string? message, string text, string jsonText) {
				// initialize members
				this.Value = new SimpleState(source, message);
				this.Text = text;
				this.JsonText = jsonText;
			}

			#endregion
		}

		public static IEnumerable<object[]> GetSamples() {
			return new Sample[] {
				//  Sample(source, message, text, jsonText)
				// general
				new Sample(
					"method1", "hello!",
					"source: \"method1\", message: \"hello!\"",
					"{ \"source\": \"method1\", \"message\": \"hello!\" }"
				),
				// properties: empty
				new Sample(
					"", "",
					"source: \"\", message: \"\"",
					"{ \"source\": \"\", \"message\": \"\" }"
				),
				// properties: contains special characters
				new Sample(
					"a\\b", "hello \"world!\"",
					"source: \"a\\\\b\", message: \"hello \\\"world!\\\"\"",
					"{ \"source\": \"a\\\\b\", \"message\": \"hello \\\"world!\\\"\" }"
				),
			}.ToTestData();
		}

		#endregion


		#region IReadOnlyDictionary<string, object>

		public class AsReadOnlyDictionary {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return SimpleStateTest.GetSamples();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
			public void General(Sample sample) {
				// arrange
				SimpleState target = sample.Value;
				KeyValuePair<string, object>[] expected = new KeyValuePair<string, object>[] {
					new KeyValuePair<string, object>(SimpleState.SourcePropertyName, target.Source),
					new KeyValuePair<string, object>(SimpleState.MessagePropertyName, target.Message)
				};
				const string invalidKey = "NotAKey";

				// assert

				// as IEnumerable
				Assert.Equal(expected, (IEnumerable)target);

				// as IEnumerable<KeyValuePair<string, object>>
				Assert.Equal(expected, (IEnumerable<KeyValuePair<string, object>>)target);

				// as IReadOnlyCollection<KeyValuePair<string, object>>
				Assert.Equal(2, target.Count);

				// IReadOnlyDictionary<string, object>
				// item
				Assert.Equal(target.Source, target[SimpleState.SourcePropertyName]);
				Assert.Equal(target.Message, target[SimpleState.MessagePropertyName]);
				Assert.Throws<KeyNotFoundException>(() => {
					object dummy = target[invalidKey];
				});
				// Keys
				Assert.Equal(new string[] { SimpleState.SourcePropertyName, SimpleState.MessagePropertyName }, target.Keys);
				// Values
				Assert.Equal(new object[] { target.Source, target.Message }, target.Values);
				// Contains
				Assert.True(target.ContainsKey(SimpleState.SourcePropertyName));
				Assert.True(target.ContainsKey(SimpleState.MessagePropertyName));
				Assert.False(target.ContainsKey(invalidKey));
				// TryGetValue
				object? value;
				bool found = target.TryGetValue(SimpleState.SourcePropertyName, out value);
				Assert.True(found);
				Assert.Equal(target.Source, value);
				found = target.TryGetValue(SimpleState.MessagePropertyName, out value);
				Assert.True(found);
				Assert.Equal(target.Message, value);
				found = target.TryGetValue(invalidKey, out value);
				Assert.False(found);
				Assert.Null(value);
			}

			#endregion
		}

		#endregion


		#region stringizing (ToString and ToJson)

		public class Stringizing {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return SimpleStateTest.GetSamples();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
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

			#endregion
		}

		#endregion


		#region comparison

		public class Comparison {
			#region tests

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("source", "message");

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.True(actual);
			}

			[Fact(DisplayName = "same; empty")]
			public void same_empty() {
				// arrange
				SimpleState x = new SimpleState(string.Empty, string.Empty);
				SimpleState y;

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.True(actual);
			}

			[Fact(DisplayName = "different; Source")]
			public void different_Source() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("different", "message");

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			[Fact(DisplayName = "different; Message")]
			public void different_Message() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("source", "different");

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
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


		#region GetHashCode

		public class HashCode {
			[Fact(DisplayName = "same")]
			public void same() {
				// arrange
				SimpleState x = new SimpleState("source", "message");
				SimpleState y = new SimpleState("source", "message");
				Debug.Assert(x.Equals(y));

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.Equal(actual_x, actual_y);
			}

			[Fact(DisplayName = "different")]
			public void different() {
				// arrange
				SimpleState x = new SimpleState("source", "message1");
				SimpleState y = new SimpleState("source", "message2");

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.NotEqual(actual_x, actual_y);
			}
		}

		#endregion
	}
}
