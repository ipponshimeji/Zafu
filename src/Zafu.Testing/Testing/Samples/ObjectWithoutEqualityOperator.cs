using System;
using System.Diagnostics;


namespace Zafu.Testing.Samples {
	/// <summary>
	/// The class which does not define its equality operator, while it overrides Equals() method.
	/// </summary>
	public class ObjectWithoutEqualityOperator {
		#region data

		public readonly int Value;

		#endregion


		#region constructor

		public ObjectWithoutEqualityOperator(int value) {
			// initialize members
			this.Value = value;
		}

		#endregion


		#region overrides

		public override bool Equals(object? obj) {
			ObjectWithoutEqualityOperator? that = obj as ObjectWithoutEqualityOperator;
			return (that != null) && (this.Value == that.Value);
		}

		public override int GetHashCode() {
			return HashCode.Combine(this.Value);
		}

		#endregion
	}
}
