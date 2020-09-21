using System;
using System.Diagnostics;


namespace Zafu.Testing {
	public class SampleBase {
		#region data

		public readonly string Description;

		#endregion


		#region constructor

		public SampleBase(string description) {
			// initialize members
			this.Description = description;
		}

		#endregion


		#region overrides

		public override string ToString() {
			return this.Description;
		}

		#endregion
	}

	public class SampleBase<T>: SampleBase {
		#region data

		public readonly T Value;

		#endregion


		#region constructor

		public SampleBase(string description, T value): base(description) {
			// initialize members
			this.Value = value;
		}

		#endregion
	}
}
