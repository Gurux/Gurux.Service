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
using System.Threading;
using System.Threading.Tasks;
namespace Gurux.Service.Orm
{
    /// <summary>Starts and stops monitoring configured tables for database changes.</summary>
    public interface IGXDatabaseChangeNotifier
    {
        /// <summary>Raised when a monitored change is detected.</summary>
        event EventHandler<GXDatabaseChangedEventArgs>? Changed;

        /// <summary>Raised when background change monitoring fails.</summary>
        event EventHandler<Exception>? Error;

        /// <summary>Initializes and starts change monitoring.</summary>
        /// <param name="cancellationToken">Token used to cancel startup.</param>
        /// <returns>A task that completes when monitoring has started.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Stops monitoring and waits for background work to finish.</summary>
        /// <param name="cancellationToken">Token used to cancel the stop operation.</param>
        /// <returns>A task representing the stop operation.</returns>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
