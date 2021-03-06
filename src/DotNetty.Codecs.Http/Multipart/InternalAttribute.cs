﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using CuteAnt.Pool;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    sealed class InternalAttribute : AbstractReferenceCounted, IInterfaceHttpData
    {
        readonly List<IByteBuffer> value = new List<IByteBuffer>();
        readonly Encoding charset;
        int size;

        internal InternalAttribute(Encoding charset)
        {
            this.charset = charset;
        }

        public HttpDataType DataType => HttpDataType.InternalAttribute;

        public void AddValue(string stringValue)
        {
            if (null == stringValue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stringValue); }

            IByteBuffer buf = ArrayPooled.CopiedBuffer(stringValue, this.charset);
            this.value.Add(buf);
            this.size += buf.ReadableBytes;
        }

        public void AddValue(string stringValue, int rank)
        {
            if (null == stringValue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stringValue); }

            IByteBuffer buf = ArrayPooled.CopiedBuffer(stringValue, this.charset);
            this.value[rank] = buf;
            this.size += buf.ReadableBytes;
        }

        public void SetValue(string stringValue, int rank)
        {
            if (null == stringValue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stringValue); }

            IByteBuffer buf = ArrayPooled.CopiedBuffer(stringValue, this.charset);
            IByteBuffer old = this.value[rank];
            this.value[rank] = buf;
            if (old != null)
            {
                this.size -= old.ReadableBytes;
                old.Release();
            }
            this.size += buf.ReadableBytes;
        }

        public override int GetHashCode() => this.Name.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is InternalAttribute attribute)
            {
                return this.Name.Equals(attribute.Name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public int CompareTo(IInterfaceHttpData other)
        {
            if (other is InternalAttribute attr)
            {
                return this.CompareTo(attr);
            }

            return ThrowHelper.ThrowArgumentException_CompareToHttpData(this.DataType, other.DataType);
        }

        public int CompareTo(InternalAttribute other) => string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override string ToString()
        {
            var result = StringBuilderManager.Allocate();
            foreach (IByteBuffer buf in this.value)
            {
                result.Append(buf.ToString(this.charset));
            }

            return StringBuilderManager.ReturnAndFree(result);
        }

        public int Size => this.size;

        public IByteBuffer ToByteBuffer()
        {
            CompositeByteBuffer compositeBuffer = ArrayPooled.CompositeBuffer();
            compositeBuffer.AddComponents(this.value);
            compositeBuffer.SetWriterIndex(this.size);
            compositeBuffer.SetReaderIndex(0);

            return compositeBuffer;
        }

        public string Name => nameof(InternalAttribute);

        protected override void Deallocate()
        {
            // Do nothing
        }

        protected override IReferenceCounted RetainCore(int increment)
        {
            foreach (IByteBuffer buf in this.value)
            {
                buf.Retain(increment);
            }
            return this;
        }

        public override IReferenceCounted Touch(object hint)
        {
            foreach (IByteBuffer buf in this.value)
            {
                buf.Touch(hint);
            }
            return this;
        }
    }
}
