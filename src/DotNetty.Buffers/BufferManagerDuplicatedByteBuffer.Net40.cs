﻿#if NET40
namespace DotNetty.Buffers
{
    partial class BufferManagerDuplicatedByteBuffer
    {
        public override ref byte GetPinnableMemoryOffsetAddress(int elementOffset)
        {
            return ref this.Unwrap().GetPinnableMemoryOffsetAddress(elementOffset);
        }
    }
}
#endif
