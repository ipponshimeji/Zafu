using System;
using System.Diagnostics;

namespace Zafu.Testing.Logging {
	/// <summary>
	/// The base class for classes which represent logging operation.
	/// </summary>
	public abstract class LoggingData {
		#region creation

		protected LoggingData() {
		}

		protected LoggingData(LoggingData src) {
			// check argument
			if (src == null) {
				throw new ArgumentNullException(nameof(src));
			}
		}

		#endregion
	}
}
