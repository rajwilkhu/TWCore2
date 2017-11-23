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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using NATS.Client;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.Exceptions;
using System.Threading.Tasks;
using TWCore.Messaging.RawClient;
using TWCore.Threading;

// ReSharper disable NotAccessedField.Local
// ReSharper disable InconsistentNaming

namespace TWCore.Messaging.NATS
{
    /// <inheritdoc />
    /// <summary>
    /// NATS Queue Raw Client
    /// </summary>
    public class NATSQueueRawClient : MQueueRawClientBase
    {
        private static readonly ConcurrentDictionary<Guid, NATSQueueMessage> ReceivedMessages = new ConcurrentDictionary<Guid, NATSQueueMessage>();
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false);

        #region Fields
        private ConnectionFactory _factory;
        private List<(MQConnection, ObjectPool<IConnection>)> _senders;
        private IConnection _receiverNASTConnection;
        private IAsyncSubscription _receiver;
        private MQConnection _receiverConnection;
        private MQClientQueues _clientQueues;
        private MQClientSenderOptions _senderOptions;
        private MQClientReceiverOptions _receiverOptions;
        #endregion

        #region Properties
        /// <summary>
        /// Use Single Response Queue
        /// </summary>
        public bool UseSingleResponseQueue { get; private set; }
        #endregion

        #region Nested Type
        private class NATSQueueMessage
        {
            public Guid CorrelationId;
            public SubArray<byte> Body;
            public readonly AsyncManualResetEvent WaitHandler = new AsyncManualResetEvent(false);
            public IConnection Connection;
            public IAsyncSubscription Consumer;
            public string Route;
            public string Name;
        }
        private static void MessageHandler(object sender, MsgHandlerEventArgs e)
        {
            (var body, var correlationId, var _) = GetFromRawMessageBody(e.Message.Data);
            var rMsg = ReceivedMessages.GetOrAdd(correlationId, cId => new NATSQueueMessage());
            rMsg.CorrelationId = correlationId;
            rMsg.Body = body;
            rMsg.WaitHandler.Set();
        }
        #endregion

        #region .ctor
        /// <inheritdoc />
        /// <summary>
        /// NATS Queue Raw Client
        /// </summary>
        public NATSQueueRawClient()
        {
            _factory = new ConnectionFactory();
            System.Net.ServicePointManager.DefaultConnectionLimit = 500;
        }
        #endregion


