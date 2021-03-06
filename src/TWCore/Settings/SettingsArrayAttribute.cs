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

using System;
using System.Runtime.CompilerServices;

namespace TWCore.Settings
{
    /// <inheritdoc />
    /// <summary>
    /// Array attribute attribute for Settings parser
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingsArrayAttribute : Attribute
    {
        /// <summary>
        /// String separator
        /// </summary>
        public char[] Separators { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Array attribute attribute for Settings parser
        /// </summary>
        /// <param name="separators">String separator</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SettingsArrayAttribute(char[] separators)
        {
            Separators = separators;
        }

        /// <inheritdoc />
        /// <summary>
        /// Array attribute attribute for Settings parser
        /// </summary>
        /// <param name="separator">String separator</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SettingsArrayAttribute(char separator)
        {
            Separators = new[] { separator };
        }

        /// <inheritdoc />
        /// <summary>
        /// Array attribute attribute for Settings parser
        /// </summary>
        /// <param name="separator">String separator</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SettingsArrayAttribute(string separator)
        {
            Separators = separator?.ToCharArray();
        }
    }
}
