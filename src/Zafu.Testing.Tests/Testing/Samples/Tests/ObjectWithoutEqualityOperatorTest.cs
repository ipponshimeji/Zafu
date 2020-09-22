using System;
using System.Diagnostics;
using Xunit;


namespace Zafu.Testing.Samples.Tests {
	public class ObjectWithoutEqualityOperatorTest {
		#region tests

		[Fact(DisplayName = "constructor")]
		public void Constructor() {
			// arrange
			int value = 7;

			// act
			ObjectWithoutEqualityOperator target = new ObjectWithoutEqualityOperator(value);

			// assert
			Assert.Equal(value, target.Value);
		}

		[Fact(DisplayName = "comparison: same")]
		public void Comparison_same() {
			// arrange
			int value = 7;
			ObjectWithoutEqualityOperator target1 = new ObjectWithoutEqualityOperator(value);
			ObjectWithoutEqualityOperator target2 = new ObjectWithoutEqualityOperator(value);
			Debug.Assert(object.ReferenceEquals(target1, target2) == false);

			// act
			bool actual_Equal = target1.Equals(target2);
			bool actual_EqualityOperator = (target1 == target2);

			// assert
			// Equals returns true because they have the same value.
			Assert.True(actual_Equal);
			// Equality operator returns false because they are different instances.
			// (Note that the equality operator of System.Object is used because ObjectWithoutEqualityOperator does not define its equality operator.)
			Assert.False(actual_EqualityOperator);
		}

		[Fact(DisplayName = "comparison: different")]
		public void Comparison_different() {
			// arrange
			ObjectWithoutEqualityOperator target1 = new ObjectWithoutEqualityOperator(5);
			ObjectWithoutEqualityOperator target2 = new ObjectWithoutEqualityOperator(-4);
			Debug.Assert(object.ReferenceEquals(target1, target2) == false);

			// act
			bool actual_Equal = target1.Equals(target2);
			bool actual_EqualityOperator = (target1 == target2);

			// assert
			Assert.False(actual_Equal);
			Assert.False(actual_EqualityOperator);
		}

		#endregion
	}
}
