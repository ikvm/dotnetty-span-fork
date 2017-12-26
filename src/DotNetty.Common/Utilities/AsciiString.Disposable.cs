﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System;

    partial class AsciiString
    {
        public void Dispose()
        {
            this.value = null;
            this.stringValue = null;
        }
    }
}
