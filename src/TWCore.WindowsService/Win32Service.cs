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

using DasMulli.Win32.ServiceUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace TWCore.Services.Windows
{
    /// <summary>
    /// Win32 Service for IService
    /// </summary>
    internal class Win32Service : IWin32Service
    {
        IService _service;
        string _serviceName;

        #region Properties
        /// <summary>
        /// Get the Service Name
        /// </summary>
        public string ServiceName => _serviceName;
        #endregion

        #region .ctor
        /// <summary>
        /// Win32 Service for IService
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="service">IService instance</param>
        public Win32Service(string serviceName, IService service)
        {
            _service = service;
            _serviceName = serviceName;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Start service
        /// </summary>
        /// <param name="startupArguments">Startup arguments</param>
        /// <param name="serviceStoppedCallback">Service stopped callback</param>
        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            _service.OnStart(startupArguments);
        }
        /// <summary>
        /// Stop service
        /// </summary>
        public void Stop()
        {
            _service.OnStop();
            try
            {
                Core.Dispose();
            }
            catch
            { }
        }
        #endregion
    }
}
