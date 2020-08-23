using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;
using Zafu.Testing.Logging;

namespace Zafu.Logging.Tests {
	public class BeginScopeDataTest {
		#region types

		// The derived class from BeginScopeData to expose GetBeginScopeMethodInfo() and GetBeginScopeArguments() as public.
		class TestingBeginScopeData: BeginScopeData {
			#region creation

			public TestingBeginScopeData(BeginScopeData src) : base(src) {
			}

			#endregion


			#region methods

			public new MethodInfo GetBeginScopeMethodInfo() {
				return base.GetBeginScopeMethodInfo();
			}

			public new object?[] GetBeginScopeArguments() {
				return base.GetBeginScopeArguments();
			}

			#endregion
		}

		class ScopeLogger: ILogger {
			#region types

			class Scope: IDisposable {
				public void Dispose() {
					// do nothing
				}
			}

			#endregion


			#region data

			private bool nullScope;

			public BeginScopeData? Data { get; private set; } = null;

			#endregion


			#region creation

			public ScopeLogger(bool nullScope = false) {
				// initialize member
				this.nullScope = nullScope;
			}

			#endregion


			#region ILogger

			public IDisposable BeginScope<TState>(TState state) {
				// check state
				if (this.Data != null) {
					throw new InvalidOperationException("This logger can begin only one scope.");
				}

				// record scope data
				Scope? scope = this.nullScope ? null : new Scope();
				this.Data = new BeginScopeData(typeof(TState), state, scope);

				return scope!;
			}

			public bool IsEnabled(LogLevel logLevel) {
				throw new NotSupportedException();
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
				throw new NotSupportedException();
			}

			#endregion
		}

		#endregion


		#region samples

		public static readonly object SampleScope = new object();

		public static readonly BeginScopeData Sample = new BeginScopeData(typeof(Version), new Version(3, 7), SampleScope);

		#endregion


		#region constructor

		public class Constructor {
			[Fact(DisplayName = "general constructor; general")]
			public void General_general() {
				// arrange
				Type stateType = typeof(Uri);
				object state = new Uri("http://example.org/");
				object scope = SampleScope;

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
				object? state = new Uri("http://example.org/");
				object scope = SampleScope;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					new BeginScopeData(stateType, state, scope);
				});

				// assert
				Assert.Equal("stateType", actual.ParamName);
			}

			[Fact(DisplayName = "general constructor; scope: null")]
			public void General_scope_null() {
				// arrange
				Type stateType = typeof(Uri);
				object state = new Uri("http://example.org/");
				object? scope = null;

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
		}

		#endregion


		#region comparison

		public class Comparison {
			#region utilities

			protected void Test(BeginScopeData? x, BeginScopeData? y, bool expected) {
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
				Type differentValue = typeof(Uri);
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
				object differentValue = new Version(9, 5);
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
				object differentValue = new object();
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


		#region BeginScopeOn

		public class BeginScopeOn {
			[Fact(DisplayName = "single; general")]
			public void single_general() {
				// arrange
				BeginScopeData sample = Sample;
				ScopeLogger logger = new ScopeLogger();
				Debug.Assert(logger.Data == null);

				// act
				IDisposable? scope = sample.BeginScopeOn(logger);

				// assert
				BeginScopeData? actual = logger.Data;
				Assert.NotNull(actual);
				Debug.Assert(actual != null);
				Assert.Equal(sample.StateType, actual.StateType);
				Assert.Equal(sample.State, actual.State);
				Assert.Equal(scope, actual.Scope);
			}

			[Fact(DisplayName = "single; null scope")]
			public void single_nullscope() {
				// arrange
				BeginScopeData sample = Sample;
				ScopeLogger logger = new ScopeLogger(nullScope: true);
				Debug.Assert(logger.Data == null);

				// act
				IDisposable? scope = sample.BeginScopeOn(logger);

				// assert
				Assert.Null(scope);
				BeginScopeData? actual = logger.Data;
				Assert.NotNull(actual);
				Debug.Assert(actual != null);
				Assert.Equal(sample.StateType, actual.StateType);
				Assert.Equal(sample.State, actual.State);
				Assert.Null(actual.Scope);
			}

			[Fact(DisplayName = "single; logger: null")]
			public void single_logger_null() {
				// arrange
				BeginScopeData sample = Sample;
				ILogger logger = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					sample.BeginScopeOn(logger);
				});

				// assert
				Assert.Equal("logger", actual.ParamName);
			}

