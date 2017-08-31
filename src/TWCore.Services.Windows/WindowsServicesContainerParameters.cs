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

namespace TWCore.Services.Windows
{
    /// <inheritdoc />
    /// <summary>
    /// Windows Services container parameters
    /// </summary>
    public class WindowsServicesContainerParameters : ICoreStart
    {
        public void CoreInit(Factories factories)
        {
            if (factories.PlatformType != PlatformType.Windows) return;
            Core.ServiceContainerFactory = (service, action) => new WindowsServiceContainer(service, action);
            Console.WriteLine("WINDOWS SERVICE CORE INIT");
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Windows Service container
    /// </summary>
    internal class WindowsServiceContainer : ServiceContainer
    {
        #region .ctor
        public WindowsServiceContainer(IService service, Action initAction) : base(service, initAction) { }
        public WindowsServiceContainer(IService service) : base(service) { }
        public WindowsServiceContainer(string serviceName, IService service) : base(serviceName, service) { }
        public WindowsServiceContainer(string serviceName, IService service, Action initAction) : base(serviceName, service, initAction)
        {
            RegisterParametersHandler("i", "Install Windows Service", InstallService);
            RegisterParametersHandler("u", "Uninstall Windows Service", UninstallService);
        }
        #endregion

        static void InstallService(ParameterHandlerInfo parameterHandlerInfo)
        {
            throw new NotImplementedException();
        }
        static void UninstallService(ParameterHandlerInfo parameterHandlerInfo)
        {
            throw new NotImplementedException();
        }

    }
}