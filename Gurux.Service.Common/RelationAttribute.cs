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
    /// Identifies a relationship to another mapped entity.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a property that represents a relationship between tables.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the related entity type.
        /// </summary>
        public Type? Target { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationAttribute"/> class.
        /// </summary>
        public RelationAttribute()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationAttribute"/> class.
        /// </summary>
        /// <param name="target">The related entity type.</param>
        public RelationAttribute(Type target)
        {
            Target = target;
        }
    }
}
