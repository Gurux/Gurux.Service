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

using Gurux.Service.Orm.Common.Enums;
using System;
using System.ComponentModel;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Foreign key attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Foreign key attribute type.
        /// </summary>
        public Type Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Foreign key attribute map table type.
        /// </summary>
        public Type MapTable
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ForeignKeyAttribute()
        {
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Foreign key type.</param>
        public ForeignKeyAttribute(Type type)
        {
            Type = type;
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Foreign key type.</param>
        /// <param name="mapTable">Map type.</param>
        public ForeignKeyAttribute(Type type, Type mapTable)
        {
            OnDelete = ForeignKeyDelete.None;
            OnUpdate = ForeignKeyUpdate.None;
            Type = type;
            MapTable = mapTable;
        }

        /// <summary>
        /// Specify what happens to the items in the table when the corresponding items in the parent table are deleted.
        /// </summary>
        [DefaultValue(ForeignKeyDelete.None)]
        public ForeignKeyDelete OnDelete
        {
            get;
            set;
        }

        /// <summary>
        /// Specify what happens to the items in the table when the corresponding items in the parent table are updated.
        /// </summary>
        [DefaultValue(ForeignKeyDelete.None)]
        public ForeignKeyUpdate OnUpdate
        {
            get;
            set;
        }
    }
}
