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
    /// Describes a database table, its columns, indexes, and foreign keys.
    /// </summary>
    /// <remarks>
    /// This model contains metadata, not table rows. Columns and indexes are mutable collections
    /// with no property setter; serializers must populate those collections when reading JSON.
    /// </remarks>
    public sealed class GXTableSchema
    {
        /// <summary>
        /// Gets or sets the database or catalog name, or null if unavailable.
        /// </summary>
        public string? Catalog { get; set; }

        /// <summary>
        /// Gets or sets the schema or owner name, such as dbo or public, or null if unavailable.
        /// </summary>
        public string? Schema { get; set; }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the database table type, such as BASE TABLE or VIEW, or null if unavailable.
        /// </summary>
        public string? TableType { get; set; }

        /// <summary>
        /// Gets or sets the table description, or null if unavailable.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets the mutable collection of column schemas for the table.
        /// </summary>
        public List<GXColumnSchema> Columns { get; } = new();

        /// <summary>
        /// Gets the mutable collection of indexes for the table.
        /// </summary>
        public List<GXIndex> Indexes { get; } = new();

        /// <summary>
        /// Gets or sets the foreign key constraints defined on the table.
        /// </summary>
        public List<GXForeignKeySchema> ForeignKeys { get; set; } = [];

        /// <summary>
        /// Returns the table name qualified by its schema when a schema is specified.
        /// </summary>
        /// <returns>The table name if the schema is null or empty; otherwise, the schema and table name separated by a period.</returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Schema))
            {
                return Name;
            }
            return $"{Schema}.{Name}";
        }
    }
}
