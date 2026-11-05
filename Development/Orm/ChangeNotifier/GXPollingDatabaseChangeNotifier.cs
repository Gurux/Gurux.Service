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
            if (monitors == null)
            {
                return;
            }

            foreach (var monitor in monitors)
            {
                string[] columns = monitor.Columns.ToArray();
                if (columns.Length == 0)
                {
                    continue;
                }

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
                {
                    Changed?.Invoke(_connection, new GXDatabaseChangedEventArgs { Table = monitor.Table, ChangeType = type });
                }
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
