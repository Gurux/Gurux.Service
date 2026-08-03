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
using System.Linq;
using System;
namespace Gurux.Service.Orm.Model
{
    /// <summary>
    /// Generic SQL table schema information.
    /// </summary>
    public sealed class GXTableSchema
    {
        /// <summary>
        /// Database or catalog name.
        /// </summary>
        public string? Catalog { get; set; }

        /// <summary>
        /// Schema or owner name.
        /// Examples: dbo, public, SYSTEM.
        /// </summary>
        public string? Schema { get; set; }

        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Table type.
        /// Examples: BASE TABLE, VIEW, TEMPORARY.
        /// </summary>
        public string? TableType { get; set; }

        /// <summary>
        /// Table comment or description.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Table columns.
        /// </summary>
        public List<GXColumnSchema> Columns { get; } = new();

        /// <summary>
        /// Returns a string representation of the table schema.
        /// </summary>
        /// <returns>A string representing the table schema.</returns>
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
