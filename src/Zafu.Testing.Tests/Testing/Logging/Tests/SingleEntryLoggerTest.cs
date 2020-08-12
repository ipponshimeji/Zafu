using System;
using System.Diagnostics;
using Xunit;


namespace Zafu.Testing.Logging.Tests {
	public class SingleEntryLoggerTest {
		#region GetDisplayText

		public class GetDisplayText {
			#region types

			/// <summary>
			/// The sample class whose ToString() method returns null.
			/// </summary>
			public class ToStringNullObject {
				public override string ToString() {
					return null!;
				}
			}

			#endregion


			#region tests

			[Fact(DisplayName = "(object); value: null")]
			public void object_value_null() {
				// arrange
				object? value = null;

				// act
				string actual = TestingUtil.GetDisplayText(value);

				// assert
				Assert.Equal("(null)", actual);
			}

			[Fact(DisplayName = "(object); value: non-null")]
			public void object_value_non_null() {
				// arrange
				object? value = new Version(1, 2);

				// act
				string actual = TestingUtil.GetDisplayText(value);

				// assert
				Assert.Equal("1.2", actual);
			}

			[Fact(DisplayName = "(object); value: ToStringNullObject")]
			public void object_value_ToStringNullObject() {
				// arrange
				object? value = new ToStringNullObject();

				// act
				string actual = TestingUtil.GetDisplayText(value);

				// assert
				Assert.Equal("", actual);
			}

			#endregion
		}

		#endregion
	}
}
