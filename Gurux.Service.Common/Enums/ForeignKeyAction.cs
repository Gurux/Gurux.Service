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
    /// Defines referential actions reported by a database foreign key constraint.
    /// </summary>
    public enum ForeignKeyAction
    {
        /// <summary>
        /// No action has been specified or the action is unknown.
        /// </summary>
        None,
        /// <summary>
        /// Applies NO ACTION: rejects a change if the foreign key constraint is violated when checked.
        /// </summary>
        NoAction,
        /// <summary>
        /// Rejects deleting or changing a referenced key while referencing rows exist.
        /// </summary>
        Restrict,
        /// <summary>
        /// Changes made to the referenced row are cascaded to the referencing rows.
        /// </summary>
        Cascade,
        /// <summary>
        /// Sets the foreign key column to NULL when the referenced row is deleted or updated.
        /// </summary>
        SetNull,
        /// <summary>
        /// Sets the foreign key column to its default value when the referenced row is deleted or updated.
        /// </summary>
        SetDefault
    }
}
