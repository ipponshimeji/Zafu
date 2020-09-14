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
		#region types

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Some methods of this interface are defined as generic methods.
		/// For example, <c>WriteValue</c> method is not defined as <c>WriteValue(TextWriter, object?)</c> but <c>WriteValue<T>(TextWriter, T)</c>.
		/// It is designed to prevent boxing for value type value.
		/// Note that the first reason to provide this Json feature is for logging and
		/// SimpleState, which is frequently processed as log contents in this library, is a value type. 
		/// </remarks>
		public interface IJsonFormatter {
			void WriteNull(TextWriter writer);

			void WriteBoolean(TextWriter writer, bool value);

			void WriteNumber(TextWriter writer, double value);

			void WriteString(TextWriter writer, string? value);

			void WriteObject<T>(TextWriter writer, T value) where T : IEnumerable<KeyValuePair<string, object?>>;

			void WriteArray<T>(TextWriter writer, T value) where T : IEnumerable<object?>;

			void WriteValue<T>(TextWriter writer, T value);


			void WriteObjectStart(TextWriter writer);

			void WriteObjectEnd(TextWriter writer);

			void WriteObjectProperty<T>(TextWriter writer, KeyValuePair<string, T> prop, ref bool firstItem);

			void WriteArrayStart(TextWriter writer);

			void WriteArrayEnd(TextWriter writer);

			void WriteArrayItem<T>(TextWriter writer, T item, ref bool firstItem);
		}

		public class JsonFormatter: IJsonFormatter {
			public virtual void WriteNull(TextWriter writer) {
				JsonUtil.WriteJsonNull(writer);
			}

			public virtual void WriteBoolean(TextWriter writer, bool value) {
				JsonUtil.WriteJsonBoolean(writer, value);
			}

			public virtual void WriteNumber(TextWriter writer, double value) {
				JsonUtil.WriteJsonNumber(writer, value);
			}

			public virtual void WriteString(TextWriter writer, string? value) {
				JsonUtil.WriteJsonString(writer, value);
			}

			public virtual void WriteObject<T>(TextWriter writer, T value) where T : IEnumerable<KeyValuePair<string, object?>> {
				JsonUtil.WriteJsonObject<T>(writer, value, this);
			}

			public virtual void WriteArray<T>(TextWriter writer, T value) where T : IEnumerable<object?> {
				JsonUtil.WriteJsonArray<T>(writer, value, this);
			}

			public virtual void WriteValue<T>(TextWriter writer, T value) {
				JsonUtil.WriteJsonValue<T>(writer, value, this);
			}


			public virtual void WriteObjectStart(TextWriter writer) {
				writer.Write('{');
			}

			public virtual void WriteObjectEnd(TextWriter writer) {
				writer.Write('}');
			}

			public virtual void WriteObjectProperty<T>(TextWriter writer, KeyValuePair<string, T> prop, ref bool firstItem) {
				if (firstItem) {
					firstItem = false;
				} else {
					writer.Write(',');
				}
				WriteString(writer, prop.Key);
				writer.Write(':');
				WriteValue<T>(writer, prop.Value);
			}

			public virtual void WriteArrayStart(TextWriter writer) {
				writer.Write('[');
			}

			public virtual void WriteArrayEnd(TextWriter writer) {
				writer.Write(']');
			}

			public virtual void WriteArrayItem<T>(TextWriter writer, T item, ref bool firstItem) {
				if (firstItem) {
					firstItem = false;
				} else {
					writer.Write(',');
				}
				WriteValue<T>(writer, item);
			}
		}

		public class CompactJsonFormatter: JsonFormatter {
		}

		public class LineJsonFormatter: JsonFormatter {
			#region IJsonFormatter

			public override void WriteObjectEnd(TextWriter writer) {
				writer.Write(" }");
			}

			public override void WriteObjectProperty<T>(TextWriter writer, KeyValuePair<string, T> prop, ref bool firstItem) {
				// check argument
				if (writer == null) {
					throw new ArgumentNullException(nameof(writer));
				}

				// write an object property
				if (firstItem) {
					firstItem = false;
					writer.Write(' ');
				} else {
					writer.Write(", ");
				}
				WriteString(writer, prop.Key);
				writer.Write(": ");
				WriteValue<T>(writer, prop.Value);
			}

			public override void WriteArrayEnd(TextWriter writer) {
				writer.Write(" ]");
			}

			public override void WriteArrayItem<T>(TextWriter writer, T item, ref bool firstItem) {
				// check argument
				if (writer == null) {
					throw new ArgumentNullException(nameof(writer));
				}

				// write an array item
				if (firstItem) {
					firstItem = false;
					writer.Write(' ');
				} else {
					writer.Write(", ");
				}
				WriteValue<T>(writer, item);
			}

			#endregion
		}

		// TODO: IndentJsonFormatter
		// Unlike LineJsonFormatter or CompactJsonFormatter, IndentJsonFormatter will have state such as current indent level.
		// That means user cannot use shared instance but has to create IndentJsonFormatter instance.

		#endregion


		#region constants

		public const int DefaultIndentWidth = 2;

		public const string JsonNull = "null";

		public const string JsonTrue = "true";

		public const string JsonFalse = "false";

		#endregion


		#region data

		public static readonly CompactJsonFormatter CompactFormatter = new CompactJsonFormatter();

		public static readonly LineJsonFormatter LineFormatter = new LineJsonFormatter();

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
				formatter.WriteObjectProperty(writer, prop, ref firstItem);
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
