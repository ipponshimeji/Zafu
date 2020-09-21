using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Zafu.Text;

namespace Zafu.Logging {
	/// <summary>
	/// The structure to hold simple state for logging.
	/// </summary>
	/// <remarks>
	/// This structure is designed not to allocate object in text logging scenario,
	/// while it can provide <see cref="IReadOnlyDictionary{string, object?}"/> in structured logging scenario.
	/// </remarks>
	public struct SimpleState<T>: IReadOnlyDictionary<string, object?>, IFormattable {
		#region data

		// Note that source, message, and extraPropName are not nullable in its use,
		// but it is unavoidable to be null in the instance of default(SimpleState).
		// So they are adjusted in their public getter (Source and Message properties).

		private readonly string? source;

		private readonly string? message;

		private readonly string? extraPropName;

		private readonly T extraPropValue;

		#endregion


		#region properties

		public string Source => this.source ?? string.Empty;

		public string Message => this.message ?? string.Empty;

		public string ExtraPropertyName => this.extraPropName ?? string.Empty;

		public T ExtraPropertyValue => this.extraPropValue;

		#endregion


		#region creation

		public SimpleState(string? source, string? message, string? extraPropName, T extraPropValue) {
			// check arguments
			if (source == null) {
				source = string.Empty;
			}
			if (message == null) {
				message = string.Empty;
			}
			if (extraPropName == null) {
				extraPropName = string.Empty;
			}

			// initialize members
			this.message = message;
			this.source = source;
			this.extraPropName = extraPropName;
			this.extraPropValue = extraPropValue;
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion


		#region IEnumerable<KeyValuePair<string, object?>>

		public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() {
			yield return new KeyValuePair<string, object?>(SimpleState.SourcePropertyName, this.Source);
			yield return new KeyValuePair<string, object?>(this.ExtraPropertyName, this.ExtraPropertyValue);
			yield return new KeyValuePair<string, object?>(SimpleState.MessagePropertyName, this.Message);
		}

		#endregion


		#region IReadOnlyCollection<KeyValuePair<string, object?>>

		public int Count {
			get {
				return 3;
			}
		}

		#endregion


		#region IReadOnlyDictionary<string, object?>

		public object? this[string key] {
			get {
				object? value;
				if (TryGetValue(key, out value)) {
					return value;
				} else {
					throw new KeyNotFoundException();
				}
			}
		}

		public IEnumerable<string> Keys {
			get {
				yield return SimpleState.SourcePropertyName;
				yield return this.ExtraPropertyName;
				yield return SimpleState.MessagePropertyName;
			}
		}

		public IEnumerable<object?> Values {
			get{
				yield return this.Source;
				yield return this.ExtraPropertyValue;
				yield return this.Message;
			}
		}

		public bool ContainsKey(string key) {
			object? dummy;
			return TryGetValue(key, out dummy);
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value) {
			// try to get value
			value = key switch {
				SimpleState.SourcePropertyName => this.Source,
				SimpleState.MessagePropertyName => this.Message,
				_ => (key == this.ExtraPropertyName)? (object?)this.ExtraPropertyValue: null,
			};

			return value != null;
		}

		#endregion


		#region IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider) {
			// check argument
			if (string.IsNullOrEmpty(format)) {
				format = "G";
			}

			switch (format.ToUpperInvariant()) {
				case "G":
				case "g":
					return ToString();
				case "J":
				case "j":
					return ToJson();
				default:
					throw new FormatException($"The {format} format string is not supported.");
			}
		}

		#endregion


		#region methods

		public string ToJson(IJsonFormatter? formatter = null) {
			// check argument
			if (formatter == null) {
				formatter = JsonUtil.LineFormatter;
			}

			// write the state as Json text
			bool firstItem = true;
			using (StringWriter writer = new StringWriter()) {
				formatter.WriteObjectStart(writer);
				formatter.WriteStringObjectProperty(writer, SimpleState.SourcePropertyName, this.Source, ref firstItem);
				formatter.WriteObjectProperty<T>(writer, this.ExtraPropertyName, this.ExtraPropertyValue, ref firstItem);
				formatter.WriteStringObjectProperty(writer, SimpleState.MessagePropertyName, this.Message, ref firstItem);
				formatter.WriteObjectEnd(writer);

				return writer.ToString();
			}
		}

		#endregion


		#region overrides

		public override string ToString() {
			static void writePropHeader(TextWriter w, string name, bool firstItem) {
				if (firstItem == false) {
					w.Write(", ");
				}
				w.Write(name);
				w.Write(": ");
			}

			using (StringWriter writer = new StringWriter()) {
				IJsonFormatter formatter = JsonUtil.LineFormatter;
				writePropHeader(writer, SimpleState.SourcePropertyName, true);
				formatter.WriteString(writer, this.Source);
				writePropHeader(writer, this.ExtraPropertyName, false);
				formatter.WriteValue<T>(writer, this.ExtraPropertyValue);
				writePropHeader(writer, SimpleState.MessagePropertyName, false);
				formatter.WriteString(writer, this.Message);

				return writer.ToString();
			}
		}

		public override bool Equals(object? obj) {
			if (obj is SimpleState<T>) {
				SimpleState<T> that = (SimpleState<T>)obj;
				return (
					this.Source == that.Source &&
					this.Message == that.Message &&
					this.ExtraPropertyName == that.ExtraPropertyName &&
					object.Equals(this.ExtraPropertyValue, that.ExtraPropertyValue)
				);
			} else {
				return false;
			}
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.Source, this.Message, this.ExtraPropertyName, this.ExtraPropertyValue);
		}

		#endregion
	}
}
