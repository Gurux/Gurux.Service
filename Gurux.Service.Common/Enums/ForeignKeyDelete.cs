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

namespace Gurux.Service.Orm.Common.Enums
{
    /// <summary>
    /// Defines the behavior of a foreign key when a referenced row is deleted.
    /// </summary>
    public enum ForeignKeyDelete
    {
        /// <summary>
        /// Leaves the delete action unspecified. This is the default value.
        /// </summary>
        None,
        /// <summary>
        /// Deletes referencing rows when the referenced row is deleted.
        /// </summary>
        Cascade,
        /// <summary>
        /// Rejects deletion of a referenced row if referencing rows exist, using the default database action.
        /// </summary>
        Empty,
        /// <summary>
        /// Explicitly restricts deletion of a row that is still referenced.
        /// </summary>
        Restrict
    }
}
