using System;
using System.Collections.Generic;
using System.IO;

namespace Zafu.Text {
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// Some methods of this interface are defined as generic methods.
	/// For example, <c>WriteValue</c> method is not defined as <c>WriteValue(TextWriter, object?)</c> but <c>WriteValue&lt;T&gt;(TextWriter, T)</c>.
	/// This design is to prevent value type values from boxing.
	/// Note that the first reason to provide Json feature of Zafu core is for logging.
	/// And SimpleState, which is frequently processed as log contents in this library, is a value type. 
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

		void WriteObjectPropertyHeader(TextWriter writer, string name, ref bool firstItem);

		void WriteArrayStart(TextWriter writer);

		void WriteArrayEnd(TextWriter writer);

		void WriteArrayItemHeader(TextWriter writer, ref bool firstItem);


		void WriteNullObjectProperty(TextWriter writer, string name, ref bool firstItem) {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteNull(writer);
		}

		void WriteBooleanObjectProperty(TextWriter writer, string name, bool value, ref bool firstItem) {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteBoolean(writer, value);
		}

		void WriteNumberObjectProperty(TextWriter writer, string name, double value, ref bool firstItem) {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteNumber(writer, value);
		}

		void WriteStringObjectProperty(TextWriter writer, string name, string? value, ref bool firstItem) {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteString(writer, value);
		}

		void WriteObjectObjectProperty<T>(TextWriter writer, string name, T value, ref bool firstItem) where T : IEnumerable<KeyValuePair<string, object?>> {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteObject(writer, value);
		}

		void WriteArrayObjectProperty<T>(TextWriter writer, string name, T value, ref bool firstItem) where T : IEnumerable<object?> {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteArray(writer, value);
		}

		void WriteObjectProperty<T>(TextWriter writer, string name, T value, ref bool firstItem) {
			WriteObjectPropertyHeader(writer, name, ref firstItem);
			WriteValue<T>(writer, value);
		}

		void WriteNullArrayItem(TextWriter writer, ref bool firstItem) {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteNull(writer);
		}

		void WriteBooleanArrayItem(TextWriter writer, bool item, ref bool firstItem) {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteBoolean(writer, item);
		}

		void WriteNumberArrayItem(TextWriter writer, double item, ref bool firstItem) {
			// check arguments
			WriteArrayItemHeader(writer, ref firstItem);
			WriteNumber(writer, item);
		}

		void WriteStringArrayItem(TextWriter writer, string? item, ref bool firstItem) {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteString(writer, item);
		}

		void WriteObjectArrayItem<T>(TextWriter writer, T item, ref bool firstItem) where T : IEnumerable<KeyValuePair<string, object?>> {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteObject(writer, item);
		}

		void WriteArrayArrayItem<T>(TextWriter writer, T item, ref bool firstItem) where T : IEnumerable<object?> {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteArray(writer, item);
		}

		void WriteArrayItem<T>(TextWriter writer, T item, ref bool firstItem) {
			WriteArrayItemHeader(writer, ref firstItem);
			WriteValue<T>(writer, item);
		}
	}
}
