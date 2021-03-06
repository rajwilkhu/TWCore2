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
using System.Runtime.Serialization;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TWCore.Net.RPC
{
    /// <inheritdoc />
    /// <summary>
    /// Push message type
    /// </summary>
	[Serializable, DataContract]
    public class RPCPushMessage : RPCMessage
    {
        /// <summary>
        /// Message scope
        /// </summary>
		[DataMember]
        public RPCMessageScope Scope { get; set; }
        /// <summary>
        /// Message description
        /// </summary>
		[DataMember]
        public string Description { get; set; }
        /// <summary>
        /// Push data
        /// </summary>
		[DataMember]
        public object Data { get; set; }
    }
}
