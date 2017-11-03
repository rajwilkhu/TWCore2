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
using System.Threading.Tasks;
using TWCore.Diagnostics.Log;
using TWCore.Diagnostics.Status;
using TWCore.Diagnostics.Trace.Storages;
using TWCore.Messaging;
using TWCore.Services;
using TWCore.Services.Messaging;

namespace TWCore.Diagnostics.Api
{
    public class DiagnosticMessagingBusiness : BusinessAsyncBase<List<LogItem>, List<MessagingTraceItem>, StatusItemCollection>
    {
        protected override Task<object> OnProcessAsync(List<LogItem> message)
        {
            Core.Log.Warning("Log Items Received.");
            return Task.FromResult(ResponseMessage.NoResponse);
        }

        protected override Task<object> OnProcessAsync(List<MessagingTraceItem> message)
        {
            Core.Log.Warning("Trace Items Received.");
            return Task.FromResult(ResponseMessage.NoResponse);
        }

        protected override Task<object> OnProcessAsync(StatusItemCollection message)
        {
            Core.Log.Warning("Status Received.");
            return Task.FromResult(ResponseMessage.NoResponse);
        }
    }
}