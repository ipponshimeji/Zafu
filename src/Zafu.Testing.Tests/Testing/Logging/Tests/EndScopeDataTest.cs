using System;
using System.Diagnostics;
using Xunit;
using Zafu.Testing.Disposing;

namespace Zafu.Testing.Logging.Tests {
	public class EndScopeDataTest {
		#region samples

		public static readonly IDisposable SampleScope = new TestingDisposable();

		public static readonly EndScopeData Sample = new EndScopeData(SampleScope);

		#endregion


		#region constructor

		public class Constructor {
			[Fact(DisplayName = "general constructor; general")]
			public void General_general() {
				// arrange
				IDisposable scope = SampleScope;

				// act
				EndScopeData actual = new EndScopeData(scope);

				// assert
				Assert.Equal(scope, actual.Scope);
			}

			[Fact(DisplayName = "general constructor; scope: null")]
			public void General_scope_null() {
				// arrange
				IDisposable? scope = null;	// null

				// act
				EndScopeData actual = new EndScopeData(scope);

				// assert
				Assert.Null(actual.Scope);
			}

			[Fact(DisplayName = "copy constructor; general")]
			public void Copy_general() {
				// arrange
				EndScopeData src = Sample;

				// act
				EndScopeData actual = new EndScopeData(src);

				// assert
				Assert.Equal(src.Scope, actual.Scope);
			}

			[Fact(DisplayName = "copy constructor; src: null")]
			public void Copy_src_null() {
				// arrange
				EndScopeData src = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new EndScopeData(src);
				});

				// assert
				Assert.Equal("src", actual.ParamName);
			}
		}

		#endregion


		#region comparison

		public class Comparison {
			#region utilities

			protected void Test(EndScopeData? x, EndScopeData? y, bool expected) {
				// test operators
				// act
				bool actual_equality = (x == y);
				bool actual_inequality = (x != y);

				// assert
				Assert.Equal(expected, actual_equality);
				Assert.Equal(!expected, actual_inequality);

				// test Equals methods
				if (object.ReferenceEquals(x, null) == false) {
					// act
					bool actual_equal = x.Equals(y);
					bool actual_objectEqual = x.Equals((object?)y);

					// assert
					Assert.Equal(expected, actual_equal);
					Assert.Equal(expected, actual_objectEqual);
				}
			}

			#endregion


			#region tests

			[Fact(DisplayName = "same; general")]
			public void same_general() {
				// arrange
				EndScopeData x = Sample;
				// create a clone not to equal to x as a reference 
				EndScopeData y = new EndScopeData(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "same; null")]
			public void same_null() {
				// arrange
				EndScopeData? x = null;
				EndScopeData? y = null;

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "different; null")]
			public void different_null() {
				// act & assert
				// test (Sample == null), (Sample != null), Sample.Equals((EndScopeData?)null), and Sample.Equals((object?)null)
				Test(Sample, null, expected: false);

				// test (null == Sample) and (null != Sample)
				Test(null, Sample, expected: false);
			}

			[Fact(DisplayName = "different; Scope")]
			public void different_Scope() {
				// arrange
				EndScopeData x = Sample;
				// different from x only at Exception
				IDisposable differentValue = new TestingDisposable();
				Debug.Assert(differentValue != x.Scope);
				EndScopeData y = new EndScopeData(differentValue);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; incompatible type")]
			public void different_incompatible_type() {
				// arrange
				EndScopeData x = Sample;
				// object of incompatible type (string)
				object y = "abc";

				// act
				bool actual = x.Equals(y);

				// assert
				Assert.False(actual);
			}

			#endregion
		}

		#endregion


		#region GetHashCode

		public class HashCode {
			[Fact(DisplayName = "same")]
			public void same() {
				// arrange
				EndScopeData x = Sample;
				// create a clone not to equal to x as a reference 
				EndScopeData y = new EndScopeData(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.Equal(actual_x, actual_y);
			}

			[Fact(DisplayName = "different")]
			public void different() {
				// arrange
				EndScopeData x = Sample;
				EndScopeData y = new EndScopeData(new TestingDisposable());

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.NotEqual(actual_x, actual_y);
			}
		}

		#endregion
	}
}
