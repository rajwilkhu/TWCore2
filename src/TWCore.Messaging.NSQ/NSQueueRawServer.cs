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
using System.Linq;
using NsqSharp;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.RawServer;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace TWCore.Messaging.NSQ
{
    /// <inheritdoc />
    /// <summary>
    /// NSQ Raw Server Implementation
    /// </summary>
    public class NSQueueRawServer : MQueueRawServerBase
    {
        private readonly NonBlocking.ConcurrentDictionary<string, ObjectPool<Producer>> _rQueue = new NonBlocking.ConcurrentDictionary<string, ObjectPool<Producer>>();

        #region .ctor
        /// <summary>
        /// NSQ Server Implementation
        /// </summary>
        public NSQueueRawServer()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 500;
        }
        #endregion

        /// <inheritdoc />
        /// <summary>
        /// On Create all server listeners
        /// </summary>
        /// <param name="connection">Queue server listener</param>
        /// <param name="responseServer">true if the server is going to act as a response server</param>
        /// <returns>IMQueueServerListener</returns>
        protected override IMQueueRawServerListener OnCreateQueueServerListener(MQConnection connection, bool responseServer = false)
            => new NSQueueRawServerListener(connection, this, responseServer);

        /// <inheritdoc />
        /// <summary>
        /// On Send message data
        /// </summary>
        /// <param name="message">Response message instance</param>
        /// <param name="e">Event Args</param>
        protected override async Task<int> OnSendAsync(SubArray<byte> message, RawRequestReceivedEventArgs e)
        {
            var queues = e.ResponseQueues;
            queues.Add(new MQConnection
            {
                Route = e.Sender.Route,
                Parameters = e.Sender.Parameters
            });

            var senderOptions = Config.ResponseOptions.ServerSenderOptions;
            if (senderOptions == null)
                throw new ArgumentNullException("ServerSenderOptions");

            var body = NSQueueRawClient.CreateRawMessageBody(message, e.CorrelationId, e.Metadata["ReplyTo"]);
            var replyTo = e.Metadata["ReplyTo"];

            var response = true;
            foreach (var queue in e.ResponseQueues)
            {
                try
                {
                    var nsqProducerPool = _rQueue.GetOrAdd(queue.Route, q => new ObjectPool<Producer>(pool =>
                    {
                        Core.Log.LibVerbose("New Producer from RawQueueServer");
                        return new Producer(q);
                    }, null, 1));
                    var nsqProducer = nsqProducerPool.New();

                    if (!string.IsNullOrEmpty(replyTo))
                    {
                        if (string.IsNullOrEmpty(queue.Name))
                        {
                            Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", body.Length, queue.Route + "/" + replyTo, e.CorrelationId);
                            await nsqProducer.PublishAsync(replyTo, body).ConfigureAwait(false);
                        }
                        else if (queue.Name.StartsWith(replyTo, StringComparison.Ordinal))
                        {
                            Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", body.Length, queue.Route + "/" + queue.Name + "_" + replyTo, e.CorrelationId);
                            await nsqProducer.PublishAsync(queue.Name + "_" + replyTo, body).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", body.Length, queue.Route + "/" + queue.Name, e.CorrelationId);
                        await nsqProducer.PublishAsync(queue.Name, body).ConfigureAwait(false);
                    }

                    nsqProducerPool.Store(nsqProducer);
                }
                catch (Exception ex)
                {
                    response = false;
                    Core.Log.Write(ex);
                }
            }
            return response ? message.Count : -1;
        }

        /// <inheritdoc />
        /// <summary>
        /// On Dispose
        /// </summary>
        protected override void OnDispose()
        {
            var producers = _rQueue.SelectMany(i => i.Value.GetCurrentObjects()).ToArray();
            Parallel.ForEach(producers, p => p.Stop());
            foreach (var sender in _rQueue)
                sender.Value.Clear();
            _rQueue.Clear();
        }
    }
}