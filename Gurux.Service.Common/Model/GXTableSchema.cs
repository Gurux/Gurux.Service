//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Describes a database table, its columns, indexes, and foreign keys.
    /// </summary>
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
