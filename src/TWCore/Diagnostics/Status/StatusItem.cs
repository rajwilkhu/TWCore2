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
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TWCore.Diagnostics.Status
{
    /// <summary>
    /// Status Item
    /// </summary>
    [DataContract]
    public class StatusItem
    {
        /// <summary>
        /// Item Id
        /// </summary>
        [XmlAttribute, DataMember]
        public string Id { get; set; }
        /// <summary>
        /// Item name
        /// </summary>
        [XmlAttribute, DataMember]
        public string Name { get; set; }
        /// <summary>
        /// Item values
        /// </summary>
        [XmlElement("Value"), DataMember]
        public StatusItemValuesCollection Values { get; set; } = new StatusItemValuesCollection();
        /// <summary>
        /// Children
        /// </summary>
        [DataMember]
        public List<StatusItem> Children { get; set; } = new List<StatusItem>();
    }
}
