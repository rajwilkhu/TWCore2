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
using System.Runtime.CompilerServices;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace TWCore.Messaging.Server
{
    /// <inheritdoc />
    /// <summary>
    /// Event args for response sent event
    /// </summary>
    public class ResponseSentEventArgs : EventArgs
    {
        /// <summary>
        /// Client name
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Response message received
        /// </summary>
        public ResponseMessage Message { get; }
	    /// <summary>
	    /// Message Length
	    /// </summary>
	    public int MessageLength { get; set;  }
	    
		/// <inheritdoc />
		/// <summary>
		/// Event args for response received event
		/// </summary>
		/// <param name="name">Client name</param>
		/// <param name="message">Response message received</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ResponseSentEventArgs(string name, ResponseMessage message)
        {
            Name = name;
            Message = message;
        }
    }
}
