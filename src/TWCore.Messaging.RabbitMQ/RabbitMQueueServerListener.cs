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
using System.Threading;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.Server;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TWCore.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ server listener implementation
    /// </summary>
    public class RabbitMQueueServerListener : MQueueServerListenerBase
    {
        #region Fields
        readonly object _lock = new object();
        readonly List<Task> _processingTasks = new List<Task>();
        Type _messageType;
        string _name;
        RabbitMQueue _receiver;
        EventingBasicConsumer _receiverConsumer;
        string _receiverConsumerTag;
        CancellationToken _token;
        Task _monitorTask;
        #endregion

        #region Nested Type
        class RabbitMessage
        {
            public Guid CorrelationId;
            public IBasicProperties Properties;
            public byte[] Body;
        }
        #endregion

        #region .ctor
        /// <summary>
        /// RabbitMQ server listener implementation
        /// </summary>
        /// <param name="connection">Queue server listener</param>
        /// <param name="server">Message queue server instance</param>
        /// <param name="responseServer">true if the server is going to act as a response server</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RabbitMQueueServerListener(MQConnection connection, IMQueueServer server, bool responseServer) : base(connection, server, responseServer)
        {
            _messageType = responseServer ? typeof(ResponseMessage) : typeof(RequestMessage);
            _name = server.Name;
            Core.Status.Attach(collection =>
            {
                collection.Add(nameof(_messageType), _messageType);
            });
        }
        #endregion

        #region Override Methods
        /// <summary>
        /// Start the queue listener for request messages
        /// </summary>
        /// <param name="token">Cancellation token</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override async Task OnListenerTaskStartAsync(CancellationToken token)
        {
            _token = token;
            _receiver = new RabbitMQueue(Connection);
            _receiver.EnsureConnection();
            _receiver.EnsureQueue();
            _receiverConsumer = new EventingBasicConsumer(_receiver.Channel);
            _receiverConsumer.Received += (ch, ea) =>
            {
                var message = new RabbitMessage
                {
                    CorrelationId = Guid.Parse(ea.BasicProperties.CorrelationId),
                    Properties = ea.BasicProperties,
                    Body = ea.Body
                };
                lock (_lock)
                {
                    Counters.IncrementMessages();
                    _processingTasks.Add(Task.Factory.StartNew(ProcessingTask, message, _token).
                        ContinueWith(tsk =>
                        {
                            lock (_lock)
                                _processingTasks.Remove(tsk);
                            Counters.DecrementMessages();
                        }, TaskContinuationOptions.ExecuteSynchronously));
                }
                _receiver.Channel.BasicAck(ea.DeliveryTag, false);
            };
            _receiverConsumerTag = _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
            _monitorTask = Task.Run(MonitorProcess);

            await token.WhenCanceledAsync().ConfigureAwait(false);

            Task[] tasksToWait;
            lock (_lock)
                tasksToWait = _processingTasks.Concat(_monitorTask).ToArray();
            if (tasksToWait.Length > 0)
                Task.WaitAll(tasksToWait, TimeSpan.FromSeconds(Config.RequestOptions.ServerReceiverOptions.ProcessingWaitOnFinalizeInSec));
        }
        /// <summary>
        /// On Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnDispose()
        {
            if (_receiver != null)
            {
                if (!string.IsNullOrEmpty(_receiverConsumerTag))
                    _receiver.Channel.BasicCancel(_receiverConsumerTag);
                _receiver.Close();
                _receiver = null;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Monitors the maximum concurrent message allowed for the listener
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task MonitorProcess()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    if (Counters.CurrentMessages >= Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue)
                    {
                        if (_receiverConsumerTag != null)
                            _receiver.Channel.BasicCancel(_receiverConsumerTag);
                        Core.Log.Warning("Maximum simultaneous messages per queue has been reached, the message needs to wait to be processed, consider increase the MaxSimultaneousMessagePerQueue value, CurrentValue={0}.", Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue);

                        while (!_token.IsCancellationRequested && Counters.CurrentMessages >= Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue)
                            await Task.Delay(1000, _token).ConfigureAwait(false);

                        _receiverConsumerTag = _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
                    }

                    await Task.Delay(100, _token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                    await Task.Delay(2000, _token).ConfigureAwait(false);
                }
            }
        }
        /// <summary>
        /// Process a received message from the queue
        /// </summary>
        /// <param name="obj">Object message instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessingTask(object obj)
        {
            try
            {
                Counters.IncrementProcessingThreads();
                if (obj is RabbitMessage message)
                {
                    Core.Log.LibVerbose("Received {0} bytes from the Queue '{1}'", message.Body.Length, _receiver.Route + "/" + _receiver.Name);
                    var messageBody = ReceiverSerializer.Deserialize(message.Body, _messageType);
                    if (messageBody is RequestMessage request && request.Header != null)
                    {
                        request.Header.ApplicationReceivedTime = Core.Now;
                        Counters.IncrementReceivingTime(request.Header.TotalTime);
                        if (request.Header.ClientName != Config.Name)
                            Core.Log.Warning("The Message Client Name '{0}' is different from the Server Name '{1}'", request.Header.ClientName, Config.Name);
                        var evArgs = new RequestReceivedEventArgs(_name, _receiver, request);
                        if (request.Header.ResponseQueue != null)
                            evArgs.ResponseQueues.Add(request.Header.ResponseQueue);
                        OnRequestReceived(evArgs);
                    }
                    else if (messageBody is ResponseMessage response && response.Header != null)
                    {
                        response.Header.Response.ApplicationReceivedTime = Core.Now;
                        Counters.IncrementReceivingTime(response.Header.Response.TotalTime);
                        OnResponseReceived(new ResponseReceivedEventArgs(_name, response));
                    }
                    Counters.IncrementTotalMessagesProccesed();
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementTotalExceptions();
                Core.Log.Write(ex);
            }
            finally
            {
                Counters.DecrementProcessingThreads();
            }
        }
        #endregion
    }
}