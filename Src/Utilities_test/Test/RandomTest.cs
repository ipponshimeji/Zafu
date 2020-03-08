using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zafu.Utilities;
using Xunit;

namespace Zafu.Utilities.Test {
	public class RandomTest {
		#region utilities

		[Flags]
		public enum Variety {
			None               = 0,
			UInt8LowSpecific   = 0x0001,	// 0x00000000 - 0x0000007F
			UInt8HighSpecific  = 0x0002,	// 0x00000080 - 0x000000FF
			UInt16LowSpecific  = 0x0004,	// 0x00000100 - 0x00007FFF
			UInt16HighSpecific = 0x0008,	// 0x00008000 - 0x0000FFFF
			UInt32LowSpecific  = 0x0010,    // 0x00010000 - 0x7FFFFFFF
			UInt32HighSpecific = 0x0020,    // 0x80000000 - 0xFFFFFFFF
			UInt64LowSpecific  = 0x0040,	// 0x100000000 - 0x7FFFFFFFFFFFFFFF
			UInt64HighSpecific = 0x0080,	// 0x8000000000000000 - 0xFFFFFFFFFFFFFFFF
		}


		public class VarietyChecker {
			#region data

			public readonly Variety Expected;

			public readonly int MinRepetition;

			public int SampleCount { get; private set; }

			public Variety Actual { get; private set; }

			#endregion


			#region constructor

			public VarietyChecker(Variety expected, int minRepetition = 0) {
				// initialize members
				this.Expected = expected;
				this.MinRepetition = minRepetition;
				this.SampleCount = 0;
				this.Actual = Variety.None;
			}

			#endregion


			#region methods

			public static Variety GetVariety(ulong value) {
				if (value < 0x80UL) {
					return Variety.UInt8LowSpecific;
				} else if (value < 0x100UL) {
					return Variety.UInt8HighSpecific;
				} else if (value < 0x8000UL) {
					return Variety.UInt16LowSpecific;
				} else if (value < 0x10000UL) {
					return Variety.UInt16HighSpecific;
				} else if (value < 0x80000000UL) {
					return Variety.UInt32LowSpecific;
				} else if (value < 0x100000000UL) {
					return Variety.UInt32HighSpecific;
				} else if (value < 0x8000000000000000UL) {
					return Variety.UInt64LowSpecific;
				} else {
					return Variety.UInt64HighSpecific;
				}
			}

			public bool Add(sbyte value) {
				// use zero extension instead of sign extension
				return AddInternal((byte)value);
			}

			public bool Add(byte value) {
				return AddInternal(value);
			}

			public bool Add(short value) {
				// use zero extension instead of sign extension
				return AddInternal((ushort)value);
			}

			public bool Add(ushort value) {
				return AddInternal(value);
			}

			public bool Add(int value) {
				// use zero extension instead of sign extension
				return AddInternal((uint)value);
			}

			public bool Add(uint value) {
				return AddInternal(value);
			}

			public bool Add(long value) {
				return AddInternal((ulong)value);
			}

			public bool Add(ulong value) {
				return AddInternal(value);
			}

			public bool HasExpectedVariety() {
				return (this.Actual & this.Expected) == this.Expected;
			}

			#endregion


			#region privates

			public bool AddInternal(ulong value) {
				this.Actual |= GetVariety(value);
				++this.SampleCount;
				return (this.MinRepetition <= this.SampleCount) && HasExpectedVariety();
			}

			#endregion
		}

		#endregion


		#region VarietyChecker

