using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Zafu.Text {
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
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write('{');
		}

		public virtual void WriteObjectEnd(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write('}');
		}

		public virtual void WriteObjectPropertyHeader(TextWriter writer, string name, ref bool firstItem) {
			// check arguments
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}
			if (name == null) {
				throw new ArgumentNullException(nameof(name));
			}

			// write an object property header
			if (firstItem) {
				firstItem = false;
			} else {
				writer.Write(',');
			}
			WriteString(writer, name);
			writer.Write(':');
		}

		public virtual void WriteArrayStart(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write('[');
		}

		public virtual void WriteArrayEnd(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(']');
		}

		public virtual void WriteArrayItemHeader(TextWriter writer, ref bool firstItem) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			// write an array item header
			if (firstItem) {
				firstItem = false;
			} else {
				writer.Write(',');
			}
		}
	}
}
