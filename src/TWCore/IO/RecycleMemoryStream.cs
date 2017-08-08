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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TWCore.IO
{
    /// <summary>
    /// Recycle ByteArray MemoryStream
    /// </summary>
    public class RecycleMemoryStream : Stream
    {
		static Queue<List<byte[]>> _lstPool = new Queue<List<byte[]>>();
        static long _lstPoolCount = 0;
        static Queue<byte[]> _pool = new Queue<byte[]>();
        static long _poolCount = 0;
        static int _maxLength = 255;
        bool _canWrite = true;
        int _length = 0;
        int _maxRow = 0;
        int _rowIndex = 0;
        int _position = 0;
        List<byte[]> _buffer;
        byte[] _currentBuffer;

        #region Properties
        /// <summary>
        ///  Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;
        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => true;
        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => _canWrite;
        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length => ((_buffer.Count - 1) * _maxLength) + _length;
        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_rowIndex * _maxLength) + _position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var val = ((double)value / _maxLength);
                _rowIndex = (int)val;
                _position = (int)((val - _rowIndex) * _maxLength);
                _currentBuffer = _buffer[_rowIndex];
            }
        }
        #endregion

        #region .ctor
        /// <summary>
        /// Recycle ByteArray MemoryStream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecycleMemoryStream() : this(null, 0, 0, true) { }
        /// <summary>
        /// Recycle ByteArray MemoryStream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecycleMemoryStream(byte[] buffer) : this(buffer, 0, buffer?.Length ?? 0, true) { }
        /// <summary>
        /// Recycle ByteArray MemoryStream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecycleMemoryStream(byte[] buffer, bool writable) : this(buffer, 0, buffer?.Length ?? 0, writable) { }
        /// <summary>
        /// Recycle ByteArray MemoryStream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecycleMemoryStream(byte[] buffer, int index, int count) : this(buffer, index, count, true) { }
        /// <summary>
        /// Recycle ByteArray MemoryStream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecycleMemoryStream(byte[] buffer, int index, int count, bool writable)
        {
            _currentBuffer = GetArray();
			_buffer = GetList();
			_buffer.Add(_currentBuffer);
            _maxRow = 0;
            if (buffer != null)
                Write(buffer, index, count);
            _canWrite = writable;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ~RecycleMemoryStream()
        {
            Dispose(true);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (_buffer != null)
            {
				_currentBuffer = null;
                foreach (var array in _buffer)
                    StoreArray(array);
				StoreList(_buffer);
				_buffer = null;
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Abstract Override Methods
        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Flush() { }
        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin)
        {
            var length = ((_buffer.Count - 1) * _maxLength) + _length;
            var currentPosition = (_rowIndex * _maxLength) + _position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    currentPosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    currentPosition += (int)offset;
                    break;
                case SeekOrigin.End:
                    currentPosition = length + (int)offset;
                    break;
            }
            Position = currentPosition;
            return currentPosition;
        }
        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetLength(long value)
        {
            var val = ((double)value / _maxLength);
            var rowIndex = (int)val;
            var nRows = rowIndex + 1;
            var nlength = (int)((val - rowIndex) * _maxLength);
            if (nRows == _buffer.Count)
            {
                if (_length > nlength)
                    Array.Clear(_currentBuffer, nlength - 1, _length - nlength);
                return;
            }
            if (nRows < _buffer.Count)
            {
                while (nRows < _buffer.Count)
                {
                    StoreArray(_currentBuffer);
                    var lastIdx = _buffer.Count - 1;
                    _currentBuffer = _buffer[lastIdx];
                    _buffer.RemoveAt(lastIdx);
                    _length = nlength;
                }
                Array.Clear(_currentBuffer, _length - 1, _maxLength - _length);
            }
            if (nRows > _buffer.Count)
            {
                for (var i = _buffer.Count; i < nRows; i++)
                {
                    _currentBuffer = GetArray();
                    _buffer.Add(_currentBuffer);
                    _maxRow++;
                }
            }
        }
        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
		{
			int total = 0;
			while (count > 0)
			{
				var clength = (_rowIndex == _maxRow) ? _length : _maxLength;
				var remain = clength - _position;
				if (remain == 0)
				{
					if (_rowIndex < _maxRow)
					{
						_rowIndex++;
						_currentBuffer = _buffer[_rowIndex];
						_position = 0;
						remain = (_rowIndex == _maxRow) ? _length : _maxLength;
					}
					else
						return total;
				}
				var canRead = remain < count ? remain : count;
				Buffer.BlockCopy(_currentBuffer, _position, buffer, offset, canRead);
				count -= canRead;
				offset += canRead;
				total += canRead;
				_position += canRead;
			}
			return total;
		}
        /// <summary>
        ///  When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				var remain = _maxLength - _position;
				if (remain == 0)
				{
					_currentBuffer = GetArray();
					_buffer.Add(_currentBuffer);
					_maxRow++;
					_rowIndex++;
					_position = 0;
					_length = 0;
					remain = _maxLength;
				}
				var canWrite = remain < count ? remain : count;
				Buffer.BlockCopy(buffer, offset, _currentBuffer, _position, canWrite);
				count -= canWrite;
				offset += canWrite;
				_position += canWrite;
				if (_position > _length) _length = _position;
			}
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte value)
        {
			if (_maxLength > _position)
			{
				_currentBuffer[_position] = value;
				_position++;
				if (_position > _length) _length = _position;
				return;
			}
			_currentBuffer = GetArray();
			_buffer.Add(_currentBuffer);
			_maxRow++;
			_rowIndex++;
			_position = 1;
			_length = 1;
			_currentBuffer[0] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int ReadByte()
        {
			if (_rowIndex == _maxRow)
			{
				if (_length > _position)
					return _currentBuffer[_position++];
				return -1;
			}
			if (_maxLength > _position)
				return _currentBuffer[_position++];
			if (_rowIndex < _maxRow)
			{
				_rowIndex++;
				_currentBuffer = _buffer[_rowIndex];
				_position = 1;
				return _currentBuffer[0];
			}
			return -1;
        }


        #endregion

        #region Pool Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] GetArray()
        {
            if (Interlocked.Read(ref _poolCount) > 0)
                lock (_pool)
                    return _pool.Dequeue();
            return new byte[_maxLength];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void StoreArray(byte[] array)
        {
            Array.Clear(array, 0, _maxLength);
            lock(_pool)
                _pool.Enqueue(array);
            Interlocked.Increment(ref _poolCount);
        }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static List<byte[]> GetList()
		{
            if (Interlocked.Read(ref _lstPoolCount) > 0)
                lock (_lstPool)
                    return _lstPool.Dequeue();
            return new List<byte[]>(10);
        }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void StoreList(List<byte[]> lst)
		{
			lst.Clear();
            lock(_lstPool)
                _lstPool.Enqueue(lst);
            Interlocked.Increment(ref _lstPoolCount);
        }
        #endregion
    }
}
