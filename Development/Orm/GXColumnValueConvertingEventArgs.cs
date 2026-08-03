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

using System;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Arguments for column value conversion.
    /// </summary>
    public sealed class GXColumnValueConvertingEventArgs : EventArgs
    {
        /// <summary>
        /// Table name where column is located.
        /// </summary>
        public required string TableName { get; init; }
        /// <summary>
        /// Column name where value is located.
        /// </summary>
        public required string ColumnName { get; init; }

        /// <summary>
        /// Old column type. This is used to convert value to new type.
        /// </summary>
        public required Type OldType { get; init; }
        /// <summary>
        /// New column type. This is used to convert value to new type.
        /// </summary>
        public required Type NewType { get; init; }

        /// <summary>
        /// Current column value. This can be changed to new value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Indicates whether value is converted to new type. 
        /// If this is false, value is not converted and framework converts the value.
        /// </summary>
        public bool IsConverted { get; set; }

    }
}
