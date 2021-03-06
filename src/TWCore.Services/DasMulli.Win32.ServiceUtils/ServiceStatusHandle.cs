﻿using System;
using System.Runtime.InteropServices;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace
// ReSharper disable MemberCanBePrivate.Global

namespace DasMulli.Win32.ServiceUtils
{
    internal class ServiceStatusHandle : SafeHandle
    {
        internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

        internal ServiceStatusHandle() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeInterop.CloseServiceHandle(handle);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
}