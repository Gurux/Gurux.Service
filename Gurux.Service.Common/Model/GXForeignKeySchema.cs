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

using Gurux.Service.Orm.Common.Enums;
using System.Collections.Generic;

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Describes a foreign key constraint and its referenced table.
    /// </summary>
    public sealed class GXForeignKeySchema
    {
        /// <summary>
        /// Gets or sets the foreign key constraint name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema name of the referenced table.
        /// </summary>
        public string ReferencedSchema { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the referenced table name.
        /// </summary>
        public string ReferencedTable { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the column pairs that map referencing columns to referenced columns.
        /// </summary>
        public List<GXForeignKeyColumnSchema> Columns { get; set; } = [];

        /// <summary>
        /// Gets or sets the action applied when a referenced row is deleted.
        /// </summary>
        public ForeignKeyAction OnDelete { get; set; }
        /// <summary>
        /// Gets or sets the action applied when a referenced key is updated.
        /// </summary>
        public ForeignKeyAction OnUpdate { get; set; }
    }
}
