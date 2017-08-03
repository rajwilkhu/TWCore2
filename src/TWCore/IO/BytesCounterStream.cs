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
using System.IO;
using System.Runtime.CompilerServices;

namespace TWCore.IO
{
    /// <summary>
    /// Stream decorator with transfered bytes count
    /// </summary>
    public class BytesCounterStream : Stream
    {
        long _bytesRead;
        long _bytesWrite;

        #region Properties
        /// <summary>
        /// Base stream decorated
        /// </summary>
        public Stream BaseStream { get; set; }
        /// <summary>
        ///  Bytes read
        /// </summary>
        public long BytesRead => _bytesRead;
        /// <summary>
        /// Bytes write
        /// </summary>
        public long BytesWrite => _bytesWrite;
        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => BaseStream.CanSeek;
        /// <summary>
        ///  Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => BaseStream.CanRead;
        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => BaseStream.CanWrite;
        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length => BaseStream.Length;
        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BaseStream.Position;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                BaseStream.Position = value;
            }
        }
        #endregion

        #region .ctor
        /// <summary>
        /// Stream decorator with transfered bytes count
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BytesCounterStream(Stream baseStream)
        {
            BaseStream = baseStream;
        }
        #endregion

        #region Public Methods
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
            int res = BaseStream.Read(buffer, offset, count);
            if (res > 0)
                _bytesRead += res;
            return res;
        }
        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int ReadByte()
        {
            int res = BaseStream.ReadByte();
            if (res >= 0)
                _bytesRead += res;
            return res;
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
            BaseStream.Write(buffer, offset, count);
            _bytesWrite += count;
        }
        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte value)
        {
            BaseStream.WriteByte(value);
            _bytesWrite++;
        }
        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Flush()
        {
            BaseStream.Flush();
        }
        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetLength(long value) => BaseStream.SetLength(value);
        /// <summary>
        /// Clear Bytes counter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBytesCounters()
        {
            _bytesRead = 0;
            _bytesWrite = 0;
        }
        #endregion
    }
}
