﻿/*
Copyright 2015-2018 Daniel Adrian Redondo Suarez

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TWCore
{
    /// <inheritdoc />
    /// <summary>
    /// Byte array comparer
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        /// <summary>
        /// Byte Array comparer instace
        /// </summary>
        public static ByteArrayComparer Instance = new ByteArrayComparer();

        private ByteArrayComparer()
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">Byte array to compare</param>
        /// <param name="y">Byte array to compare</param>
        /// <returns>true if both arrays has the same items; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool Equals(byte[] x, byte[] y)
        {
            if (x == y)
                return true;
            if (x == null || y == null || x.Length != y.Length)
                return false;
            fixed (byte* bytes1 = x, bytes2 = y)
            {
                var len = x.Length;
                var rem = len % (sizeof(long) * 16);
                var b1 = (long*)bytes1;
                var b2 = (long*)bytes2;
                var e1 = (long*)(bytes1 + len - rem);

                while (b1 < e1)
                {
                    if (*(b1) != *(b2) || *(b1 + 1) != *(b2 + 1) ||
                        *(b1 + 2) != *(b2 + 2) || *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) || *(b1 + 5) != *(b2 + 5) ||
                        *(b1 + 6) != *(b2 + 6) || *(b1 + 7) != *(b2 + 7) ||
                        *(b1 + 8) != *(b2 + 8) || *(b1 + 9) != *(b2 + 9) ||
                        *(b1 + 10) != *(b2 + 10) || *(b1 + 11) != *(b2 + 11) ||
                        *(b1 + 12) != *(b2 + 12) || *(b1 + 13) != *(b2 + 13) ||
                        *(b1 + 14) != *(b2 + 14) || *(b1 + 15) != *(b2 + 15))
                        return false;
                    b1 += 16;
                    b2 += 16;
                }

                for (var i = 0; i < rem; i++)
                    if (x[len - 1 - i] != y[len - 1 - i])
                        return false;
                return true;
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="data">Byte array to get the hash code</param>
        /// <returns>Byte array hash value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(byte[] data)
        {
            var res = 0x2D2816FE;
            var step = (data.Length / 64) + 1;
            for (var i = 0; i < data.Length; i += step)
                res = res * 31 + data[i];
            return res;
        }
    }
}
