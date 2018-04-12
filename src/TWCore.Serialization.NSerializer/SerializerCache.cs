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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TWCore.Serialization.NSerializer
{
    internal class SerializerCache<T>
    {
        private readonly Dictionary<T, int> _serializationCache;
        private readonly Dictionary<int, T> _deserializationCache;
        private const int MaxIndex = 2047;
        private int _serCurrentIndex;
        private int _desCurrentIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SerializerCache(IEqualityComparer<T> sercomparer = null, IEqualityComparer<int> descomparer = null)
        {
            _serializationCache = new Dictionary<T, int>(sercomparer);
            _deserializationCache = new Dictionary<int, T>(descomparer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_serCurrentIndex > 0)
            {
                _serCurrentIndex = 0;
                _serializationCache.Clear();
                return;
            }
            if (_desCurrentIndex > 0)
            {
                _desCurrentIndex = 0;
                _deserializationCache.Clear();
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SerializerGet(T value)
            => _serializationCache.TryGetValue(value, out var cIdx) ? cIdx : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SerializerTryGetValue(T value, out int index)
            => _serializationCache.TryGetValue(value, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializerSet(T value)
        {
            if (_serCurrentIndex < MaxIndex)
                _serializationCache.Add(value, _serCurrentIndex++);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T DeserializerGet(int index)
            => _deserializationCache[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeserializerSet(T value)
        {
            if (_desCurrentIndex < MaxIndex)
                _deserializationCache.Add(_desCurrentIndex++, value);
        }
    }


    internal class SerializerStringCache
    {
        private readonly Dictionary<string, int> _serializationCache;
        private readonly Dictionary<int, string> _deserializationCache;
        private const int MaxIndex = 2047;
        private int _serCurrentIndex;
        private int _desCurrentIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SerializerStringCache()
        {
            _serializationCache = new Dictionary<string, int>();
            _deserializationCache = new Dictionary<int, string>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_serCurrentIndex > 0)
            {
                _serCurrentIndex = 0;
                _serializationCache.Clear();
                return;
            }
            if (_desCurrentIndex > 0)
            {
                _desCurrentIndex = 0;
                _deserializationCache.Clear();
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SerializerGet(string value)
            => _serializationCache.TryGetValue(value, out var cIdx) ? cIdx : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SerializerTryGetValue(string value, out int index)
            => _serializationCache.TryGetValue(value, out index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializerSet(string value)
        {
            if (_serCurrentIndex < MaxIndex)
                _serializationCache.Add(value, _serCurrentIndex++);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DeserializerGet(int index)
            => _deserializationCache[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeserializerSet(string value)
        {
            if (_desCurrentIndex < MaxIndex)
                _deserializationCache.Add(_desCurrentIndex++, value);
        }
    }
}