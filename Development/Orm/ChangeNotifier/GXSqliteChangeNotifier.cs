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
            return (long)GXSchemaManager.ExecuteScalarInternal(_connection.Connection,
            null, "PRAGMA data_version", typeof(Int64));
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
