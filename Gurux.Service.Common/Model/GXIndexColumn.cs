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

namespace Gurux.Service.Orm.Common.Model
{
    /// <summary>
    /// Describes an indexed column, its sort direction, and its position.
    /// </summary>
    public sealed class GXIndexColumn
    {
        /// <summary>
        /// Gets or sets the indexed column name.
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the column sort direction. The default is ascending.
        /// </summary>
        public IndexOrder Order { get; set; } = default;
        /// <summary>
        /// Gets or sets the zero-based position of the column within the index.
        /// </summary>
        public int Position { get; set; }
    }
}
