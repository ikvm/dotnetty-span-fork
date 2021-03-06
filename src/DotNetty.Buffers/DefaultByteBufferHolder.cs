﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using DotNetty.Common;

    public class DefaultByteBufferHolder : IByteBufferHolder, IEquatable<IByteBufferHolder>
    {
        readonly IByteBuffer data;

        public DefaultByteBufferHolder(IByteBuffer data)
        {
            if (null == data) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.data); }

            this.data = data;
        }

        public IByteBuffer Content
        {
            get
            {
                if (this.data.ReferenceCount <= 0)
                {
                    ThrowHelper.ThrowIllegalReferenceCountException(this.data.ReferenceCount);
                }

                return this.data;
            }
        }

        public virtual IByteBufferHolder Copy() => this.Replace(this.data.Copy());

        public virtual IByteBufferHolder Duplicate() => this.Replace(this.data.Duplicate());

        public virtual IByteBufferHolder RetainedDuplicate() => this.Replace(this.data.RetainedDuplicate());

        public virtual IByteBufferHolder Replace(IByteBuffer content) => new DefaultByteBufferHolder(content);

        public virtual int ReferenceCount => this.data.ReferenceCount;

        public IReferenceCounted Retain()
        {
            this.data.Retain();
            return this;
        }

        public IReferenceCounted Retain(int increment)
        {
            this.data.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            this.data.Touch();
            return this;
        }

        public IReferenceCounted Touch(object hint)
        {
            this.data.Touch(hint);
            return this;
        }

        public bool Release() => this.data.Release();

        public bool Release(int decrement) => this.data.Release(decrement);

        protected string ContentToString() => this.data.ToString();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is IByteBufferHolder holder)
            {
                return this.data.Equals(holder.Content);
            }

            return false;
        }

        public bool Equals(IByteBufferHolder other)
        {
            if (ReferenceEquals(this, other)) { return true; }
            return other != null && this.data.Equals(other.Content);

        }

        public override int GetHashCode() => this.data.GetHashCode();
    }
}
