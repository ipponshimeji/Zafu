using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;
using Zafu.Testing;

namespace Zafu.Text.Tests {
	public class JsonUtilTest {
		#region types

		public abstract class SimpleValueTestBase<T> {
			#region utilities

			public void TestJsonText(T value, string expected) {
				// check arguments
				Debug.Assert(expected != null);

				// act
				string actual = GetJsonText(value);

				// assert
				Assert.Equal(expected, actual);
			}

			#endregion


			#region overridables

			protected abstract string GetJsonText(T value);

			#endregion
		}

		public abstract class WriteSimpleValueTestBase<T>: SimpleValueTestBase<T> {
			#region overrides

			protected override string GetJsonText(T value) {
				using (StringWriter writer = new StringWriter()) {
					WriteJsonValue(writer, value);
					return writer.ToString();
				}
			}

			#endregion


			#region overridables

			protected abstract void WriteJsonValue(TextWriter writer, T value);

			#endregion


			#region tests

			[Fact(DisplayName = "writer: null")]
			public void writer_null() {
				// arrange
				TextWriter writer = null!;
				T value = default(T)!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					WriteJsonValue(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			#endregion
		}

		public abstract class ComplexValueTestBase {
			#region overridables

			protected abstract JsonUtil.IJsonFormatter GetFormatter();

			#endregion
		}

		#endregion


		#region samples

		public class Sample<T> {
			#region data

			public readonly T Value;

			public readonly string JsonText;

			#endregion


			#region constructor

			public Sample(T value, string jsonText) {
				// initialize members
				this.Value = value;
				this.JsonText = jsonText;
			}

			#endregion
		}

		public class StringSample: Sample<string?> {
			#region data

			/// <summary>
			/// Whether the <see cref="Value"/> contains any special character to be escaped.
			/// </summary>
			public readonly bool ContainsSpecialChar;

			#endregion


			#region constructor

			public StringSample(string? value, string? jsonText, bool containsSpecialChar) : base(value, jsonText) {
				// initialize members
				this.ContainsSpecialChar = containsSpecialChar;
			}

			#endregion
		}

		public static IEnumerable<StringSample> GetStringSamples() {
			return new StringSample[] {
				//              (value, jsonText, containsSpecialChar)
				// no special char
				new StringSample("abc123#-$-", "\"abc123#-$-\"", false),
				// single special char
				new StringSample("\"", "\"\\\"\"", true),
				new StringSample("\\abc", "\"\\\\abc\"", true),
				new StringSample("abc\n123", "\"abc\\n123\"", true),
				new StringSample("123\u0000", "\"123\\u0000\"", true),
				// multiple special chars
				new StringSample("\u0001\\", "\"\\u0001\\\\\"", true),
				new StringSample("\babc\f", "\"\\babc\\f\"", true),
				new StringSample("123\"\"abc\t+-#", "\"123\\\"\\\"abc\\t+-#\"", true),
				// null
				new StringSample(null, "null", false)
			};
		}

		#endregion


		#region JsonNull

		public class JsonNull {
			#region utilities

			/// <summary>
			/// 
			/// </summary>
			/// <remarks>
			/// <c>JsonNullTestBase</c> does not need the type parameter for <see cref="WriteSimpleValueTestBase{T}"/> class,
			/// so it specifies <c>object?</c> as a dummy.
			/// JsonNullT
			/// </remarks>
			public abstract class JsonNullTestBase: WriteSimpleValueTestBase<object?> {
				#region tests

				[Fact(DisplayName = "general")]
				public void General() {
					TestJsonText(null, "null");
				}

				#endregion
			}

			#endregion


			#region tests

			public class WriteJsonNull: JsonNullTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, object? value) {
					JsonUtil.WriteJsonNull(writer);
				}

				#endregion
			}

			public class LineFormatter: JsonNullTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, object? value) {
					JsonUtil.LineFormatter.WriteNull(writer);
				}

				#endregion
			}

