using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Testing.Logging;

namespace Zafu.Logging.Tests {
	public class EndScopeDataTest {
		#region types

		class Scope: IDisposable {
			#region data

			public bool Disposed { get; private set; } = false;

			#endregion


			#region creation & disposal

			public void Dispose() {
				// check state
				if (this.Disposed) {
					// This object does not support repeated Dispose().
					throw new ObjectDisposedException(null);
				}

				this.Disposed = true;
			}

			#endregion
		}

		#endregion


		#region samples

		public static readonly object SampleScope = new Scope();

		public static readonly EndScopeData Sample = new EndScopeData(SampleScope);

		#endregion


		#region constructor

		public class Constructor {
			[Fact(DisplayName = "general constructor; general")]
			public void General_general() {
				// arrange
				object scope = SampleScope;

				// act
				EndScopeData actual = new EndScopeData(scope);

				// assert
				Assert.Equal(scope, actual.Scope);
			}

			[Fact(DisplayName = "general constructor; scope: null")]
			public void General_scope_null() {
				// arrange
				object? scope = null;	// null

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
				// act (operators)
				bool actual_equality = (x == y);
				bool actual_inequality = (x != y);

				// assert (operators)
				Assert.Equal(expected, actual_equality);
				Assert.Equal(!expected, actual_inequality);

				if (object.ReferenceEquals(x, null) == false) {
					// act (Equals methods)
					bool actual_equal = x.Equals(y);
					bool actual_objectEqual = x.Equals((object?)y);

					// assert (Equals methods)
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
				object differentValue = new Scope();
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
				EndScopeData y = new EndScopeData(new Scope());

				// act
				int actual_x = x.GetHashCode();
				int actual_y = y.GetHashCode();

				// assert
				Assert.NotEqual(actual_x, actual_y);
			}
		}

		#endregion


		#region EndScope

		public class EndScope {
			[Fact(DisplayName = "single; general")]
			public void single_general() {
				// arrange
				EndScopeData sample = Sample;
				Scope scope = new Scope();
				Debug.Assert(scope.Disposed == false);

				// act
				sample.EndScope(scope);

				// assert
				Assert.True(scope.Disposed);
			}

			[Fact(DisplayName = "single; scope: null")]
			public void single_scope_null() {
				// arrange
				EndScopeData sample = Sample;
				Scope scope = null!;

				// act
				sample.EndScope(scope);

				// assert
				// no exception should be thrown
			}

			[Fact(DisplayName = "multiple; general")]
			public void multiple_general() {
				// arrange
				EndScopeData sample = Sample;
				Scope scope1 = new Scope();
				Debug.Assert(scope1.Disposed == false);
				Scope scope2 = new Scope();
				Debug.Assert(scope2.Disposed == false);
				// Note that null scope should be skipped without any exceptioin.
				IDisposable?[] scopes = new IDisposable?[] { scope1, null, scope2 };

				// act
				sample.EndScope(scopes);

				// assert
				Assert.True(scope1.Disposed);
				Assert.True(scope2.Disposed);
			}

			[Fact(DisplayName = "multiple; scopes: null")]
			public void multiple_scopes_null() {
				// arrange
				EndScopeData sample = Sample;
				IEnumerable<IDisposable?> scopes = null!;

				sample.EndScope(scopes);

				// assert
				// no exception should be thrown
			}
		}

		#endregion
	}
}
