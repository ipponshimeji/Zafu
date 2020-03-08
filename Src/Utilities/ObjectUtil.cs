using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Zafu.Utilities {
	public static class ObjectUtil {
		#region methods

		public static object? CloneJsonObject(object? src) {
			// argument checks
			if (src == null) {
				// clone null
				return null;
			}

			// clone the json object
			switch (src) {
				case IDictionary<string, object> obj:
					// object
					return obj.ToDictionary(key => key, value => CloneJsonObject(value));
				case IReadOnlyDictionary<string, object> obj:
					// object
					return obj.ToDictionary(key => key, value => CloneJsonObject(value));
				case IList<object> array:
					// array
					return array.Select(child => CloneJsonObject(child)).ToList();
				case IReadOnlyList<object> array:
					// array
					return array.Select(child => CloneJsonObject(child)).ToList();
				default:
					// string or value type
					return src;
			}
		}

		#endregion
	}
}
