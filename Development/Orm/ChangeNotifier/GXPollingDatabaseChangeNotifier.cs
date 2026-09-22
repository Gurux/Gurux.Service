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

using Gurux.Service.Orm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm.Enums
{
    /// <summary>
    /// Base class for polling based database change notifications.
    /// </summary>
    internal abstract class GXPollingDatabaseChangeNotifier :
        IGXDatabaseChangeNotifier
    {
        /// <summary>
        /// Tables and columns to being monitored.
        /// </summary>
        protected readonly IEnumerable<DatabaseMonitor>? _createdColumns;
        protected readonly IEnumerable<DatabaseMonitor>? _updatedColumns;
        protected readonly IEnumerable<DatabaseMonitor>? _deletedColumns;
        /// <summary>
        /// Database connection used for polling.
        /// </summary>
        protected readonly GXDbConnection _connection;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private readonly Dictionary<(DatabaseChangeType Type, string Table, string Columns), string> _tokens = new();

        /// <summary>
        /// Constructor.
        /// </summary>
        protected GXPollingDatabaseChangeNotifier(
            GXDbConnection connection,
            IEnumerable<DatabaseMonitor>? createdColumns,
            IEnumerable<DatabaseMonitor>? updatedColumns,
            IEnumerable<DatabaseMonitor>? deletedColumns,
            TimeSpan interval)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _createdColumns = createdColumns;
            _updatedColumns = updatedColumns;
            _deletedColumns = deletedColumns;
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(interval));
            }
            _interval = interval;
        }

        /// <inheritdoc/>
        public event EventHandler<GXDatabaseChangedEventArgs>? Changed;

        /// <inheritdoc/>
        public event EventHandler<Exception>? Error;

        /// <inheritdoc/>
        public bool IsRunning =>
            _listenerTask != null &&
            !_listenerTask.IsCompleted;

        /// <summary>
        /// SQL query that reads the monitored columns for a row snapshot.
        /// </summary>
        protected virtual string GetChangeTokenQuery(string table, IEnumerable<string> columns)
        {
            string Quote(string name) => _connection.Builder.Settings.EscapeIdentifier(null, name);
            return $"SELECT {string.Join(", ", columns.Select(Quote))} FROM {Quote(table)}";
        }

        /// <inheritdoc/>
        public async Task StartAsync(
            CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                return;
            }

            _tokens.Clear();
            _cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            try
            {
                await CheckAsync(DatabaseChangeType.Insert, _createdColumns, cancellationToken);
                await CheckAsync(DatabaseChangeType.Update, _updatedColumns, cancellationToken);
                await CheckAsync(DatabaseChangeType.Delete, _deletedColumns, cancellationToken);
                _listenerTask = RunAsync(_cancellationTokenSource.Token);
            }
            catch
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            if (_listenerTask == null)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();

            try
            {
                await _listenerTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            _listenerTask = null;

            _cancellationTokenSource?.Dispose();
            _tokens.Clear();
            _cancellationTokenSource = null;
        }

        private async Task RunAsync(
            CancellationToken cancellationToken)
        {
            using PeriodicTimer timer =
                new(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(
                    cancellationToken))
                {
                    await CheckAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private async Task CheckAsync(DatabaseChangeType type,
            IEnumerable<DatabaseMonitor>? monitors,
            CancellationToken cancellationToken)
        {
            if (monitors == null) return;
            foreach (var monitor in monitors)
            {
                string[] columns = monitor.Columns.ToArray();
                if (columns.Length == 0) continue;
                using var command = _connection.Connection.CreateCommand();
                command.CommandText = GetChangeTokenQuery(monitor.Table, columns);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var rows = new List<string>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    object[] values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    rows.Add(Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values))));
                }
                // Ignore row order, retaining duplicates so inserts and deletes are visible.
                rows.Sort(StringComparer.Ordinal);
                string token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("", rows))));
                var key = (type, monitor.Table, JsonSerializer.Serialize(columns));
                bool changed = _tokens.TryGetValue(key, out string? previous) && previous != token;
                _tokens[key] = token;
                if (changed)
                    Changed?.Invoke(_connection, new GXDatabaseChangedEventArgs { Table = monitor.Table, ChangeType = type });
            }
        }
        private async Task CheckAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await CheckAsync(DatabaseChangeType.Insert, _createdColumns, cancellationToken);
                await CheckAsync(DatabaseChangeType.Update, _updatedColumns, cancellationToken);
                await CheckAsync(DatabaseChangeType.Delete, _deletedColumns, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
