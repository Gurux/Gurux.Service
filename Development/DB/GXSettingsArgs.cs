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
using System.Diagnostics;

namespace Gurux.Service.Orm
{
    class GXSettingsArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        GXDBSettings m_Settings;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int m_Index, m_Count;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal bool m_Updated, m_Distinct, m_Descending;

        bool m_Relations;

        internal bool Updated
        {
            get
            {
                return m_Updated;
            }
            set
            {
                m_Updated = value;
            }
        }

        internal bool Relations
        {
            get
            {
                return m_Relations;
            }
            set
            {
                Updated = true;
                m_Relations = value;
            }
        }
        

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSettingsArgs()
        {
            //MySql settings are default settings because of MariaDB (https://mariadb.org/).
            m_Settings = new GXMySqlSettings();
            Updated = true;
            m_Relations = true;
        }

        internal GXDBSettings Settings
        {
            get
            {
                return m_Settings;
            }
            set
            {
                Updated = true;
                m_Settings = value;
            }
        }

        /// <summary>
        /// Clear all default settings.
        /// </summary>        
        public void Clear()
        {
            m_Index = m_Count = 0;
            m_Distinct = m_Descending = false;
            Updated = true;
        }

        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get
            {
                return m_Index;
            }
            set
            {
                Updated = true;
                m_Index = value;
            }
        }

        /// <summary>
        /// How many items are retreaved.
        /// </summary>
        /// <remarks>
        /// If value is zero there are no limitations.
        /// </remarks>
        public int Count
        {
            get
            {
                return m_Count;
            }
            set
            {
                Updated = true;
                m_Count = value;
            }
        }

        /// <summary>
        /// Is select distinct.
        /// </summary>
        public bool Distinct
        {
            get
            {
                return m_Distinct;
            }
            set
            {
                Updated = true;
                m_Distinct = value;
            }
        }

        /// <summary>
        /// Is select made by Ascending (default) or Descending.
        /// </summary>
        public bool Descending
        {
            get
            {
                return m_Descending;
            }
            set
            {
                Updated = true;
                m_Descending = value;
            }
        }   
    }
}
