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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 649

namespace TWCore
{
    /// <inheritdoc />
    /// <summary>
    /// Object Pool
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    public sealed class ObjectPool<T> : IPool<T>
    {
        private readonly ConcurrentStack<T> _objectStack;
        private readonly PoolResetMode _resetMode;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Func<ObjectPool<T>, T> _createFunc;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Action<T> _resetAction;

        public int Count => _objectStack.Count;

        /// <summary>
        /// Object pool
        /// </summary>
        /// <param name="createFunc">Function to create a new object</param>
        /// <param name="resetAction">Reset action before storing back the item in the pool</param>
        /// <param name="initialBufferSize">Initial buffer size</param>
        /// <param name="resetMode">Pool reset mode</param>
        /// <param name="dropTimeFrequencyInSeconds">Drop time frequency in seconds</param>
        /// <param name="dropAction">Drop action over the drop item</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectPool(Func<ObjectPool<T>, T> createFunc, Action<T> resetAction = null, int initialBufferSize = 0, PoolResetMode resetMode = PoolResetMode.AfterUse, int dropTimeFrequencyInSeconds = 60, Action<T> dropAction = null)
        {
            _objectStack = new ConcurrentStack<T>();
            _createFunc = createFunc;
            _resetAction = resetAction;
            _resetMode = resetMode;
            if (initialBufferSize > 0)
                Preallocate(initialBufferSize);
            if (dropTimeFrequencyInSeconds > 0)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                dropTimeFrequencyInSeconds = dropTimeFrequencyInSeconds * 1000;
                var token = cancellationTokenSource.Token;
                Task.Delay(dropTimeFrequencyInSeconds, token).ContinueWith(async tsk =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        var count = _objectStack.Count;
                        if (count > 2 && _objectStack.TryPop(out var item))
                            dropAction?.Invoke(item);
                        await Task.Delay(dropTimeFrequencyInSeconds, token).ConfigureAwait(false);                        
                    }
                }, token);
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Preallocate a number of objects on the pool
        /// </summary>
        /// <param name="number">Number of objects to allocate</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Preallocate(int number)
        {
            for (var i = 0; i < number; i++)
                _objectStack.Push(_createFunc(this));
        }
        /// <inheritdoc />
        /// <summary>
        /// Get a new instance from the pool
        /// </summary>
        /// <returns>Object instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T New()
        {
            if (!_objectStack.TryPop(out var value))
                return _createFunc(this);
            if (_resetMode == PoolResetMode.BeforeUse)
                _resetAction?.Invoke(value);
            return value;
        }
        /// <inheritdoc />
        /// <summary>
        /// Store the instance back to the pool
        /// </summary>
        /// <param name="obj">Object to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(T obj)
        {
            if (_resetMode == PoolResetMode.AfterUse)
                _resetAction?.Invoke(obj);
            _objectStack.Push(obj);
        }
        /// <inheritdoc />
        /// <summary>
        /// Get current objects in the pool
        /// </summary>
        /// <returns>IEnumerable with the current objects</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetCurrentObjects()
        {
            return _objectStack.ToReadOnly();
        }
        /// <inheritdoc />
        /// <summary>
        /// Clear the current object stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _objectStack.Clear();
        }
    }

    /// <summary>
    /// Object pool
    /// </summary>
    public sealed class ObjectPool<T, TPoolObjectLifecycle> : IPool<T>
        where TPoolObjectLifecycle : struct, IPoolObjectLifecycle<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly ConcurrentStack<T> _objectStack;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private TPoolObjectLifecycle _allocator;

        /// <summary>
        /// Object pool
        /// </summary>
        public ObjectPool()
        {
            _objectStack = new ConcurrentStack<T>();
            for (var i = 0; i < _allocator.InitialSize; i++)
                _objectStack.Push(_allocator.New());
            if (_allocator.DropTimeFrequencyInSeconds > 0)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var dropTimeFrequencyInSeconds = _allocator.DropTimeFrequencyInSeconds * 1000;
                var token = cancellationTokenSource.Token;
                Task.Delay(dropTimeFrequencyInSeconds, token).ContinueWith(async tsk =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        var count = _objectStack.Count;
                        if (count > 2 && _objectStack.TryPop(out var item))
                            _allocator.DropAction(item);
                        await Task.Delay(dropTimeFrequencyInSeconds, token).ConfigureAwait(false);                        
                    }
                }, token);
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Preallocate a number of objects on the pool
        /// </summary>
        /// <param name="number">Number of objects to allocate</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Preallocate(int number)
        {
            for (var i = 0; i < number; i++)
                _objectStack.Push(_allocator.New());
        }
        /// <inheritdoc />
        /// <summary>
        /// Get a new instance from the pool
        /// </summary>
        /// <returns>Object instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T New()
        {
            if (!_objectStack.TryPop(out var value))
                return _allocator.New();
            if (_allocator.ResetMode == PoolResetMode.BeforeUse)
                _allocator.Reset(value);
            return value;
        }
        /// <inheritdoc />
        /// <summary>
        /// Store the instance back to the pool
        /// </summary>
        /// <param name="obj">Object to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(T obj)
        {
            if (_allocator.ResetMode == PoolResetMode.AfterUse)
                _allocator.Reset(obj);
            _objectStack.Push(obj);
        }
        /// <inheritdoc />
        /// <summary>
        /// Get current objects in the pool
        /// </summary>
        /// <returns>IEnumerable with the current objects</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetCurrentObjects()
        {
            return _objectStack.ToReadOnly();
        }
        /// <inheritdoc />
        /// <summary>
        /// Clear the current object stack
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _objectStack.Clear();
        }
    }
}
