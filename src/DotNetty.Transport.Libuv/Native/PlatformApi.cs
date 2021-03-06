﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
namespace DotNetty.Transport.Libuv.Native
{
    using System;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;

    static partial class PlatformApi
    {
        const int AF_INET6_LINUX = 10;
        const int AF_INET6_OSX = 30;

        internal static uint GetAddressFamily(AddressFamily addressFamily)
        {
            // AF_INET 2
            if (addressFamily == AddressFamily.InterNetwork || IsWindows)
            {
                return (uint)addressFamily;
            }

            if (IsLinux)
            {
                return AF_INET6_LINUX;
            }

            if (IsDarwin)
            {
                return AF_INET6_OSX;
            }

            return ThrowHelper.ThrowInvalidOperationException_Dispatch(addressFamily);
        }

        internal static bool GetReuseAddress(TcpHandle tcpHandle)
        {
            IntPtr socketHandle = GetSocketHandle(tcpHandle);

            return IsWindows 
                ? WindowsApi.GetReuseAddress(socketHandle) 
                : UnixApi.GetReuseAddress(socketHandle);
        }

        internal static void SetReuseAddress(TcpHandle tcpHandle, int value)
        {
            IntPtr socketHandle = GetSocketHandle(tcpHandle);
            if (IsWindows)
            {
                WindowsApi.SetReuseAddress(socketHandle, value);
            }
            else
            {
                UnixApi.SetReuseAddress(socketHandle, value);
            }
        }

        internal static bool GetReusePort(TcpHandle tcpHandle)
        {
            if (IsWindows)
            {
                return GetReuseAddress(tcpHandle);
            }

            IntPtr socketHandle = GetSocketHandle(tcpHandle);
            return UnixApi.GetReusePort(socketHandle);
        }

        internal static void SetReusePort(TcpHandle tcpHandle, int value)
        {
            IntPtr socketHandle = GetSocketHandle(tcpHandle);
            // Ignore SO_REUSEPORT on Windows because it is controlled
            // by SO_REUSEADDR
            if (IsWindows)
            {
                return;
            }

            UnixApi.SetReusePort(socketHandle, value);
        }

        static IntPtr GetSocketHandle(TcpHandle handle)
        {
            IntPtr socket = IntPtr.Zero;
            NativeMethods.uv_fileno(handle.Handle, ref socket);
            return socket;
        }
    }
}
