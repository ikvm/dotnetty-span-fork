﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public static class PlatformProvider
    {
        static IPlatform defaultPlatform;

        public static IPlatform Platform
        {
            [MethodImpl(InlineMethod.Value)]
            get => Volatile.Read(ref defaultPlatform) ?? EnsurePlatformCreated();
            set
            {
                if (null == value) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value); }
                Interlocked.Exchange(ref defaultPlatform, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IPlatform EnsurePlatformCreated()
        {
            var platform = new DefaultPlatform();
            IPlatform current = Interlocked.CompareExchange(ref defaultPlatform, platform, null);
            return current ?? platform;
        }
    }
}