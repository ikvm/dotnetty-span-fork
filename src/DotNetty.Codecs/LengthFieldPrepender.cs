﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    /// <summary>
    ///     An encoder that prepends the length of the message.  The length value is
    ///     prepended as a binary form.
    ///     <p />
    ///     For example, <tt>{@link LengthFieldPrepender}(2)</tt> will encode the
    ///     following 12-bytes string:
    ///     <pre>
    ///         +----------------+
    ///         | "HELLO, WORLD" |
    ///         +----------------+
    ///     </pre>
    ///     into the following:
    ///     <pre>
    ///         +--------+----------------+
    ///         + 0x000C | "HELLO, WORLD" |
    ///         +--------+----------------+
    ///     </pre>
    ///     If you turned on the {@code lengthIncludesLengthFieldLength} flag in the
    ///     constructor, the encoded data would look like the following
    ///     (12 (original data) + 2 (prepended data) = 14 (0xE)):
    ///     <pre>
    ///         +--------+----------------+
    ///         + 0x000E | "HELLO, WORLD" |
    ///         +--------+----------------+
    ///     </pre>
    /// </summary>
    public class LengthFieldPrepender : MessageToMessageEncoder2<IByteBuffer>
    {
        readonly ByteOrder byteOrder;
        readonly int lengthFieldLength;
        readonly bool lengthFieldIncludesLengthFieldLength;
        readonly int lengthAdjustment;

        /// <summary>
        ///     Creates a new <see cref="LengthFieldPrepender" /> instance.
        /// </summary>
        /// <param name="lengthFieldLength">
        ///     The length of the prepended length field.
        ///     Only 1, 2, 3, 4, and 8 are allowed.
        /// </param>
        public LengthFieldPrepender(int lengthFieldLength)
            : this(lengthFieldLength, false)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="LengthFieldPrepender" /> instance.
        /// </summary>
        /// <param name="lengthFieldLength">
        ///     The length of the prepended length field.
        ///     Only 1, 2, 3, 4, and 8 are allowed.
        /// </param>
        /// <param name="lengthFieldIncludesLengthFieldLength">
        ///     If <c>true</c>, the length of the prepended length field is added
        ///     to the value of the prepended length field.
        /// </param>
        public LengthFieldPrepender(int lengthFieldLength, bool lengthFieldIncludesLengthFieldLength)
            : this(lengthFieldLength, 0, lengthFieldIncludesLengthFieldLength)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="LengthFieldPrepender" /> instance.
        /// </summary>
        /// <param name="lengthFieldLength">
        ///     The length of the prepended length field.
        ///     Only 1, 2, 3, 4, and 8 are allowed.
        /// </param>
        /// <param name="lengthAdjustment">The compensation value to add to the value of the length field.</param>
        public LengthFieldPrepender(int lengthFieldLength, int lengthAdjustment)
            : this(lengthFieldLength, lengthAdjustment, false)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="LengthFieldPrepender" /> instance.
        /// </summary>
        /// <param name="lengthFieldLength">
        ///     The length of the prepended length field.
        ///     Only 1, 2, 3, 4, and 8 are allowed.
        /// </param>
        /// <param name="lengthFieldIncludesLengthFieldLength">
        ///     If <c>true</c>, the length of the prepended length field is added
        ///     to the value of the prepended length field.
        /// </param>
        /// <param name="lengthAdjustment">The compensation value to add to the value of the length field.</param>
        public LengthFieldPrepender(int lengthFieldLength, int lengthAdjustment, bool lengthFieldIncludesLengthFieldLength)
            : this(ByteOrder.BigEndian, lengthFieldLength, lengthAdjustment, lengthFieldIncludesLengthFieldLength)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="LengthFieldPrepender" /> instance.
        /// </summary>
        /// <param name="byteOrder">The <see cref="ByteOrder" /> of the length field.</param>
        /// <param name="lengthFieldLength">
        ///     The length of the prepended length field.
        ///     Only 1, 2, 3, 4, and 8 are allowed.
        /// </param>
        /// <param name="lengthFieldIncludesLengthFieldLength">
        ///     If <c>true</c>, the length of the prepended length field is added
        ///     to the value of the prepended length field.
        /// </param>
        /// <param name="lengthAdjustment">The compensation value to add to the value of the length field.</param>
        public LengthFieldPrepender(ByteOrder byteOrder, int lengthFieldLength, int lengthAdjustment, bool lengthFieldIncludesLengthFieldLength)
        {
            if (lengthFieldLength != 1 && lengthFieldLength != 2 && lengthFieldLength != 3 &&
                lengthFieldLength != 4 && lengthFieldLength != 8)
            {
                throw new ArgumentException(
                    "lengthFieldLength must be either 1, 2, 3, 4, or 8: " +
                        lengthFieldLength, nameof(lengthFieldLength));
            }

            this.byteOrder = byteOrder;
            this.lengthFieldLength = lengthFieldLength;
            this.lengthFieldIncludesLengthFieldLength = lengthFieldIncludesLengthFieldLength;
            this.lengthAdjustment = lengthAdjustment;
        }

        /// <inheritdoc />
        protected internal override void Encode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            int length = message.ReadableBytes + this.lengthAdjustment;
            var lengthFieldLen = this.lengthFieldLength;
            if (this.lengthFieldIncludesLengthFieldLength)
            {
                length += lengthFieldLen;
            }

            const uint TooBigOrNegative = int.MaxValue;
            uint nlen = unchecked((uint)length);
            if (nlen > TooBigOrNegative)
            {
                CThrowHelper.ThrowArgumentException_LessThanZero(length);
            }

            switch (lengthFieldLen)
            {
                case 1:
                    if (nlen >= 256u)
                    {
                        CThrowHelper.ThrowArgumentException_Byte(length);
                    }
                    output.Add(context.Allocator.Buffer(1).WriteByte((byte)length));
                    break;
                case 2:
                    if (nlen >= 65536u)
                    {
                        CThrowHelper.ThrowArgumentException_Short(length);
                    }
                    output.Add(this.byteOrder == ByteOrder.BigEndian 
                        ? context.Allocator.Buffer(2).WriteShort((short)length) 
                        : context.Allocator.Buffer(2).WriteShortLE((short)length));
                    break;
                case 3:
                    if (nlen >= 16777216u)
                    {
                        CThrowHelper.ThrowArgumentException_Medium(length);
                    }
                    output.Add(this.byteOrder == ByteOrder.BigEndian
                        ? context.Allocator.Buffer(3).WriteMedium(length)
                        : context.Allocator.Buffer(3).WriteMediumLE(length));
                    break;
                case 4:
                    output.Add(this.byteOrder == ByteOrder.BigEndian
                        ? context.Allocator.Buffer(4).WriteInt(length)
                        : context.Allocator.Buffer(4).WriteIntLE(length));
                    break;
                case 8:
                    output.Add(this.byteOrder == ByteOrder.BigEndian
                        ? context.Allocator.Buffer(8).WriteLong(length)
                        : context.Allocator.Buffer(8).WriteLongLE(length));
                    break;
                default:
                    CThrowHelper.ThrowException_UnknownLen(); break;
            }

            output.Add(message.Retain());
        }
    }
}
