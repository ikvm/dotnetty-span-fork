﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http2
{
    using System;
    using CuteAnt.Pool;

    /// <summary>
    /// Provides utility methods for accessing specific flags as defined by the HTTP/2 spec.
    /// </summary>
    public sealed class Http2Flags : IEquatable<Http2Flags>
    {
        public const int EndStream = 0x1;

        public const int EndHeaders = 0x4;

        public const int ACK = 0x1;

        public const int Padded = 0x8;

        public const int Priority = 0x20;

        int value;

        public Http2Flags()
        {
        }

        public Http2Flags(int value)
        {
            this.value = value;
        }

        /// <summary>
        /// Gets the underlying flags value.
        /// </summary>
        public int Value => this.value;

        /// <summary>
        /// Determines whether the <see cref="EndStream"/> flag is set. Only applies to DATA and HEADERS
        /// frames.
        /// </summary>
        /// <returns></returns>
        public bool EndOfStream()
        {
            return IsFlagSet(EndStream);
        }

        /// <summary>
        /// Determines whether the <see cref="EndHeaders"/> flag is set. Only applies for HEADERS,
        /// PUSH_PROMISE, and CONTINUATION frames.
        /// </summary>
        /// <returns></returns>
        public bool EndOfHeaders()
        {
            return IsFlagSet(EndHeaders);
        }

        /// <summary>
        /// Determines whether the flag is set indicating the presence of the exclusive, stream
        /// dependency, and weight fields in a HEADERS frame.
        /// </summary>
        /// <returns></returns>
        public bool PriorityPresent()
        {
            return IsFlagSet(Priority);
        }

        /// <summary>
        /// Determines whether the flag is set indicating that this frame is an ACK. Only applies for
        /// SETTINGS and PING frames.
        /// </summary>
        /// <returns></returns>
        public bool Ack()
        {
            return IsFlagSet(ACK);
        }

        /// <summary>
        /// For frames that include padding, indicates if the <see cref="Padded"/> field is present. Only
        /// applies to DATA, HEADERS, PUSH_PROMISE and CONTINUATION frames.
        /// </summary>
        /// <returns></returns>
        public bool PaddingPresent()
        {
            return IsFlagSet(Padded);
        }

        /// <summary>
        /// Gets the number of bytes expected for the priority fields of the payload. This is determined
        /// by the <see cref="PriorityPresent()"/> flag.
        /// </summary>
        /// <returns></returns>
        public int GetNumPriorityBytes()
        {
            return PriorityPresent() ? 5 : 0;
        }

        /// <summary>
        /// Gets the length in bytes of the padding presence field expected in the payload. This is
        /// determined by the <see cref="PaddingPresent()"/> flag.
        /// </summary>
        /// <returns></returns>
        public int GetPaddingPresenceFieldLength()
        {
            return PaddingPresent() ? 1 : 0;
        }

        /// <summary>
        /// Sets the <see cref="EndStream"/> flag.
        /// </summary>
        /// <param name="endOfStream"></param>
        /// <returns></returns>
        public Http2Flags EndOfStream(bool endOfStream)
        {
            return SetFlag(endOfStream, EndStream);
        }

        /// <summary>
        /// Sets the <see cref="EndHeaders"/> flag.
        /// </summary>
        /// <param name="endOfHeaders"></param>
        /// <returns></returns>
        public Http2Flags EndOfHeaders(bool endOfHeaders)
        {
            return SetFlag(endOfHeaders, EndHeaders);
        }

        /// <summary>
        /// Sets the <see cref="Priority"/> flag.
        /// </summary>
        /// <param name="priorityPresent"></param>
        /// <returns></returns>
        public Http2Flags PriorityPresent(bool priorityPresent)
        {
            return SetFlag(priorityPresent, Priority);
        }

        /// <summary>
        /// Sets the <see cref="Padded"/> flag.
        /// </summary>
        /// <param name="paddingPresent"></param>
        /// <returns></returns>
        public Http2Flags PaddingPresent(bool paddingPresent)
        {
            return SetFlag(paddingPresent, Padded);
        }

        /// <summary>
        /// Sets the <see cref="ACK"/> flag.
        /// </summary>
        /// <param name="ack"></param>
        /// <returns></returns>
        public Http2Flags Ack(bool ack)
        {
            return SetFlag(ack, ACK);
        }

        /// <summary>
        /// Generic method to set any flag.
        /// </summary>
        /// <param name="on">if the flag should be enabled or disabled.</param>
        /// <param name="mask">the mask that identifies the bit for the flag.</param>
        /// <returns>this instance.</returns>
        public Http2Flags SetFlag(bool on, int mask)
        {
            if (on)
            {
                this.value |= mask;
            }
            else
            {
                var oldValue = this.value;
                if ((oldValue & mask) != 0)
                {
                    this.value = (int)(oldValue & ~mask);
                }
            }
            return this;
        }

        /// <summary>
        /// Indicates whether or not a particular flag is set.
        /// </summary>
        /// <param name="mask">the mask identifying the bit for the particular flag being tested</param>
        /// <returns><c>true</c> if the flag is set</returns>
        public bool IsFlagSet(int mask)
        {
            return (this.value & mask) != 0;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + this.value;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is Http2Flags other) { return Equals(other); }

            return false;
        }

        public bool Equals(Http2Flags other)
        {
            if (ReferenceEquals(this, other)) { return true; }

            if (null == other) { return false; }

            return this.value == other.value;
        }

        public override string ToString()
        {
            var builder = StringBuilderManager.Allocate();
            builder.Append("value = ").Append(this.value).Append(" (");
            if (Ack())
            {
                builder.Append("ACK,");
            }
            if (EndOfHeaders())
            {
                builder.Append("END_OF_HEADERS,");
            }
            if (EndOfStream())
            {
                builder.Append("END_OF_STREAM,");
            }
            if (PriorityPresent())
            {
                builder.Append("PRIORITY_PRESENT,");
            }
            if (PaddingPresent())
            {
                builder.Append("PADDING_PRESENT,");
            }
            builder.Append(')');
            return StringBuilderManager.ReturnAndFree(builder);
        }
    }
}