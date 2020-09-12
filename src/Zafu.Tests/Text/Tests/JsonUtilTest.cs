using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Zafu.Testing;

namespace Zafu.Text.Tests {
	public class JsonUtilTest {
		#region samples

		public class StringSample {
			#region data

			public readonly string? Value;

			public readonly bool ContainsSpecialChar;

			public readonly string? JsonText;

			#endregion


			#region constructor

			public StringSample(string? value, bool containsSpecialChar, string? jsonText) {
				// initialize members
				this.Value = value;
				this.ContainsSpecialChar = containsSpecialChar;
				this.JsonText = jsonText;
			}

			#endregion
		}

		public static IEnumerable<object[]> GetStringSamples() {
			return new StringSample[] {
				//              (value, containsSpecialChar, jsonText)
				// no special char
				new StringSample("abc123#-$-", false, "\"abc123#-$-\""),
				// single special char
				new StringSample("\"", true, "\"\\\"\""),
				new StringSample("\\abc", true, "\"\\\\abc\""),
				new StringSample("abc\n123", true, "\"abc\\n123\""),
				new StringSample("123\u0000", true, "\"123\\u0000\""),
				// multiple special chars
				new StringSample("\u0001\\", true, "\"\\u0001\\\\\""),
				new StringSample("\babc\f", true, "\"\\babc\\f\""),
				new StringSample("123\"\"abc\t+-#", true, "\"123\\\"\\\"abc\\t+-#\""),
				// null
				new StringSample(null, false, "null")
			}.ToTestData();
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
				return GetStringSamples();
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


		#region WriteJsonString/GetJsonString

		public class JsonString {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return GetStringSamples();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
			public void General(StringSample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				string? actual1;
				using (StringWriter writer = new StringWriter()) {
					JsonUtil.WriteJsonString(writer, sample.Value);
					actual1 = writer.ToString();
				}

				string? actual2 = JsonUtil.GetJsonString(sample.Value);

				// assert
				Assert.Equal(sample.JsonText, actual1);
				Assert.Equal(sample.JsonText, actual2);
			}

			[Fact(DisplayName = "writer: null")]
			public void writer_null() {
				// arrange
				TextWriter writer = null!;
				string value = "abc";

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					JsonUtil.WriteJsonString(writer, value);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			#endregion
		}

		#endregion
	}
}
