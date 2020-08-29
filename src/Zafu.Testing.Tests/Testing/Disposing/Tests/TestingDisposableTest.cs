using System;
using System.Diagnostics;
using Xunit;


namespace Zafu.Testing.Disposing.Tests {
	public class TestingDisposableTest {
		#region Dispose

		public class Dispose {
			#region tests

			[Fact(DisplayName = "normal")]
			public void normal() {
				// arrange
				TestingDisposable target = new TestingDisposable();

				// act & assert

				// initial state
				Assert.Equal(0, target.DisposeCount);
				Assert.False(target.ForbidMultipleDispose);
				Assert.Null(target.ExceptionOnDispose);

				// Dispose() can be called multiple times.
				target.Dispose();
				Assert.Equal(1, target.DisposeCount);

				target.Dispose();
				Assert.Equal(2, target.DisposeCount);

				target.Dispose();
				Assert.Equal(3, target.DisposeCount);
			}

			[Fact(DisplayName = "exception")]
			public void exception() {
				// arrange
				string message = "OK?";
				Exception exception = new NotSupportedException(message);
				TestingDisposable target = new TestingDisposable(exception);

				// act & assert

				// initial state
				Assert.Equal(0, target.DisposeCount);
				Assert.False(target.ForbidMultipleDispose);
				Assert.Equal(exception, target.ExceptionOnDispose);

				// The exception should be thrown when Dispose() is called.
				NotSupportedException actual = Assert.Throws<NotSupportedException>(() => {
					target.Dispose();
				});
				Assert.Equal(message, actual.Message);
				Assert.Equal(1, target.DisposeCount);

				actual = Assert.Throws<NotSupportedException>(() => {
					target.Dispose();
				});
				Assert.Equal(message, actual.Message);
				Assert.Equal(2, target.DisposeCount);
			}

			[Fact(DisplayName = "forbidMultipleDispose")]
			public void forbidMultipleDispose() {
				// arrange
				bool forbidMultipleDispose = true;
				TestingDisposable target = new TestingDisposable(forbidMultipleDispose);

				// act & assert

				// initial state
				Assert.Equal(0, target.DisposeCount);
				Assert.True(target.ForbidMultipleDispose);
				Assert.Null(target.ExceptionOnDispose);

				// The first Dispose() call should succeed.
				target.Dispose();
				Assert.Equal(1, target.DisposeCount);

				// An ObjectDisposedException should be thrown when Dispose() is called multiple times.
				ObjectDisposedException actual = Assert.Throws<ObjectDisposedException>(() => {
					target.Dispose();
				});
				Assert.Equal(2, target.DisposeCount);

				actual = Assert.Throws<ObjectDisposedException>(() => {
					target.Dispose();
				});
				Assert.Equal(3, target.DisposeCount);
			}

			#endregion
		}

		#endregion
	}
}
