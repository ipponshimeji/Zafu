using System;
using System.Diagnostics;

namespace Zafu.Text {
	public class CompactJsonFormatter: JsonFormatter {
		#region data

		public static readonly CompactJsonFormatter Instance = new CompactJsonFormatter();

		#endregion
	}
}
