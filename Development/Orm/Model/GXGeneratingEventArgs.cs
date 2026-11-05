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
namespace Gurux.Service.Orm.Model
{
    /// <summary>Contains the table and update choice for a generation operation.</summary>
    public sealed class GXGeneratingEventArgs : EventArgs
    {
        /// <summary>Initializes generation options for a table.</summary>
        /// <param name="tableName">Name of the table being processed.</param>
        public GXGeneratingEventArgs(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>Gets the name of the table being processed.</summary>
        public string TableName
        {
            get;
        }

        /// <summary>Gets or sets whether to update the table. Defaults to true.</summary>
        public bool Update { get; set; } = true;
    }
}