			public class CompactFormatter: JsonNullTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, object? value) {
					JsonUtil.CompactFormatter.WriteNull(writer);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonBoolean

		public class JsonBoolean {
			#region utilities

			protected static void TestValues(SimpleValueTestBase<bool> test) {
				// check argument
				Debug.Assert(test != null);

				// test values
				test.TestJsonText(false, "false");
				test.TestJsonText(true, "true");
			}

			public abstract class JsonBooleanTestBase: WriteSimpleValueTestBase<bool> {
				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			#endregion


			#region tests

			public class GetJsonBoolean: SimpleValueTestBase<bool> {
				#region overridables

				protected override string GetJsonText(bool value) {
					return JsonUtil.GetJsonBoolean(value);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			public class WriteJsonBoolean: JsonBooleanTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, bool value) {
					JsonUtil.WriteJsonBoolean(writer, value);
				}

				#endregion
			}

			public class LineFormatter: JsonBooleanTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, bool value) {
					JsonUtil.LineFormatter.WriteBoolean(writer, value);
				}

				#endregion
			}

			public class CompactFormatter: JsonBooleanTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, bool value) {
					JsonUtil.CompactFormatter.WriteBoolean(writer, value);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonNumber

		public class JsonNumber {
			#region utilities

			protected static void TestValues(SimpleValueTestBase<double> test) {
				// check argument
				Debug.Assert(test != null);

				// test values

				// integer
				test.TestJsonText(123, "123");
				// floating point
				test.TestJsonText(123.456, "123.456");
				// floating point (exponential notation)
				test.TestJsonText(1.23456e-12, "1.23456e-12");
			}

			public abstract class JsonNumberTestBase: WriteSimpleValueTestBase<double> {
				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			#endregion


			#region tests

			public class GetJsonNumber: SimpleValueTestBase<double> {
				#region overridables

				protected override string GetJsonText(double value) {
					return JsonUtil.GetJsonNumber(value);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			public class WriteJsonNumber: JsonNumberTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, double value) {
					JsonUtil.WriteJsonNumber(writer, value);
				}

				#endregion
			}

			public class LineFormatter: JsonNumberTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, double value) {
					JsonUtil.LineFormatter.WriteNumber(writer, value);
				}

				#endregion
			}

			public class CompactFormatter: JsonNumberTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, double value) {
					JsonUtil.CompactFormatter.WriteNumber(writer, value);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonString

		public class JsonString {
			#region utilities

			protected static void TestValues(SimpleValueTestBase<string?> test) {
				// check argument
				Debug.Assert(test != null);

				// test values
				foreach (StringSample sample in GetStringSamples()) {
					test.TestJsonText(sample.Value, sample.JsonText);
				}
			}

			public abstract class JsonStringTestBase: WriteSimpleValueTestBase<string?> {
				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			#endregion


			#region tests

			public class GetJsonString: SimpleValueTestBase<string?> {
				#region overridables

				protected override string GetJsonText(string? value) {
					return JsonUtil.GetJsonString(value);
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value")]
				public void value() {
					TestValues(this);
				}

				#endregion
			}

			public class WriteJsonString: JsonStringTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, string? value) {
					JsonUtil.WriteJsonString(writer, value);
				}

				#endregion
			}

			public class LineFormatter: JsonStringTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, string? value) {
					JsonUtil.LineFormatter.WriteString(writer, value);
				}

				#endregion
			}

			public class CompactFormatter: JsonStringTestBase {
				#region overridables

				protected override void WriteJsonValue(TextWriter writer, string? value) {
					JsonUtil.CompactFormatter.WriteString(writer, value);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonObject

		public class JsonObject {
			#region utilities

			public abstract class JsonObjectTestBase: ComplexValueTestBase {
				#region samples

				public Dictionary<string, object?> GetEmptySample() {
					return new Dictionary<string, object?>();
				}

				public Dictionary<string, object?> GetGeneralSample() {
					return new Dictionary<string, object?> {
						// null
						{ "null", null },
						// boolean
						{ "boolean", true },
						// number
						{ "number", 3 },
						// string
						{ "string", "a\"bc" },
						// object
						{ "object", new Dictionary<string, object?>() {
							{ "boolean", false },
							{ "number", 6.2 },
						} },
						// array
						{ "array", new object[] {
							true,
							3.4567e-10,
						} }
					};
				}

				#endregion


				#region utilities

				protected void TestValue<T>(T value, string expected) where T : IEnumerable<KeyValuePair<string, object?>> {
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// test formatter directly
					string? actual;
					using (StringWriter writer = new StringWriter()) {
						formatter.WriteObject(writer, value);
						actual = writer.ToString();
					}

					Assert.Equal(expected, actual);

					// test JsonUtil.WriteJsonObject()/GetJsonObject() with formatter parameter
					string? actual1;
					using (StringWriter writer = new StringWriter()) {
						JsonUtil.WriteJsonObject(writer, value, formatter);
						actual1 = writer.ToString();
					}

					string? actual2 = JsonUtil.GetJsonObject(value, formatter);

					Assert.Equal(expected, actual1);
					Assert.Equal(expected, actual2);

					// test JsonUtil.WriteJsonObject()/GetJsonObject() omitting formatter parameter
					if (formatter == JsonUtil.LineFormatter) {
						using (StringWriter writer = new StringWriter()) {
							JsonUtil.WriteJsonObject(writer, value);
							actual1 = writer.ToString();
						}

						actual2 = JsonUtil.GetJsonObject(value);

						Assert.Equal(expected, actual1);
						Assert.Equal(expected, actual2);
					}
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: null")]
				public void value_null() {
					// arrange
					// In principle, value is not nullable, but it should emit "null" if the value is null.
					Dictionary<string, object?> value = null!;
					string expected = "null";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "writer: null")]
				public void writer_null() {
					// arrange
					TextWriter writer = null!;
					Dictionary<string, object?> value = GetEmptySample();
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// act

					// test formatter.WriteObject()
					ArgumentNullException actual1 = Assert.Throws<ArgumentNullException>(() => {
						formatter.WriteObject(writer, value);
					});

					// test JsonUtil.WriteJsonObject()
					ArgumentNullException actual2 = Assert.Throws<ArgumentNullException>(() => {
						JsonUtil.WriteJsonObject(writer, value, formatter);
					});

					// assert
					Assert.Equal("writer", actual1.ParamName);
					Assert.Equal("writer", actual2.ParamName);
				}

				#endregion
			}

			#endregion


			#region tests

			public class LineFormatter: JsonObjectTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.LineFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: empty")]
				public void value_empty() {
					// arrange
					Dictionary<string, object?> value = GetEmptySample();
					string expected = "{ }";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: general")]
				public void value_general() {
					// arrange
					Dictionary<string, object?> value = GetGeneralSample();
					string expected = "{ \"null\": null, \"boolean\": true, \"number\": 3, \"string\": \"a\\\"bc\", \"object\": { \"boolean\": false, \"number\": 6.2 }, \"array\": [ true, 3.4567e-10 ] }";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			public class CompactFormatter: JsonObjectTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.CompactFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: empty")]
				public void value_empty() {
					// arrange
					Dictionary<string, object?> value = GetEmptySample();
					string expected = "{}";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: general")]
				public void value_general() {
					// arrange
					Dictionary<string, object?> value = GetGeneralSample();
					string expected = "{\"null\":null,\"boolean\":true,\"number\":3,\"string\":\"a\\\"bc\",\"object\":{\"boolean\":false,\"number\":6.2},\"array\":[true,3.4567e-10]}";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonArray

		public class JsonArray {
			#region utilities

			public abstract class JsonArrayTestBase: ComplexValueTestBase {
				#region samples

				public object?[] GetEmptySample() {
					return Array.Empty<object?>();
				}

				public object?[] GetGeneralSample() {
					return new object?[] {
						// null
						null,
						// boolean
						false,
						// number
						123.45,
						// string
						"abc",
						// object
						new Dictionary<string, object?>() {
							{ "boolean", false },
							{ "number", -67 },
						},
						// array
						new object[] {
							true,
							3.4567e-10,
							"123\\4",
						}
					};
				}

				#endregion


				#region utilities

				protected void TestValue<T>(T value, string expected) where T : IEnumerable<object?> {
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// test formatter directly
					string? actual;
					using (StringWriter writer = new StringWriter()) {
						formatter.WriteArray(writer, value);
						actual = writer.ToString();
					}

					Assert.Equal(expected, actual);

					// test JsonUtil.WriteJsonArray()/GetJsonArray() with formatter parameter
					string? actual1;
					using (StringWriter writer = new StringWriter()) {
						JsonUtil.WriteJsonArray(writer, value, formatter);
						actual1 = writer.ToString();
					}

					string? actual2 = JsonUtil.GetJsonArray(value, formatter);

					Assert.Equal(expected, actual1);
					Assert.Equal(expected, actual2);

					// test JsonUtil.WriteJsonArray()/GetJsonArray() omitting formatter parameter
					if (formatter == JsonUtil.LineFormatter) {
						using (StringWriter writer = new StringWriter()) {
							JsonUtil.WriteJsonArray(writer, value);
							actual1 = writer.ToString();
						}

						actual2 = JsonUtil.GetJsonArray(value);

						Assert.Equal(expected, actual1);
						Assert.Equal(expected, actual2);
					}
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: null")]
				public void value_null() {
					// arrange
					// In principle, value is not nullable, but it should emit "null" if the value is null.
					object?[] value = null!;
					string expected = "null";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "writer: null")]
				public void writer_null() {
					// arrange
					TextWriter writer = null!;
					object?[] value = GetEmptySample();
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// act

					// test formatter.WriteArray()
					ArgumentNullException actual1 = Assert.Throws<ArgumentNullException>(() => {
						formatter.WriteArray(writer, value);
					});

					// test JsonUtil.WriteJsonArray()
					ArgumentNullException actual2 = Assert.Throws<ArgumentNullException>(() => {
						JsonUtil.WriteJsonArray(writer, value, formatter);
					});

					// assert
					Assert.Equal("writer", actual1.ParamName);
					Assert.Equal("writer", actual2.ParamName);
				}

				#endregion
			}

			#endregion


			#region tests

			public class LineFormatter: JsonArrayTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.LineFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: empty")]
				public void value_empty() {
					// arrange
					object?[] value = GetEmptySample();
					string expected = "[ ]";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: general")]
				public void value_general() {
					// arrange
					object?[] value = GetGeneralSample();
					string expected = "[ null, false, 123.45, \"abc\", { \"boolean\": false, \"number\": -67 }, [ true, 3.4567e-10, \"123\\\\4\" ] ]";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			public class CompactFormatter: JsonArrayTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.CompactFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: empty")]
				public void value_empty() {
					// arrange
					object?[] value = GetEmptySample();
					string expected = "[]";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: general")]
				public void value_general() {
					// arrange
					object?[] value = GetGeneralSample();
					string expected = "[null,false,123.45,\"abc\",{\"boolean\":false,\"number\":-67},[true,3.4567e-10,\"123\\\\4\"]]";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region JsonValue

		public class JsonValue {
			#region utilities

			public abstract class JsonValueTestBase: ComplexValueTestBase {
				#region samples

				public static Dictionary<string, object?> GetObjectSample() {
					return new Dictionary<string, object?> {
						{ "prop1", 123 },
						{ "prop2", false },
					};
				}

				public static object?[] GetArraySample() {
					return new object?[] { true, "abc" };
				}

				#endregion


				#region utilities

				protected void TestValue<T>(T value, string expected) {
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// test formatter directly
					string? actual;
					using (StringWriter writer = new StringWriter()) {
						formatter.WriteValue<T>(writer, value);
						actual = writer.ToString();
					}

					Assert.Equal(expected, actual);

					// test JsonUtil.WriteJsonValue()/GetJsonValue() with formatter parameter
					string? actual1;
					using (StringWriter writer = new StringWriter()) {
						JsonUtil.WriteJsonValue<T>(writer, value, formatter);
						actual1 = writer.ToString();
					}

					string? actual2 = JsonUtil.GetJsonValue<T>(value, formatter);

					Assert.Equal(expected, actual1);
					Assert.Equal(expected, actual2);

					// test JsonUtil.WriteJsonValue()/GetJsonValue() omitting formatter parameter
					if (formatter == JsonUtil.LineFormatter) {
						using (StringWriter writer = new StringWriter()) {
							JsonUtil.WriteJsonValue<T>(writer, value);
							actual1 = writer.ToString();
						}

						actual2 = JsonUtil.GetJsonValue<T>(value);

						Assert.Equal(expected, actual1);
						Assert.Equal(expected, actual2);
					}
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: null")]
				public void value_null() {
					// arrange
					object? value = null;
					string expected = "null";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: true")]
				public void value_true() {
					// arrange
					bool value = true;
					string expected = "true";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: false")]
				public void value_false() {
					// arrange
					bool value = false;
					string expected = "false";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: double")]
				public void value_double() {
					// arrange
					double value = 1.234567e19;
					string expected = "1.234567e+19";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: float")]
				public void value_float() {
					// The value which implements IConvertible should be treated as a number.

					// arrange
					float value = -987.654f;
					string expected = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture).ToString();

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: int")]
				public void value_int() {
					// The value which implements IConvertible should be treated as a number.

					// arrange
					int value = -5;
					string expected = "-5";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: string")]
				public void value_string() {
					// arrange
					string value = "abcde";
					string expected = "\"abcde\"";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: string (includes special char)")]
				public void value_string_includes_special_char() {
					// arrange
					string value = "123\\";
					string expected = "\"123\\\\\"";    // should be escaped

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "writer: null")]
				public void writer_null() {
					// arrange
					TextWriter writer = null!;
					object? value = null;
					// This implementation assumes that the formatter is reusable
					JsonUtil.IJsonFormatter formatter = GetFormatter();

					// act

					// test formatter.WriteArray()
					ArgumentNullException actual1 = Assert.Throws<ArgumentNullException>(() => {
						formatter.WriteValue(writer, value);
					});

					// test JsonUtil.WriteJsonArray()
					ArgumentNullException actual2 = Assert.Throws<ArgumentNullException>(() => {
						JsonUtil.WriteJsonValue(writer, value, formatter);
					});

					// assert
					Assert.Equal("writer", actual1.ParamName);
					Assert.Equal("writer", actual2.ParamName);
				}

				#endregion
			}

			#endregion


			#region tests

			public class LineFormatter: JsonValueTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.LineFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: object")]
				public void value_object() {
					// arrange
					Dictionary<string, object?> value = GetObjectSample();
					string expected = "{ \"prop1\": 123, \"prop2\": false }";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: array")]
				public void value_array() {
					// arrange
					object?[] value = GetArraySample();
					string expected = "[ true, \"abc\" ]";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			public class CompactFormatter: JsonValueTestBase {
				#region overrides

				protected override JsonUtil.IJsonFormatter GetFormatter() {
					return JsonUtil.CompactFormatter;
				}

				#endregion


				#region tests

				[Fact(DisplayName = "value: object")]
				public void value_object() {
					// arrange
					Dictionary<string, object?> value = GetObjectSample();
					string expected = "{\"prop1\":123,\"prop2\":false}";

					// act & assert
					TestValue(value, expected);
				}

				[Fact(DisplayName = "value: array")]
				public void value_array() {
					// arrange
					object?[] value = GetArraySample();
					string expected = "[true,\"abc\"]";

					// act & assert
					TestValue(value, expected);
				}

				#endregion
			}

			#endregion
		}

		#endregion


		#region EscapeSpecialChar 

		public class EscapeSpecialChar {
			#region samples

			public class Sample {
				#region data

				public readonly char Char;

				public readonly string? Expected;

				#endregion


				#region constructor

				public Sample(char c, string? expected) {
					// initialize members
					this.Char = c;
					this.Expected = expected;
				}

				#endregion
			}

			public static IEnumerable<object[]> GetSamples() {
				return new Sample[] {
					//        (char, expected)
					new Sample('a', null),
					new Sample('"', "\\\""),
					new Sample('\\', "\\\\"),
					new Sample('\b', "\\b"),
					new Sample('\f', "\\f"),
					new Sample('\n', "\\n"),
					new Sample('\r', "\\r"),
					new Sample('\t', "\\t"),
					// only chars between U+0000 and U+001F should be escaped
					new Sample('\u0000', "\\u0000"),
					new Sample('\u001F', "\\u001F"),
					new Sample('\u0020', null),
				}.ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
			public void General(Sample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				string? actual = JsonUtil.EscapeSpecialChar(sample.Char);

				// assert
				Assert.Equal(sample.Expected, actual);
			}

			#endregion
		}

		#endregion


		#region ContainsSpecialChar 

		public class ContainsSpecialChar {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return GetStringSamples().ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
			public void General(StringSample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				bool actual = JsonUtil.ContainsSpecialChar(sample.Value);

				// assert
				Assert.Equal(sample.ContainsSpecialChar, actual);
			}

			#endregion
		}

		#endregion
	}
}