			[Fact(DisplayName = "multiple; general")]
			public void multiple_general() {
				// arrange
				BeginScopeData sample = Sample;
				ScopeLogger logger1 = new ScopeLogger(nullScope: false);
				Debug.Assert(logger1.Data == null);
				// logger2 return null scope
				ScopeLogger logger2 = new ScopeLogger(nullScope: true);
				Debug.Assert(logger2.Data == null);
				ScopeLogger logger3 = new ScopeLogger(nullScope: false);
				Debug.Assert(logger3.Data == null);
				// Note that null logger should be skipped without any exceptioin.
				ILogger?[] loggers = new ILogger?[] { logger1, logger2, null, logger3 };

				// act
				IEnumerable<IDisposable> result = sample.BeginScopeOn(loggers);
				IDisposable[] scopes = result.ToArray();

				// assert
				// Note that null scope should be skipped.
				Assert.Equal(2, scopes.Length);

				BeginScopeData? actual1 = logger1.Data;
				Assert.NotNull(actual1);
				Debug.Assert(actual1 != null);
				Assert.Equal(sample.StateType, actual1.StateType);
				Assert.Equal(sample.State, actual1.State);
				Assert.Equal(scopes[0], actual1.Scope);

				BeginScopeData? actual2 = logger2.Data;
				Assert.NotNull(actual2);
				Debug.Assert(actual2 != null);
				Assert.Equal(sample.StateType, actual2.StateType);
				Assert.Equal(sample.State, actual2.State);
				Assert.Null(actual2.Scope);

				BeginScopeData? actual3 = logger3.Data;
				Assert.NotNull(actual3);
				Debug.Assert(actual3 != null);
				Assert.Equal(sample.StateType, actual3.StateType);
				Assert.Equal(sample.State, actual3.State);
				Assert.Equal(scopes[1], actual3.Scope);
			}

			[Fact(DisplayName = "multiple; loggers: null")]
			public void multiple_loggers_null() {
				// arrange
				BeginScopeData sample = Sample;
				IEnumerable<ILogger> loggers = null!;

				// act
				ArgumentNullException actual = Assert.Throws<ArgumentNullException>(() => {
					sample.BeginScopeOn(loggers);
				});

				// assert
				Assert.Equal("loggers", actual.ParamName);
			}
		}

		#endregion


		#region GetBeginScopeMethodInfo

		public class GetBeginScopeMethodInfo {
			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				// Use wrapper class (TestingBeginScopeData) to access protected methods of BeginScopeData. 
				TestingBeginScopeData sample = new TestingBeginScopeData(Sample);

				// act
				MethodInfo actual = sample.GetBeginScopeMethodInfo();
				string? str = actual.ToString();

				// assert
				Assert.Equal("System.IDisposable BeginScope[Version](System.Version)", actual.ToString());
			}
		}

		#endregion


		#region GetBeginScopeArguments

		public class GetBeginScopeArguments {
			[Fact(DisplayName = "general")]
			public void general() {
				// arrange
				// Use wrapper class (TestingBeginScopeData) to access protected methods of BeginScopeData. 
				TestingBeginScopeData sample = new TestingBeginScopeData(Sample);
				object? expected = new object?[] { sample.State };

				// act
				object?[] actual = sample.GetBeginScopeArguments();

				// assert
				Assert.Equal(expected, actual);
			}
		}

		#endregion
	}
}
