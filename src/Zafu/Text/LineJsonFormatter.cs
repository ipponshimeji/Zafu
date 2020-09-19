using System;
using System.Diagnostics;
using System.IO;

namespace Zafu.Text {
	public class LineJsonFormatter: JsonFormatter {
		#region data

		public static readonly LineJsonFormatter Instance = new LineJsonFormatter();

		#endregion


		#region IJsonFormatter

		public override void WriteObjectEnd(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(" }");
		}

		public override void WriteObjectPropertyHeader(TextWriter writer, string name, ref bool firstItem) {
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
				writer.Write(' ');
			} else {
				writer.Write(", ");
			}
			WriteString(writer, name);
			writer.Write(": ");
		}

		public override void WriteArrayEnd(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(" ]");
		}

		public override void WriteArrayItemHeader(TextWriter writer, ref bool firstItem) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			// write an array item header
			if (firstItem) {
				firstItem = false;
				writer.Write(' ');
			} else {
				writer.Write(", ");
			}
		}

		#endregion
	}
}
