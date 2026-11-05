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
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm.Enums
{
    /// <summary>
    /// SQLite database change notifier.
    /// </summary>
    internal sealed class GXSqliteChangeNotifier :
        IGXDatabaseChangeNotifier
    {
        private GXDbConnection _connection;
        private readonly TimeSpan _interval;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private long? _dataVersion;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">SQLite connection.</param>
        /// <param name="interval">
        /// Polling interval.
        /// </param>
        public GXSqliteChangeNotifier(
            GXDbConnection connection,
            TimeSpan interval)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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

        /// <inheritdoc/>
        public async Task StartAsync(
            CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                return;
            }
            _cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationToken token = _cancellationTokenSource.Token;
            _dataVersion = await GetDataVersionAsync(token);
            _listenerTask = ListenAsync(token);
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
                await _listenerTask.WaitAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            _listenerTask = null;
            _dataVersion = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task ListenAsync(
            CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new(_interval);
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

        private async Task CheckAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                long value = await GetDataVersionAsync(cancellationToken);
                if (_dataVersion == value)
                {
                    return;
                }

                _dataVersion = value;

                Changed?.Invoke(
                    _connection,
                    new GXDatabaseChangedEventArgs
                    {
                        ChangeType = DatabaseChangeType.Unknown
                    });
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private async Task<long> GetDataVersionAsync(
            CancellationToken cancellationToken)
        {
            return (long)(GXSchemaManager.ExecuteScalarInternal(_connection.Connection,
            null, "PRAGMA data_version", typeof(Int64)) ?? throw new InvalidOperationException("SQLite did not return a data version."));
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
