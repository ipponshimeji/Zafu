using System;
using System.IO;

namespace Zafu.Text {
	/// <summary>
	/// The class which provides some helper methods to write JSON text.
	/// </summary>
	/// <remarks>
	/// It may be better to use an appropriate JSON support library to generate JSON text,
	/// but I don't want Zafu Core library to rely on specific JSON support library.
	/// So simple implementations are provided here.
	/// </remarks>
	public static class JsonUtil {
		#region constants

		public const string JsonNull = "null";

		#endregion


		#region methods

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
				writer.Write(JsonNull);
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

		public static string GetJsonString(string? value) {
			// check arguments
			// value can be null

			return TextUtil.GetWrittenString(WriteJsonString, value);
		}

		#endregion
	}
}
