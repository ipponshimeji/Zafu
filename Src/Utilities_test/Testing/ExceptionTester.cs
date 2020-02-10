using System;
using System.Diagnostics;
using Xunit;

namespace Zafu.Utilities.Testing {
	public class ExceptionTester<TException>: ReferenceTester<TException> where TException : Exception {
		#region data

		public readonly string Message;

		#endregion


		#region creation

		public ExceptionTester(string message) {
			// argument checks
			if (message == null) {
				throw new ArgumentNullException(nameof(message));
			}

			// initialize members
			this.Message = message;
		}

		#endregion


		#region overrides

		public override void Test(TException target) {
			// by default, check only its Message
			Assert.Equal(this.Message, target.Message);
		}

		#endregion
	}
}
