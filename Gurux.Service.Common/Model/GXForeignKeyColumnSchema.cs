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

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Maps a column in a foreign key to its corresponding referenced column.
    /// </summary>
    /// <remarks>
    /// Column names are stored instead of object references so the relationship can be described
    /// without loading the referenced table. Position preserves the order of a composite key.
    /// </remarks>
    public sealed class GXForeignKeyColumnSchema
    {
        /// <summary>
        /// Gets or sets the referencing column name.
        /// </summary>
        public string Column { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the corresponding column name in the referenced table.
        /// </summary>
        public string ReferencedColumn { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the zero-based position of this column pair within the foreign key.
        /// </summary>
        public int Position { get; set; }
    }
}
