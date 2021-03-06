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

namespace TWCore.Diagnostics.Status
{
    /// <inheritdoc />
    /// <summary>
    /// Attribute to define an item to show in the status library
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StatusPropertyAttribute : Attribute
    {
        /// <summary>
        /// Name to show in the status library
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Status to show in the status library
        /// </summary>
        public StatusItemValueStatus Status { get; private set; }
        /// <summary>
        /// Enable for plot
        /// </summary>
        public bool PlotEnabled { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Attribute to define an item to show in the status library
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusPropertyAttribute()
        {
        }
        /// <inheritdoc />
        /// <summary>
        /// Attribute to define an item to show in the status library
        /// </summary>
        /// <param name="name">Name to show in the status library</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusPropertyAttribute(string name)
        {
            Name = name;
        }
        /// <inheritdoc />
        /// <summary>
        /// Attribute to define an item to show in the status library
        /// </summary>
        /// <param name="name">Name to show in the status library</param>
        /// <param name="status">Status to show in the status library</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusPropertyAttribute(string name, StatusItemValueStatus status)
        {
            Name = name;
            Status = status;
        }
        /// <inheritdoc />
        /// <summary>
        /// Attribute to define an item to show in the status library
        /// </summary>
        /// <param name="name">Name to show in the status library</param>
        /// <param name="status">Status to show in the status library</param>
        /// <param name="plotEnabled">Enable for plot</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusPropertyAttribute(string name, StatusItemValueStatus status, bool plotEnabled)
        {
            Name = name;
            Status = status;
            PlotEnabled = plotEnabled;
        }
        /// <inheritdoc />
        /// <summary>
        /// Attribute to define an item to show in the status library
        /// </summary>
        /// <param name="name">Name to show in the status library</param>
        /// <param name="plotEnabled">Enable for plot</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusPropertyAttribute(string name, bool plotEnabled)
        {
            Name = name;
            PlotEnabled = plotEnabled;
        }
    }
}
