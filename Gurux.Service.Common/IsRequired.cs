// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Specifies whether a mapped database column requires a non-null value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IsRequiredAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether null values are prohibited.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Initializes an attribute that prohibits null column values.
        /// </summary>
        public IsRequiredAttribute()
        {
            IsRequired = true;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsRequiredAttribute"/> class.
        /// </summary>
        /// <param name="required"><see langword="true"/> to prohibit null values; otherwise, <see langword="false"/>.</param>
        public IsRequiredAttribute(bool required)
        {
            IsRequired = required;
        }
    }
}
