using System;
using System.Diagnostics;

namespace Zafu.Text.Tests {
	public class CompactJsonFormatterTest: JsonFormatterTestBase {
		#region overrides

		protected override IJsonFormatter GetFormatter() {
			return CompactJsonFormatter.Instance;
		}

		public override string GetExpectedJsonText(object sample) {
			if (object.ReferenceEquals(sample, EmptyObjectValueSample)) {
				return "{}";
			} else if (object.ReferenceEquals(sample, GeneralObjectValueSample)) {
				return "{\"null\":null,\"boolean\":true,\"number\":3,\"string\":\"a\\\"bc\",\"object\":{\"boolean\":false,\"number\":6.2},\"array\":[true,3.4567e-10]}";
			} else if (object.ReferenceEquals(sample, EmptyArrayValueSample)) {
				return "[]";
			} else if (object.ReferenceEquals(sample, GeneralArrayValueSample)) {
				return "[null,false,123.45,\"abc\",{\"boolean\":false,\"number\":-67},[true,3.4567e-10,\"123\\\\4\"]]";
			} else {
				throw new ArgumentException("Not supported.", nameof(sample));
			}
		}

		#endregion
	}
}
