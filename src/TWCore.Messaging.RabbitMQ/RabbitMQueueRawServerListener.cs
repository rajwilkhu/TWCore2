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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.RawServer;

namespace TWCore.Messaging.RabbitMQ
{
	/// <summary>
	/// RabbitMQ server listener implementation
	/// </summary>
	public class RabbitMQueueRawServerListener : MQueueRawServerListenerBase
    {
        #region Fields
        readonly ConcurrentDictionary<Task, object> _processingTasks = new ConcurrentDictionary<Task, object>();
        readonly object _lock = new object();
        string _name;
        RabbitMQueue _receiver;
        EventingBasicConsumer _receiverConsumer;
        string _receiverConsumerTag;
        CancellationToken _token;
        Task _monitorTask;
        bool _exceptionSleep = false;
        #endregion

        #region Nested Type
        class RabbitMessage
        {
            public Guid CorrelationId;
            public IBasicProperties Properties;
            public SubArray<byte> Body;
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
        public RabbitMQueueRawServerListener(MQConnection connection, IMQueueRawServer server, bool responseServer) : base(connection, server, responseServer)
        {
            _name = server.Name;
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
                Counters.IncrementMessages();
                var tsk = Task.Factory.StartNew(ProcessingTask, message, _token);
                _processingTasks.TryAdd(tsk, null);
                tsk.ContinueWith(_tsk =>
                {
                    _processingTasks.TryRemove(tsk, out var ts);
                    Counters.DecrementMessages();
                });
                _receiver.Channel.BasicAck(ea.DeliveryTag, false);
            };
            _receiverConsumerTag = _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
            _monitorTask = Task.Run(MonitorProcess, _token);

            await token.WhenCanceledAsync().ConfigureAwait(false);
            if (_receiverConsumerTag != null)
                _receiver.Channel.BasicCancel(_receiverConsumerTag);
            _receiver.Close();

            Task[] tasksToWait;
            lock (_lock)
                tasksToWait = _processingTasks.Keys.Concat(_monitorTask).ToArray();
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
                    _receiver.Channel?.BasicCancel(_receiverConsumerTag);
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
                    bool exSleep = false;
                    lock (_lock)
                        exSleep = _exceptionSleep;
                    if (exSleep)
                    {
                        if (_receiverConsumerTag != null)
                            _receiver.Channel.BasicCancel(_receiverConsumerTag);
                        Core.Log.Warning("An exception has been thrown, the listener has been stoped for {0} seconds.", Config.RequestOptions.ServerReceiverOptions.SleepOnExceptionInSec);
                        await Task.Delay(Config.RequestOptions.ServerReceiverOptions.SleepOnExceptionInSec * 1000, _token).ConfigureAwait(false);
                        lock (_lock)
                            _exceptionSleep = false;
                        _receiverConsumerTag = _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
                    }

                    if (Counters.CurrentMessages >= Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue)
                    {
                        if (_receiverConsumerTag != null)
                            _receiver.Channel.BasicCancel(_receiverConsumerTag);
                        Core.Log.Warning("Maximum simultaneous messages per queue has been reached, the message needs to wait to be processed, consider increase the MaxSimultaneousMessagePerQueue value, CurrentValue={0}.", Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue);

                        while (!_token.IsCancellationRequested && Counters.CurrentMessages >= Config.RequestOptions.ServerReceiverOptions.MaxSimultaneousMessagesPerQueue)
                            await Task.Delay(500, _token).ConfigureAwait(false);

                        _receiverConsumerTag = _receiver.Channel.BasicConsume(_receiver.Name, false, _receiverConsumer);
                    }

                    await Task.Delay(100, _token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    Core.Log.Write(ex);
                    if (!_token.IsCancellationRequested)
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
                    Core.Log.LibVerbose("Received {0} bytes from the Queue '{1}'", message.Body.Count, _receiver.Route + "/" + _receiver.Name);
                    Counters.IncrementTotalReceivingBytes(message.Body.Count);
                    if (ResponseServer)
                    {
                        var evArgs = new RawResponseReceivedEventArgs(_name, message.Body, message.CorrelationId);
                        evArgs.Metadata["AppId"] = message.Properties.AppId;
                        evArgs.Metadata["ContentEncoding"] = message.Properties.ContentEncoding;
                        evArgs.Metadata["ContentType"] = message.Properties.ContentType;
                        evArgs.Metadata["DeliveryMode"] = message.Properties.DeliveryMode.ToString();
                        evArgs.Metadata["Expiration"] = message.Properties.Expiration;
                        evArgs.Metadata["Priority"] = message.Properties.Priority.ToString();
                        evArgs.Metadata["Timestamp"] = message.Properties.Timestamp.ToString();
                        evArgs.Metadata["Type"] = message.Properties.Type;
                        evArgs.Metadata["UserId"] = message.Properties.UserId;
                        evArgs.Metadata["ReplyTo"] = message.Properties.ReplyTo;
                        evArgs.Metadata["MessageId"] = message.Properties.MessageId;
                        OnResponseReceived(evArgs);
                    }
                    else
                    {
                        var evArgs = new RawRequestReceivedEventArgs(_name, _receiver, message.Body, message.CorrelationId);
                        evArgs.Metadata["AppId"] = message.Properties.AppId;
                        evArgs.Metadata["ContentEncoding"] = message.Properties.ContentEncoding;
                        evArgs.Metadata["ContentType"] = message.Properties.ContentType;
                        evArgs.Metadata["DeliveryMode"] = message.Properties.DeliveryMode.ToString();
                        evArgs.Metadata["Expiration"] = message.Properties.Expiration;
                        evArgs.Metadata["Priority"] = message.Properties.Priority.ToString();
                        evArgs.Metadata["Timestamp"] = message.Properties.Timestamp.ToString();
                        evArgs.Metadata["Type"] = message.Properties.Type;
                        evArgs.Metadata["UserId"] = message.Properties.UserId;
                        evArgs.Metadata["ReplyTo"] = message.Properties.ReplyTo;
                        evArgs.Metadata["MessageId"] = message.Properties.MessageId;
                        OnRequestReceived(evArgs);
                    }
                    Counters.IncrementTotalMessagesProccesed();
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementTotalExceptions();
                Core.Log.Write(ex);
                lock (_lock)
                    _exceptionSleep = true;
            }
            finally
            {
                Counters.DecrementProcessingThreads();
            }
        }
        #endregion
    }
}