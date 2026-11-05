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
    /// Specifies index options for a mapped type or member.
    /// </summary>
    /// <remarks>
    /// Support for clustered and filtered indexes depends on the database provider.
    /// IncludeOnlyNull and ExcludeNull are mutually exclusive.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the index requires unique values.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Gets or sets whether index values are sorted in descending order.
        /// </summary>
        public bool Descend { get; set; }

        /// <summary>
        /// Gets or sets whether the index includes only null values.
        /// </summary>
        public bool IncludeOnlyNull { get; set; }

        /// <summary>
        /// Gets or sets whether null values are excluded from the index.
        /// </summary>
        public bool ExcludeNull { get; set; }

        /// <summary>
        /// Gets or sets whether the index is clustered.
        /// </summary>
        public bool Clustered { get; set; }

        /// <summary>
        /// Initializes a unique index with ascending sort order.
        /// </summary>
        public IndexAttribute()
        {
            Unique = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        public IndexAttribute(bool unique)
        {
            Unique = unique;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="descend"><see langword="true"/> to sort in descending order; otherwise, <see langword="false"/>.</param>
        public IndexAttribute(bool unique, bool descend)
        {
            Unique = unique;
            Descend = descend;
        }
    }
}
