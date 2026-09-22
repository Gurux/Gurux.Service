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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        UInt32 index, count;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal bool distinct, descending;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="queryCache">Query cache instance.</param>
        public GXSettingsArgs(GXQueryCache? queryCache = null)
        {
            settings = GXSqlBuilder.CreateSettings(queryCache != null ? queryCache.DatabaseType : GXDbConnection.DefaultDatabaseType);
            QueryCache = queryCache ?? new GXQueryCache(TimeSpan.FromMinutes(10), GXDbConnection.DefaultDatabaseType);
        }

        internal GXQueryCache QueryCache
        {
            get;
            set;
        }

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
            index = count = 0;
            distinct = descending = false;
            if (QueryCache != null)
            {
                QueryCache.Clear();
            }
        }

        /// <summary>
        /// Start index.
        /// </summary>
        public UInt32 Index
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
            }
        }

        /// <summary>
        /// How many items are retreaved.
        /// </summary>
        /// <remarks>
        /// If value is zero there are no limitations.
        /// </remarks>
        public UInt32 Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
            }
        }

        /// <summary>
        /// Is select distinct.
        /// </summary>
        public bool Distinct
        {
            get
            {
                return distinct;
            }
            set
            {
                distinct = value;
            }
        }

        /// <summary>
        /// Is select made by Ascending (default) or Descending.
        /// </summary>
        public bool Descending
        {
            get
            {
                return descending;
            }
            set
            {
                descending = value;
            }
        }
    }
}
