﻿#if NET40
namespace DotNetty.Buffers
{
    partial class BufferManagerSlicedByteBuffer
    {
        public override ref byte GetPinnableMemoryAddress() => ref this.Unwrap().GetPinnableMemoryOffsetAddress(this.adjustment);

        public override ref byte GetPinnableMemoryOffsetAddress(int elementOffset)
        {
            return ref this.Unwrap().GetPinnableMemoryOffsetAddress(elementOffset);
        }
    }
}
#endif