﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Concurrency
{
    using System;
    using DotNetty.Common.Internal.Logging;

    public static class IPromiseExtensions
    {
        public static void TrySetCanceled(this IPromise promise, IInternalLogger logger)
        {
            if (!promise.TrySetCanceled() && logger != null && logger.WarnEnabled)
            {
                var err = promise.Task.Exception;
                if (null == err)
                {
                    logger.Warn($"Failed to cancel promise because it has succeeded already: {promise}");
                }
                else
                {
                    logger.Warn($"Failed to cancel promise because it has failed already: {promise}, unnotified cause:", err);
                }
            }
        }

        public static void TryComplete(this IPromise promise, IInternalLogger logger)
        {
            if (!promise.TryComplete() && logger != null && logger.WarnEnabled)
            {
                var err = promise.Task.Exception;
                if (null == err)
                {
                    logger.Warn($"Failed to mark a promise as success because it has succeeded already: {promise}");
                }
                else
                {
                    logger.Warn($"Failed to mark a promise as success because it has failed already: {promise}, unnotified cause:", err);
                }
            }
        }

        public static void TrySetException(this IPromise promise, Exception cause, IInternalLogger logger)
        {
            if (!promise.TrySetException(cause) && logger != null && logger.WarnEnabled)
            {
                var err = promise.Task.Exception;
                if (null == err)
                {
                    logger.Warn($"Failed to mark a promise as failure because it has succeeded already: {promise}");
                }
                else
                {
                    logger.Warn($"Failed to mark a promise as failure because it has failed already: {promise}, unnotified cause:{cause.ToString()}", err);
                }
            }
        }
    }
}