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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace TWCore.Net.RPC.Client
{
    /// <summary>
    /// RPC Proxy base class
    /// </summary>
    public abstract class RPCProxy
    {
	    private RPCClient _client;
	    private string _serviceName;
	    private readonly Dictionary<string, FieldInfo> _events;
        private readonly NonBlocking.ConcurrentDictionary<string, string> _memberNames = new NonBlocking.ConcurrentDictionary<string, string>();

        #region .ctor
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected RPCProxy()
        {
            _events = GetType().GetRuntimeFields().ToDictionary(k => k.Name, v => v);
        }
        #endregion

	    /// <summary>
	    /// Sets the RPC client to the proxy
	    /// </summary>
	    /// <param name="client">RPCClient object instance</param>
	    /// <param name="serviceName">Service name</param>
	    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetClient(RPCClient client, string serviceName)
        {
            Ensure.ArgumentNotNull(client, "RPC Client can't be null.");
            Ensure.ArgumentNotNull(serviceName, "ServiceName can't be null.");
            _serviceName = serviceName;
            _client = client;
            _client.OnEventReceived += Client_OnEventReceived;

            Core.Status.Attach(collection =>
            {
                Core.Status.AttachChild(_client, this);
            });
        }

        #region Private Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Client_OnEventReceived(object sender, EventDataEventArgs e)
        {
	        if (e.ServiceName != _serviceName || !_events.TryGetValue(e.EventName, out var value) ||
	            !(value.GetValue(this) is MulticastDelegate evHandler)) return;
	        foreach (var handler in evHandler.GetInvocationList())
		        handler.DynamicInvoke(this, e.EventArgs);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetMemberName(string memberName)
            => _memberNames.GetOrAdd(memberName, key => key?.EndsWith("Async") == true && key.Length > 5 ? key.Substring(0, key.Length - 5) : key);
        #endregion


        #region Invoke Generic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>([CallerMemberName]string memberName = "") 
            => _client.ServerInvokeAsync<T>(_serviceName, memberName).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>(object arg1, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>(object arg1, object arg2, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3, arg4).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Invoke<T>(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3, arg4, arg5).WaitAndResults();
        #endregion

        #region Invoke 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke([CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke(object arg1, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke(object arg1, object arg2, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3, arg4).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected object Invoke(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3, arg4, arg5).WaitAndResults();
        #endregion



        #region InvokeAsync Generic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>([CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>(object arg1, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>(object arg1, object arg2, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3, arg4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsync<T>(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync<T>(_serviceName, memberName, arg1, arg2, arg3, arg4, arg5);
        #endregion

        #region InvokeAsync 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync([CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync(object arg1, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync(object arg1, object arg2, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3, arg4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsync(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "") 
			=> _client.ServerInvokeAsync(_serviceName, memberName, arg1, arg2, arg3, arg4, arg5);
        #endregion

        #region Alternative Invokes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TResult InvokeArgs<TResult>([CallerMemberName]string memberName = "")
            => _client.ServerInvokeNoArgumentsAsync<TResult>(_serviceName, memberName).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsync<TResult>([CallerMemberName]string memberName = "")
            => _client.ServerInvokeNoArgumentsAsync<TResult>(_serviceName, memberName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TResult InvokeArgs<TArg1, TResult>(TArg1 arg1, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TResult>(_serviceName, memberName, arg1).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsync<TArg1, TResult>(TArg1 arg1, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1,TResult>(_serviceName, memberName, arg1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TResult InvokeArgs<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TResult>(_serviceName, memberName, arg1, arg2).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsync<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TResult>(_serviceName, memberName, arg1, arg2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TResult InvokeArgs<TArg1, TArg2, TArg3, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TResult>(_serviceName, memberName, arg1, arg2, arg3).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsync<TArg1, TArg2, TArg3, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TResult>(_serviceName, memberName, arg1, arg2, arg3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TResult InvokeArgs<TArg1, TArg2, TArg3, TArg4, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TArg4, TResult>(_serviceName, memberName, arg1, arg2, arg3, arg4).WaitAndResults();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsync<TArg1, TArg2, TArg3, TArg4, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TArg4, TResult>(_serviceName, memberName, arg1, arg2, arg3, arg4);
        #endregion



        #region InvokeAsAsync Generic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>([CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>(object arg1, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName), arg1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>(object arg1, object arg2, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName), arg1, arg2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName), arg1, arg2, arg3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName), arg1, arg2, arg3, arg4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<T> InvokeAsAsync<T>(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<T>(_serviceName, GetMemberName(memberName), arg1, arg2, arg3, arg4, arg5);
        #endregion

        #region InvokeAsAsync 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync([CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync(object arg1, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName), arg1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync(object arg1, object arg2, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName), arg1, arg2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync(object arg1, object arg2, object arg3, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName), arg1, arg2, arg3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync(object arg1, object arg2, object arg3, object arg4, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName), arg1, arg2, arg3, arg4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<object> InvokeAsAsync(object arg1, object arg2, object arg3, object arg4, object arg5, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync(_serviceName, GetMemberName(memberName), arg1, arg2, arg3, arg4, arg5);
        #endregion

        #region Alternative InvokesWithAsync
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsAsync<TResult>([CallerMemberName]string memberName = "")
            => _client.ServerInvokeNoArgumentsAsync<TResult>(_serviceName, GetMemberName(memberName));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsAsync<TArg1, TResult>(TArg1 arg1, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TResult>(_serviceName, GetMemberName(memberName), arg1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsAsync<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TResult>(_serviceName, GetMemberName(memberName), arg1, arg2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsAsync<TArg1, TArg2, TArg3, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TResult>(_serviceName, GetMemberName(memberName), arg1, arg2, arg3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task<TResult> InvokeArgsAsAsync<TArg1, TArg2, TArg3, TArg4, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [CallerMemberName]string memberName = "")
            => _client.ServerInvokeAsync<TArg1, TArg2, TArg3, TArg4, TResult>(_serviceName, GetMemberName(memberName), arg1, arg2, arg3, arg4);
        #endregion



        /// <summary>
        /// Dispose all resource
        /// </summary>
        public void Dispose()
        {
            try
            {
                _client?.Dispose();
            }
	        catch
	        {
		        // ignored
	        }
	        _client = null;
            Core.Status.DeAttachObject(this);
        }
    }
}
