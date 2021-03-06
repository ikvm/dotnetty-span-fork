﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http2
{
    using System;
    using System.Runtime.CompilerServices;
    using DotNetty.Buffers;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Transport.Channels;

    /// <summary>
    /// Logs HTTP2 frames for debugging purposes.
    /// </summary>
    public class Http2FrameLogger : ChannelHandlerAdapter, IHttp2FrameLogger
    {
        private const int BufferLengthThreshold = 64;
        readonly IInternalLogger logger;
        readonly InternalLogLevel level;

        public Http2FrameLogger(InternalLogLevel level)
            : this(level, InternalLoggerFactory.GetInstance<Http2FrameLogger>())
        {
        }

        public Http2FrameLogger(InternalLogLevel level, string name)
            : this(level, InternalLoggerFactory.GetInstance(name))
        {
        }

        public Http2FrameLogger(InternalLogLevel level, Type type)
            : this(level, InternalLoggerFactory.GetInstance(type))
        {
        }

        private Http2FrameLogger(InternalLogLevel level, IInternalLogger logger)
        {
            if (null == logger) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.logger); }
            this.level = level;
            this.logger = logger;
        }

        [MethodImpl(InlineMethod.Value)]
        public bool IsEnabled() => logger.IsEnabled(level);

        public void LogData(Direction direction, IChannelHandlerContext ctx, int streamId, IByteBuffer data,
            int padding, bool endStream)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} DATA: streamId={} padding={} endStream={} length={} bytes={}",
                    ctx.Channel, direction, streamId, padding, endStream, data.ReadableBytes, ToString(data));
            }
        }

        public void LogHeaders(Direction direction, IChannelHandlerContext ctx, int streamId, IHttp2Headers headers,
            int padding, bool endStream)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} HEADERS: streamId={} headers={} padding={} endStream={}",
                    ctx.Channel, direction, streamId, headers, padding, endStream);
            }
        }

        public void LogHeaders(Direction direction, IChannelHandlerContext ctx, int streamId, IHttp2Headers headers,
            int streamDependency, short weight, bool exclusive, int padding, bool endStream)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} HEADERS: streamId={} headers={} streamDependency={} weight={} exclusive={} padding={} endStream={}",
                    ctx.Channel, direction, streamId, headers, streamDependency, weight, exclusive, padding, endStream);
            }
        }

        public void LogPriority(Direction direction, IChannelHandlerContext ctx, int streamId, int streamDependency,
            short weight, bool exclusive)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} PRIORITY: streamId={} streamDependency={} weight={} exclusive={}",
                    ctx.Channel, direction, streamId, streamDependency, weight, exclusive);
            }
        }

        public void LogRstStream(Direction direction, IChannelHandlerContext ctx, int streamId, Http2Error errorCode)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} RST_STREAM: streamId={} errorCode={}",
                    ctx.Channel, direction, streamId, errorCode);
            }
        }

        public void LogSettingsAck(Direction direction, IChannelHandlerContext ctx)
        {
            logger.Log(level, "{} {} SETTINGS: ack=true", ctx.Channel, direction);
        }

        public void LogSettings(Direction direction, IChannelHandlerContext ctx, Http2Settings settings)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} SETTINGS: ack=false settings={}", ctx.Channel, direction, settings);
            }
        }

        public void LogPing(Direction direction, IChannelHandlerContext ctx, long data)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} PING: ack=false bytes={}", ctx.Channel, direction, data);
            }
        }

        public void LogPingAck(Direction direction, IChannelHandlerContext ctx, long data)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} PING: ack=true bytes={}", ctx.Channel, direction, data);
            }
        }

        public void LogPushPromise(Direction direction, IChannelHandlerContext ctx, int streamId, int promisedStreamId,
            IHttp2Headers headers, int padding)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} PUSH_PROMISE: streamId={} promisedStreamId={} headers={} padding={}",
                    ctx.Channel, direction, streamId, promisedStreamId, headers, padding);
            }
        }

        public void LogGoAway(Direction direction, IChannelHandlerContext ctx, int lastStreamId, Http2Error errorCode, IByteBuffer debugData)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} GO_AWAY: lastStreamId={} errorCode={} length={} bytes={}",
                    ctx.Channel, direction, lastStreamId, errorCode, debugData.ReadableBytes, ToString(debugData));
            }
        }

        public void LogWindowsUpdate(Direction direction, IChannelHandlerContext ctx, int streamId, int windowSizeIncrement)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} WINDOW_UPDATE: streamId={} windowSizeIncrement={}",
                    ctx.Channel, direction, streamId, windowSizeIncrement);
            }
        }

        public void LogUnknownFrame(Direction direction, IChannelHandlerContext ctx, Http2FrameTypes frameType, int streamId,
            Http2Flags flags, IByteBuffer data)
        {
            if (IsEnabled())
            {
                logger.Log(level, "{} {} UNKNOWN: frameType={} streamId={} flags={} length={} bytes={}",
                    ctx.Channel, direction, (byte)frameType & 0xFF, streamId, flags.Value, data.ReadableBytes, ToString(data));
            }
        }

        private string ToString(IByteBuffer buf)
        {
            if (level == InternalLogLevel.TRACE || buf.ReadableBytes <= BufferLengthThreshold)
            {
                // Log the entire buffer.
                return ByteBufferUtil.HexDump(buf);
            }

            // Otherwise just log the first 64 bytes.
            int length = Math.Min(buf.ReadableBytes, BufferLengthThreshold);
            return ByteBufferUtil.HexDump(buf, buf.ReaderIndex, length) + "...";
        }

    }
}
