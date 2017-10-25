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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace TWCore.Diagnostics.Status
{
    /// <summary>
    /// Represent a status item value
    /// </summary>
    [DataContract]
    public class StatusItemValue
    {
        /// <summary>
        /// Key of the value
        /// </summary>
        [XmlAttribute, DataMember]
        public string Key { get; set; }
        /// <summary>
        /// Value
        /// </summary>
        [XmlAttribute, DataMember]
        public string Value { get; set; }
        /// <summary>
        /// Value Type
        /// </summary>
        [XmlAttribute, DataMember]
        public StatusItemValueType Type { get; set; }
        /// <summary>
        /// Value status
        /// </summary>
        [XmlAttribute, DataMember]
        public StatusItemValueStatus Status { get; set; }
        /// <summary>
        /// Values list
        /// </summary>
        [XmlElement("Value"), DataMember]
        public List<StatusItemValueItem> Values { get; set; }
        /// <summary>
        /// Enable to plot
        /// </summary>
        [XmlAttribute, DataMember]
        public bool PlotEnabled { get; set; }

        #region .ctor
        /// <summary>
        /// Represent a status item value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusItemValue() { }
        /// <summary>
        /// Represent a status item value
        /// </summary>
        /// <param name="key">Key of the value</param>
        /// <param name="value">Value</param>
        /// <param name="status">Value status</param>
        /// <param name="plotEnabled">Enable to plot</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatusItemValue(string key, object value, StatusItemValueStatus status, bool plotEnabled)
        {
            Key = key;
            if (value == null)
            {
                Value = null;
                Type = StatusItemValueType.Text;
                plotEnabled = false;
            }
            else
            {
                switch (value)
                {
                    case int intValue:
                        Type = StatusItemValueType.Number;
                        Value = intValue.ToString("0.####");
                        break;
                    case decimal decValue:
                        Type = StatusItemValueType.Number;
                        Value = decValue.ToString("0.####");
                        break;
                    case double doubleValue:
                        Type = StatusItemValueType.Number;
                        Value = doubleValue.ToString("0.####");
                        break;
                    case float floatValue:
                        Type = StatusItemValueType.Number;
                        Value = floatValue.ToString("0.####");
                        break;
                    case long longValue:
                        Type = StatusItemValueType.Number;
                        Value = longValue.ToString("0.####");
                        break;
                    case short shortValue:
                        Type = StatusItemValueType.Number;
                        Value = shortValue.ToString("0.####");
                        break;
                    case byte byteValue:
                        Type = StatusItemValueType.Number;
                        Value = byteValue.ToString("0.####");
                        break;
                    case uint intValue:
                        Type = StatusItemValueType.Number;
                        Value = intValue.ToString("0.####");
                        break;
                    case ulong longValue:
                        Type = StatusItemValueType.Number;
                        Value = longValue.ToString("0.####");
                        break;
                    case ushort shortValue:
                        Type = StatusItemValueType.Number;
                        Value = shortValue.ToString("0.####");
                        break;
                    case sbyte byteValue:
                        Type = StatusItemValueType.Number;
                        Value = byteValue.ToString("0.####");
                        break;
                    case DateTime date:
                        Type = StatusItemValueType.Date;
                        Value = date.TimeOfDay.TotalSeconds > 0 ? date.ToString("s") : date.ToString("yyyy-MM-dd");
                        break;
                    case TimeSpan time:
                        Type = StatusItemValueType.Time;
                        Value = time.ToString();
                        break;
                    default:
                        Type = StatusItemValueType.Text;
                        Value = value.ToString();
                        plotEnabled = false;
                        break;
                }
            }
            Status = status;
            PlotEnabled = plotEnabled;
        }
        #endregion
    }
}
