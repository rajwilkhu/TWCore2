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
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TWCore.Messaging.Client;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.Exceptions;
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Global

namespace TWCore.Messaging.RabbitMQ
{
    /// <inheritdoc />
    /// <summary>
    /// RabbitMQ Queue Client
    /// </summary>
    public class RabbitMQueueClient : MQueueClientBase
    {
        private static readonly ConcurrentDictionary<Guid, RabbitResponseMessage> ReceivedMessages = new ConcurrentDictionary<Guid, RabbitResponseMessage>();
        private readonly ConcurrentDictionary<string, ObjectPool<RabbitMQueue>> _routeConnection = new ConcurrentDictionary<string, ObjectPool<RabbitMQueue>>();
        private readonly ConcurrentDictionary<Guid, string> _correlationIdConsumers = new ConcurrentDictionary<Guid, string>();

        #region Fields
        private List<RabbitMQueue> _senders;
        private RabbitMQueue _receiver;
        private EventingBasicConsumer _receiverConsumer;
        private string _receiverConsumerTag;
        private MQClientQueues _clientQueues;
        private MQClientSenderOptions _senderOptions;
        private MQClientReceiverOptions _receiverOptions;
        private long _receiverThreads;
        private Action _receiverStopBuffered;
        #endregion

        #region Properties
        /// <summary>
        /// Use Single Response Queue
        /// </summary>
        public bool UseSingleResponseQueue { get; private set; }
        #endregion

        #region Nested Type
        private class RabbitResponseMessage
        {
            public Guid CorrelationId;
            public IBasicProperties Properties;
            public byte[] Body;
            public readonly ManualResetEventSlim WaitHandler = new ManualResetEventSlim(false);
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
            _senders = new List<RabbitMQueue>();
            _receiver = null;
            _receiverStopBuffered = ActionDelegate.Create(RemoveReceiverConsumer).CreateBufferedAction(2000);
            
            if (Config != null)
            {
                if (Config.ClientQueues != null)
                {
                    _clientQueues = Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName?.SplitAndTrim(",").Contains(Core.EnvironmentName) == true && c.MachineName?.SplitAndTrim(",").Contains(Core.MachineName) == true)
                                    ?? Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName?.SplitAndTrim(",").Contains(Core.EnvironmentName) == true)
                                    ?? Config.ClientQueues.FirstOrDefault(c => c.EnvironmentName.IsNullOrWhitespace());
                }
                _senderOptions = Config.RequestOptions?.ClientSenderOptions;
                _receiverOptions = Config.ResponseOptions?.ClientReceiverOptions;
                UseSingleResponseQueue = _receiverOptions?.Parameters?[ParameterKeys.SingleResponseQueue].ParseTo(false) ?? false;

                if (_clientQueues != null)
                {
                    if (_clientQueues.SendQueues?.Any() == true)
                    {
                        foreach (var queue in _clientQueues.SendQueues)
                        {
                            var rabbitQueue = new RabbitMQueue(queue);
                            rabbitQueue.EnsureConnection();
                            rabbitQueue.EnsureExchange();
                            _senders.Add(rabbitQueue);
                        }
                    }

                    if (_clientQueues.RecvQueue != null)
                        _receiver = new RabbitMQueue(_clientQueues.RecvQueue);
                }
            }

