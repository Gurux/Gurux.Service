﻿//
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

namespace Gurux.Service.Orm.Common.Enums
{
    /// <summary>
    /// Foreign key update actions.
    /// </summary>
    public enum ForeignKeyUpdate
    {
        /// <summary>
        /// Foreign key update action is not used. This is a default.
        /// </summary>
        None,
        /// <summary>
        /// Cross-table updates are allowed.
        /// </summary>
        Cascade,
        /// <summary>
        /// Rejects updates to the rows in the child table when the rows in the parent table are updated.
        /// </summary>
        Reject,
        /// <summary>
        /// Updates are not allowed.
        /// </summary>
        Restrict,
        /// <summary>
        /// Resets the values in the rows in the child table to NULL.
        /// </summary>
        Null
    }
}
