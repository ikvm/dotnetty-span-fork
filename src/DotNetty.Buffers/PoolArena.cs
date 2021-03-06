﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using CuteAnt.Pool;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;
#if !NET40
    using System.Buffers;
#endif

    enum SizeClass
    {
        Tiny,
        Small,
        Normal
    }

    abstract class PoolArena<T> : IPoolArenaMetric
    {
        internal const int NumTinySubpagePools = 512 >> 4;

        internal readonly PooledByteBufferAllocator Parent;

        readonly int maxOrder;
        internal readonly int PageSize;
        internal readonly int PageShifts;
        internal readonly int ChunkSize;
        internal readonly int SubpageOverflowMask;
        internal readonly int NumSmallSubpagePools;
        readonly PoolSubpage<T>[] tinySubpagePools;
        readonly PoolSubpage<T>[] smallSubpagePools;

        readonly PoolChunkList<T> q050;
        readonly PoolChunkList<T> q025;
        readonly PoolChunkList<T> q000;
        readonly PoolChunkList<T> qInit;
        readonly PoolChunkList<T> q075;
        readonly PoolChunkList<T> q100;

#if NET40
        readonly IList<IPoolChunkListMetric> chunkListMetrics;
#else
        readonly IReadOnlyList<IPoolChunkListMetric> chunkListMetrics;
#endif

        // Metrics for allocations and deallocations
        long allocationsNormal;

        // We need to use the LongCounter here as this is not guarded via synchronized block.
        long allocationsTiny;

        long allocationsSmall;
        long allocationsHuge;
        long activeBytesHuge;

        long deallocationsTiny;
        long deallocationsSmall;
        long deallocationsNormal;

        // We need to use the LongCounter here as this is not guarded via synchronized block.
        long deallocationsHuge;

        // Number of thread caches backed by this arena.
        int numThreadCaches;

        // TODO: Test if adding padding helps under contention
        //private long pad0, pad1, pad2, pad3, pad4, pad5, pad6, pad7;

        protected PoolArena(
            PooledByteBufferAllocator parent,
            int pageSize,
            int maxOrder,
            int pageShifts,
            int chunkSize)
        {
            this.Parent = parent;
            this.PageSize = pageSize;
            this.maxOrder = maxOrder;
            this.PageShifts = pageShifts;
            this.ChunkSize = chunkSize;
            this.SubpageOverflowMask = ~(pageSize - 1);
            this.tinySubpagePools = this.NewSubpagePoolArray(NumTinySubpagePools);
            for (int i = 0; i < this.tinySubpagePools.Length; i++)
            {
                this.tinySubpagePools[i] = this.NewSubpagePoolHead(pageSize);
            }

            this.NumSmallSubpagePools = pageShifts - 9;
            this.smallSubpagePools = this.NewSubpagePoolArray(this.NumSmallSubpagePools);
            for (int i = 0; i < this.smallSubpagePools.Length; i++)
            {
                this.smallSubpagePools[i] = this.NewSubpagePoolHead(pageSize);
            }

            this.q100 = new PoolChunkList<T>(this, null, 100, int.MaxValue, chunkSize);
            this.q075 = new PoolChunkList<T>(this, this.q100, 75, 100, chunkSize);
            this.q050 = new PoolChunkList<T>(this, this.q075, 50, 100, chunkSize);
            this.q025 = new PoolChunkList<T>(this, this.q050, 25, 75, chunkSize);
            this.q000 = new PoolChunkList<T>(this, this.q025, 1, 50, chunkSize);
            this.qInit = new PoolChunkList<T>(this, this.q000, int.MinValue, 25, chunkSize);

            this.q100.PrevList(this.q075);
            this.q075.PrevList(this.q050);
            this.q050.PrevList(this.q025);
            this.q025.PrevList(this.q000);
            this.q000.PrevList(null);
            this.qInit.PrevList(this.qInit);

            var metrics = new List<IPoolChunkListMetric>(6);
            metrics.Add(this.qInit);
            metrics.Add(this.q000);
            metrics.Add(this.q025);
            metrics.Add(this.q050);
            metrics.Add(this.q075);
            metrics.Add(this.q100);
            this.chunkListMetrics = metrics;
        }

        PoolSubpage<T> NewSubpagePoolHead(int pageSize)
        {
            var head = new PoolSubpage<T>(pageSize);
            head.Prev = head;
            head.Next = head;
            return head;
        }

        PoolSubpage<T>[] NewSubpagePoolArray(int size) => new PoolSubpage<T>[size];

        internal abstract bool IsDirect { get; }

        internal PooledByteBuffer<T> Allocate(PoolThreadCache<T> cache, int reqCapacity, int maxCapacity)
        {
            PooledByteBuffer<T> buf = this.NewByteBuf(maxCapacity);
            this.Allocate(cache, buf, reqCapacity);
            return buf;
        }

        internal static int TinyIdx(int normCapacity) => normCapacity.RightUShift(4);

        internal static int SmallIdx(int normCapacity)
        {
            int tableIdx = 0;
            int i = normCapacity.RightUShift(10);
            while (i != 0)
            {
                i = i.RightUShift(1);
                tableIdx++;
            }
            return tableIdx;
        }

        // capacity < pageSize
        internal bool IsTinyOrSmall(int normCapacity) => 0u >= (uint)(normCapacity & this.SubpageOverflowMask) ? true : false;

        // normCapacity < 512
        internal static bool IsTiny(int normCapacity) => 0u >= (uint)(normCapacity & 0xFFFFFE00) ? true : false;

        void Allocate(PoolThreadCache<T> cache, PooledByteBuffer<T> buf, int reqCapacity)
        {
            int normCapacity = this.NormalizeCapacity(reqCapacity);
            if (this.IsTinyOrSmall(normCapacity))
            {
                // capacity < pageSize
                int tableIdx;
                PoolSubpage<T>[] table;
                bool tiny = IsTiny(normCapacity);
                if (tiny)
                {
                    // < 512
                    if (cache.AllocateTiny(this, buf, reqCapacity, normCapacity))
                    {
                        // was able to allocate out of the cache so move on
                        return;
                    }
                    tableIdx = TinyIdx(normCapacity);
                    table = this.tinySubpagePools;
                }
                else
                {
                    if (cache.AllocateSmall(this, buf, reqCapacity, normCapacity))
                    {
                        // was able to allocate out of the cache so move on
                        return;
                    }
                    tableIdx = SmallIdx(normCapacity);
                    table = this.smallSubpagePools;
                }

                PoolSubpage<T> head = table[tableIdx];

                //
                //  Synchronize on the head. This is needed as {@link PoolSubpage#allocate()} and
                // {@link PoolSubpage#free(int)} may modify the doubly linked list as well.
                // 
                lock (head)
                {
                    PoolSubpage<T> s = head.Next;
                    if (s != head)
                    {
                        Debug.Assert(s.DoNotDestroy && s.ElemSize == normCapacity);
                        long handle = s.Allocate();
                        Debug.Assert(handle >= 0);
                        s.Chunk.InitBufWithSubpage(buf, handle, reqCapacity);
                        this.IncTinySmallAllocation(tiny);
                        return;
                    }
                }

                lock (this)
                {
                    this.AllocateNormal(buf, reqCapacity, normCapacity);
                }

                this.IncTinySmallAllocation(tiny);
                return;
            }
            if (normCapacity <= this.ChunkSize)
            {
                if (cache.AllocateNormal(this, buf, reqCapacity, normCapacity))
                {
                    // was able to allocate out of the cache so move on
                    return;
                }

                lock (this)
                {
                    this.AllocateNormal(buf, reqCapacity, normCapacity);
                    this.allocationsNormal++;
                }
            }
            else
            {
                // Huge allocations are never served via the cache so just call allocateHuge
                this.AllocateHuge(buf, reqCapacity);
            }
        }

        void AllocateNormal(PooledByteBuffer<T> buf, int reqCapacity, int normCapacity)
        {
            if (this.q050.Allocate(buf, reqCapacity, normCapacity) || this.q025.Allocate(buf, reqCapacity, normCapacity)
                || this.q000.Allocate(buf, reqCapacity, normCapacity) || this.qInit.Allocate(buf, reqCapacity, normCapacity)
                || this.q075.Allocate(buf, reqCapacity, normCapacity))
            {
                return;
            }

            // Add a new chunk.
            PoolChunk<T> c = this.NewChunk(this.PageSize, this.maxOrder, this.PageShifts, this.ChunkSize);
            long handle = c.Allocate(normCapacity);
            Debug.Assert(handle > 0);
            c.InitBuf(buf, handle, reqCapacity);
            this.qInit.Add(c);
        }

        void IncTinySmallAllocation(bool tiny)
        {
            if (tiny)
            {
                Interlocked.Increment(ref this.allocationsTiny);
            }
            else
            {
                Interlocked.Increment(ref this.allocationsSmall);
            }
        }

        void AllocateHuge(PooledByteBuffer<T> buf, int reqCapacity)
        {
            PoolChunk<T> chunk = this.NewUnpooledChunk(reqCapacity);
            Interlocked.Add(ref this.activeBytesHuge, chunk.ChunkSize);
            buf.InitUnpooled(chunk, reqCapacity);
            Interlocked.Increment(ref this.allocationsHuge);
        }

        internal void Free(PoolChunk<T> chunk, long handle, int normCapacity, PoolThreadCache<T> cache)
        {
            if (chunk.Unpooled)
            {
                int size = chunk.ChunkSize;
                this.DestroyChunk(chunk);
                Interlocked.Add(ref this.activeBytesHuge, -size);
                Interlocked.Increment(ref this.deallocationsHuge);
            }
            else
            {
                SizeClass sizeClass = this.SizeClass(normCapacity);
                if (cache != null && cache.Add(this, chunk, handle, normCapacity, sizeClass))
                {
                    // cached so not free it.
                    return;
                }

                this.FreeChunk(chunk, handle, sizeClass);
            }
        }

        SizeClass SizeClass(int normCapacity)
        {
            if (!this.IsTinyOrSmall(normCapacity))
            {
                return Buffers.SizeClass.Normal;
            }

            return IsTiny(normCapacity) ? Buffers.SizeClass.Tiny : Buffers.SizeClass.Small;
        }

        internal void FreeChunk(PoolChunk<T> chunk, long handle, SizeClass sizeClass)
        {
            bool destroyChunk;
            lock (this)
            {
                switch (sizeClass)
                {
                    case Buffers.SizeClass.Normal:
                        ++this.deallocationsNormal;
                        break;
                    case Buffers.SizeClass.Small:
                        ++this.deallocationsSmall;
                        break;
                    case Buffers.SizeClass.Tiny:
                        ++this.deallocationsTiny;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(); break;
                }
                destroyChunk = !chunk.Parent.Free(chunk, handle);
            }
            if (destroyChunk)
            {
                // destroyChunk not need to be called while holding the synchronized lock.
                this.DestroyChunk(chunk);
            }
        }

        internal PoolSubpage<T> FindSubpagePoolHead(int elemSize)
        {
            int tableIdx;
            PoolSubpage<T>[] table;
            if (IsTiny(elemSize))
            {
                // < 512
                tableIdx = elemSize.RightUShift(4);
                table = this.tinySubpagePools;
            }
            else
            {
                tableIdx = 0;
                elemSize = elemSize.RightUShift(10);
                while (elemSize != 0)
                {
                    elemSize = elemSize.RightUShift(1);
                    tableIdx++;
                }
                table = this.smallSubpagePools;
            }

            return table[tableIdx];
        }

        internal int NormalizeCapacity(int reqCapacity)
        {
            if (reqCapacity < 0) { ThrowHelper.ThrowArgumentException_PositiveOrZero(reqCapacity, ExceptionArgument.reqCapacity); }

            if (reqCapacity >= this.ChunkSize)
            {
                return reqCapacity;
            }

            if (!IsTiny(reqCapacity))
            {
                // >= 512
                // Doubled

                int normalizedCapacity = reqCapacity;
                normalizedCapacity--;
                normalizedCapacity |= normalizedCapacity.RightUShift(1);
                normalizedCapacity |= normalizedCapacity.RightUShift(2);
                normalizedCapacity |= normalizedCapacity.RightUShift(4);
                normalizedCapacity |= normalizedCapacity.RightUShift(8);
                normalizedCapacity |= normalizedCapacity.RightUShift(16);
                normalizedCapacity++;

                if (normalizedCapacity < 0)
                {
                    normalizedCapacity = normalizedCapacity.RightUShift(1);
                }

                return normalizedCapacity;
            }

            // Quantum-spaced
            if (0u >= (uint)(reqCapacity & 15))
            {
                return reqCapacity;
            }

            return (reqCapacity & ~15) + 16;
        }

        internal void Reallocate(PooledByteBuffer<T> buf, int newCapacity, bool freeOldMemory)
        {
            if (newCapacity < 0 || newCapacity > buf.MaxCapacity) { ThrowHelper.ThrowIndexOutOfRangeException(); }

            int oldCapacity = buf.Length;
            if (oldCapacity == newCapacity)
            {
                return;
            }

            PoolChunk<T> oldChunk = buf.Chunk;
            long oldHandle = buf.Handle;
            T oldMemory = buf.Memory;
            int oldOffset = buf.Offset;
            int oldMaxLength = buf.MaxLength;
            int readerIndex = buf.ReaderIndex;
            int writerIndex = buf.WriterIndex;

            this.Allocate(this.Parent.ThreadCache<T>(), buf, newCapacity);
            if (newCapacity > oldCapacity)
            {
                this.MemoryCopy(
                    oldMemory, oldOffset,
                    buf.Memory, buf.Offset, oldCapacity);
            }
            else if (newCapacity < oldCapacity)
            {
                if (readerIndex < newCapacity)
                {
                    if (writerIndex > newCapacity)
                    {
                        writerIndex = newCapacity;
                    }
                    this.MemoryCopy(
                        oldMemory, oldOffset + readerIndex,
                        buf.Memory, buf.Offset + readerIndex, writerIndex - readerIndex);
                }
                else
                {
                    readerIndex = writerIndex = newCapacity;
                }
            }

            buf.SetIndex(readerIndex, writerIndex);

            if (freeOldMemory)
            {
                this.Free(oldChunk, oldHandle, oldMaxLength, buf.Cache);
            }
        }

        internal void IncrementNumThreadCaches() => Interlocked.Increment(ref this.numThreadCaches);

        internal void DecrementNumThreadCaches() => Interlocked.Decrement(ref this.numThreadCaches);

        public int NumThreadCaches => Volatile.Read(ref this.numThreadCaches);

        public int NumTinySubpages => this.tinySubpagePools.Length;

        public int NumSmallSubpages => this.smallSubpagePools.Length;

        public int NumChunkLists => this.chunkListMetrics.Count;

#if NET40
        public IList<IPoolSubpageMetric> TinySubpages => SubPageMetricList(this.tinySubpagePools);

        public IList<IPoolSubpageMetric> SmallSubpages => SubPageMetricList(this.smallSubpagePools);

        public IList<IPoolChunkListMetric> ChunkLists => this.chunkListMetrics;
#else
        public IReadOnlyList<IPoolSubpageMetric> TinySubpages => SubPageMetricList(this.tinySubpagePools);

        public IReadOnlyList<IPoolSubpageMetric> SmallSubpages => SubPageMetricList(this.smallSubpagePools);

        public IReadOnlyList<IPoolChunkListMetric> ChunkLists => this.chunkListMetrics;
#endif

        static List<IPoolSubpageMetric> SubPageMetricList(PoolSubpage<T>[] pages)
        {
            var metrics = new List<IPoolSubpageMetric>();
            foreach (PoolSubpage<T> head in pages)
            {
                if (head.Next == head)
                {
                    continue;
                }
                PoolSubpage<T> s = head.Next;
                while (true)
                {
                    metrics.Add(s);
                    s = s.Next;
                    if (s == head)
                    {
                        break;
                    }
                }
            }
            return metrics;
        }

        public long NumAllocations
        {
            get
            {
                long allocsNormal;
                lock (this)
                {
                    allocsNormal = this.allocationsNormal;
                }

                return this.NumTinyAllocations + this.NumSmallAllocations + allocsNormal + this.NumHugeAllocations;
            }
        }

        public long NumTinyAllocations => Volatile.Read(ref this.allocationsTiny);

        public long NumSmallAllocations => Volatile.Read(ref this.allocationsSmall);

        public long NumNormalAllocations => Volatile.Read(ref this.allocationsNormal);

        public long NumDeallocations
        {
            get
            {
                long deallocs;
                lock (this)
                {
                    deallocs = this.deallocationsTiny + this.deallocationsSmall + this.deallocationsNormal;
                }

                return deallocs + this.NumHugeDeallocations;
            }
        }

        public long NumTinyDeallocations => Volatile.Read(ref this.deallocationsTiny);

        public long NumSmallDeallocations => Volatile.Read(ref this.deallocationsSmall);

        public long NumNormalDeallocations => Volatile.Read(ref this.deallocationsNormal);

        public long NumHugeAllocations => Volatile.Read(ref this.allocationsHuge);

        public long NumHugeDeallocations => Volatile.Read(ref this.deallocationsHuge);

        public long NumActiveAllocations
        {
            get
            {
                long val = this.NumTinyAllocations + this.NumSmallAllocations + this.NumHugeAllocations
                    - this.NumHugeDeallocations;
                lock (this)
                {
                    val += this.allocationsNormal - (this.deallocationsTiny + this.deallocationsSmall + this.deallocationsNormal);
                }
                return Math.Max(val, 0);
            }
        }

        public long NumActiveTinyAllocations => Math.Max(this.NumTinyAllocations - this.NumTinyDeallocations, 0);

        public long NumActiveSmallAllocations => Math.Max(this.NumSmallAllocations - this.NumSmallDeallocations, 0);

        public long NumActiveNormalAllocations
        {
            get
            {
                long val;
                lock (this)
                {
                    val = this.allocationsNormal - this.deallocationsNormal;
                }
                return Math.Max(val, 0);
            }
        }

        public long NumActiveHugeAllocations => Math.Max(this.NumHugeAllocations - this.NumHugeDeallocations, 0);

        public long NumActiveBytes
        {
            get
            {
                long val = Volatile.Read(ref this.activeBytesHuge);
                lock (this)
                {
                    foreach (IPoolChunkListMetric t in this.chunkListMetrics)
                    {
                        foreach (IPoolChunkMetric m in t)
                        {
                            val += m.ChunkSize;
                        }
                    }
                }
                return Math.Max(0, val);
            }
        }

        protected abstract PoolChunk<T> NewChunk(int pageSize, int maxOrder, int pageShifts, int chunkSize);

        protected abstract PoolChunk<T> NewUnpooledChunk(int capacity);

        protected abstract PooledByteBuffer<T> NewByteBuf(int maxCapacity);

        protected abstract void MemoryCopy(T src, int srcOffset, T dst, int dstOffset, int length);

        protected internal abstract void DestroyChunk(PoolChunk<T> chunk);

        public override string ToString()
        {
            var buf = StringBuilderManager.Allocate()
                .Append("Chunk(s) at 0~25%:")
                .Append(StringUtil.Newline)
                .Append(this.qInit)
                .Append(StringUtil.Newline)
                .Append("Chunk(s) at 0~50%:")
                .Append(StringUtil.Newline)
                .Append(this.q000)
                .Append(StringUtil.Newline)
                .Append("Chunk(s) at 25~75%:")
                .Append(StringUtil.Newline)
                .Append(this.q025)
                .Append(StringUtil.Newline)
                .Append("Chunk(s) at 50~100%:")
                .Append(StringUtil.Newline)
                .Append(this.q050)
                .Append(StringUtil.Newline)
                .Append("Chunk(s) at 75~100%:")
                .Append(StringUtil.Newline)
                .Append(this.q075)
                .Append(StringUtil.Newline)
                .Append("Chunk(s) at 100%:")
                .Append(StringUtil.Newline)
                .Append(this.q100)
                .Append(StringUtil.Newline)
                .Append("tiny subpages:");
            AppendPoolSubPages(buf, this.tinySubpagePools);
            buf.Append(StringUtil.Newline)
                .Append("small subpages:");
            AppendPoolSubPages(buf, this.smallSubpagePools);
            buf.Append(StringUtil.Newline);

            return StringBuilderManager.ReturnAndFree(buf);
        }

        static void AppendPoolSubPages(StringBuilder buf, PoolSubpage<T>[] subpages)
        {
            for (int i = 0; i < subpages.Length; i++)
            {
                PoolSubpage<T> head = subpages[i];
                if (head.Next == head)
                {
                    continue;
                }

                buf.Append(StringUtil.Newline)
                    .Append(i)
                    .Append(": ");
                PoolSubpage<T> s = head.Next;
                while (true)
                {
                    buf.Append(s);
                    s = s.Next;
                    if (s == head)
                    {
                        break;
                    }
                }
            }
        }

        ~PoolArena()
        {
            DestroyPoolSubPages(this.smallSubpagePools);
            DestroyPoolSubPages(this.tinySubpagePools);
            this.DestroyPoolChunkLists(this.qInit, this.q000, this.q025, this.q050, this.q075, this.q100);
        }

        static void DestroyPoolSubPages(PoolSubpage<T>[] pages)
        {
            foreach (PoolSubpage<T> page in pages)
            {
                page.Destroy();
            }
        }

        void DestroyPoolChunkLists(params PoolChunkList<T>[] chunkLists)
        {
            foreach (PoolChunkList<T> chunkList in chunkLists)
            {
                chunkList.Destroy(this);
            }
        }
    }

    sealed class HeapArena : PoolArena<byte[]>
    {
        public HeapArena(PooledByteBufferAllocator parent, int pageSize, int maxOrder, int pageShifts, int chunkSize)
            : base(parent, pageSize, maxOrder, pageShifts, chunkSize)
        {
        }

        static byte[] NewByteArray(int size) => new byte[size];

        internal override bool IsDirect => false;

        protected override PoolChunk<byte[]> NewChunk(int pageSize, int maxOrder, int pageShifts, int chunkSize) =>
            new PoolChunk<byte[]>(this, NewByteArray(chunkSize), pageSize, maxOrder, pageShifts, chunkSize, 0, IntPtr.Zero);

        protected override PoolChunk<byte[]> NewUnpooledChunk(int capacity) =>
            new PoolChunk<byte[]>(this, NewByteArray(capacity), capacity, 0, IntPtr.Zero);

        protected internal override void DestroyChunk(PoolChunk<byte[]> chunk)
        {
            // Rely on GC.
        }

        protected override PooledByteBuffer<byte[]> NewByteBuf(int maxCapacity) =>
            PooledHeapByteBuffer.NewInstance(maxCapacity);

        protected override void MemoryCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
        {
            //if (0u >= (uint)length)
            //{
            //    return;
            //}

            PlatformDependent.CopyMemory(src, srcOffset, dst, dstOffset, length);
        }
    }

    // TODO: Maybe use Memory or OwnedMemory as direct arena/byte buffer type parameter in NETStandard 2.0
    // 鉴于几个方面考虑还是暂时不需要包装 IMemoryOwner<byte> 或 MemoryManager<byte>
    // 1、IByteBuffer直接操作数组性能更高，参考 System.IO.Pipelines 和 System.Buffers 的内部实现
    // 2、IByetBuffer实现 IReferenceCounted 接口，IMemoryOwner的管理会更加混乱
    // 3、现在 IByteBuffer 已经实现了 IBufferWriter<byte> 接口
    sealed class DirectArena : PoolArena<byte[]>
    {
        readonly List<MemoryChunk> memoryChunks;

        public DirectArena(PooledByteBufferAllocator parent, int pageSize, int maxOrder, int pageShifts, int chunkSize)
            : base(parent, pageSize, maxOrder, pageShifts, chunkSize)
        {
            this.memoryChunks = new List<MemoryChunk>();
        }

        static MemoryChunk NewMemoryChunk(int size) => new MemoryChunk(size);

        internal override bool IsDirect => true;

        protected override PoolChunk<byte[]> NewChunk(int pageSize, int maxOrder, int pageShifts, int chunkSize)
        {
            MemoryChunk memoryChunk = NewMemoryChunk(chunkSize);
            this.memoryChunks.Add(memoryChunk);
            var chunk = new PoolChunk<byte[]>(this, memoryChunk.Bytes, pageSize, maxOrder, pageShifts, chunkSize, 0, memoryChunk.NativePointer);
            return chunk;
        }

        protected override PoolChunk<byte[]> NewUnpooledChunk(int capacity)
        {
            MemoryChunk memoryChunk = NewMemoryChunk(capacity);
            this.memoryChunks.Add(memoryChunk);
            var chunk = new PoolChunk<byte[]>(this, memoryChunk.Bytes, capacity, 0, memoryChunk.NativePointer);
            return chunk;
        }

        protected override PooledByteBuffer<byte[]> NewByteBuf(int maxCapacity) =>
            PooledUnsafeDirectByteBuffer.NewInstance(maxCapacity);

        protected override unsafe void MemoryCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length) =>
