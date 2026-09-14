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
using System;
using System.ComponentModel;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Defines a foreign key relationship for a mapped property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the referenced entity type, or null if no type was supplied.
        /// </summary>
        public Type? Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the mapping table type, or null if no mapping table was supplied.
        /// </summary>
        public Type? MapTable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyAttribute"/> class.
        /// </summary>
        public ForeignKeyAttribute()
        {
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyAttribute"/> class.
        /// </summary>
        /// <param name="type">The referenced entity type.</param>
        public ForeignKeyAttribute(Type type)
        {
            Type = type;
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyAttribute"/> class.
        /// </summary>
        /// <param name="type">The referenced entity type.</param>
        /// <param name="mapTable">The entity type of the mapping table.</param>
        public ForeignKeyAttribute(Type type, Type mapTable)
        {
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
            Type = type;
            MapTable = mapTable;
        }

        /// <summary>
        /// Gets or sets the action applied to referencing rows when a referenced row is deleted.
        /// </summary>
        [DefaultValue(ForeignKeyDelete.None)]
        public ForeignKeyDelete OnDelete
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the action applied to referencing rows when a referenced key is updated.
        /// </summary>
        [DefaultValue(ForeignKeyDelete.None)]
        public ForeignKeyUpdate OnUpdate
        {
            get;
            set;
        }
    }
}
