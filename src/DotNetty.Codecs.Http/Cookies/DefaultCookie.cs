﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace DotNetty.Codecs.Http.Cookies
{
    using System;
    using System.Text;

    using static CookieUtil;

    public sealed class DefaultCookie : ICookie
    {
        // Constant for undefined MaxAge attribute value.
        const long UndefinedMaxAge = long.MinValue;

        readonly string name;
        string value;
        bool wrap;
        string domain;
        string path;
        long maxAge = UndefinedMaxAge;
        bool secure;
        bool httpOnly;

        public DefaultCookie(string name, string value)
        {
            if (string.IsNullOrEmpty(name?.Trim())) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name); }
            if (null == value) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value); }

            this.name = name;
            this.value = value;
        }

        public string Name => this.name;

        public string Value
        {
            get => this.value;
            set
            {
                if (null == value) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value); }
                this.value = value;
            }
        }

        public bool Wrap
        {
            get => this.wrap;
            set => this.wrap = value;
        }

        public string Domain
        {
            get => this.domain;
            set => this.domain = ValidateAttributeValue(nameof(this.domain), value);
        }

        public string Path
        {
            get => this.path;
            set => this.path = ValidateAttributeValue(nameof(this.path), value);
        }

        public long MaxAge
        {
            get => this.maxAge;
            set => this.maxAge = value;
        }

        public bool IsSecure
        {
            get => this.secure;
            set => this.secure = value;
        }

        public bool IsHttpOnly
        {
            get => this.httpOnly;
            set => this.httpOnly = value;
        }

        public override int GetHashCode() => this.name.GetHashCode();

        public override bool Equals(object obj) => obj is DefaultCookie cookie && this.Equals(cookie);

        public bool Equals(ICookie other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!string.Equals(this.name, other.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (this.path == null)
            {
                if (other.Path != null)
                {
                    return false;
                }
            }
            else if (other.Path == null)
            {
                return false;
            }
            else if (!string.Equals(this.path, other.Path, StringComparison.Ordinal))
            {
                return false;
            }

            if (this.domain == null)
            {
                if (other.Domain != null)
                {
                    return false;
                }
            }
            else
            {
                return string.Equals(this.domain, other.Domain, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        public int CompareTo(ICookie other)
        {
            int v = string.Compare(this.name, other.Name, StringComparison.Ordinal);
            if (v != 0)
            {
                return v;
            }

            if (this.path == null)
            {
                if (other.Path != null)
                {
                    return -1;
                }
            }
            else if (other.Path == null)
            {
                return 1;
            }
            else
            {
                v = string.Compare(this.path, other.Path, StringComparison.Ordinal);
                if (v != 0)
                {
                    return v;
                }
            }

            if (this.domain == null)
            {
                if (other.Domain != null)
                {
                    return -1;
                }
            }
            else if (other.Domain == null)
            {
                return 1;
            }
            else
            {
                v = string.Compare(this.domain, other.Domain, StringComparison.OrdinalIgnoreCase);
                return v;
            }

            return 0;
        }

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case null:
                    return 1;

                case ICookie cookie:
                    return this.CompareTo(cookie);

                default:
                    return ThrowHelper.ThrowArgumentException_CompareToCookie();
            }
        }

        public override string ToString()
        {
            StringBuilder buf = StringBuilder();
            buf.Append($"{this.name}={this.Value}");
            if (this.domain != null)
            {
                buf.Append($", domain={this.domain}");
            }
            if (this.path != null)
            {
                buf.Append($", path={this.path}");
            }
            if (this.maxAge >= 0)
            {
                buf.Append($", maxAge={this.maxAge}s");
            }
            if (this.secure)
            {
                buf.Append(", secure");
            }
            if (this.httpOnly)
            {
                buf.Append(", HTTPOnly");
            }

            return buf.ToString();
        }
    }
}
