﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Common;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public abstract class MessageToMessageEncoder<T> : ChannelHandlerAdapter
    {
        /// <summary>
        ///     Returns {@code true} if the given message should be handled. If {@code false} it will be passed to the next
        ///     {@link ChannelHandler} in the {@link ChannelPipeline}.
        /// </summary>
        public virtual bool AcceptOutboundMessage(object msg) => msg is T;

        public override void Write(IChannelHandlerContext ctx, object msg, IPromise promise)
        {
            ThreadLocalObjectList output = null;
            try
            {
                if (this.AcceptOutboundMessage(msg))
                {
                    output = ThreadLocalObjectList.NewInstance();
                    var cast = (T)msg;
                    try
                    {
                        this.Encode(ctx, cast, output);
                    }
                    finally
                    {
                        ReferenceCountUtil.Release(cast);
                    }

                    if (0u >= (uint)output.Count)
                    {
                        output.Return();
                        output = null;

                        CThrowHelper.ThrowEncoderException_MustProduceAtLeastOneMsg(this.GetType());
                    }
                }
                else
                {
                    ctx.WriteAsync(msg, promise);
                }
            }
            catch (EncoderException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CThrowHelper.ThrowEncoderException(ex); // todo: we don't have a stack on EncoderException but it's present on inner exception.
            }
            finally
            {
                if (output != null)
                {
                    int lastItemIndex = output.Count - 1;
                    if (0u >= (uint)lastItemIndex)
                    {
                        ctx.WriteAsync(output[0], promise);
                    }
                    else if (lastItemIndex > 0)
                    {
                        // Check if we can use a voidPromise for our extra writes to reduce GC-Pressure
                        // See https://github.com/netty/netty/issues/2525
                        if (promise == ctx.VoidPromise())
                        {
                            WriteVoidPromise(ctx, output);
                        }
                        else
                        {
                            WritePromiseCombiner(ctx, output, promise);
                        }
                    }
                    output.Return();
                }
            }
        }

        static void WriteVoidPromise(IChannelHandlerContext ctx, List<object> output)
        {
            IPromise voidPromise = ctx.VoidPromise();
            for (int i = 0; i < output.Count; i++)
            {
                ctx.WriteAsync(output[i], voidPromise);
            }
        }

        static void WritePromiseCombiner(IChannelHandlerContext ctx, List<object> output, IPromise promise)
        {
            PromiseCombiner combiner = new PromiseCombiner();
            for (int i = 0; i < output.Count; i++)
            {
                combiner.Add(ctx.WriteAsync(output[i]));
            }
            combiner.Finish(promise);
        }

        /// <summary>
        ///     Encode from one message to an other. This method will be called for each written message that can be handled
        ///     by this encoder.
        ///     @param context           the {@link ChannelHandlerContext} which this {@link MessageToMessageEncoder} belongs to
        ///     @param message           the message to encode to an other one
        ///     @param output           the {@link List} into which the encoded message should be added
        ///     needs to do some kind of aggragation
        ///     @throws Exception    is thrown if an error accour
        /// </summary>
        protected internal abstract void Encode(IChannelHandlerContext context, T message, List<object> output);
    }
}