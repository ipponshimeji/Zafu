using System;
using System.IO;

namespace Zafu.Text {
	public static class TextUtil {
		#region methods

		public static string GetWrittenString(Action<TextWriter> writeTo) {
			// check argument
			if (writeTo == null) {
				throw new ArgumentNullException(nameof(writeTo));
			}

			// return written string
			using (StringWriter writer = new StringWriter()) {
				writeTo(writer);
				return writer.ToString();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="writeTo"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// This is an optimized version of <see cref="GetWrittenString(Action{TextWriter})"/>.
		/// In some cases such as a static method can be specified as the <paramref name="writeTo"/> argument,
		/// allocation of lambda function can be omitted.
		/// </remarks>
		public static string GetWrittenString<T>(Action<TextWriter, T> writeTo, T value) {
			// check argument
			if (writeTo == null) {
				throw new ArgumentNullException(nameof(writeTo));
			}

			// return written string
			using (StringWriter writer = new StringWriter()) {
				writeTo(writer, value);
				return writer.ToString();
			}
		}

		public static void WriteEscapedString(TextWriter writer, string str, Func<char, string?> escape) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (str == null) {
				throw new ArgumentNullException(nameof(str));
			}
			if (escape == null) {
				throw new ArgumentNullException(nameof(escape));
			}
			int strLen = str.Length;
			if (strLen == 0) {
				return;
			}

			// scan the string and escape special characters
			int baseIndex = 0;
			for (int i = 0; i < strLen; ++i) {
				char c = str[i];
				string? escaped = escape(c);
				if (escaped != null) {
					if (baseIndex < i) {
						writer.Write(str.AsSpan(baseIndex, i - baseIndex));
					}
					writer.Write(escaped);
					baseIndex = i + 1;
				}
			}
			if (baseIndex < strLen) {
				writer.Write(str.AsSpan(baseIndex, strLen - baseIndex));
			}
		}

		#endregion
	}
}
