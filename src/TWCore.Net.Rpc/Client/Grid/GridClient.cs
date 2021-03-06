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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TWCore.Net.RPC.Client.Transports;
using TWCore.Threading;

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace TWCore.Net.RPC.Client.Grid
{
    /// <summary>
    /// Grid client
    /// </summary>
    public class GridClient
    {
        private readonly AsyncLock _asyncWaitLock = new AsyncLock();

        #region Properties
        /// <summary>
        /// Gets the Node items collection
        /// </summary>
        public NodeClientCollection Items { get; } = new NodeClientCollection();
        #endregion

        #region Events
        /// <summary>
        /// Event triggered when a node response is received.
        /// </summary>
        public EventHandler<EventArgs<NodeClientResult>> OnNodeResults;
        #endregion

        #region .ctor
        /// <summary>
        /// Grid client
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GridClient()
        {
            Core.Status.Attach(collection =>
            {
                collection.Add("Items Count", Items.Count);
                foreach (var item in Items)
                    Core.Status.AttachChild(item, this);
            });
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a node from a transport
        /// </summary>
        /// <param name="transport">Transport to connect to the node</param>
        /// <param name="args">Arguments to send to the node Init method</param>
        /// <returns>Node response from on Init call</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<object> AddNodeAsync(ITransportClient transport, params object[] args)
        {
            Core.Log.LibVerbose("Adding Node and initializing");
            var client = new RPCClient(transport);
            var node = await client.CreateProxyAsync<NodeProxy>().ConfigureAwait(false);
            var response = await node.InitAsync(args).ConfigureAwait(false);
            Items.Add(new NodeClient(node));
            Core.Log.LibVerbose("Node was initializated and added to the collection.");
            return response;
        }
        /// <summary>
        /// Process the specified args using an available node.
        /// </summary>
        /// <param name="args">Arguments to be processed by the node</param>
        /// <returns>Process results</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<NodeClientResult> ProcessAsync(params object[] args)
        {
            if (Items.Count <= 0) return null;
            NodeClient item;
            Core.Log.LibDebug("Selecting an available node...");
            using (await _asyncWaitLock.LockAsync().ConfigureAwait(false))
            {
                item = await Items.WaitForAvailableAsync().ConfigureAwait(false);
                item.Lock();
            }
            Core.Log.LibDebug("Calling process on Node '{0}'", item.NodeInfo.Id);
            var response = await item.ProcessAsync(args).ConfigureAwait(false);
            Core.Log.LibDebug("Received response from Node '{0}'", item.NodeInfo.Id);
            OnNodeResults?.Invoke(this, new EventArgs<NodeClientResult>(response));
            return response;
        }
        /// <summary>
        /// Processes the batch on the available nodes of the grid.
        /// Each element on the enumerable will be processed on a node of the grid.
        /// </summary>
        /// <param name="argsCollection">The arguments batch collection.</param>
        /// <returns>The IEnumerable results from the nodes</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<IEnumerable<NodeClientResult>> ProcessBatchAsync(IEnumerable<object[]> argsCollection)
        {
            Ensure.ArgumentNotNull(argsCollection);
            var collection = argsCollection as object[][] ?? argsCollection.ToArray();
            Core.Log.Debug("Processing batch of {0} elements", collection.Length);
            var cTask = collection.Select(ProcessAsync).ToArray();
            await Task.WhenAll(cTask).ConfigureAwait(false);
            return cTask.Select(i => i.Result);
        }
        #endregion
    }
}
