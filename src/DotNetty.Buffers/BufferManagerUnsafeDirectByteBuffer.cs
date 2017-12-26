﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.Buffers;
using DotNetty.Common;

namespace DotNetty.Buffers
{
    unsafe partial class BufferManagerUnsafeDirectByteBuffer : BufferManagerByteBuffer
    {
        static readonly ThreadLocalPool<BufferManagerUnsafeDirectByteBuffer> Recycler = new ThreadLocalPool<BufferManagerUnsafeDirectByteBuffer>(handle => new BufferManagerUnsafeDirectByteBuffer(handle, 0));

        internal static BufferManagerUnsafeDirectByteBuffer NewInstance(BufferManagerByteBufferAllocator allocator, BufferManager bufferManager, byte[] buffer, int length, int maxCapacity)
        {
            var buf = Recycler.Take();
            buf.Reuse(allocator, bufferManager, buffer, length, maxCapacity);
            return buf;
        }

        internal BufferManagerUnsafeDirectByteBuffer(ThreadLocalPool.Handle recyclerHandle, int maxCapacity)
            : base(recyclerHandle, maxCapacity)
        {
        }

        public override bool IsDirect => true;

        protected internal override byte _GetByte(int index) => this.Memory[index];

        protected internal override short _GetShort(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetShort(addr);
        }

        protected internal override short _GetShortLE(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetShortLE(addr);
        }

        protected internal override int _GetUnsignedMedium(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetUnsignedMedium(addr);
        }

        protected internal override int _GetUnsignedMediumLE(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetUnsignedMediumLE(addr);
        }

        protected internal override int _GetInt(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetInt(addr);
        }

        protected internal override int _GetIntLE(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetIntLE(addr);
        }

        protected internal override long _GetLong(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetLong(addr);
        }

        protected internal override long _GetLongLE(int index)
        {
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.GetLongLE(addr);
        }

        public override IByteBuffer GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
            return this;
        }

        public override IByteBuffer GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.GetBytes(this, addr, index, dst, dstIndex, length);
            return this;
        }

        protected internal override void _SetByte(int index, int value) => this.Memory[index] = unchecked((byte)value);

        protected internal override void _SetShort(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetShort(addr, value);
        }

        protected internal override void _SetShortLE(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetShortLE(addr, value);
        }

        protected internal override void _SetMedium(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetMedium(addr, value);
        }

        protected internal override void _SetMediumLE(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetMediumLE(addr, value);
        }

        protected internal override void _SetInt(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetInt(addr, value);
        }

        protected internal override void _SetIntLE(int index, int value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetIntLE(addr, value);
        }

        protected internal override void _SetLong(int index, long value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetLong(addr, value);
        }

        protected internal override void _SetLongLE(int index, long value)
        {
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetLongLE(addr, value);
        }

        public override IByteBuffer SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetBytes(this, addr, index, src, srcIndex, length);
            return this;
        }

        public override IByteBuffer SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            if (length != 0)
            {
                fixed (byte* addr = &this.Addr(index))
                    UnsafeByteBufferUtil.SetBytes(this, addr, index, src, srcIndex, length);
            }
            return this;
        }

        public override IByteBuffer GetBytes(int index, Stream output, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.GetBytes(this, addr, index, output, length);
            return this;
        }

        public override Task<int> SetBytesAsync(int index, Stream src, int length, CancellationToken cancellationToken)
        {
            this.CheckIndex(index, length);
            int read;
            fixed (byte* addr = &this.Addr(index))
                read = UnsafeByteBufferUtil.SetBytes(this, addr, index, src, length);
#if NET40
            return TaskEx.FromResult(read);
#else
            return Task.FromResult(read);
#endif
        }

        public override int IoBufferCount => 1;

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex(index, length);
            return new ArraySegment<byte>(this.Memory, index, length);
        }

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => new[] { this.GetIoBuffer(index, length) };

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                return UnsafeByteBufferUtil.Copy(this, addr, index, length);
        }

        [MethodImpl(InlineMethod.Value)]
        ref byte Addr(int index) => ref this.Memory[index];

        public override IByteBuffer SetZero(int index, int length)
        {
            this.CheckIndex(index, length);
            fixed (byte* addr = &this.Addr(index))
                UnsafeByteBufferUtil.SetZero(addr, length);
            return this;
        }

        public override IByteBuffer WriteZero(int length)
        {
            if (length == 0) { return this; }

            this.EnsureWritable(length);
            int wIndex = this.WriterIndex;
            this.CheckIndex0(wIndex, length);
            fixed (byte* addr = &this.Addr(wIndex))
                UnsafeByteBufferUtil.SetZero(addr, length);
            this.SetWriterIndex(wIndex + length);

            return this;
        }
    }
}
