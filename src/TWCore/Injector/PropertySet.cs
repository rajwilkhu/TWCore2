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

using System.Runtime.Serialization;
using System.Xml.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InheritdocConsiderUsage
// ReSharper disable ClassNeverInstantiated.Global

namespace TWCore.Injector
{
    /// <summary>
    /// Instance property set
    /// </summary>
    [DataContract]
    public class PropertySet : Argument
    {
        /// <summary>
        /// Global argument name
        /// </summary>
        [XmlAttribute, DataMember]
        public string ArgumentName { get; set; }
    }
}
