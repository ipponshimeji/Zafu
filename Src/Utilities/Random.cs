using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace Zafu.Utilities {
    public class Random: IDisposable {
        #region types

        private class DisposableData {
            #region data

            public readonly RandomNumberGenerator Generator;

            public readonly byte[] Buf;

            #endregion

            #region creation and disposal

            public DisposableData(RandomNumberGenerator generator, int bufLen) {
                // argument checks
                Debug.Assert(generator != null);
                Debug.Assert(0 < bufLen);

                // initialize members
                this.Generator = generator;
                this.Buf = new byte[bufLen];
            }

            public void Dispose() {
                // dispose this.Generator
                this.Generator.Dispose();
            }

            #endregion
        }

        #endregion


        #region data

        public const int MinBufLen = 16;

        public const int MaxBufLen = 1024 * 1024;   // 1M

        public const int DefaultBufLen = 64;


        private readonly object instanceLocker = new object();

        private DisposableData? disposableData = null;

        private int nextIndex = 0;

        #endregion


        #region properties

        public RandomNumberGenerator Generator {
            get {
                return GetDisposableData().Generator;
            }
        }


        private byte[] Buf {
            get {
                return GetDisposableData().Buf;
            }
        }

        #endregion


        #region creation and disposal

        public Random(RandomNumberGenerator? generator, int bufLen = DefaultBufLen) {
            // argument checks
            if (generator == null) {
                generator = RandomNumberGenerator.Create();
            }
            if (bufLen < MinBufLen || MaxBufLen < bufLen) {
                throw new ArgumentOutOfRangeException(nameof(bufLen));
            }

            // initialize members
            this.disposableData = new DisposableData(generator, bufLen);
            this.nextIndex = bufLen;
        }

        public virtual void Dispose() {
            // dispose this.generator
            DisposableData? data = Interlocked.Exchange(ref this.disposableData, null);
            if (data != null) {
                data.Dispose();
            }
        }

        #endregion


        #region methods

        // Special version for byte.
        public byte NextByte() {
            lock (this.instanceLocker) {
                return NextByteInternal();
            }
        }

        public int NextInt32() {
            lock (this.instanceLocker) {
                return NextInt32Internal();
            }
        }

        public int Next(int limit) {
            // argument checks
            if (limit <= 0) {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            // get a positive random value
            int random;
            lock (this.instanceLocker) {
                if (limit <= Byte.MaxValue) {
                    random = NextByteInternal();
                } else if (limit <= Int16.MaxValue) {
                    random = NextInt16Internal();
                } else {
                    random = NextInt32Internal();
                }
            }

            if (random < 0) {
                random = -random;
            }
            return random % limit;
        }


        // Selects length integers from [0, range) randomly.
        public void GetPermutation(int range, Span<int> dest) {
            // argument checks
            if (range < 0) {
                throw new ArgumentOutOfRangeException(nameof(range));
            }
            if (range < dest.Length) {
                throw new ArgumentOutOfRangeException($"{nameof(dest)}.Length");
            }

            GetPermutationInternal(range, dest);
        }

        public void GetPermutation(Span<int> dest) {
            GetPermutationInternal(dest.Length, dest);
        }

        public int[] GetPermutation(int range, int length) {
            // argument checks
            if (range < 0) {
                throw new ArgumentOutOfRangeException(nameof(range));
            }
            if (length < 0 || range < length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int[] result = new int[length];
            GetPermutationInternal(range, result.AsSpan());
            return result;
        }

        public int[] GetPermutation(int length) {
            return GetPermutation(length, length);
        }

        public void Permutate<T>(ReadOnlySpan<T> source, Span<T> dest) {
            // argument checks
            if (source.Length < dest.Length) {
                throw new ArgumentOutOfRangeException($"{nameof(dest)}.Length");
            }

            int[] indexes = GetPermutation(source.Length, dest.Length);
            for (int i = 0; i < dest.Length; ++i) {
                dest[i] = source[indexes[i]];
            }
        }

        public T[] Permutate<T>(ReadOnlySpan<T> source, int length) {
            // argument checks
            if (length < 0 || source.Length < length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            T[] result = new T[length];
            Permutate<T>(source, result.AsSpan());
            return result;
        }

        public T[] Permutate<T>(ReadOnlySpan<T> source) {
            return Permutate<T>(source, source.Length);
        }

        #endregion


        #region privates

        private DisposableData GetDisposableData() {
            DisposableData? data = this.disposableData;
            if (data == null) {
                // this object was disposed
                throw new ObjectDisposedException(null);
            }

            return data;
        }

        private byte[] ReserveRandomBytes(int len) {
            // argument checks
            DisposableData data = GetDisposableData();
            byte[] buf = data.Buf;
            Debug.Assert(0 < len && len <= buf.Length);

            // has enough random bytes?
            if (buf.Length - len < this.nextIndex) {
                // refill random bytes
                data.Generator.GetBytes(buf);
                this.nextIndex = 0;
            }
            Debug.Assert(this.nextIndex <= buf.Length - len);

            return buf;
        }

        private byte NextByteInternal() {
            byte[] buf = ReserveRandomBytes(1);
            return buf[this.nextIndex++];
        }

        private short NextInt16Internal() {
            const int len = 2;
            byte[] buf = ReserveRandomBytes(len);
            short value = BitConverter.ToInt16(buf, this.nextIndex);
            this.nextIndex += len;
            return value;
        }

        private int NextInt32Internal() {
            const int len = 4;
            byte[] buf = ReserveRandomBytes(len);
            int value = BitConverter.ToInt32(buf, this.nextIndex);
            this.nextIndex += len;
            return value;
        }

        private long NextInt64Internal() {
            const int len = 8;
            byte[] buf = ReserveRandomBytes(len);
            long value = BitConverter.ToInt64(buf, this.nextIndex);
            this.nextIndex += len;
            return value;
        }

        // Selects length integers from [0, range) randomly.
        private void GetPermutationInternal(int range, Span<int> dest) {
            // checks
            Debug.Assert(0 <= range);
            Debug.Assert(dest.Length <= range);

            // create a pool of source integers
            int[] pool = new int[range];
            for (int i = 0; i < range; ++i) {
                pool[i] = i;
            }

            int poolLimit = range;
            for (int i = 0; i < dest.Length; ++i) {
                // select an integer from the pool
                int nextIndex = Next(poolLimit);
                dest[i] = pool[nextIndex];

                // reduce the pool
                --poolLimit;
                if (nextIndex < poolLimit) {
                    pool[nextIndex] = pool[poolLimit];
                }
            }

            return;
        }

        #endregion
    }
}
