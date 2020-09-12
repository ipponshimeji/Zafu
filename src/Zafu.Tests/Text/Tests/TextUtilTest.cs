using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Zafu.Testing;

namespace Zafu.Text.Tests {
	public class TextUtilTest {
		#region GetWrittenString

		public class GetWrittenString {
			#region tests

			[Fact(DisplayName = "general")]
			public void General() {
				// arrange
				static void writeTo1(TextWriter writer) {
					writer.Write("abc");
					writer.Write(123);
					writer.Write("+-*");
				}
				static void writeTo2(TextWriter writer, string? value) {
					writer.Write(value);
				}
				string value = "abc";

				// act
				// overload: (Action<TextWriter>)
				string actual1 = TextUtil.GetWrittenString(writeTo1);
				// overload: (Action<TextWriter, T>, T)
				string actual2 = TextUtil.GetWrittenString(writeTo2, value);

				// assert
				Assert.Equal("abc123+-*", actual1);
				Assert.Equal("abc", actual2);
			}

			[Fact(DisplayName = "writeTo: null")]
			public void writeTo_null() {
				// arrange
				Action<TextWriter> writeTo1 = null!;
				Action<TextWriter, string> writeTo2 = null!;
				string value = "abc";

				// act
				// overload: (Action<TextWriter>)
				ArgumentNullException actual1 = Assert.Throws<ArgumentNullException>(() => {
					TextUtil.GetWrittenString(writeTo1);
				});
				// overload: (Action<TextWriter, T>, T)
				ArgumentNullException actual2 = Assert.Throws<ArgumentNullException>(() => {
					TextUtil.GetWrittenString(writeTo2, value);
				});

				// assert
				Assert.Equal("writeTo", actual1.ParamName);
				Assert.Equal("writeTo", actual2.ParamName);
			}

			[Fact(DisplayName = "writeTo: throws an exception")]
			public void writeTo_exception() {
				// arrange
				InvalidOperationException exception = new InvalidOperationException();
				void writeTo1(TextWriter writer) {
					throw exception;
				}
				void writeTo2(TextWriter writer, string value) {
					throw exception;
				}
				string value = "abc";

				// act
				// overload: (Action<TextWriter>)
				InvalidOperationException actual1 = Assert.Throws<InvalidOperationException>(() => {
					TextUtil.GetWrittenString(writeTo1);
				});
				// overload: (Action<TextWriter, T>, T)
				InvalidOperationException actual2 = Assert.Throws<InvalidOperationException>(() => {
					TextUtil.GetWrittenString(writeTo2, value);
				});

				// assert
				Assert.Equal(exception, actual1);
				Assert.Equal(exception, actual2);
			}

			#endregion
		}

		#endregion


		#region WriteEscapedString

		public class WriteEscapedString {
			#region samples

			public class Sample {
				#region data

				public readonly string Value;

				public readonly string Escaped;

				#endregion


				#region constructor

				public Sample(string value, string escaped) {
					// initialize members
					this.Value = value;
					this.Escaped = escaped;
				}

				#endregion
			}

			public static IEnumerable<object[]> GetSamples() {
				return new Sample[] {
					//  Sample(value, escaped)
					// no special char
					new Sample("abc123#-$-", "abc123#-$-"),
					// single special char
					new Sample("<", "&lt;"),
					new Sample("\"abc", "&quot;abc"),
					new Sample("abc>123", "abc&gt;123"),
					new Sample("123&", "123&amp;"),
					// multiple special chars
					new Sample("<>", "&lt;&gt;"),
					new Sample("\"abc&", "&quot;abc&amp;"),
					new Sample("123<abc>+-*", "123&lt;abc&gt;+-*"),
					// empty
					new Sample("", "")
				}.ToTestData();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "general")]
			[MemberData(nameof(GetSamples))]
			public void General(Sample sample) {
				// arrange
				Debug.Assert(sample != null);
				static string? escape(char c) {
					return c switch {
						'<' => "&lt;",
						'>' => "&gt;",
						'"' => "&quot;",
						'&' => "&amp;",
						_ => null
					};
				}

				// act
				string? actual;
				using (StringWriter writer = new StringWriter()) {
					TextUtil.WriteEscapedString(writer, sample.Value, escape);
					actual = writer.ToString();
				}

				// assert
				Assert.Equal(sample.Escaped, actual);
			}

			[Fact(DisplayName = "writer: null")]
			public void writer_null() {
				// arrange
				TextWriter writer = null!;
				string str = "abc";
				static string? escape(char c) {
					return null;
				}

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					TextUtil.WriteEscapedString(writer, str, escape);
				});

				// assert
				Assert.Equal("writer", actual.ParamName);
			}

			[Fact(DisplayName = "str: null")]
			public void str_null() {
				// arrange
				TextWriter writer = TextWriter.Null;
				string str = null!;
				static string? escape(char c) {
					return null;
				}

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					TextUtil.WriteEscapedString(writer, str, escape);
				});

				// assert
				Assert.Equal("str", actual.ParamName);
			}

			[Fact(DisplayName = "escape: null")]
			public void escape_null() {
				// arrange
				TextWriter writer = TextWriter.Null;
				string str = "abc";
				Func<char, string?> escape = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					TextUtil.WriteEscapedString(writer, str, escape);
				});

				// assert
				Assert.Equal("escape", actual.ParamName);
			}

			#endregion
		}

		#endregion
	}
}
