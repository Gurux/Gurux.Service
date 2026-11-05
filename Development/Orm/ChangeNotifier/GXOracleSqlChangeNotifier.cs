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
using System.Collections.Generic;

namespace Gurux.Service.Orm.Enums
{
    /// <summary>
    /// Polling based Oracle Sql DB database change notifier.
    /// </summary>
    internal sealed class GXOracleSqlChangeNotifier :
        GXPollingDatabaseChangeNotifier
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="createdColumns">Tables and columns to monitor for insert.</param>
        /// <param name="updatedColumns">Tables and columns to monitor for update.</param>
        /// <param name="deletedColumns">Tables and columns to monitor for delete.</param>
        /// <param name="interval">
        /// Polling interval.
        /// </param>
        public GXOracleSqlChangeNotifier(
            GXDbConnection connection,
            IEnumerable<DatabaseMonitor>? createdColumns,
            IEnumerable<DatabaseMonitor>? updatedColumns,
            IEnumerable<DatabaseMonitor>? deletedColumns,
            TimeSpan interval)
            : base(connection, createdColumns, updatedColumns, deletedColumns, interval)
        {
        }


    }
}
