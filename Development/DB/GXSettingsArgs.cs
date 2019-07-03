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

namespace Gurux.Service.Orm
{
    class GXSettingsArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        GXDBSettings settings;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        UInt32 index, count;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal bool updated, distinct, descending;

        internal bool Updated
        {
            get
            {
                return updated;
            }
            set
            {
                updated = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSettingsArgs()
        {
            //MySql settings are default settings because of MariaDB (https://mariadb.org/).
            settings = new GXMySqlSettings();
            Updated = true;
        }

        internal GXDBSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                Updated = true;
                settings = value;
            }
        }

        /// <summary>
        /// Clear all default settings.
        /// </summary>
        public void Clear()
        {
            index = count = 0;
            distinct = descending = false;
            Updated = true;
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
                Updated = true;
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
                Updated = true;
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
                Updated = true;
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
                Updated = true;
                descending = value;
            }
        }
    }
}