#if NET40
            PlatformDependent.CopyMemory(src, srcOffset, dst, dstOffset, length);
#else
            PlatformDependent.CopyMemory((byte*)Unsafe.AsPointer(ref src[srcOffset]), (byte*)Unsafe.AsPointer(ref dst[dstOffset]), length);
#endif

        protected internal override void DestroyChunk(PoolChunk<byte[]> chunk)
        {
            for (int i = 0; i < this.memoryChunks.Count; i++)
            {
                MemoryChunk memoryChunk = this.memoryChunks[i];
                if (ReferenceEquals(chunk.Memory, memoryChunk.Bytes))
                {
                    this.memoryChunks.Remove(memoryChunk);
                    memoryChunk.Dispose();
                    break;
                }
            }
        }

        sealed class MemoryChunk : IDisposable
        {
            internal byte[] Bytes;
#if !NET40
            GCHandle handle;
#endif
            internal IntPtr NativePointer;

            internal MemoryChunk(int size)
            {
                this.Bytes = new byte[size];
#if !NET40
                this.handle = GCHandle.Alloc(this.Bytes, GCHandleType.Pinned);
                NativePointer = this.handle.AddrOfPinnedObject();
#else
                NativePointer = IntPtr.Zero;
#endif
            }

            void Release()
            {
#if !NET40
                if (this.handle.IsAllocated)
                {
                    try
                    {
                        this.handle.Free();
                    }
                    catch (InvalidOperationException)
                    {
                        // Free is not thread safe
                    }
                }
                this.NativePointer = IntPtr.Zero;
#endif
                this.Bytes = null;
            }

            public void Dispose()
            {
                this.Release();
                GC.SuppressFinalize(this);
            }

            ~MemoryChunk()
            {
                this.Release();
            }
        }

