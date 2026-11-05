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

using Gurux.Service.Orm.Settings;
using System;
using System.Diagnostics;
using Gurux.Service.DB;

namespace Gurux.Service.Orm
{
    class GXSettingsArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        GXDBSettings settings;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="queryCache">Query cache instance.</param>
        public GXSettingsArgs(GXQueryCache? queryCache = null)
        {
            settings = GXSqlBuilder.CreateSettings(queryCache != null ? queryCache.DatabaseType : GXDbConnection.DefaultDatabaseType);
            QueryCache = queryCache ?? new GXQueryCache(TimeSpan.FromMinutes(10), GXDbConnection.DefaultDatabaseType);
        }

        internal GXQueryCache QueryCache { get; set; }

        internal GXDBSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
                if (QueryCache != null)
                {
                    QueryCache.Clear();
                }
            }
        }

        /// <summary>
        /// Clear all default settings.
        /// </summary>
        public void Clear()
        {
            Index = Count = 0;
            Distinct = Descending = false;
            if (QueryCache != null)
            {
                QueryCache.Clear();
            }
        }

        /// <summary>
        /// Start index.
        /// </summary>
        internal long Index { get; set; }

        /// <summary>
        /// How many items are retreaved.
        /// </summary>
        /// <remarks>
        /// If value is zero there are no limitations.
        /// </remarks>
        internal long Count { get; set; }

        /// <summary>
        /// Is select distinct.
        /// </summary>
        internal bool Distinct { get; set; }

        /// <summary>
        /// Is select made by Ascending (default) or Descending.
        /// </summary>
        internal bool Descending { get; set; }
    }
}
