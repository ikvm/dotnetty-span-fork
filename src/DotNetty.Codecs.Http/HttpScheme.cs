﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
namespace DotNetty.Codecs.Http
{
    using System;
    using DotNetty.Common.Utilities;

    public sealed class HttpScheme : IEquatable<HttpScheme>
    {
        // Scheme for non-secure HTTP connection.
        public static readonly HttpScheme Http = new HttpScheme(80, "http");

        // Scheme for secure HTTP connection.
        public static readonly HttpScheme Https = new HttpScheme(443, "https");

        readonly int port;
        readonly AsciiString name;

        HttpScheme(int port, string name)
        {
            this.port = port;
            this.name = AsciiString.Cached(name);
        }

        public AsciiString Name => this.name;

        public int Port => this.port;

        public override bool Equals(object obj)
        {
            if (obj is HttpScheme other)
            {
                return other.port == this.port && other.name.Equals(this.name);
            }

            return false;
        }

        public bool Equals(HttpScheme other)
        {
            if (ReferenceEquals(this, other)) { return true; }
            return other != null && other.port == this.port && other.name.Equals(this.name);
        }

        public override int GetHashCode() => this.port * 31 + this.name.GetHashCode();

        public override string ToString() => this.name.ToString();
    }
}
