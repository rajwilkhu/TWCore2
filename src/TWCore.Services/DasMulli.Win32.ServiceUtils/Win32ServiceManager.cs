﻿using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Local

// ReSharper disable CheckNamespace

namespace DasMulli.Win32.ServiceUtils
{
    public sealed class Win32ServiceManager
    {
        private readonly string _machineName;
        private readonly string _databaseName;
        private readonly INativeInterop _nativeInterop;

        public Win32ServiceManager(string machineName = null, string databaseName = null)
            : this(machineName, databaseName, Win32Interop.Wrapper)
        {
        }
        
        internal Win32ServiceManager(string machineName, string databaseName, INativeInterop nativeInterop)
        {
            _machineName = machineName;
            _databaseName = databaseName;
            _nativeInterop = nativeInterop;
        }

        public void CreateService(string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials,
            bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            CreateService(serviceName, displayName, description, binaryPath, credentials, null, false, autoStart, startImmediately, errorSeverity);
        }

        public void CreateService(string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, ServiceFailureActions serviceFailureActions, bool failureActionsOnNonCrashFailures, bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(binaryPath));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All))
                {
                    DoCreateService(mgr, serviceName, displayName, description, binaryPath, credentials, autoStart, startImmediately, errorSeverity, serviceFailureActions, failureActionsOnNonCrashFailures);
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }

        private void DoCreateService(ServiceControlManager serviceControlManager, string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart, bool startImmediately, ErrorSeverity errorSeverity, ServiceFailureActions serviceFailureActions, bool failureActionsOnNonCrashFailures)
        {
            using (var svc = serviceControlManager.CreateService(serviceName, displayName, binaryPath, ServiceType.Win32OwnProcess,
                    autoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, errorSeverity, credentials))
            {
                if (!string.IsNullOrEmpty(description))
                {
                    svc.SetDescription(description);
                }

                if (serviceFailureActions != null)
                {
                    svc.SetFailureActions(serviceFailureActions);
                    svc.SetFailureActionFlag(failureActionsOnNonCrashFailures);
                }

                if (startImmediately)
                {
                    svc.Start();
                }
            }
        }

        public void CreateOrUpdateService(string serviceName, string displayName, string description, string binaryPath,
            Win32ServiceCredentials credentials, bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            CreateOrUpdateService(serviceName, displayName, description, binaryPath, credentials, null, false, autoStart, startImmediately, errorSeverity);
        }


        public void CreateOrUpdateService(string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, ServiceFailureActions serviceFailureActions, bool failureActionsOnNonCrashFailures, bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(binaryPath));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All))
                {
                    if (mgr.TryOpenService(serviceName, ServiceControlAccessRights.All, out var existingService, out var errorException)) {
                        using(existingService)
                        {
                            DoUpdateService(displayName, description, binaryPath, credentials, autoStart, errorSeverity, existingService, serviceFailureActions, failureActionsOnNonCrashFailures);
                        }
                    } else {
                        if (errorException.NativeErrorCode == KnownWin32ErrorCoes.ERROR_SERVICE_DOES_NOT_EXIST) {
                            DoCreateService(mgr, serviceName, displayName, description, binaryPath, credentials, autoStart, startImmediately, errorSeverity, serviceFailureActions, failureActionsOnNonCrashFailures);
                        } else {
                            throw errorException;
                        }
                    }
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }

        private static void DoUpdateService(string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart, ErrorSeverity errorSeverity, ServiceHandle existingService, ServiceFailureActions serviceFailureActions, bool failureActionsOnNonCrashFailures)
        {
            existingService.ChangeConfig(displayName, binaryPath, ServiceType.Win32OwnProcess,
                autoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, errorSeverity, credentials);
            existingService.SetDescription(description);
            if (serviceFailureActions != null)
            {
                existingService.SetFailureActions(serviceFailureActions);
                existingService.SetFailureActionFlag(failureActionsOnNonCrashFailures);
            }
            existingService.Start(throwIfAlreadyRunning: false);
        }

        public void DeleteService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(_nativeInterop, _machineName, _databaseName, ServiceControlManagerAccessRights.All))
                {
                    using (var svc = mgr.OpenService(serviceName, ServiceControlAccessRights.All))
                    {
                        svc.Delete();
                    }
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }
    }
}
