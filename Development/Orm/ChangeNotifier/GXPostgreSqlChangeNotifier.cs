//
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
using System.Collections.Generic;

namespace Gurux.Service.Orm.Enums
{

    /// <summary>
    /// Polling based PostgreSql database change notifier.
    /// </summary>
    internal sealed class GXPostgreSqlChangeNotifier :
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
        public GXPostgreSqlChangeNotifier(
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
