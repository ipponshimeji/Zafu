using System;
using System.Collections.Generic;
using Zafu.Testing;
using Xunit;
using System.Diagnostics;

namespace Zafu.Utilities.Test {
	public class LoggingUtilTest {
		#region types

		public class LogMessageSample {
			#region data

			public readonly string? Header;

			public readonly string? Message;

			public readonly string? ExpectedMessage;

			#endregion


			#region constructor

			public LogMessageSample(string? header, string? message, string? expectedMessage) {
				// initialize members
				this.Header = header;
				this.Message = message;
				this.ExpectedMessage = expectedMessage;
			}

			#endregion


			#region overrides

			public override string ToString() {
				string header = TestingUtil.GetDisplayText(this.Header, quote: true);
				string message = TestingUtil.GetDisplayText(this.Message, quote: true);

				return $"{{header: {header}, message: {message}}}";
			}

			#endregion
		}

		#endregion


		#region samples

		public static IEnumerable<object[]> GetLogMessageSamples() {
			return new LogMessageSample[] {
				//                  (header, message, expectedMessage)
				new LogMessageSample("name", "log content", "[name] log content"),
				new LogMessageSample("name", "", "[name] "),
				new LogMessageSample("name", null, "[name] "),
				new LogMessageSample("", "log content", "log content"),
				new LogMessageSample("", "", ""),
				new LogMessageSample("", null, ""),
				new LogMessageSample(null, "log content", "log content"),
				new LogMessageSample(null, "", ""),
				new LogMessageSample(null, null, "")
			}.ToTestData();
		}

		#endregion


		#region FormatMessage

		public class FormatMessage {
			#region samples

			public static IEnumerable<object[]> GetSamples() {
				return GetLogMessageSamples();
			}

			#endregion


			#region tests

			[Theory(DisplayName = "arguments")]
			[MemberData(nameof(GetSamples))]
			public void Arguments(LogMessageSample sample) {
				// arrange
				Debug.Assert(sample != null);

				// act
				string actual = LoggingUtil.FormatMessage(sample.Header, sample.Message);

				// assert
				Assert.Equal(sample.ExpectedMessage, actual);
			}

			#endregion
		}

		#endregion
	}
}