#if !NET40
        sealed class OwnedPinnedBlock : MemoryManager<byte>, IPoolMemoryOwner<byte>
        {
            private byte[] _array;
            private IntPtr _origin;
            private readonly int _offset;
            private readonly int _length;

            private volatile int _disposed;

            public unsafe OwnedPinnedBlock(byte[] array, void* origin, int offset, int length)
            {
                _array = array;
                _origin = new IntPtr(Unsafe.Add<byte>(origin, offset));
                _offset = offset;
                _length = length;
            }

            public IntPtr Origin => _origin;

            public byte[] Array => _array;

            public int Offset => _offset;

            public int Length => _length;

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                segment = new ArraySegment<byte>(_array, _offset, _length);
                return true; ;
            }

            public unsafe override Span<byte> GetSpan()
            {
                if (IsDisposed) { ThrowObjectDisposedException(); }
                return new Span<byte>(_origin.ToPointer(), _length);
            }

            public unsafe override MemoryHandle Pin(int elementIndex = 0)
            {
                if (IsDisposed) { ThrowObjectDisposedException(); }
                if (elementIndex != 0 && ((uint)elementIndex - 1) >= (uint)_length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.elementIndex);
                }
                return new MemoryHandle(Unsafe.Add<byte>(_origin.ToPointer(), elementIndex), default, this);
            }

            public override void Unpin()
            {
                // no-op
            }

            public bool IsDisposed => Constants.True == _disposed;

            protected override void Dispose(bool disposing)
            {
                if (Interlocked.Exchange(ref _disposed, Constants.True) == Constants.True) { return; }

                _array = null;
                _origin = IntPtr.Zero;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowObjectDisposedException()
            {
                throw GetObjectDisposedException();

                ObjectDisposedException GetObjectDisposedException()
                {
                    return new ObjectDisposedException(nameof(OwnedPinnedBlock));
                }
            }
        }
#endif
    }
}