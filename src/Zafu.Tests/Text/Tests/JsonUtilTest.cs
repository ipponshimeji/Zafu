using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;
using Zafu.Testing;

namespace Zafu.Text.Tests {
	public class JsonUtilTest {
		#region Constants

		public class Constants {
			#region tests

			[Fact(DisplayName = "constant values")]
			public void TestValues() {
				// assert
				Assert.Equal("null", JsonUtil.JsonNull);
				Assert.Equal("false", JsonUtil.JsonFalse);
				Assert.Equal("true", JsonUtil.JsonTrue);
			}

			#endregion
		}

		#endregion


		#region WriteJsonX

		public class WriteJsonX: JsonTestBase {
			#region tests

			[Fact(DisplayName = "WriteJsonNull; general")]
			public void WriteJsonNull_general() {
				// arrange
				string expected = JsonUtil.JsonNull;

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonNull(writer);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "WriteJsonNull; writer: null")]
			public void WriteJsonNull_writer_null() {
				// arrange
				TextWriter writer = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonNull(writer);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Theory(DisplayName = "WriteJsonBoolean; general")]
			[MemberData(nameof(GetBooleanValueSampleData))]
			public void WriteJsonBoolean_general(ValueSample<bool> sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonBoolean(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Fact(DisplayName = "WriteJsonBoolean; writer: null")]
			public void WriteJsonBoolean_writer_null() {
				// arrange
				TextWriter writer = null!;
				bool value = default(bool);

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonBoolean(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Theory(DisplayName = "WriteJsonNumber; general")]
			[MemberData(nameof(GetNumberValueSampleData))]
			public void WriteJsonNumber_general(ValueSample<double> sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonNumber(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Fact(DisplayName = "WriteJsonNumber; writer: null")]
			public void WriteJsonNumber_writer_null() {
				// arrange
				TextWriter writer = null!;
				double value = default(double);

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonNumber(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Theory(DisplayName = "WriteJsonString; general")]
			[MemberData(nameof(GetStringValueSampleData))]
			public void WriteJsonString_general(StringValueSample sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonString(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Fact(DisplayName = "WriteJsonString; writer: null")]
			public void WriteJsonString_writer_null() {
				// arrange
				TextWriter writer = null!;
				string value = string.Empty;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonString(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Theory(DisplayName = "WriteJsonObject; general")]
			[MemberData(nameof(GetObjectValueSampleData))]
			public void WriteJsonObject_general(SampleObject sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonObject(writer, sample);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "WriteJsonObject; writer: null")]
			public void WriteJsonObject_writer_null() {
				// arrange
				TextWriter writer = null!;
				SampleObject value = EmptyObjectValueSample;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonObject(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Theory(DisplayName = "WriteJsonArray; general")]
			[MemberData(nameof(GetArrayValueSampleData))]
			public void WriteJsonArray_general(SampleArray sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonArray(writer, sample);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "WriteJsonArray; writer: null")]
			public void WriteJsonArray_writer_null() {
				// arrange
				TextWriter writer = null!;
				SampleArray value = EmptyArrayValueSample;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonArray(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Fact(DisplayName = "WriteJsonValue; value: null")]
			public void WriteJsonValue_value_null() {
				// arrange
				object? value = null;
				string expected = JsonUtil.JsonNull;

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Theory(DisplayName = "WriteJsonValue; value: bool")]
			[MemberData(nameof(GetBooleanValueSampleData))]
			public void WriteJsonValue_value_bool(ValueSample<bool> sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "WriteJsonValue; value: double")]
			[MemberData(nameof(GetNumberValueSampleData))]
			public void WriteJsonValue_value_double(ValueSample<double> sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "WriteJsonValue; value: string")]
			[MemberData(nameof(GetStringValueSampleData))]
			public void WriteJsonValue_value_string(StringValueSample sample) {
				// arrange

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, sample.Value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "WriteJsonValue; value: object")]
			[MemberData(nameof(GetObjectValueSampleData))]
			public void WriteJsonValue_value_object(SampleObject sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, sample);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Theory(DisplayName = "WriteJsonValue; value: array")]
			[MemberData(nameof(GetArrayValueSampleData))]
			public void WriteJsonValue_value_array(SampleArray sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, sample);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "WriteJsonValue; value: IConvertible")]
			public void WriteJsonValue_value_IConvertible() {
				// arrange
				Decimal value = -567.89m;
#pragma warning disable CS0183
				Debug.Assert(value is IConvertible);
#pragma warning restore CS0183
				string expected = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture).ToString();

