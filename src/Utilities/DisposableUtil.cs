using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Zafu.Utilities {
	public static class DisposableUtil {
		#region methods

		public static void ClearDisposable<T>(ref T? target) where T: class, IDisposable {
			if (target != null) {
				IDisposable old = System.Threading.Interlocked.Exchange(ref target, null);
				old.Dispose();
			}
		}

		#endregion
	}
}
