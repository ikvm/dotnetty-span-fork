﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal
{
    partial class AppendableCharSequence
    {
        public void Dispose() { this.chars = null; }
    }
}