		public class VarietyCheckerTest {
			[Fact(DisplayName = "specific range of UInt8 low")]
			public void UInt8LowSpecific() {
				// Arrange
				const Variety expected = Variety.UInt8LowSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const sbyte minSByte = 0;
				const sbyte maxSByte = 0x7F;
				const byte minByte = 0;
				const byte maxByte = 0x7F;
				const short minInt16 = 0;
				const short maxInt16 = 0x7F;
				const ushort minUInt16 = 0;
				const ushort maxUInt16 = 0x7F;
				const int minInt32 = 0;
				const int maxInt32 = 0x7F;
				const uint minUInt32 = 0;
				const uint maxUInt32 = 0x7F;
				const long minInt64 = 0;
				const long maxInt64 = 0x7F;
				const ulong minUInt64 = 0;
				const ulong maxUInt64 = 0x7F;

				// Act
				target.Add(minSByte);
				target.Add(maxSByte);
				target.Add(minByte);
				target.Add(maxByte);
				target.Add(minInt16);
				target.Add(maxInt16);
				target.Add(minUInt16);
				target.Add(maxUInt16);
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt8 high")]
			public void UInt8HighSpecific() {
				// Arrange
				const Variety expected = Variety.UInt8HighSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const sbyte minSByte = sbyte.MinValue;
				const sbyte maxSByte = -1;
				const byte minByte = 0x80;
				const byte maxByte = 0xFF;
				const short minInt16 = 0x80;
				const short maxInt16 = 0xFF;
				const ushort minUInt16 = 0x80;
				const ushort maxUInt16 = 0xFF;
				const int minInt32 = 0x80;
				const int maxInt32 = 0xFF;
				const uint minUInt32 = 0x80;
				const uint maxUInt32 = 0xFF;
				const long minInt64 = 0x80;
				const long maxInt64 = 0xFF;
				const ulong minUInt64 = 0x80;
				const ulong maxUInt64 = 0xFF;

				// Act
				target.Add(minSByte);
				target.Add(maxSByte);
				target.Add(minByte);
				target.Add(maxByte);
				target.Add(minInt16);
				target.Add(maxInt16);
				target.Add(minUInt16);
				target.Add(maxUInt16);
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt16 low")]
			public void UInt16LowSpecific() {
				// Arrange
				const Variety expected = Variety.UInt16LowSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const short minInt16 = 0x100;
				const short maxInt16 = 0x7FFF;
				const ushort minUInt16 = 0x100;
				const ushort maxUInt16 = 0x7FFF;
				const int minInt32 = 0x100;
				const int maxInt32 = 0x7FFF;
				const uint minUInt32 = 0x100;
				const uint maxUInt32 = 0x7FFF;
				const long minInt64 = 0x100;
				const long maxInt64 = 0x7FFF;
				const ulong minUInt64 = 0x100;
				const ulong maxUInt64 = 0x7FFF;

				// Act
				target.Add(minInt16);
				target.Add(maxInt16);
				target.Add(minUInt16);
				target.Add(maxUInt16);
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt16 high")]
			public void UInt16HighSpecific() {
				// Arrange
				const Variety expected = Variety.UInt16HighSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const short minInt16 = short.MinValue;
				const short maxInt16 = -1;
				const ushort minUInt16 = 0x8000;
				const ushort maxUInt16 = 0xFFFF;
				const int minInt32 = 0x8000;
				const int maxInt32 = 0xFFFF;
				const uint minUInt32 = 0x8000;
				const uint maxUInt32 = 0xFFFF;
				const long minInt64 = 0x8000;
				const long maxInt64 = 0xFFFF;
				const ulong minUInt64 = 0x8000;
				const ulong maxUInt64 = 0xFFFF;

				// Act
				target.Add(minInt16);
				target.Add(maxInt16);
				target.Add(minUInt16);
				target.Add(maxUInt16);
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt32 low")]
			public void UInt32LowSpecific() {
				// Arrange
				const Variety expected = Variety.UInt32LowSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const int minInt32 = 0x10000;
				const int maxInt32 = 0x7FFFFFFF;
				const uint minUInt32 = 0x10000;
				const uint maxUInt32 = 0x7FFFFFFF;
				const long minInt64 = 0x10000;
				const long maxInt64 = 0x7FFFFFFF;
				const ulong minUInt64 = 0x10000;
				const ulong maxUInt64 = 0x7FFFFFFF;

				// Act
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt32 high")]
			public void UInt32HighSpecific() {
				// Arrange
				const Variety expected = Variety.UInt32HighSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const int minInt32 = int.MinValue;
				const int maxInt32 = -1;
				const uint minUInt32 = 0x80000000;
				const uint maxUInt32 = 0xFFFFFFFF;
				const long minInt64 = 0x80000000;
				const long maxInt64 = 0xFFFFFFFF;
				const ulong minUInt64 = 0x80000000;
				const ulong maxUInt64 = 0xFFFFFFFF;

				// Act
				target.Add(minInt32);
				target.Add(maxInt32);
				target.Add(minUInt32);
				target.Add(maxUInt32);
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt64 low")]
			public void UInt64LowSpecific() {
				// Arrange
				const Variety expected = Variety.UInt64LowSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const long minInt64 = 0x100000000L;
				const long maxInt64 = 0x7FFFFFFFFFFFFFFFL;
				const ulong minUInt64 = 0x100000000UL;
				const ulong maxUInt64 = 0x7FFFFFFFFFFFFFFFUL;

				// Act
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}

			[Fact(DisplayName = "specific range of UInt64 high")]
			public void UInt64HighSpecific() {
				// Arrange
				const Variety expected = Variety.UInt64HighSpecific;
				VarietyChecker target = new VarietyChecker(expected);
				const long minInt64 = long.MinValue;
				const long maxInt64 = -1;
				const ulong minUInt64 = 0x8000000000000000UL;
				const ulong maxUInt64 = 0xFFFFFFFFFFFFFFFFUL;

				// Act
				target.Add(minInt64);
				target.Add(maxInt64);
				target.Add(minUInt64);
				target.Add(maxUInt64);

				// Assert
				Assert.Equal(expected, target.Actual);
			}
		}

		#endregion


		#region NextX

		public class NextX {
			[Fact]
			public void NextByte() {
				// Arrange
				const int dataLen = 1;  // byte
				const int bufLen = 16;  // Random.MinBufLen
				Random random = new Random(null, bufLen);
				const int minRepetition = 50;
				const int maxRepetition = 1024;
				const Variety expected = (Variety.UInt8LowSpecific | Variety.UInt8HighSpecific);
				VarietyChecker checker = new VarietyChecker(expected, minRepetition);

				// Act
				// repeat until the buffer in the Random object is refilled.
				Debug.Assert(bufLen < minRepetition * dataLen);
				for (int i = 0; i < maxRepetition; ++i) {
					if (checker.Add(random.NextByte())) {
						break;
					}
				}

				// Assert
				Assert.Equal(expected, checker.Actual);
			}

			[Fact]
			public void NextInt32() {
				// Arrange
				const int dataLen = 4;  // int
				const int bufLen = 16;  // Random.MinBufLen
				Random random = new Random(null, bufLen);
				const int minRepetition = 32;
				const int maxRepetition = 1024;
				// Only checks 32-bit specific range because numbers in other range rarely appear.
				// ex. the probability to get a number in UInt8LowSpecific is (128 / 2^32). 
				const Variety expected = (Variety.UInt32LowSpecific | Variety.UInt32HighSpecific);
				VarietyChecker checker = new VarietyChecker(expected, minRepetition);

				// Act
				// repeat until the buffer in the Random object is refilled.
				Debug.Assert(bufLen < minRepetition * dataLen);
				for (int i = 0; i < maxRepetition; ++i) {
					if (checker.Add(random.NextInt32())) {
						break;
					}
				}

				// Assert
				Assert.True(checker.HasExpectedVariety());
			}


			[Fact(DisplayName = "refilling buffer at insufficient bytes")]
			public void InsufficientBytes() {
				// Arrange
				const int bufLen = 16;  // Random.MinBufLen
				Random random = new Random(null, bufLen);

				// Act
				// It tests the case when no enough random bytes are available
				// in the buffer in the random object.
				random.NextInt32();	// 4 bytes consumed
				random.NextInt32(); // 8 bytes consumed
				random.NextInt32(); // 12 bytes consumed
				random.NextByte();  // 13 bytes consumed
				random.NextInt32();	// 17 bytes required
				// buffer should be refilled because there is no enough bytes on it 

				// Assert
				// OK if the Act part is executed with no error.
			}
		}

		#endregion


		#region Next

		public class Next {
			[Fact(DisplayName = "requesting an UInt8 range value")]
			public void UInt8Range() {
				// Arrange
				Random random = new Random(null);
				const int limit = 200;
				Debug.Assert(0 < limit && limit <= byte.MaxValue);
				const int repetition = 100;
				const Variety expected = (Variety.UInt8LowSpecific | Variety.UInt8HighSpecific);
				VarietyChecker checker = new VarietyChecker(expected, repetition);

				// Act and Assert
				for (int i = 0; i < repetition; ++i) {
					int actual = random.Next(limit);
					Assert.True(0 <= actual && actual < limit);
					checker.Add(actual);
				}
				Assert.True(checker.HasExpectedVariety());
			}

			[Fact(DisplayName = "requesting an UInt16 range value")]
			public void UInt16Range() {
				// Arrange
				Random random = new Random(null);
				const int limit = 30000;
				Debug.Assert(0 < limit && limit <= short.MaxValue);
				const int repetition = 100;
				const Variety expected = Variety.UInt16LowSpecific;
				VarietyChecker checker = new VarietyChecker(expected, repetition);

				// Act and Assert
				for (int i = 0; i < repetition; ++i) {
					int actual = random.Next(limit);
					Assert.True(0 <= actual && actual < limit);
					checker.Add(actual);
				}
				Assert.True(checker.HasExpectedVariety());
			}

			[Fact(DisplayName = "requesting an UInt32 range value")]
			public void UInt32Range() {
				// Arrange
				Random random = new Random(null);
				const int limit = 1000000;
				Debug.Assert(0 < limit && limit <= int.MaxValue);
				const int repetition = 100;
				const Variety expected = Variety.UInt32LowSpecific;
				VarietyChecker checker = new VarietyChecker(expected, repetition);

				// Act and Assert
				for (int i = 0; i < repetition; ++i) {
					int actual = random.Next(limit);
					Assert.True(0 <= actual && actual < limit);
					checker.Add(actual);
				}
				Assert.True(checker.HasExpectedVariety());
			}
		}

		#endregion


		#region GetPermutation

		public class GetPermutation {
			private static void TestPermutation(int range, Span<int> actual) {
				HashSet<int> items = new HashSet<int>(range);
				for (int i = 0; i < range; ++i) {
					items.Add(i);
				}

				foreach (int item in actual) {
					Assert.True(items.Remove(item), "There is an unexpected item in the actual permutation.");
				}
			}

			private static Span<int> GetDestination(int length) {
				Debug.Assert(0 <= length);

				Span<int> dest = (new int[length]).AsSpan();
				dest.Fill(-1);
				return dest;
			}

			[Fact(DisplayName = "length: full")]
			public void length_full() {
				// Arrange
				Random random = new Random(null);
				const int range = 7;
				const int length = range;	// full

				// Act
				Span<int> actual1 = GetDestination(length);
				Debug.Assert(actual1.Length == length);
				random.GetPermutation(range, actual1);

				Span<int> actual2 = GetDestination(length);
				Debug.Assert(actual2.Length == length);
				random.GetPermutation(actual2);

				Span<int> actual3 = random.GetPermutation(range, length).AsSpan();

				Span<int> actual4 = random.GetPermutation(length).AsSpan();

				// Assert
				TestPermutation(range, actual1);
				TestPermutation(range, actual2);
				Assert.Equal(length, actual3.Length);
				TestPermutation(range, actual3);
				Assert.Equal(length, actual4.Length);
				TestPermutation(range, actual4);
			}

			[Fact(DisplayName = "length: partial")]
			public void length_partial() {
				// Arrange
				Random random = new Random(null);
				const int range = 11;
				const int length = 6;
				Debug.Assert(length < range);	// partial

				// Act
				Span<int> actual1 = GetDestination(length);
				Debug.Assert(actual1.Length == length);
				random.GetPermutation(range, actual1);

				Span<int> actual2 = random.GetPermutation(range, length).AsSpan();

				// Assert
				TestPermutation(range, actual1);
				Assert.Equal(length, actual2.Length);
				TestPermutation(range, actual2);
			}

			[Fact(DisplayName = "length: negative")]
			public void length_negative() {
				// Arrange
				Random random = new Random(null);
				const int range = 3;
				const int length = -1;	// negative

				// Act and Assert
				const string paramName = "length";

				Assert.Throws<ArgumentOutOfRangeException>(paramName, () => {
					random.GetPermutation(range, length);
				});
			}

			[Fact(DisplayName = "length: over")]
			public void length_over() {
				// Arrange
				Random random = new Random(null);
				const int range = 3;
				const int length = 5;
				Debug.Assert(range < length);	// over

				// Act and Assert
				Assert.Throws<ArgumentOutOfRangeException>("dest.Length", () => {
					random.GetPermutation(range, GetDestination(length));
				});
				Assert.Throws<ArgumentOutOfRangeException>("length", () => {
					random.GetPermutation(range, length);
				});
			}
			[Fact(DisplayName = "range: negative")]

			public void range_negative() {
				// Arrange
				Random random = new Random(null);
				const int range = -1;	// negative
				const int length = 2;

				// Act and Assert
				const string paramName = "range";

				Assert.Throws<ArgumentOutOfRangeException>(paramName, () => {
					random.GetPermutation(range, GetDestination(length));
				});
				Assert.Throws<ArgumentOutOfRangeException>(paramName, () => {
					random.GetPermutation(range, length);
				});
				Assert.Throws<ArgumentOutOfRangeException>(paramName, () => {
					random.GetPermutation(range);
				});
			}

		}

		#endregion


		#region Permutate

		public class Permutate {
			private static void TestPermutation<T>(ReadOnlySpan<T> source, Span<T> actual) {
				List<T> items = new List<T>(source.Length);
				foreach (T item in source) {
					items.Add(item);
				}

				foreach (T item in actual) {
					Assert.True(items.Remove(item), "There is an unexpected item in the actual permutation.");
				}
			}

			[Fact(DisplayName = "length: full")]
			public void length_full() {
				// Arrange
				Random random = new Random(null);
				Span<string?> source = new string?[] {
					null, "", "A", "B", "C", "D", "E" 
				}.AsSpan();
				int length = source.Length;	// full

				// Act
				Span<string?> actual1 = (new string?[length]).AsSpan();
				Debug.Assert(actual1.Length == length);
				random.Permutate(source, actual1);

				Span<string?> actual2 = random.Permutate<string?>(source, length);

				Span<string?> actual3 = random.Permutate<string?>(source);

				// Assert
				TestPermutation(source, actual1);
				Assert.Equal(length, actual2.Length);
				TestPermutation(source, actual2);
				Assert.Equal(length, actual3.Length);
				TestPermutation(source, actual3);
			}

			[Fact(DisplayName = "length: partial")]
			public void length_partial() {
				// Arrange
				Random random = new Random(null);
				Span<string?> source = new string?[] {
					null, "", "A", "B", "C", "D", "E"
				}.AsSpan();
				const int length = 4;
				Debug.Assert(length < source.Length);	// partial

				// Act
				Span<string?> actual1 = (new string?[length]).AsSpan();
				Debug.Assert(actual1.Length == length);
				random.Permutate(source, actual1);

				Span<string?> actual2 = random.Permutate<string?>(source, length);

				// Assert
				TestPermutation(source, actual1);
				Assert.Equal(length, actual2.Length);
				TestPermutation(source, actual2);
			}

			[Fact(DisplayName = "length: negative")]
			public void length_negative() {
				// Arrange
				Random random = new Random(null);
				string[] source = new string[] {
					"A", "B", "C"
				};
				const int length = -1;	// negative

				// Act and Assert
				const string paramName = "length";

				Assert.Throws<ArgumentOutOfRangeException>(paramName, () => {
					random.Permutate<string>(source.AsSpan(), length);
				});
			}

			[Fact(DisplayName = "length: over")]
			public void length_toolarge() {
				// Arrange
				Random random = new Random(null);
				string[] source = new string[] {
					"A", "B", "C"
				};
				const int length = 5;
				Debug.Assert(source.Length < length);   // over

				// Act and Assert
				string[] buf =  new string[length];
				Assert.Throws<ArgumentOutOfRangeException>("dest.Length", () => {
					random.Permutate<string>(source, buf.AsSpan());
				});
				Assert.Throws<ArgumentOutOfRangeException>("length", () => {
					random.Permutate<string>(source, length);
				});
			}

			[Fact(DisplayName = "source: has duplication")]
			public void source_duplication() {
				// Arrange
				Random random = new Random(null);
				Span<string?> source = new string?[] {
					null, "", "A", "B", null, "", "A"
				}.AsSpan();	// has duplication
				int length = source.Length; // full

				// Act
				Span<string?> actual1 = (new string?[length]).AsSpan();
				Debug.Assert(actual1.Length == length);
				random.Permutate(source, actual1);

				Span<string?> actual2 = random.Permutate<string?>(source, length);

				Span<string?> actual3 = random.Permutate<string?>(source);

				// Assert
				TestPermutation(source, actual1);
				Assert.Equal(length, actual2.Length);
				TestPermutation(source, actual2);
				Assert.Equal(length, actual3.Length);
				TestPermutation(source, actual3);
			}
		}

		#endregion
	}
}
