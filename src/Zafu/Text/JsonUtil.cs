using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Zafu.Text {
	/// <summary>
	/// The class which provides some helper methods to write JSON text.
	/// </summary>
	/// <remarks>
	/// It may be better to use an appropriate JSON support library to generate JSON text,
	/// but I don't want Zafu Core library to rely on specific JSON support library.
	/// So some simple implementations are provided here.
	/// </remarks>
	public static class JsonUtil {
		#region constants

		public const string JsonNull = "null";

		public const string JsonTrue = "true";

		public const string JsonFalse = "false";

		#endregion


		#region data

		public static readonly CompactJsonFormatter CompactFormatter = CompactJsonFormatter.Instance;

		public static readonly LineJsonFormatter LineFormatter = LineJsonFormatter.Instance;

		// TODO: IndentJsonFormatter
		// Unlike LineJsonFormatter or CompactJsonFormatter, IndentJsonFormatter will have state such as current indent level.
		// That means user cannot use the shared instance but has to create IndentJsonFormatter instance.

		#endregion


		#region methods

		public static void WriteJsonNull(TextWriter writer) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(JsonNull);
		}

		public static string GetJsonBoolean(bool value) {
			return value ? JsonTrue : JsonFalse;
		}

		public static void WriteJsonBoolean(TextWriter writer, bool value) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(GetJsonBoolean(value));
		}

		public static string GetJsonNumber(double value) {
			return value.ToString("g", CultureInfo.InvariantCulture);
		}

		public static void WriteJsonNumber(TextWriter writer, double value) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(GetJsonNumber(value));
		}

		public static string GetJsonString(string? value) {
			// check arguments
			// value can be null

			return TextUtil.GetWrittenString(WriteJsonString, value);
		}

		/// <summary>
		/// Writes a string in JSON string representation.
		/// That is, the string is quoted by double quotation mark (") and special characters
		/// such as double quotation mark or backslash are escaped.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void WriteJsonString(TextWriter writer, string? value) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (value == null) {
				WriteJsonNull(writer);
				return;
			}

			// scan the string and escape special characters
			const char quotationMark = '"';
			int strLen = value.Length;
			writer.Write(quotationMark);
			int baseIndex = 0;
			for (int i = 0; i < strLen; ++i) {
				char c = value[i];
				string? escaped = EscapeSpecialChar(c);
				if (escaped != null) {
					if (baseIndex < i) {
						writer.Write(value.AsSpan(baseIndex, i - baseIndex));
					}
					writer.Write(escaped);
					baseIndex = i + 1;
				}
			}
			if (baseIndex < strLen) {
				writer.Write(value.AsSpan(baseIndex, strLen - baseIndex));
			}
			writer.Write(quotationMark);
		}

		public static string GetJsonObject<T>(T value, IJsonFormatter? formatter = null) where T : IEnumerable<KeyValuePair<string, object?>> {
			return TextUtil.GetWrittenString(writer => WriteJsonObject<T>(writer, value, formatter));
		}

		public static void WriteJsonObject<T>(TextWriter writer, T value, IJsonFormatter? formatter = null) where T : IEnumerable<KeyValuePair<string, object?>> {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (formatter == null) {
				formatter = LineFormatter;
			}
			if (value is null) {
				formatter.WriteNull(writer);
				return;
			}

			// write JSON object
			formatter.WriteObjectStart(writer);
			bool firstItem = true;
			foreach (KeyValuePair<string, object?> prop in value) {
				formatter.WriteObjectProperty(writer, prop.Key, prop.Value, ref firstItem);
			}
			formatter.WriteObjectEnd(writer);
		}

		public static string GetJsonArray<T>(T value, IJsonFormatter? formatter = null) where T : IEnumerable<object?> {
			return TextUtil.GetWrittenString(writer => WriteJsonArray<T>(writer, value, formatter));
		}

		public static void WriteJsonArray<T>(TextWriter writer, T value, IJsonFormatter? formatter = null) where T : IEnumerable<object?> {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (formatter == null) {
				formatter = LineFormatter;
			}
			if (value is null) {
				formatter.WriteNull(writer);
				return;
			}

			// write JSON array
			formatter.WriteArrayStart(writer);
			bool firstItem = true;
			foreach (object? item in value) {
				formatter.WriteArrayItem(writer, item, ref firstItem);
			}
			formatter.WriteArrayEnd(writer);
		}


		public static string GetJsonValue<T>(T value, IJsonFormatter? formatter = null) {
			return TextUtil.GetWrittenString(writer => WriteJsonValue<T>(writer, value, formatter));
		}

		public static void WriteJsonValue<T>(TextWriter writer, T value, IJsonFormatter? formatter = null) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (formatter == null) {
				formatter = LineFormatter;
			}

			switch (value) {
				case null:
					formatter.WriteNull(writer);
					break;
				case string s:
					formatter.WriteString(writer, s);
					break;
				case bool b:
					formatter.WriteBoolean(writer, b);
					break;
				case double d:
					formatter.WriteNumber(writer, d);
					break;
				case IEnumerable<KeyValuePair<string, object?>> o:
					formatter.WriteObject(writer, o);
					break;
				case IEnumerable<object?> a:
					formatter.WriteArray(writer, a);
					break;
				case IConvertible c:
					formatter.WriteNumber(writer, c.ToDouble(CultureInfo.InvariantCulture));
					break;
				default:
					throw new ArgumentException("Not supported as a JSON value.", nameof(value));
			}
		}


		public static string? EscapeSpecialChar(char c) {
			return c switch {
				'"' => "\\\"",
				'\\' => "\\\\",
				'\b' => "\\b",
				'\f' => "\\f",
				'\n' => "\\n",
				'\r' => "\\r",
				'\t' => "\\t",
				_ => (c <= 0x1F) ? string.Format("\\u{0:X4}", (int)c) : null
			};
		}

		public static bool ContainsSpecialChar(string? value) {
			// check argument
			if (value == null) {
				return false;
			}

			// check each character
			foreach (char c in value) {
				if (EscapeSpecialChar(c) != null) {
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}
