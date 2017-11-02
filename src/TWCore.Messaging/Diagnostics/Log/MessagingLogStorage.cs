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
using System.Collections.Generic;
using System.Threading;
using TWCore.Messaging.Client;
using TWCore.Services;
// ReSharper disable ImpureMethodCallOnReadonlyValueField

// ReSharper disable UnusedMember.Global

namespace TWCore.Diagnostics.Log.Storages
{
    /// <inheritdoc />
    /// <summary>
    /// Messaging log storage
    /// </summary>
    public class MessagingLogStorage : ILogStorage
    {
        private readonly object _locker = new object();
        private readonly IMQueueClient _queueClient;
        private readonly Timer _timer;
        private readonly List<ILogItem> _logItems;
        private readonly LogLevel _logLevels;

        #region .ctor
        /// <summary>
        /// Messaging log storage
        /// </summary>
        /// <param name="queueName">Queue pair config name</param>
        /// <param name="periodInSeconds">Fetch period in seconds</param>
        /// <param name="logLevels">Log levels to register</param>
        public MessagingLogStorage(string queueName, int periodInSeconds, LogLevel logLevels)
        {
            _queueClient = Core.Services.GetQueueClient(queueName);
            _logItems = new List<ILogItem>();
            _logLevels = logLevels;
            var period = TimeSpan.FromSeconds(periodInSeconds);
            _timer = new Timer(TimerCallback, this, period, period);
        }
        #endregion
        
        #region Public methods
        /// <inheritdoc />
        /// <summary>
        /// Writes a log item to the storage
        /// </summary>
        /// <param name="item">Log Item</param>
        public void Write(ILogItem item)
        {
            if (!_logLevels.HasFlag(item.Level)) return;
            lock (_locker)
            {
                _logItems.Add(item);
            }
        }
        /// <inheritdoc />
        /// <summary>
        /// Writes a log item empty line
        /// </summary>
        public void WriteEmptyLine()
        {
        }
        /// <inheritdoc />
        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
            TimerCallback(this);
            _queueClient.Dispose();
        }
        #endregion
        
        #region Private methods
        private static void TimerCallback(object state)
        {
            try
            {
                var mStatus = (MessagingLogStorage) state;
                List<ILogItem> itemsToSend;
                lock (mStatus._locker)
                {
                    if (mStatus._logItems.Count == 0) return;
                    itemsToSend = new List<ILogItem>(mStatus._logItems);
                    mStatus._logItems.Clear();
                }
                Core.Log.LibDebug("Sending {0} log items to the diagnostic queue.", itemsToSend.Count);
                mStatus._queueClient.Send(itemsToSend);
            }
            catch (Exception ex)
            {
                Core.Log.Write(ex);
            }
        }
        #endregion
    }
}