﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Protobuf
{
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    ///
    /// An encoder that prepends the the Google Protocol Buffers
    /// http://code.google.com/apis/protocolbuffers/docs/encoding.html#varints
    /// Base 128 Varints integer length field. 
    /// For example:
    /// 
    /// BEFORE ENCODE (300 bytes)       AFTER ENCODE (302 bytes)
    ///  +---------------+               +--------+---------------+
    ///  | Protobuf Data |-------------->| Length | Protobuf Data |
    ///  |  (300 bytes)  |               | 0xAC02 |  (300 bytes)  |
    ///  +---------------+               +--------+---------------+
    public class ProtobufVarint32LengthFieldPrepender : MessageToByteEncoder2<IByteBuffer>
    {
        protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
        {
            if (null == context) { CThrowHelper.ThrowArgumentNullException(CExceptionArgument.context); }
            if (null == message) { CThrowHelper.ThrowArgumentNullException(CExceptionArgument.message); }
            if (null == output) { CThrowHelper.ThrowArgumentNullException(CExceptionArgument.output); }

            int bodyLength = message.ReadableBytes;
            int headerLength = ComputeRawVarint32Size(bodyLength);
            output.EnsureWritable(headerLength + bodyLength);

            WriteRawVarint32(output, bodyLength);
            output.WriteBytes(message, message.ReaderIndex, bodyLength);
        }

        internal static void WriteRawVarint32(IByteBuffer output, int value)
        {
            if (null == output) { CThrowHelper.ThrowArgumentNullException(CExceptionArgument.output); }

            while (true)
            {
                if ((value & ~0x7F) == 0)
                {
                    output.WriteByte(value);
                    return;
                }

                output.WriteByte((value & 0x7F) | 0x80);
                value >>= 7;
            }
        }

        public static int ComputeRawVarint32Size(int value)
        {
            if ((value & (0xffffffff << 7)) == 0)
            {
                return 1;
            }

            if ((value & (0xffffffff << 14)) == 0)
            {
                return 2;
            }

            if ((value & (0xffffffff << 21)) == 0)
            {
                return 3;
            }

            if ((value & (0xffffffff << 28)) == 0)
            {
                return 4;
            }

            return 5;
        }

        public override bool IsSharable => true;
    }
}
