﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http2
{
    using System;
    using DotNetty.Codecs.Http;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    /// <summary>
    /// Translates HTTP/1.x object writes into HTTP/2 frames.
    /// <para>See <see cref="InboundHttp2ToHttpAdapter"/> to get translation from HTTP/2 frames to HTTP/1.x objects.</para>
    /// </summary>
    public class HttpToHttp2ConnectionHandler : Http2ConnectionHandler
    {
        private readonly bool validateHeaders;
        private int currentStreamId;

        public HttpToHttp2ConnectionHandler(IHttp2ConnectionDecoder decoder, IHttp2ConnectionEncoder encoder,
            Http2Settings initialSettings, bool validateHeaders)
            : base(decoder, encoder, initialSettings)
        {
            this.validateHeaders = validateHeaders;
        }

        /// <summary>
        /// Get the next stream id either from the <see cref="HttpHeaders"/> object or HTTP/2 codec
        /// </summary>
        /// <param name="httpHeaders">The HTTP/1.x headers object to look for the stream id</param>
        /// <returns>The stream id to use with this <see cref="HttpHeaders"/> object</returns>
        /// <exception cref="Exception">If the <paramref name="httpHeaders"/> object specifies an invalid stream id</exception>
        private int GetStreamId(HttpHeaders httpHeaders)
        {
            return httpHeaders.GetInt(HttpConversionUtil.ExtensionHeaderNames.StreamId,
                                      this.Connection.Local.IncrementAndGetNextStreamId);
        }

        /// <summary>
        /// Handles conversion of <see cref="IHttpMessage"/> and <see cref="IHttpContent"/> to HTTP/2 frames.
        /// </summary>
        public override void Write(IChannelHandlerContext ctx, object msg, IPromise promise)
        {
            var httpMsg = msg as IHttpMessage;
            var contentMsg = msg as IHttpContent;

            if (null == httpMsg && null == contentMsg)
            {
                ctx.WriteAsync(msg, promise);
                return;
            }

            var release = true;
            var promiseAggregator = new SimplePromiseAggregator(promise);
            try
            {
                var encoder = this.Encoder;
                var endStream = false;
                if (httpMsg != null)
                {
                    // Provide the user the opportunity to specify the streamId
                    this.currentStreamId = this.GetStreamId(httpMsg.Headers);

                    // Convert and write the headers.
                    var http2Headers = HttpConversionUtil.ToHttp2Headers(httpMsg, this.validateHeaders);
                    endStream = msg is IFullHttpMessage fullHttpMsg && !fullHttpMsg.Content.IsReadable();
                    WriteHeaders(ctx, encoder, this.currentStreamId, httpMsg.Headers, http2Headers, endStream, promiseAggregator);
                }

                if (!endStream && contentMsg != null)
                {
                    var isLastContent = false;
                    HttpHeaders trailers = EmptyHttpHeaders.Default;
                    IHttp2Headers http2Trailers = EmptyHttp2Headers.Instance;
                    if (msg is ILastHttpContent lastContentMsg)
                    {
                        isLastContent = true;

                        // Convert any trailing headers.
                        trailers = lastContentMsg.TrailingHeaders;
                        http2Trailers = HttpConversionUtil.ToHttp2Headers(trailers, this.validateHeaders);
                    }

                    // Write the data
                    var content = contentMsg.Content;
                    endStream = isLastContent && trailers.IsEmpty;
                    release = false;
                    encoder.WriteDataAsync(ctx, this.currentStreamId, content, 0, endStream, promiseAggregator.NewPromise());

                    if (!trailers.IsEmpty)
                    {
                        // Write trailing headers.
                        WriteHeaders(ctx, encoder, this.currentStreamId, trailers, http2Trailers, true, promiseAggregator);
                    }
                }
            }
            catch (Exception t)
            {
                this.OnError(ctx, true, t);
                promiseAggregator.SetException(t);
            }
            finally
            {
                if (release)
                {
                    ReferenceCountUtil.Release(msg);
                }
                promiseAggregator.DoneAllocatingPromises();
            }
        }

        private static void WriteHeaders(IChannelHandlerContext ctx, IHttp2ConnectionEncoder encoder, int streamId,
                                         HttpHeaders headers, IHttp2Headers http2Headers, bool endStream,
                                         SimplePromiseAggregator promiseAggregator)
        {
            int dependencyId = headers.GetInt(
                    HttpConversionUtil.ExtensionHeaderNames.StreamDependencyId, 0);
            short weight = headers.GetShort(
                    HttpConversionUtil.ExtensionHeaderNames.StreamWeight, Http2CodecUtil.DefaultPriorityWeight);
            encoder.WriteHeadersAsync(ctx, streamId, http2Headers, dependencyId, weight, false,
                    0, endStream, promiseAggregator.NewPromise());
        }
    }
}
