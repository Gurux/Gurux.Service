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

using System.Collections.Generic;

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Describes a database index and its columns.
    /// </summary>
    public sealed class GXIndex
    {
        /// <summary>
        /// Gets or sets the database index name.
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets whether the index requires unique combinations of indexed values.
        /// </summary>
        public bool Unique { get; set; }
        /// <summary>
        /// Gets or sets the columns that make up the index.
        /// </summary>
        public List<GXIndexColumn> Columns { get; set; } = new();
    }
}
