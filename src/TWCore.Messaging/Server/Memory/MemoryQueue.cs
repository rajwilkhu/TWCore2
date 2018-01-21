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
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using TWCore.Threading;

namespace TWCore.Messaging
{
    /// <summary>
    /// Memory Queue
    /// </summary>
    public class MemoryQueue
    {
        private readonly ConcurrentQueue<Guid> _messageQueue = new ConcurrentQueue<Guid>();
        private readonly ConcurrentDictionary<Guid, Message> _messageStorage = new ConcurrentDictionary<Guid, Message>();
        private readonly ConcurrentDictionary<Guid, ManualResetEventSlim> _messageEvents = new ConcurrentDictionary<Guid, ManualResetEventSlim>();
        private readonly ManualResetEventSlim _messageQueueEvent = new ManualResetEventSlim();
        
        public class Message
        {
            public Guid CorrelationId;
            public object Value;
        }

        #region Queue Methods
        /// <summary>
        /// Enqueue an object to the queue
        /// </summary>
        /// <param name="correlationId">CorrelationId value</param>
        /// <param name="value">Object value</param>
        /// <returns>true if the item was enqueued; otherwise false.</returns>
        public bool Enqueue(Guid correlationId, object value)
        {
            var message = new Message {CorrelationId = correlationId, Value = value};
            if (!_messageStorage.TryAdd(correlationId, message)) return false;
            _messageQueue.Enqueue(correlationId);
            _messageQueueEvent.Set();
            var mEvent = _messageEvents.GetOrAdd(correlationId, id => new ManualResetEventSlim());
            mEvent.Set();
            return true;
        }
        /// <summary>
        /// Dequeue an object from the queue
        /// </summary>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Object value</returns>
        public Message Dequeue(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_messageQueue.TryDequeue(out var correlationId))
                    {
                        if (!_messageStorage.TryRemove(correlationId, out var message)) continue;
                        _messageEvents.TryRemove(correlationId, out var _);
                        _messageQueueEvent.Reset();
                        return message;
                    }
                    _messageQueueEvent.Wait(250, cancellationToken);
                }
            }
            catch
            {
                //
            }
            return null;
        }
        /// <summary>
        /// Dequeue an object from the queue using the correlationId value
        /// </summary>
        /// <param name="correlationId">CorrelationId value</param>
        /// <param name="waitTime">Time to wait for the value</param>
        /// <param name="cancellationToken">CancellationToken value</param>
        /// <returns>Object value</returns>
        public Message Dequeue(Guid correlationId, int waitTime, CancellationToken cancellationToken)
        {
            try
            {
                var mEvent = _messageEvents.GetOrAdd(correlationId, id => new ManualResetEventSlim());
                if (!mEvent.Wait(waitTime, cancellationToken)) return null;
                if (!_messageStorage.TryRemove(correlationId, out var message)) return null;
                _messageEvents.TryRemove(correlationId, out var _);
                return message;
            }
            catch
            {
                //
            }
            return null;
        }
        #endregion
    }
}