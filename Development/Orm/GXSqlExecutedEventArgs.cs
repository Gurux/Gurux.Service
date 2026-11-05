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
namespace Gurux.Service.Orm
{
    /// <summary>
    /// Provides data for the SQL executed event.
    /// </summary>
    public sealed class GXSqlExecutedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the executed SQL statement.
        /// </summary>
        public required string Sql
        {
            get; init;
        }

        /// <summary>
        /// Gets the elapsed execution time.
        /// </summary>
        public TimeSpan Elapsed
        {
            get; init;
        }
    }
}
