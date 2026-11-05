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

using System;

namespace Gurux.Service.Orm.Enums
{
    /// <summary>
    /// Database permissions.
    /// </summary>
    [Flags]
    public enum DatabasePermission : UInt32
    {
        /// <summary>
        /// No permissions.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission to create database objects such as tables, views, and procedures.
        /// </summary>
        Create = 1,

        /// <summary>
        /// Permission to alter existing database objects.
        /// </summary>
        Alter = 2,

        /// <summary>
        /// Permission to drop database objects.
        /// </summary>
        Drop = 4,

        /// <summary>
        /// Permission to insert rows into tables.
        /// </summary>
        Insert = 8,

        /// <summary>
        /// Permission to update existing rows.
        /// </summary>
        Update = 0x10,

        /// <summary>
        /// Permission to delete rows from tables.
        /// </summary>
        Delete = 0x20,

        /// <summary>
        /// Permission to read data from tables and views.
        /// </summary>
        Select = 0x40,

        /// <summary>
        /// Permission to create and manage indexes.
        /// </summary>
        Index = 0x80,

        /// <summary>
        /// Permission to create foreign key and other references.
        /// </summary>
        References = 0x100,

        /// <summary>
        /// Permission to reload or refresh database metadata and caches.
        /// </summary>
        Reload = 0x200,

        /// <summary>
        /// Permission to execute stored procedures and functions.
        /// </summary>
        Execute = 0x400,

        /// <summary>
        /// Permission to create views.
        /// </summary>
        CreateView = 0x800,

        /// <summary>
        /// Permission to create stored procedures.
        /// </summary>
        CreateProcedure = 0x1000,

        /// <summary>
        /// Permission to create functions.
        /// </summary>
        CreateFunction = 0x2000,

        /// <summary>
        /// Permission to connect to the database.
        /// </summary>
        Connect = 0x4000,

        /// <summary>
        /// Full administrative access.
        /// </summary>
        Admin = UInt32.MaxValue
    }
}
