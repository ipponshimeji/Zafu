using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zafu.Utilities;
using Xunit;

namespace Zafu.Utilities.Test {
	public class ObjectUtilTest {
		#region CloneJsonObject

		public class CloneJsonObject {
			[Fact(DisplayName = "src: null")]
			public void src_null() {
				// Arrange
				object? src = null;

				// Act
				object? actual = ObjectUtil.CloneJsonObject(src);

				// Assert
				Assert.Null(actual);
			}

			[Fact(DisplayName = "src: bool")]
			public void src_bool() {
				// Arrange
				bool src = true;

				// Act
				object? actual = ObjectUtil.CloneJsonObject(src);

				// Assert
				Assert.Equal(src, actual);
			}

			[Fact(DisplayName = "src: double")]
			public void src_double() {
				// Arrange
				double src = 123.45;

				// Act
				object? actual = ObjectUtil.CloneJsonObject(src);

				// Assert
				Assert.Equal(src, actual);
			}

			[Fact(DisplayName = "src: string")]
			public void src_string() {
				// Arrange
				string src = "abcde";

				// Act
				object? actual = ObjectUtil.CloneJsonObject(src);

				// Assert
				Assert.Equal(src, actual);
			}

			[Fact(DisplayName = "src: array")]
			public void src_array() {
				// Arrange
				List<string> array = new List<string>() {
					"a", "b", "c", "d"
				};
				IList<string> listSrc = array;
				IReadOnlyList<string> readOnlyListSrc = array;

				// Act
				// TODO: imcomplete create wrapper to restrict interfaces
				object? actual1 = ObjectUtil.CloneJsonObject(listSrc);
				object? actual2 = ObjectUtil.CloneJsonObject(readOnlyListSrc);

				// Assert
				Assert.Equal(listSrc, actual1);
				Assert.NotSame(listSrc, actual1);
				Assert.Equal(readOnlyListSrc, actual2);
				Assert.NotSame(readOnlyListSrc, actual2);
			}
		}

		// TODO: test object

		#endregion
	}
}
