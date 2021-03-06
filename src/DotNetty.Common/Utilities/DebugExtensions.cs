﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System.Collections.Generic;
    using CuteAnt.Pool;

    public static class DebugExtensions
    {
        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            var sb = StringBuilderManager.Allocate();
            bool first = true;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (first)
                {
                    first = false;
                    sb.Append('{');
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append("{`").Append(pair.Key).Append("`: ").Append(pair.Value).Append('}');
            }
            sb.Append('}');
            return StringBuilderManager.ReturnAndFree(sb);
        }
    }
}