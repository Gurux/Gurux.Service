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

using Gurux.Service.Orm.Common.Enums;
using System;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Specifies how a mapped value is compared when building a filter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class FilterAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the comparison used by the filter.
        /// </summary>
        public FilterType FilterType { get; set; }

        /// <summary>
        /// Gets or sets the value for which the filter is omitted.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class.
        /// </summary>
        /// <param name="filterType">The comparison used by the filter.</param>
        public FilterAttribute(FilterType filterType)
        {
            FilterType = filterType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class.
        /// </summary>
        /// <param name="filterType">The comparison used by the filter.</param>
        /// <param name="defaultValue">The value for which the filter is omitted.</param>
        public FilterAttribute(FilterType filterType, object defaultValue)
        {
            FilterType = filterType;
            DefaultValue = defaultValue;
        }
    }
}