        #region Init and Dispose Methods
        /// <inheritdoc />
        /// <summary>
        /// On client initialization
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnInit()
        {
            OnDispose();
            _senders = new List<(MQConnection, ObjectPool<IConnection>)>();
            _receiver = null;


            if (Config != null)
            {
                if (Config.ClientQueues != null)
                {
                    _clientQueues = Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName?.SplitAndTrim(",").Contains(Core.EnvironmentName) == true && c.MachineName?.SplitAndTrim(",").Contains(Core.MachineName) == true)
                                    ?? Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName?.SplitAndTrim(",").Contains(Core.EnvironmentName) == true)
                                    ?? Config.ClientQueues.FirstOrDefault(c => c.MachineName?.SplitAndTrim(",").Contains(Core.MachineName) == true)
                                    ?? Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName.IsNullOrWhitespace());
                }
                _senderOptions = Config.RequestOptions?.ClientSenderOptions;
                _receiverOptions = Config.ResponseOptions?.ClientReceiverOptions;
                UseSingleResponseQueue = _receiverOptions?.Parameters?[ParameterKeys.SingleResponseQueue].ParseTo(false) ?? false;

                if (_clientQueues?.SendQueues?.Any() == true)
                {
                    foreach (var queue in _clientQueues.SendQueues)
                    {
                        _senders.Add((queue, new ObjectPool<IConnection>(pool =>
                        {
                            Core.Log.LibVerbose("New Producer from QueueClient");
                            return _factory.CreateConnection(queue.Route);
                        }, null, 1)));
                    }
                }
                if (_clientQueues?.RecvQueue != null)
                {
                    _receiverConnection = _clientQueues.RecvQueue;
                    if (UseSingleResponseQueue)
                    {
                        _receiverNASTConnection = _factory.CreateConnection(_receiverConnection.Route);
                        _receiver = _receiverNASTConnection.SubscribeAsync(_receiverConnection.Name, new EventHandler<MsgHandlerEventArgs>(MessageHandler));
                    }
                }
            }

            Core.Status.Attach(collection =>
            {
                if (_senders != null)
                    for (var i = 0; i < _senders.Count; i++)
                        collection.Add(nameof(_senders) + " {0} Path".ApplyFormat(i), _senders[i].Item1.Route);
                if (_clientQueues?.RecvQueue != null)
                    collection.Add(nameof(_receiver) + " Path", _clientQueues.RecvQueue.Route);
            });
        }
        /// <inheritdoc />
        /// <summary>
        /// On Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnDispose()
        {
            if (_senders != null)
            {
                foreach (var sender in _senders)
                {
                    var pool = sender.Item2;
                    foreach (var conn in pool.GetCurrentObjects())
                        conn.Dispose();
                    sender.Item2.Clear();
                }
                _senders.Clear();
                _senders = null;
            }
            if (_receiver == null) return;
            if (UseSingleResponseQueue)
            {
                _receiver.Unsubscribe();
                _receiverNASTConnection.Dispose();
            }
            _receiver = null;
            _factory = null;
        }
        #endregion

        #region Send Method
        /// <inheritdoc />
        /// <summary>
        /// On Send message data
        /// </summary>
        /// <param name="message">Request message instance</param>
        /// <param name="correlationId">Message CorrelationId</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool OnSend(byte[] message, Guid correlationId)
        {
            if (_senders?.Any() != true)
                throw new NullReferenceException("There aren't any senders queues.");
            if (_senderOptions == null)
                throw new ArgumentNullException("SenderOptions");

            var recvQueue = _clientQueues.RecvQueue;
            var name = recvQueue.Name;
            if (!UseSingleResponseQueue)
                name += "_" + correlationId;

            var body = CreateRawMessageBody(message, correlationId, name);

            foreach ((var queue, var producerPool) in _senders)
            {
                Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", body.Length, queue.Route + "/" + queue.Name, correlationId);
                var producer = producerPool.New();
                producer.Publish(queue.Name, body);
                producerPool.Store(producer);
            }
            Core.Log.LibVerbose("Message with CorrelationId={0} sent", correlationId);
            return true;
        }
        #endregion

        #region Receive Method
        /// <inheritdoc />
        /// <summary>
        /// On Receive message data
        /// </summary>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response message instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override async Task<byte[]> OnReceiveAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            if (_receiver == null && UseSingleResponseQueue)
                throw new NullReferenceException("There is not receiver queue.");
            if (_receiverOptions == null)
                throw new ArgumentNullException("SenderOptions");

            var timeout = TimeSpan.FromSeconds(_receiverOptions.TimeoutInSec);
            var sw = Stopwatch.StartNew();
            var message = ReceivedMessages.GetOrAdd(correlationId, cId => new NATSQueueMessage());

            if (!UseSingleResponseQueue)
            {
                message.Name = _receiverConnection.Name + "_" + correlationId;
                message.Route = _receiverConnection.Route;
                message.Connection = _factory.CreateConnection(message.Route);
                message.Consumer = message.Connection.SubscribeAsync(message.Name, new EventHandler<MsgHandlerEventArgs>(MessageHandler));
                var waitResult = await message.WaitHandler.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
                message.Consumer.Unsubscribe();
                message.Connection.Close();
                message.Consumer = null;
                message.Consumer = null;

                if (!waitResult) throw new MessageQueueTimeoutException(timeout, correlationId.ToString());

                Core.Log.LibVerbose("Received {0} bytes from the Queue '{1}' with CorrelationId={2}", message.Body.Count, _clientQueues.RecvQueue.Name, correlationId);
                Core.Log.LibVerbose("Correlation Message ({0}) received at: {1}ms", correlationId, sw.Elapsed.TotalMilliseconds);
                sw.Stop();
                return (byte[])message.Body;
            }

            if (!await message.WaitHandler.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                throw new MessageQueueTimeoutException(timeout, correlationId.ToString());

            ReceivedMessages.TryRemove(correlationId, out var _);
            Core.Log.LibVerbose("Received {0} bytes from the Queue '{1}' with CorrelationId={2}", message.Body.Count, _clientQueues.RecvQueue.Name, correlationId);
            Core.Log.LibVerbose("Correlation Message ({0}) received at: {1}ms", correlationId, sw.Elapsed.TotalMilliseconds);
            sw.Stop();
            return (byte[])message.Body;
        }
        #endregion

        #region Static Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] CreateRawMessageBody(SubArray<byte> message, Guid correlationId, string name)
        {
            var nameBytes = Encoding.GetBytes(name);
            var nameLength = nameBytes.Length;
            var body = new byte[16 + 4 + nameLength + message.Count];
            Buffer.BlockCopy(correlationId.ToByteArray(), 0, body, 0, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(nameLength), 0, body, 16, 4);
            nameBytes.CopyTo(body, 20);
            message.CopyTo(body, 20 + nameLength);
            return body;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (SubArray<byte>, Guid, string) GetFromRawMessageBody(byte[] message)
        {
            var body = new SubArray<byte>(message);
            var correlationId = new Guid((byte[])body.Slice(0, 16));
            var nameLength = BitConverter.ToInt32(message, 16);
            var name = Encoding.GetString(message, 20, nameLength);
            var messageBody = body.Slice(20 + nameLength);
            return (messageBody, correlationId, name);
        }
        #endregion
    }
}