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
