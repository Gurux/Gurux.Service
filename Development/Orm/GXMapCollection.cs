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
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Collection of column mappings.
    /// </summary>
    public class GXMapCollection
    {
        internal List<BinaryExpression> List = new List<BinaryExpression>();
        GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXMapCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Add map.
        /// </summary>
        public void AddMap<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> destinationColumn,
            Expression<Func<TDestinationTable, object>> sourceColumn)
        {
            if (destinationColumn == null)
            {
                throw new ArgumentNullException("destinationColumn");
            }
            if (sourceColumn == null)
            {
                throw new ArgumentNullException("sourceColumn");
            }
            BinaryExpression t = Expression.Equal(destinationColumn.Body, sourceColumn.Body);
            List.Add(t);
        }
    }
}
