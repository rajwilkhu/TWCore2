﻿/*
Copyright 2015-2017 Daniel Adrian Redondo Suarez

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


// ReSharper disable CheckNamespace

namespace TWCore
{
    /// <summary>
    /// Extensions for numbers data types
    /// </summary>
    public static partial class Extensions
    {
        #region ReadeableLength
        /// <summary>
        /// Format the length to a human readeable style format.
        /// </summary>
        /// <param name="bytes">Bytes length to format</param>
        /// <returns>Human readeable style format</returns>
        public static string ToReadeableBytes(this long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
            long max = (long)System.Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }
        /// <summary>
        /// Format the length to a human readeable style format.
        /// </summary>
        /// <param name="bytes">Bytes length to format</param>
        /// <returns>Human readeable style format</returns>
        public static string ToReadeableBytes(this double bytes) => ToReadeableBytes((long)bytes);
        /// <summary>
        /// Format the length to a human readeable style format.
        /// </summary>
        /// <param name="bytes">Bytes length to format</param>
        /// <returns>Human readeable style format</returns>
        public static string ToReadeableBytes(this float bytes) => ToReadeableBytes((long)bytes);
        /// <summary>
        /// Format the length to a human readeable style format.
        /// </summary>
        /// <param name="bytes">Bytes length to format</param>
        /// <returns>Human readeable style format</returns>
        public static string ToReadeableBytes(this int bytes) => ToReadeableBytes((long)bytes);
        #endregion

        #region Converts
        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="degrees">Degrees value</param>
        /// <returns>Radians value</returns>
        public static double ToRad(this double degrees) => degrees * (System.Math.PI / 180);
        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="radians">Radians value</param>
        /// <returns>Degrees value</returns>
        public static double ToDegrees(this double radians) => radians * 180 / System.Math.PI;
        /// <summary>
        /// Converts radians to bearing
        /// </summary>
        /// <param name="radians">Radians value</param>
        /// <returns>Bearing value</returns>
        public static double ToBearing(this double radians) => (ToDegrees(radians) + 360) % 360;
        #endregion
    }
}
