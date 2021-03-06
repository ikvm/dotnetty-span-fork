﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    sealed partial class ArrayPooledDuplicatedByteBuffer : AbstractArrayPooledDerivedByteBuffer
    {
        static readonly ThreadLocalPool<ArrayPooledDuplicatedByteBuffer> Recycler = new ThreadLocalPool<ArrayPooledDuplicatedByteBuffer>(handle => new ArrayPooledDuplicatedByteBuffer(handle));

        internal static ArrayPooledDuplicatedByteBuffer NewInstance(AbstractByteBuffer unwrapped, IByteBuffer wrapped, int readerIndex, int writerIndex)
        {
            ArrayPooledDuplicatedByteBuffer duplicate = Recycler.Take();
            duplicate.Init<ArrayPooledDuplicatedByteBuffer>(unwrapped, wrapped, readerIndex, writerIndex, unwrapped.MaxCapacity);
            duplicate.MarkReaderIndex();
            duplicate.MarkWriterIndex();

            return duplicate;
        }

        public ArrayPooledDuplicatedByteBuffer(ThreadLocalPool.Handle recyclerHandle)
            : base(recyclerHandle)
        {
        }

        public sealed override int Capacity => this.Unwrap().Capacity;

        public sealed override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.Unwrap().AdjustCapacity(newCapacity);
            return this;
        }

        public sealed override int ArrayOffset => this.Unwrap().ArrayOffset;

        public sealed override ref byte GetPinnableMemoryAddress() => ref this.Unwrap().GetPinnableMemoryAddress();

        public sealed override IntPtr AddressOfPinnedMemory() => this.Unwrap().AddressOfPinnedMemory();

        public sealed override ArraySegment<byte> GetIoBuffer(int index, int length) => this.Unwrap().GetIoBuffer(index, length);

        public sealed override ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.Unwrap().GetIoBuffers(index, length);

        public sealed override IByteBuffer Copy(int index, int length) => this.Unwrap().Copy(index, length);

        public sealed override IByteBuffer RetainedSlice(int index, int length) => ArrayPooledSlicedByteBuffer.NewInstance(this.UnwrapCore(), this, index, length);

        public sealed override IByteBuffer Duplicate() => this.Duplicate0().SetIndex(this.ReaderIndex, this.WriterIndex);

        public sealed override IByteBuffer RetainedDuplicate() => NewInstance(this.UnwrapCore(), this, this.ReaderIndex, this.WriterIndex);

        protected internal sealed override byte _GetByte(int index) => this.UnwrapCore()._GetByte(index);

        protected internal sealed override short _GetShort(int index) => this.UnwrapCore()._GetShort(index);

        protected internal sealed override short _GetShortLE(int index) => this.UnwrapCore()._GetShortLE(index);

        protected internal sealed override int _GetUnsignedMedium(int index) => this.UnwrapCore()._GetUnsignedMedium(index);

        protected internal sealed override int _GetUnsignedMediumLE(int index) => this.UnwrapCore()._GetUnsignedMediumLE(index);

        protected internal sealed override int _GetInt(int index) => this.UnwrapCore()._GetInt(index);

        protected internal sealed override int _GetIntLE(int index) => this.UnwrapCore()._GetIntLE(index);

        protected internal sealed override long _GetLong(int index) => this.UnwrapCore()._GetLong(index);

        protected internal sealed override long _GetLongLE(int index) => this.UnwrapCore()._GetLongLE(index);

        public sealed override IByteBuffer GetBytes(int index, IByteBuffer destination, int dstIndex, int length) { this.Unwrap().GetBytes(index, destination, dstIndex, length); return this; }

        public sealed override IByteBuffer GetBytes(int index, byte[] destination, int dstIndex, int length) { this.Unwrap().GetBytes(index, destination, dstIndex, length); return this; }

        public sealed override IByteBuffer GetBytes(int index, Stream destination, int length) { this.Unwrap().GetBytes(index, destination, length); return this; }

        protected internal sealed override void _SetByte(int index, int value) => this.UnwrapCore()._SetByte(index, value);

        protected internal sealed override void _SetShort(int index, int value) => this.UnwrapCore()._SetShort(index, value);

        protected internal sealed override void _SetShortLE(int index, int value) => this.UnwrapCore()._SetShortLE(index, value);

        protected internal sealed override void _SetMedium(int index, int value) => this.UnwrapCore()._SetMedium(index, value);

        protected internal sealed override void _SetMediumLE(int index, int value) => this.UnwrapCore()._SetMediumLE(index, value);

        public sealed override IByteBuffer SetBytes(int index, IByteBuffer src, int srcIndex, int length) { this.Unwrap().SetBytes(index, src, srcIndex, length); return this; }

        public sealed override Task<int> SetBytesAsync(int index, Stream src, int length, CancellationToken cancellationToken) => this.Unwrap().SetBytesAsync(index, src, length, cancellationToken);

        public sealed override IByteBuffer SetBytes(int index, byte[] src, int srcIndex, int length) { this.Unwrap().SetBytes(index, src, srcIndex, length); return this; }

        protected internal sealed override void _SetInt(int index, int value) => this.UnwrapCore()._SetInt(index, value);

        protected internal sealed override void _SetIntLE(int index, int value) => this.UnwrapCore()._SetIntLE(index, value);

        protected internal sealed override void _SetLong(int index, long value) => this.UnwrapCore()._SetLong(index, value);

        protected internal sealed override void _SetLongLE(int index, long value) => this.UnwrapCore()._SetLongLE(index, value);

        public sealed override int ForEachByte(int index, int length, IByteProcessor processor) => this.Unwrap().ForEachByte(index, length, processor);

        public sealed override int ForEachByteDesc(int index, int length, IByteProcessor processor) => this.Unwrap().ForEachByteDesc(index, length, processor);
    }
}
