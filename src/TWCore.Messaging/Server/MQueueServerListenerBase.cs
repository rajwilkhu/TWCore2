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
using System.Threading;
using System.Threading.Tasks;
using TWCore.Messaging.Configuration;
using TWCore.Serialization;
// ReSharper disable SuggestBaseTypeForParameter

namespace TWCore.Messaging.Server
{
    /// <inheritdoc />
    /// <summary>
    /// Message queue server listener base
    /// </summary>
    public abstract class MQueueServerListenerBase : IMQueueServerListener
    {
        #region Properties
        /// <inheritdoc />
        /// <summary>
        /// Message queue connection
        /// </summary>
        public MQConnection Connection { get; protected set; }
        /// <inheritdoc />
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public MQPairConfig Config { get; protected set; }
        /// <inheritdoc />
        /// <summary>
        /// Message queue listener server counters
        /// </summary>
        public MQServerCounters Counters { get; }
        /// <inheritdoc />
        /// <summary>
        /// Gets or sets the receiver serializer
        /// </summary>
        public ISerializer ReceiverSerializer { get; set; }
        /// <inheritdoc />
        /// <summary>
        /// Gets if the server is configured as response server
        /// </summary>
        public bool ResponseServer { get; }
        #endregion

        #region Events
        /// <summary>
        /// Events that fires when a request message is received
        /// </summary>
        public event EventHandler<RequestReceivedEventArgs> RequestReceived;
        /// <summary>
        /// Events that fires when a response message is received
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;
		#endregion

		#region .ctor
		/// <summary>
		/// Message queue server listener base
		/// </summary>
		/// <param name="connection">Queue server listener</param>
		/// <param name="server">Message queue server instance</param>
		/// <param name="responseServer">true if the server is going to act as a response server</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected MQueueServerListenerBase(MQConnection connection, IMQueueServer server, bool responseServer)
        {
            Connection = connection;
            Config = server.Config;
            Counters = new MQServerCounters();
            ReceiverSerializer = server.ReceiverSerializer;
            ResponseServer = responseServer;

            Core.Status.Attach(collection =>
            {
                collection.Add("Connection Route:", Connection?.Route);
                collection.Add("Connection Name:", Connection?.Name);
                collection.Add(nameof(ResponseServer), ResponseServer);
                Core.Status.AttachChild(Counters, this);
            });
        }
		#endregion

		#region Public Methods
		/// <inheritdoc />
		/// <summary>
		/// Start the queue listener for request messages
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <returns>Task of the method execution</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Task TaskStartAsync(CancellationToken token)
        {
            return OnListenerTaskStartAsync(token);
        }
		/// <inheritdoc />
		/// <summary>
		/// Dispose all resources
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
        {
            OnDispose();
            Core.Status.DeAttachObject(this);
        }
		#endregion

		#region Abstract Methods
		/// <summary>
		/// Start the queue listener for request messages
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <returns>Task of the method execution</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract Task OnListenerTaskStartAsync(CancellationToken token);
		/// <summary>
		/// On Dispose
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract void OnDispose();
        #endregion

        #region Protected Methods
        /// <summary>
        /// Fires the RequestReceived event
        /// </summary>
        /// <param name="requestReceived">Request received event args</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnRequestReceived(RequestReceivedEventArgs requestReceived)
            => RequestReceived?.Invoke(this, requestReceived);
		/// <summary>
		/// Fires the ResponseReceived event
		/// </summary>
		/// <param name="responseReceived">Response received event args</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void OnResponseReceived(ResponseReceivedEventArgs responseReceived)
            => ResponseReceived?.Invoke(this, responseReceived);
        #endregion
    }
}
