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
	/// while it can provide <see cref="IReadOnlyDictionary{string, object}"/> in structured logging scenario.
	/// </remarks>
	public struct SimpleState: IReadOnlyDictionary<string, object>, IFormattable {
		#region constants

		// Property names
		// Property names should not contain special characters to be escaped.
		// The property names are embedded directly in the JSON text for efficiency.

		public const string MessagePropertyName = "message";

		public const string SourcePropertyName = "source";

		#endregion


		#region data

		// Note that the following members are not nullable in its use,
		// but it is unavoidable to be null in the instance of default(SimpleState).
		// So they are adjusted in their public getter (Source and Message properties).

		private readonly string? source;

		private readonly string? message;

		#endregion


		#region properties

		public string Source => this.source ?? string.Empty;

		public string Message => this.message ?? string.Empty;

		#endregion


		#region creation

		public SimpleState(string? source, string? message) {
			// check arguments
			if (source == null) {
				source = string.Empty;
			}
			if (message == null) {
				message = string.Empty;
			}

			// initialize members
			this.message = message;
			this.source = source;
		}

		#endregion


		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion


		#region IEnumerable<KeyValuePair<string, object>>

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return new KeyValuePair<string, object>(SourcePropertyName, this.Source);
			yield return new KeyValuePair<string, object>(MessagePropertyName, this.Message);
		}

		#endregion


		#region IReadOnlyCollection<KeyValuePair<string, object>>

		public int Count {
			get {
				return 2;
			}
		}

		#endregion


		#region IReadOnlyDictionary<string, object>

		public object this[string key] {
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
				yield return SourcePropertyName;
				yield return MessagePropertyName;
			}
		}

		public IEnumerable<object> Values {
			get{
				yield return this.Source;
				yield return this.Message;
			}
		}

		public bool ContainsKey(string key) {
			object? dummy;
			return TryGetValue(key, out dummy);
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) {
			// try to get value
			value = key switch {
				SourcePropertyName => this.Source,
				MessagePropertyName => this.Message,
				_ => null
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
				formatter.WriteStringObjectProperty(writer, SourcePropertyName, this.Source, ref firstItem);
				formatter.WriteStringObjectProperty(writer, MessagePropertyName, this.Message, ref firstItem);
				formatter.WriteObjectEnd(writer);

				return writer.ToString();
			}
		}

		#endregion


		#region overrides

		public override string ToString() {
			using (StringWriter writer = new StringWriter()) {
				writer.Write(SourcePropertyName);
				writer.Write(": ");
				JsonUtil.WriteJsonString(writer, this.Source);
				writer.Write(", ");
				writer.Write(MessagePropertyName);
				writer.Write(": ");
				JsonUtil.WriteJsonString(writer, this.Message);

				return writer.ToString();
			}
		}

		public override bool Equals(object? obj) {
			if (obj is SimpleState) {
				SimpleState that = (SimpleState)obj;
				return (this.Source == that.Source) && (this.Message == that.Message);
			} else {
				return false;
			}
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.Source, this.Message);
		}

		#endregion
	}
}