				// act
				string actual;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonValue(writer, value);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "WriteJsonValue; writer: null")]
			public void WriteJsonValue_writer_null() {
				// arrange
				TextWriter writer = null!;
				bool value = true;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonValue(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			#endregion
		}

		#endregion


		#region GetJsonX

		public class GetJsonX: JsonTestBase {
			#region tests

			[Theory(DisplayName = "GetJsonBoolean; general")]
			[MemberData(nameof(GetBooleanValueSampleData))]
			public void GetJsonBoolean_general(ValueSample<bool> sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonBoolean(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonNumber; general")]
			[MemberData(nameof(GetNumberValueSampleData))]
			public void GetJsonNumber_general(ValueSample<double> sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonNumber(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonString; general")]
			[MemberData(nameof(GetStringValueSampleData))]
			public void GetJsonString_general(StringValueSample sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonString(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonObject; general")]
			[MemberData(nameof(GetObjectValueSampleData))]
			public void GetJsonObject_general(SampleObject sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual = JsonUtil.GetJsonObject(sample);

				// assert
				Assert.Equal(expected, actual);
			}

			[Theory(DisplayName = "GetJsonArray; general")]
			[MemberData(nameof(GetArrayValueSampleData))]
			public void GetJsonArray_general(SampleArray sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual = JsonUtil.GetJsonArray(sample);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "GetJsonValue; value: null")]
			public void GetJsonValue_value_null() {
				// arrange
				object? value = null;
				string expected = JsonUtil.JsonNull;

				// act
				string actual = JsonUtil.GetJsonValue(value);

				// assert
				Assert.Equal(expected, actual);
			}

			[Theory(DisplayName = "GetJsonValue; value: bool")]
			[MemberData(nameof(GetBooleanValueSampleData))]
			public void GetJsonValue_value_bool(ValueSample<bool> sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonValue(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonValue; value: double")]
			[MemberData(nameof(GetNumberValueSampleData))]
			public void GetJsonValue_value_double(ValueSample<double> sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonValue(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonValue; value: string")]
			[MemberData(nameof(GetStringValueSampleData))]
			public void GetJsonValue_value_string(StringValueSample sample) {
				// arrange

				// act
				string actual = JsonUtil.GetJsonValue(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual);
			}

			[Theory(DisplayName = "GetJsonValue; value: object")]
			[MemberData(nameof(GetObjectValueSampleData))]
			public void GetJsonValue_value_object(SampleObject sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual = JsonUtil.GetJsonValue(sample);

				// assert
				Assert.Equal(expected, actual);
			}

			[Theory(DisplayName = "GetJsonValue; value: array")]
			[MemberData(nameof(GetArrayValueSampleData))]
			public void GetJsonValue_value_array(SampleArray sample) {
				// arrange
				string expected = new LineJsonFormatterTest().GetExpectedJsonText(sample);

				// act
				string actual = JsonUtil.GetJsonValue(sample);

				// assert
				Assert.Equal(expected, actual);
			}

			[Fact(DisplayName = "GetJsonValue; value: IConvertible")]
			public void GetJsonValue_value_IConvertible() {
				// arrange
				Decimal value = -567.89m;
#pragma warning disable CS0183
				Debug.Assert(value is IConvertible);
#pragma warning restore CS0183
				string expected = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture).ToString();

				// act
				string actual = JsonUtil.GetJsonValue(value);

				// assert
				Assert.Equal(expected, actual);
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

		public class ContainsSpecialChar: JsonTestBase {
			#region samples

			public static IEnumerable<object[]> GetSampleData() {
				return GetStringValueSampleData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSampleData))]
			public void General(StringValueSample sample) {
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
