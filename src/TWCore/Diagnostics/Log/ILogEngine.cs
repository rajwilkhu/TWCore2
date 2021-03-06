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
using TWCore.Diagnostics.Log.Storages;
// ReSharper disable ParameterTypeCanBeEnumerable.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace TWCore.Diagnostics.Log
{
    /// <inheritdoc />
    /// <summary>
    /// Interface for Log Engines
    /// </summary>
    public interface ILogEngine : IDisposable
    {
        /// <summary>
        /// Log storages items
        /// </summary>
        LogStorageCollection Storage { get; }
        /// <summary>
        /// Max log level to register in logs
        /// </summary>
        LogLevel MaxLogLevel { get; set; }
        /// <summary>
        /// Gets or sets the log item factory
        /// </summary>
        CreateLogItemDelegate ItemFactory { get; set; }
        /// <summary>
        /// Get all pending to write log items
        /// </summary>
        /// <returns>Pending log items array</returns>
        ILogItem[] GetPendingItems();
        /// <summary>
        /// Enqueue to write the log items to the log storages
        /// </summary>
        /// <param name="items">Log items range</param>
        void EnqueueItemsArray(ILogItem[] items);
        
        #region Write
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="item">Log item</param>
        void Write(ILogItem item);
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="code">Item code</param>
        /// <param name="message">Item message</param>
        /// <param name="groupName">Item group category name</param>
        /// <param name="ex">Related exception if is available</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <param name="typeName">Type name</param>
        void Write(LogLevel level, string code, string message, string groupName, Exception ex = null, string assemblyName = null, string typeName = null);
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="code">Item code</param>
        /// <param name="message">Item message</param>
        /// <param name="ex">Related exception if is available</param>
        void Write(LogLevel level, string code, string message, Exception ex = null);
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Item message</param>
        /// <param name="ex">Related exception if is available</param>
        void Write(LogLevel level, string message, Exception ex = null);
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="ex">Related exception if is available</param>
        void Write(LogLevel level, Exception ex);
        /// <summary>
        /// Write a log item into the log storages
        /// </summary>
        /// <param name="ex">Related exception if is available</param>
        void Write(Exception ex);
        /// <summary>
        /// Write a log empty line
        /// </summary>
        void WriteEmptyLine();
        #endregion
        
        
        
        #region Debug Methods
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void Debug(string message);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Debug(string message, object arg1);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Debug(string message, object arg1, object arg2);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Debug(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Debug(string message, params object[] args);
        #endregion
        
        #region Verbose Methods
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void Verbose(string message);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Verbose(string message, object arg1);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Verbose(string message, object arg1, object arg2);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Verbose(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Verbose(string message, params object[] args);
        #endregion
        
        #region Error Methods
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Item message with pattern support</param>
        void Error(Exception ex, string message);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Error(Exception ex, string message, object arg1);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Error(Exception ex, string message, object arg1, object arg2);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Error(Exception ex, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Error(Exception ex, string message, params object[] args);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void Error(string message);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Error(string message, object arg1);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Error(string message, object arg1, object arg2);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Error(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Error(string message, params object[] args);
        #endregion
        
        #region Warning Methods
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void Warning(string message);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Warning(string message, object arg1);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Warning(string message, object arg1, object arg2);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Warning(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Warning(string message, params object[] args);
        #endregion
        
        #region InfoBasic Methods
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void InfoBasic(string message);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoBasic(string message, object arg1);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoBasic(string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoBasic(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoBasic(string message, params object[] args);
        #endregion
        
        #region InfoMedium Methods
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void InfoMedium(string message);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoMedium(string message, object arg1);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoMedium(string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoMedium(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoMedium(string message, params object[] args);
        #endregion
        
        #region InfoDetail Methods
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void InfoDetail(string message);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoDetail(string message, object arg1);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoDetail(string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoDetail(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoDetail(string message, params object[] args);
        #endregion
        
        #region Stats Methods
        /// <summary>
        /// Write a Stats item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void Stats(string message);
        /// <summary>
        /// Write a Stats item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void Stats(string message, object arg1);
        /// <summary>
        /// Write a Stats item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void Stats(string message, object arg1, object arg2);
        /// <summary>
        /// Write a Stats item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void Stats(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a Stats item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void Stats(string message, params object[] args);
        #endregion
        
        #region LibDebug Methods
        /// <summary>
        /// Write a LibDebug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void LibDebug(string message);
        /// <summary>
        /// Write a LibDebug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void LibDebug(string message, object arg1);
        /// <summary>
        /// Write a LibDebug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void LibDebug(string message, object arg1, object arg2);
        /// <summary>
        /// Write a LibDebug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void LibDebug(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a LibDebug item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void LibDebug(string message, params object[] args);
        #endregion
        
        #region LibVerbose Methods
        /// <summary>
        /// Write a LibVerbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        void LibVerbose(string message);
        /// <summary>
        /// Write a LibVerbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void LibVerbose(string message, object arg1);
        /// <summary>
        /// Write a LibVerbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void LibVerbose(string message, object arg1, object arg2);
        /// <summary>
        /// Write a LibVerbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void LibVerbose(string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a LibVerbose item into the log storages
        /// </summary>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void LibVerbose(string message, params object[] args);
        #endregion
        
        
        
        #region DebugGroup Methods
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void DebugGroup(string groupName, string message);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void DebugGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void DebugGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void DebugGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a debug item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void DebugGroup(string groupName, string message, params object[] args);
        #endregion
        
        #region VerboseGroup Methods
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void VerboseGroup(string groupName, string message);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void VerboseGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void VerboseGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void VerboseGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a verbose item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void VerboseGroup(string groupName, string message, params object[] args);
        #endregion
        
        #region ErrorGroup Methods
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void ErrorGroup(Exception ex, string groupName, string message);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void ErrorGroup(Exception ex, string groupName, string message, object arg1);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void ErrorGroup(Exception ex, string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void ErrorGroup(Exception ex, string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void ErrorGroup(Exception ex, string groupName, string message, params object[] args);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void ErrorGroup(string groupName, string message);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void ErrorGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void ErrorGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void ErrorGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a error item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void ErrorGroup(string groupName, string message, params object[] args);
        #endregion

        #region WarningGroup Methods
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void WarningGroup(string groupName, string message);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void WarningGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void WarningGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void WarningGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a warning item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void WarningGroup(string groupName, string message, params object[] args);
        #endregion
        
        #region InfoBasicGroup Methods
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void InfoBasicGroup(string groupName, string message);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoBasicGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoBasicGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoBasicGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoBasic item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoBasicGroup(string groupName, string message, params object[] args);
        #endregion
        
        #region InfoMediumGroup Methods
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void InfoMediumGroup(string groupName, string message);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoMediumGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoMediumGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoMediumGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoMedium item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoMediumGroup(string groupName, string message, params object[] args);
        #endregion
        
        #region InfoDetailGroup Methods
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        void InfoDetailGroup(string groupName, string message);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        void InfoDetailGroup(string groupName, string message, object arg1);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        void InfoDetailGroup(string groupName, string message, object arg1, object arg2);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        void InfoDetailGroup(string groupName, string message, object arg1, object arg2, object arg3);
        /// <summary>
        /// Write a InfoDetailed item into the log storages
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="message">Item message with pattern support</param>
        /// <param name="args">Arguments to bing with the pattern</param>
        void InfoDetailGroup(string groupName, string message, params object[] args);
        #endregion
        
        
        /// <summary>
        /// Enable or Disable the Trace engine
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Starts the Log Engine
        /// </summary>
        void Start();
    }
}