            Core.Status.Attach(collection =>
            {
                if (_senders != null)
                    for (var i = 0; i < _senders.Count; i++)
                    {
                        if (_senders[i]?.Factory == null) continue;
                        collection.Add(nameof(_senders) + " {0} Path".ApplyFormat(i), _senders[i].Factory.HostName);
                    }
                if (_receiver?.Factory != null)
                    collection.Add(nameof(_receiver) + " Path", _receiver.Factory.HostName);
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
                    sender.Close();
                }
                _senders.Clear();
                _senders = null;
            }
            if (_receiver != null)
            {
                RemoveReceiverConsumer();
                _receiver = null;
            }
            foreach (var pools in _routeConnection)
            {
                var pool = pools.Value;
                var connections = pool.GetCurrentObjects();
                foreach (var connection in connections)
                    connection.Close();
                pool.Clear();
            }
            _routeConnection.Clear();
        }
        #endregion

        #region Send Method
        /// <inheritdoc />
        /// <summary>
        /// On Send message data
        /// </summary>
        /// <param name="message">Request message instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool OnSend(RequestMessage message)
        {
            if (_senders?.Any() != true)
                throw new NullReferenceException("There aren't any senders queues.");
            if (_senderOptions == null)
                throw new ArgumentNullException("SenderOptions");

            var recvQueue = _clientQueues.RecvQueue;
            if (message.Header.ResponseQueue == null)
            {
                if (recvQueue != null)
                {
                    message.Header.ResponseQueue = new MQConnection(recvQueue.Route, recvQueue.Name) { Parameters = recvQueue.Parameters };
                    message.Header.ResponseExpected = true;
                    message.Header.ResponseTimeoutInSeconds = _receiverOptions?.TimeoutInSec ?? -1;
                    if (!UseSingleResponseQueue)
                    {
                        message.Header.ResponseQueue.Name += "_" + message.CorrelationId;
                        var pool = _routeConnection.GetOrAdd(_receiver.Route, r => new ObjectPool<RabbitMQueue>(p => new RabbitMQueue(_receiver)));
                        var cReceiver = pool.New();
                        cReceiver.EnsureConnection();
                        cReceiver.Channel.QueueDeclare(message.Header.ResponseQueue.Name, false, false, true, null);
                        pool.Store(cReceiver);
                    }
                }
                else
                {
                    message.Header.ResponseExpected = false;
                    message.Header.ResponseTimeoutInSeconds = -1;
                }
            }

            var correlationId = message.CorrelationId.ToString();
            var data = SenderSerializer.Serialize(message);
            var replyTo = message.Header.ResponseQueue?.Name;
            var priority = (byte)(_senderOptions.MessagePriority == MQMessagePriority.High ? 9 :
                _senderOptions.MessagePriority == MQMessagePriority.Low ? 1 : 5);
            var expiration = (_senderOptions.MessageExpirationInSec * 1000).ToString();
            var deliveryMode = (byte)(_senderOptions.Recoverable ? 2 : 1);

            foreach (var sender in _senders)
            {
                if (!sender.EnsureConnection()) continue;
                sender.EnsureExchange();
                var props = sender.Channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyTo;
                props.Priority = priority;
                props.Expiration = expiration;
                props.AppId = Core.ApplicationName;
                props.ContentType = SenderSerializer.MimeTypes[0];
                props.DeliveryMode = deliveryMode;
                props.Type = _senderOptions.Label;
                Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", data.Count, sender.Route + "/" + sender.Name, message.Header.CorrelationId);
                sender.Channel.BasicPublish(sender.ExchangeName ?? string.Empty, sender.Name, props, (byte[])data);
                sender.AutoClose();
            }
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
        protected override ResponseMessage OnReceive(Guid correlationId, CancellationToken cancellationToken)
        {
            if (_receiver == null)
                throw new NullReferenceException("There is not receiver queue.");
            if (_receiverOptions == null)
                throw new ArgumentNullException("SenderOptions");

            Interlocked.Increment(ref _receiverThreads);

            var timeout = TimeSpan.FromSeconds(_receiverOptions.TimeoutInSec);
            var sw = Stopwatch.StartNew();
            var message = ReceivedMessages.GetOrAdd(correlationId, cId => new RabbitResponseMessage());

            if (UseSingleResponseQueue)
            {
                CreateReceiverConsumer();
            }
            else
            {
                var recName = _receiver.Name + "_" + correlationId;
                var pool = _routeConnection.GetOrAdd(_receiver.Route, r => new ObjectPool<RabbitMQueue>(p => new RabbitMQueue(_receiver)));
                var cReceiver = pool.New();
                cReceiver.EnsureConnection();
                cReceiver.Channel.QueueDeclare(recName, false, false, true, null);
                var tmpConsumer = new EventingBasicConsumer(cReceiver.Channel);
                tmpConsumer.Received += (ch, ea) =>
                {
                    var crId = Guid.Parse(ea.BasicProperties.CorrelationId);
                    var rMessage = ReceivedMessages.GetOrAdd(crId, cId => new RabbitResponseMessage());
                    rMessage.CorrelationId = crId;
                    rMessage.Body = ea.Body;
                    rMessage.Properties = ea.BasicProperties;
                    rMessage.WaitHandler.Set();
                    cReceiver.Channel.BasicAck(ea.DeliveryTag, false);
                    if (_correlationIdConsumers.TryGetValue(crId, out var consumerTag))
                        cReceiver.Channel.BasicCancel(consumerTag);
                    cReceiver.Channel.QueueDeleteNoWait(recName);
                    cReceiver.AutoClose();
                    pool.Store(cReceiver);
                };
                _correlationIdConsumers.TryAdd(correlationId, cReceiver.Channel.BasicConsume(recName, false, tmpConsumer));
            }

            if (!message.WaitHandler.Wait(timeout, cancellationToken))
                throw new MessageQueueTimeoutException(timeout, correlationId.ToString());
            if (message.Body == null)
                throw new MessageQueueNotFoundException("The Message can't be retrieved, null body on CorrelationId = " + correlationId.ToString());

            Core.Log.LibVerbose("Received {0} bytes from the Queue '{1}' with CorrelationId={2}", message.Body.Length, _clientQueues.RecvQueue.Name, correlationId);
            var response = ReceiverSerializer.Deserialize<ResponseMessage>(message.Body);
            Core.Log.LibVerbose("Correlation Message ({0}) received at: {1}ms", correlationId, sw.Elapsed.TotalMilliseconds);
            sw.Stop();

            if (Interlocked.Decrement(ref _receiverThreads) <= 0)
                _receiverStopBuffered();

            return response;
        }
        #endregion

        #region Private Methods
        private void CreateReceiverConsumer()
        {
            if (_receiverConsumer != null) return;
            if (_receiver == null) return;
            lock (_receiver)
            {
                _receiver.EnsureConnection();
                _receiver.EnsureQueue();
                _receiverConsumer = new EventingBasicConsumer(_receiver.Channel);
                _receiverConsumer.Received += (ch, ea) =>
                {
                    var correlationId = Guid.Parse(ea.BasicProperties.CorrelationId);
                    var message = ReceivedMessages.GetOrAdd(correlationId,
                        cId => new RabbitResponseMessage());
                    message.CorrelationId = correlationId;
                    message.Body = ea.Body;
                    message.Properties = ea.BasicProperties;
                    message.WaitHandler.Set();
                    _receiver.Channel.BasicAck(ea.DeliveryTag, false);
                };
                _receiverConsumerTag =
                    _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
                Core.Log.LibVerbose("The Receiver for the queue \"{0}\" has been created.", _receiver.Name);
            }
        }
        private void RemoveReceiverConsumer()
        {
            if (Interlocked.Read(ref _receiverThreads) > 0) return;
            if (_receiver == null) return;
            if (!string.IsNullOrEmpty(_receiverConsumerTag))
            {
                _receiver.Channel.BasicCancel(_receiverConsumerTag);
                _receiverConsumerTag = null;
            }
            _receiverConsumer = null;
            _receiver.Close();
            Core.Log.LibVerbose("The Receiver for the queue \"{0}\" has been disposed.", _receiver.Name);
        }
        #endregion
    }
}