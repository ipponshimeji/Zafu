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


		#region methods

		public static Action<TException> GetTest(string message) {
			return new ExceptionTester<TException>(message).Test;
		}

		public static Action<TException?> GetNullableTest(string message) {
			return new ExceptionTester<TException>(message).TestNullable;
		}

		#endregion


		#region overrides

		public override void Test(TException target) {
			// by default, only checks its Message
			Assert.Equal(this.Message, target.Message);
		}

		#endregion
	}
}
