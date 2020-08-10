using System;

namespace Zafu.Utilities.Testing {
	public abstract class Tester<T> {
		#region overridables

		public abstract void Test(T target);

		#endregion
	}
}
