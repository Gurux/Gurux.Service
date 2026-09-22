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
using Gurux.Service.Orm.Enums;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
namespace Gurux.Service.Orm
{
    /// <summary>
    /// Database change notifier is used to monitor database changes. 
    /// It supports multiple database types and provides a 
    /// unified interface for change notifications.
    /// </summary>
    public class GXDatabaseChangeNotifier : IDisposable, IAsyncDisposable
    {
        private readonly IGXDatabaseChangeNotifier _notifier;

        /// <inheritdoc/>
        public event EventHandler<GXDatabaseChangedEventArgs>? Changed;

        /// <summary>Raised when background change monitoring fails.</summary>
        public event EventHandler<Exception>? Error;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        /// <param name="createdColumns">Tables and columns to monitor for insert.</param>
        /// <param name="updatedColumns">Tables and columns to monitor for update.</param>
        /// <param name="deletedColumns">Tables and columns to monitor for delete.</param>
        /// <param name="interval">Polling interval.</param>
        public GXDatabaseChangeNotifier(DbConnection connection, string? tablePrefix,
            IEnumerable<DatabaseMonitor>? createdColumns,
            IEnumerable<DatabaseMonitor>? updatedColumns,
            IEnumerable<DatabaseMonitor>? deletedColumns,
            TimeSpan interval)
        {
            var c = new GXDbConnection(connection, tablePrefix);
            _notifier = Init(c, createdColumns, updatedColumns, deletedColumns, interval);
            _notifier.Changed += OnChanged;
            _notifier.Error += OnError;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="createdColumns">Tables and columns to monitor for insert.</param>
        /// <param name="updatedColumns">Tables and columns to monitor for update.</param>
        /// <param name="deletedColumns">Tables and columns to monitor for delete.</param>
        /// <param name="interval">Polling interval.</param>
        /// <exception cref="ArgumentException">The connection uses an unsupported database provider.</exception>
        public GXDatabaseChangeNotifier(GXDbConnection connection,
            IEnumerable<DatabaseMonitor>? createdColumns,
            IEnumerable<DatabaseMonitor>? updatedColumns,
            IEnumerable<DatabaseMonitor>? deletedColumns,
            TimeSpan interval)
        {
            _notifier = Init(connection, createdColumns, updatedColumns, deletedColumns, interval);
            _notifier.Changed += OnChanged;
            _notifier.Error += OnError;
        }

        private void OnChanged(object? sender, GXDatabaseChangedEventArgs args)
        {
            Changed?.Invoke(sender, args);
        }

        private void OnError(object? sender, Exception error) => Error?.Invoke(sender, error);

        private static IGXDatabaseChangeNotifier Init(GXDbConnection connection,
            IEnumerable<DatabaseMonitor>? createdColumns,
            IEnumerable<DatabaseMonitor>? updatedColumns,
            IEnumerable<DatabaseMonitor>? deletedColumns,
            TimeSpan interval)
        {
            switch (connection.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return new GXMySqlChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.MSSQL:
                    return new GXMSSqlChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.SqLite:
                    return new GXSqliteChangeNotifier(connection, interval);
                case DatabaseType.Oracle:
                    return new GXOracleSqlChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.PostgreSQL:
                    return new GXPostgreSqlChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.MariaDB:
                    return new GXMariaDbChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.DB2:
                    return new GXDb2ChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
                case DatabaseType.SapHana:
                    return new GXSapHanaChangeNotifier(connection, createdColumns, updatedColumns, deletedColumns, interval);
            }
            throw new ArgumentException($"Database type {connection.DatabaseType} is not supported.");
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _notifier.Changed -= OnChanged;
            _notifier.Error -= OnError;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return _notifier.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _notifier.StopAsync(cancellationToken);
        }
    }
}
