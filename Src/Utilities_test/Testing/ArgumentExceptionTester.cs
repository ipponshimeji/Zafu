using System;
using System.Diagnostics;
using Xunit;

namespace Zafu.Utilities.Testing {
	public class ArgumentExceptionTester<TException>: ReferenceTester<TException> where TException : ArgumentException {
		#region data

		public readonly string ParamName;

		#endregion


		#region creation

		public ArgumentExceptionTester(string paramName) {
			// argument checks
			if (paramName == null) {
				throw new ArgumentNullException(nameof(paramName));
			}

			// initialize members
			this.ParamName = paramName;
		}

		#endregion


		#region overrides

		public override void Test(TException target) {
			// by default, check only its ParamName
			Xunit.Assert.Equal(this.ParamName, target.ParamName);
		}

		#endregion
	}
}
