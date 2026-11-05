// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common.Enums;
using System;
using System.ComponentModel;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Defines a foreign key relationship for a mapped property.
    /// </summary>
    /// <remarks>
    /// The referenced type identifies the entity, while MapTable identifies an optional relation table.
    /// Delete and update actions describe database referential behavior; they do not execute an operation themselves.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the referenced entity type, or null if no type was supplied.
        /// </summary>
        public Type? Type { get; private set; }

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
        public ForeignKeyDelete OnDelete { get; set; }

        /// <summary>
        /// Gets or sets the action applied to referencing rows when a referenced key is updated.
        /// </summary>
        [DefaultValue(ForeignKeyDelete.None)]
        public ForeignKeyUpdate OnUpdate { get; set; }
    }
}
