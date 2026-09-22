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
