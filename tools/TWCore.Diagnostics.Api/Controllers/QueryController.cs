﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TWCore.Diagnostics.Api.Models;
using TWCore.Diagnostics.Api.Models.Log;
using TWCore.Diagnostics.Api.Models.Status;
using TWCore.Diagnostics.Api.Models.Trace;
using TWCore.Diagnostics.Log;
using TWCore.Serialization;

namespace TWCore.Diagnostics.Api.Controllers
{
    [Route("api/query")]
    public class QueryController : Controller, IDiagnosticQueryHandler
    {
        [HttpGet("applications")]
        public Task<List<BasicInfo>> GetEnvironmentsAndApps()
            => DbHandlers.Instance.Query.GetEnvironmentsAndApps();

        [HttpGet("{environment}/logs/group/{group}/{application?}")]
        public Task<PagedList<NodeLogItem>> GetLogsByGroup(string environment, string group, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetLogsByGroup(environment, group, application, fromDate, toDate, page, pageSize);

        [HttpGet("{environment}/logs/search/{search?}")]
        public Task<PagedList<NodeLogItem>> GetLogsAsync(string environment, string search, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetLogsAsync(environment, search, application, fromDate, toDate, page, pageSize);

        [HttpGet("{environment}/logs/level/{level}/{search?}")]
        public Task<PagedList<NodeLogItem>> GetLogsAsync(string environment, string search, string application, LogLevel level, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetLogsAsync(environment, search, application, level, fromDate, toDate, page, pageSize);

        [HttpGet("{environment}/traces/group/{group}/{application?}")]
        public Task<PagedList<NodeTraceItem>> GetTracesByGroupAsync(string environment, string group, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetTracesByGroupAsync(environment, group, application, fromDate, toDate, page, pageSize);
        
        [HttpGet("{environment}/traces/search/{search?}")]
        public Task<PagedList<NodeTraceItem>> GetTracesAsync(string environment, string search, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetTracesAsync(environment, search, application, fromDate, toDate, page, pageSize);

        [HttpGet("{environment}/traces/raw/{id}")]
        public Task<SerializedObject> GetTraceObjectAsync(string id)
            => DbHandlers.Instance.Query.GetTraceObjectAsync(id);

        [HttpGet("{environment}/traces/object/{id}")]
        public async Task<object> GetTraceObjectValueAsync(string id)
        {
            var serObject = await DbHandlers.Instance.Query.GetTraceObjectAsync(id).ConfigureAwait(false);
            return serObject.GetValue();
        }

        [HttpGet("{environment}/status")]
        public Task<PagedList<NodeStatusItem>> GetStatusesAsync(string environment, string machine, string application, DateTime fromDate, DateTime toDate, int page, int pageSize = 50)
            => DbHandlers.Instance.Query.GetStatusesAsync(environment, machine, application, fromDate, toDate, page, pageSize);

        [HttpGet("{environment}/status/current")]
        public Task<List<NodeStatusItem>> GetCurrentStatus(string environment, string machine, string application)
            => DbHandlers.Instance.Query.GetCurrentStatus(environment, machine, application);
    }
}