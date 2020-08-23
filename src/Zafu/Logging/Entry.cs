using System;
using System.Diagnostics;

namespace Zafu.Logging {
	public abstract class Entry {
		#region creation

		protected Entry() {
		}

		protected Entry(Entry src) {
			// check argument
			if (src == null) {
				throw new ArgumentNullException(nameof(src));
			}
		}

		#endregion
	}
}
