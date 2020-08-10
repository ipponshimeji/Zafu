using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Utilities;
using Xunit;

namespace Zafu.Utilities.Test {
	public class DisposableUtilTest {
		#region types

		public class DisposableSample: IDisposable {
			#region data

			public int DisposeCount { get; private set; } = 0;

			#endregion


			#region IDisposable

			public void Dispose() {
				++this.DisposeCount;
			}

			#endregion
		}

		#endregion


		#region ClearDisposable

		public class ClearDisposable {
			[Fact(DisplayName = "general")]
			public void General() {
				// Arrange
				DisposableSample sample = new DisposableSample();
				Debug.Assert(sample.DisposeCount == 0);
				DisposableSample? value = sample;

				// Act
				DisposableUtil.ClearDisposable(ref value);

				// Assert
				Assert.Null(value);
				Assert.Equal(1, sample.DisposeCount);
			}

			[Fact(DisplayName = "target: null")]
			public void target_null() {
				// Arrange
				DisposableSample? value = null;

				// Act
				DisposableUtil.ClearDisposable(ref value);

				// Assert
				Assert.Null(value);
			}
		}

		#endregion
	}
}
