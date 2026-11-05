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

using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System.Text;
namespace Gurux.Service.Orm
{
    /// <summary>
    /// Join helper class for internal use only.
    /// </summary>
    class GXJoin
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXJoin()
        {
        }

        internal void UpdateTables(GXDBSettings settings, System.Type table1, System.Type table2)
        {
            Table1Type = table1;
            Table2Type = table2;
            Table1 = GXDbHelpers.ConvertToString(settings, TargetType.Table | TargetType.Plain, null, table1, null);
            Table2 = GXDbHelpers.ConvertToString(settings, TargetType.Table | TargetType.Plain, null, table2, null);
        }

        internal void UpdateTables(GXDBSettings settings, GXTableSchema table1, GXTableSchema table2)
        {
            Table2Type = Table1Type = typeof(GXColumnSchema);
            Table1 = GXDbHelpers.ConvertToString(settings, TargetType.Table | TargetType.Plain, null, table1, null);
            Table2 = GXDbHelpers.ConvertToString(settings, TargetType.Table | TargetType.Plain, null, table2, null);
        }


        /// <summary>
        /// Join type.
        /// </summary>
        public JoinType Type;
        /// <summary>
        /// Table 1 name.
        /// </summary>
        public string Table1 { get; private set; } = string.Empty;
        /// <summary>
        /// Table 2 name.
        /// </summary>
        public string Table2 { get; private set; } = string.Empty;
        /// <summary>
        /// Column 1 name.
        /// </summary>       
        public string Column1 = string.Empty;

        /// <summary>
        /// Column 2 name.
        /// </summary>        
        public string Column2 = string.Empty;

        /// <summary>
        /// Column 1 alias name.
        /// </summary>        
        public string? Alias1;

        /// <summary>
        /// Column 2 alias name.
        /// </summary>        
        public string? Alias2;

        /// <summary>
        /// Is null allowed in relation.
        /// </summary>
        public bool AllowNull1;

        /// <summary>
        /// Is null allowed in relation.
        /// </summary>
        public bool AllowNull2;

        /// <summary>
        /// Table 1 name.
        /// </summary>
        public System.Type? Table1Type;
        /// <summary>
        /// Table 2 name.
        /// </summary>
        public System.Type? Table2Type;

        /// <summary>
        /// If we have multiple references to same table.
        /// </summary>
        public int Index = 0;

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Alias1 != null)
            {
                sb.Append(Alias1);
                sb.Append('[');
                sb.Append(Table1);
                sb.Append("].");
                sb.Append(Column1);
            }
            else
            {
                sb.Append(Table1);
                sb.Append('.');
                sb.Append(Column1);
            }
            if (Alias2 != null)
            {
                sb.Append(Alias2);
                sb.Append('[');
                sb.Append(Table2);
                sb.Append("].");
                sb.Append(Column2);
            }
            else
            {
                sb.Append(Table2);
                sb.Append('.');
                sb.Append(Column2);
            }
            return sb.ToString();
        }
    }
}
