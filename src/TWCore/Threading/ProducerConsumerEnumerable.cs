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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable once UnusedMember.Global
// ReSharper disable InheritdocConsiderUsage

namespace TWCore.Threading
{
    /// <summary>
    /// Producer / Consumer schema enumerable
    /// </summary>
    /// <typeparam name="T">Type of the enumerable</typeparam>
    public class ProducerConsumerEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private readonly object _padlock = new object();
        private readonly Action<ProducerMethods, CancellationToken> _producerAction;
        private readonly List<T> _collection;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ManualResetEventSlim _producerAddEvent;
        private readonly ManualResetEventSlim _producerEndEvent;
        private bool _started;
        private bool _disposed;

        #region .ctor
        /// <summary>
        /// Producer / Consumer schema enumerable
        /// </summary>
        /// <param name="producerAction">Action to perform for the production of data</param>
        public ProducerConsumerEnumerable(Action<ProducerMethods, CancellationToken> producerAction)
        {
            _started = false;
            _producerAction = producerAction;
            _collection = new List<T>();
            _tokenSource = new CancellationTokenSource();
            _producerAddEvent = new ManualResetEventSlim(false);
            _producerEndEvent = new ManualResetEventSlim(true);
        }
        ~ProducerConsumerEnumerable()
        {
            Dispose();
        }
        /// <inheritdoc />
        /// <summary>
        /// Dispose instance
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _producerAddEvent?.Dispose();
            _producerEndEvent?.Dispose();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the producer thread.
        /// </summary>
        public void StartProducer()
        {
            if (_started) return;
            _started = true;
            _producerEndEvent.Reset();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _producerAction?.Invoke(new ProducerMethods(this), _tokenSource.Token);
                }
                catch (Exception ex)
                {
                    TWCore.Core.Log.Write(ex);
                }
                _producerEndEvent.Set();
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        #endregion

        #region Enumerator
        /// <inheritdoc />
        /// <summary>
        /// Enumerator for the produced data.
        /// </summary>
        /// <returns>ConsumerEnumerator instance instance</returns>
        public IEnumerator<T> GetEnumerator() => new ConsumerEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Nested Classes
        /// <summary>
        /// Methods to be called from the producer thread
        /// </summary>
        public class ProducerMethods
        {
            private readonly ProducerConsumerEnumerable<T> _data;

            #region .ctor
            internal ProducerMethods(ProducerConsumerEnumerable<T> data)
            {
                _data = data;
            }
            #endregion

            /// <summary>
            /// Adds an item to the enumerable
            /// </summary>
            /// <param name="item">Item instance</param>
            public void Add(T item)
            {
                lock (_data._padlock)
                {
                    _data._collection.Add(item);
                    _data._producerAddEvent.Set();
                }
            }
            /// <summary>
            /// Adds a IEnumerable to the internal enumerable
            /// </summary>
            /// <param name="enumerable">Enumerable of items</param>
            public void AddRange(IEnumerable<T> enumerable)
            {
                lock (_data._padlock)
                {
                    _data._collection.AddRange(enumerable);
                    _data._producerAddEvent.Set();
                }
            }
        }
        private class ConsumerEnumerator : IEnumerator<T>
        {
            private readonly ProducerConsumerEnumerable<T> _data;
            private int _index;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public ConsumerEnumerator(ProducerConsumerEnumerable<T> data)
            {
                _data = data;
            }

            public bool MoveNext()
            {
                _data.StartProducer();
                if (_data._collection.Count == 0 || _data._collection.Count <= _index)
                {
                    if (_data._producerEndEvent.IsSet)
                        return false;
                    var waitValue = WaitHandle.WaitAny(new[] {
                        _data._producerAddEvent.WaitHandle,
                        _data._producerEndEvent.WaitHandle,
                        _data._tokenSource.Token.WaitHandle
                    });
                    if (waitValue != 0)
                        return false;
                    _data._producerAddEvent.Reset();
                }
                lock (_data._padlock)
                {
                    Current = _data._collection[_index];
                    _index++;
                    return true;
                }
            }

            public void Reset()
            {
                _index = 0;
            }
            public void Dispose()
            {
            }
        }
        #endregion
    }
}