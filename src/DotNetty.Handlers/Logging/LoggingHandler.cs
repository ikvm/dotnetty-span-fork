﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Handlers.Logging
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using CuteAnt.Pool;
    using DotNetty.Buffers;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Transport.Channels;

    /// <summary>
    ///     A <see cref="IChannelHandler" /> that logs all events using a logging framework.
    ///     By default, all events are logged at <tt>DEBUG</tt> level.
    /// </summary>
    public class LoggingHandler : ChannelHandlerAdapter
    {
        const LogLevel DefaultLevel = LogLevel.DEBUG;

        protected readonly InternalLogLevel InternalLevel;
        protected readonly IInternalLogger Logger;

        /// <summary>
        ///     Creates a new instance whose logger name is the fully qualified class
        ///     name of the instance with hex dump enabled.
        /// </summary>
        public LoggingHandler()
            : this(DefaultLevel)
        {
        }

        /// <summary>
        ///     Creates a new instance whose logger name is the fully qualified class
        ///     name of the instance
        /// </summary>
        /// <param name="level">the log level</param>
        public LoggingHandler(LogLevel level)
            : this(typeof(LoggingHandler), level)
        {
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name and with hex dump
        ///     enabled
        /// </summary>
        /// <param name="type">the class type to generate the logger for</param>
        public LoggingHandler(Type type)
            : this(type, DefaultLevel)
        {
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name.
        /// </summary>
        /// <param name="type">the class type to generate the logger for</param>
        /// <param name="level">the log level</param>
        public LoggingHandler(Type type, LogLevel level)
        {
            if (type == null)
            {
                ThrowHelper.ThrowNullReferenceException(ExceptionArgument.type);
            }

            this.Logger = InternalLoggerFactory.GetInstance(type);
            this.Level = level;
            this.InternalLevel = level.ToInternalLevel();
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name using the default log level.
        /// </summary>
        /// <param name="name">the name of the class to use for the logger</param>
        public LoggingHandler(string name)
            : this(name, DefaultLevel)
        {
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name.
        /// </summary>
        /// <param name="name">the name of the class to use for the logger</param>
        /// <param name="level">the log level</param>
        public LoggingHandler(string name, LogLevel level)
        {
            if (name == null)
            {
                ThrowHelper.ThrowNullReferenceException(ExceptionArgument.name);
            }

            this.Logger = InternalLoggerFactory.GetInstance(name);
            this.Level = level;
            this.InternalLevel = level.ToInternalLevel();
        }

        public override bool IsSharable => true;

        /// <summary>
        ///     Returns the <see cref="LogLevel" /> that this handler uses to log
        /// </summary>
        public LogLevel Level { get; }

        public override void ChannelRegistered(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "REGISTERED"));
            }
            ctx.FireChannelRegistered();
        }

        public override void ChannelUnregistered(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "UNREGISTERED"));
            }
            ctx.FireChannelUnregistered();
        }

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "ACTIVE"));
            }
            ctx.FireChannelActive();
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "INACTIVE"));
            }
            ctx.FireChannelInactive();
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "EXCEPTION", cause), cause);
            }
            ctx.FireExceptionCaught(cause);
        }

        public override void UserEventTriggered(IChannelHandlerContext ctx, object evt)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "USER_EVENT", evt));
            }
            ctx.FireUserEventTriggered(evt);
        }

        public override Task BindAsync(IChannelHandlerContext ctx, EndPoint localAddress)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "BIND", localAddress));
            }
            return ctx.BindAsync(localAddress);
        }

        public override Task ConnectAsync(IChannelHandlerContext ctx, EndPoint remoteAddress, EndPoint localAddress)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "CONNECT", remoteAddress, localAddress));
            }
            return ctx.ConnectAsync(remoteAddress, localAddress);
        }

        public override void Disconnect(IChannelHandlerContext ctx, IPromise promise)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "DISCONNECT"));
            }
            ctx.DisconnectAsync(promise);
        }

        public override void Close(IChannelHandlerContext ctx, IPromise promise)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "CLOSE"));
            }
            ctx.CloseAsync(promise);
        }

        public override void Deregister(IChannelHandlerContext ctx, IPromise promise)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "DEREGISTER"));
            }
            ctx.DeregisterAsync(promise);
        }

        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "RECEIVED", message));
            }
            ctx.FireChannelRead(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "RECEIVED_COMPLETE"));
            }
            ctx.FireChannelReadComplete();
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "WRITABILITY", ctx.Channel.IsWritable));
            }
            ctx.FireChannelWritabilityChanged();
        }

        public override void HandlerAdded(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "HANDLER_ADDED"));
            }
        }
        public override void HandlerRemoved(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "HANDLER_REMOVED"));
            }
        }

        public override void Read(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "READ"));
            }
            ctx.Read();
        }

        public override void Write(IChannelHandlerContext ctx, object msg, IPromise promise)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "WRITE", msg));
            }
            ctx.WriteAsync(msg, promise);
        }

        public override void Flush(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "FLUSH"));
            }
            ctx.Flush();
        }

        /// <summary>
        ///     Formats an event and returns the formatted message
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="eventName">the name of the event</param>
        protected virtual string Format(IChannelHandlerContext ctx, string eventName)
        {
            string chStr = ctx.Channel.ToString();
            var sb = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length)
                .Append(chStr)
                .Append(' ')
                .Append(eventName);
            return StringBuilderManager.ReturnAndFree(sb);
        }

        /// <summary>
        ///     Formats an event and returns the formatted message.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="eventName">the name of the event</param>
        /// <param name="arg">the argument of the event</param>
        protected virtual string Format(IChannelHandlerContext ctx, string eventName, object arg)
        {
            switch (arg)
            {
                case IByteBuffer byteBuffer:
                    return this.FormatByteBuffer(ctx, eventName, byteBuffer);

                case IByteBufferHolder byteBufferHolder:
                    return this.FormatByteBufferHolder(ctx, eventName, byteBufferHolder);

                default:
                    return this.FormatSimple(ctx, eventName, arg);
            }
        }

        /// <summary>
        ///     Formats an event and returns the formatted message.  This method is currently only used for formatting
        ///     <see cref="IChannelHandler.ConnectAsync(IChannelHandlerContext, EndPoint, EndPoint)" />
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="eventName">the name of the event</param>
        /// <param name="firstArg">the first argument of the event</param>
        /// <param name="secondArg">the second argument of the event</param>
        protected virtual string Format(IChannelHandlerContext ctx, string eventName, object firstArg, object secondArg)
        {
            if (secondArg == null)
            {
                return this.FormatSimple(ctx, eventName, firstArg);
            }
            string chStr = ctx.Channel.ToString();
            string arg1Str = firstArg.ToString();
            string arg2Str = secondArg.ToString();

            var buf = StringBuilderManager.Allocate(
                chStr.Length + 1 + eventName.Length + 2 + arg1Str.Length + 2 + arg2Str.Length);
            buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
                .Append(arg1Str).Append(", ").Append(arg2Str);
            return StringBuilderManager.ReturnAndFree(buf);
        }

        /// <summary>
        ///     Generates the default log message of the specified event whose argument is a  <see cref="IByteBuffer" />.
        /// </summary>
        string FormatByteBuffer(IChannelHandlerContext ctx, string eventName, IByteBuffer msg)
        {
            string chStr = ctx.Channel.ToString();
            int length = msg.ReadableBytes;
            if (0u >= (uint)length)
            {
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 4);
                buf.Append(chStr).Append(' ').Append(eventName).Append(": 0B");
                return StringBuilderManager.ReturnAndFree(buf);
            }
            else
            {
                int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + 10 + 1 + 2 + rows * 80);

                buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(length).Append('B').Append('\n');
                ByteBufferUtil.AppendPrettyHexDump(buf, msg);

                return StringBuilderManager.ReturnAndFree(buf);
            }
        }

        /// <summary>
        ///     Generates the default log message of the specified event whose argument is a <see cref="IByteBufferHolder" />.
        /// </summary>
        string FormatByteBufferHolder(IChannelHandlerContext ctx, string eventName, IByteBufferHolder msg)
        {
            string chStr = ctx.Channel.ToString();
            string msgStr = msg.ToString();
            IByteBuffer content = msg.Content;
            int length = content.ReadableBytes;
            if (0u >= (uint)length)
            {
                var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 4);
                buf.Append(chStr).Append(' ').Append(eventName).Append(", ").Append(msgStr).Append(", 0B");
                return StringBuilderManager.ReturnAndFree(buf);
            }
            else
            {
                int rows = length / 16 + (length % 15 == 0 ? 0 : 1) + 4;
                var buf = StringBuilderManager.Allocate(
                    chStr.Length + 1 + eventName.Length + 2 + msgStr.Length + 2 + 10 + 1 + 2 + rows * 80);

                buf.Append(chStr).Append(' ').Append(eventName).Append(": ")
                    .Append(msgStr).Append(", ").Append(length).Append('B').Append('\n');
                ByteBufferUtil.AppendPrettyHexDump(buf, content);

                return StringBuilderManager.ReturnAndFree(buf);
            }
        }

        /// <summary>
        ///     Generates the default log message of the specified event whose argument is an arbitrary object.
        /// </summary>
        string FormatSimple(IChannelHandlerContext ctx, string eventName, object msg)
        {
            string chStr = ctx.Channel.ToString();
            string msgStr = msg.ToString();
            var buf = StringBuilderManager.Allocate(chStr.Length + 1 + eventName.Length + 2 + msgStr.Length);
            return StringBuilderManager.ReturnAndFree(buf.Append(chStr).Append(' ').Append(eventName).Append(": ").Append(msgStr));
        }
    }
}