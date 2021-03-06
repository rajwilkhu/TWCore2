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

using TWCore.Threading;

// ReSharper disable EventNeverSubscribedTo.Global

namespace TWCore.Messaging.Client
{
    /// <summary>
    /// General Message Queue Client Events
    /// </summary>
    public static class MQueueClientEvents
    {
        /// <summary>
        /// Events that fires when a request message is sent
        /// </summary>
        public static AsyncEvent<RequestSentEventArgs> OnRequestSent { get; set; }
        /// <summary>
        /// Events that fires when a request message is about to be sent
        /// </summary>
        public static AsyncEvent<RequestSentEventArgs> OnBeforeSendRequest { get; set; }
        /// <summary>
        /// Events that fires when a response message is received
        /// </summary>
        public static AsyncEvent<ResponseReceivedEventArgs> OnResponseReceived { get; set; }
    }
}
