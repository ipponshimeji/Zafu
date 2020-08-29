using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Zafu.Testing.Disposing;

namespace Zafu.Testing.Logging.Tests {
	public class BeginScopeDataTest {
		#region samples

		public static readonly IDisposable SampleScope = new TestingDisposable();

		public static readonly BeginScopeData Sample = BeginScopeData.Create<Uri>(new Uri("http://example.org"), SampleScope);

		#endregion


		#region constructor

		public class Constructor {
			[Fact(DisplayName = "general constructor; general")]
			public void General_general() {
				// arrange
				Type stateType = typeof(Version);
				object state = new Version(5, 6, 7, 8);
				IDisposable scope = SampleScope;

				// act
				BeginScopeData actual = new BeginScopeData(stateType, state, scope);

				// assert
				Assert.Equal(stateType, actual.StateType);
				Assert.Equal(state, actual.State);
				Assert.Equal(scope, actual.Scope);
			}

			[Fact(DisplayName = "general constructor; stateType: null")]
			public void General_stateType_null() {
				// arrange
				Type stateType = null!;
				object? state = new Version(2, 3);
				IDisposable scope = SampleScope;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new BeginScopeData(stateType, state, scope);
				});

				// assert
				Assert.Equal("stateType", actual.ParamName);
			}

			[Fact(DisplayName = "general constructor; state: type mismatch")]
			public void General_state_typeMismatch() {
				// arrange
				Type stateType = typeof(Uri);
				object? state = new Version(6, 3);
				IDisposable scope = SampleScope;

				// act
				ArgumentException actual = Assert.Throws<ArgumentException>(() => {
					new BeginScopeData(stateType, state, scope);
				});

				// assert
				Assert.Equal("state", actual.ParamName);
				Assert.Equal("It is not an instance of System.Uri (Parameter 'state')", actual.Message);
			}

			[Fact(DisplayName = "general constructor; scope: null")]
			public void General_scope_null() {
				// arrange
				Type stateType = typeof(Version);
				object state = new Version(11, 2);
				IDisposable? scope = null;

				// act
				BeginScopeData actual = new BeginScopeData(stateType, state, scope);

				// assert
				Assert.Equal(stateType, actual.StateType);
				Assert.Equal(state, actual.State);
				// scope can be null
				Assert.Null(actual.Scope);
			}

			[Fact(DisplayName = "copy constructor; general")]
			public void Copy_general() {
				// arrange
				BeginScopeData src = Sample;

				// act
				BeginScopeData actual = new BeginScopeData(src);

				// assert
				Assert.Equal(src.StateType, actual.StateType);
				Assert.Equal(src.State, actual.State);
				Assert.Equal(src.Scope, actual.Scope);
			}

			[Fact(DisplayName = "copy constructor; src: null")]
			public void Copy_src_null() {
				// arrange
				BeginScopeData src = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new BeginScopeData(src);
				});

				// assert
				Assert.Equal("src", actual.ParamName);
			}

			[Fact(DisplayName = "Create")]
			public void Create() {
				// arrange
				Version state = new Version(12, 2, 1);
				IDisposable scope = SampleScope;

				// act
				BeginScopeData actual = BeginScopeData.Create<Version>(state, scope);

				// assert
				Assert.Equal(typeof(Version), actual.StateType);
				Assert.Equal(state, actual.State);
				Assert.Equal(scope, actual.Scope);
			}
		}

		#endregion


		#region comparison

		public class Comparison {
			#region utilities

			protected void Test(BeginScopeData? x, BeginScopeData? y, bool expected) {
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
				BeginScopeData x = Sample;
				// create a clone not to equal to x as a reference 
				BeginScopeData y = new BeginScopeData(x);
				Debug.Assert(object.ReferenceEquals(x, y) == false);

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "same; null")]
			public void same_null() {
				// arrange
				BeginScopeData? x = null;
				BeginScopeData? y = null;

				// act & assert
				Test(x, y, expected: true);
			}

			[Fact(DisplayName = "different; null")]
			public void different_null() {
				// act & assert
				// test (Sample == null), (Sample != null), Sample.Equals((BeginScopeData?)null), and Sample.Equals((object?)null)
				Test(Sample, null, expected: false);

				// test (null == Sample) and (null != Sample)
				Test(null, Sample, expected: false);
			}

			[Fact(DisplayName = "different; StateType")]
			public void different_StateType() {
				// arrange
				BeginScopeData x = Sample;
				// different from x only at StateType
				Type differentValue = typeof(object);
				Debug.Assert(differentValue != x.StateType);
				BeginScopeData y = new BeginScopeData(differentValue, x.State, x.Scope);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; State")]
			public void different_State() {
				// arrange
				BeginScopeData x = Sample;
				// different from x only at State
				object differentValue = new Uri("https://different.example.org");
				Debug.Assert(differentValue != x.State);
				BeginScopeData y = new BeginScopeData(x.StateType, differentValue, x.Scope);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; Scope")]
			public void different_Scope() {
				// arrange
				BeginScopeData x = Sample;
				// different from x only at Exception
				IDisposable differentValue = new TestingDisposable();
				Debug.Assert(differentValue != x.Scope);
				BeginScopeData y = new BeginScopeData(x.StateType, x.State, differentValue);

				// act & assert
				Test(x, y, expected: false);
			}

			[Fact(DisplayName = "different; incompatible type")]
			public void different_incompatible_type() {
				// arrange
				BeginScopeData x = Sample;
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
				BeginScopeData x = Sample;
				// create a clone not to equal to x as a reference 
				BeginScopeData y = new BeginScopeData(x);
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
				BeginScopeData x = Sample;
				BeginScopeData y = new BeginScopeData(typeof(Uri), null, null);

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
