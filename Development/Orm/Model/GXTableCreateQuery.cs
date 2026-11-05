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

using Gurux.Service.Orm.Internal;
using System;
using System.Collections.Generic;

namespace Gurux.Service.Orm.Model
{
    class GXTableCreateQuery
    {
        public required Type Table;
        /// <summary>
        /// List of queries to execute.
        /// </summary>
        public List<string> Queries = [];
        /// <summary>
        /// List of tables that must create first.
        /// </summary>
        public List<GXTableCreateQuery> Dependencies = [];

        /// <inheritdoc/>
        public override string ToString()
        {
            string? str = null;
            foreach (var it in Dependencies)
            {
                str += GXDbHelpers.ConvertToString(null, TargetType.Table, null, it.Table.GetProperty("Name"), null) + ", ";
            }
            return GXDbHelpers.ConvertToString(null, TargetType.Table, null, Table.GetProperty("Name"), null) + " depends from : " + str;
        }

        public bool CheckDependency(GXTableCreateQuery debency)
        {
            //Check that there is not cross reference.
            foreach (var it in debency.Dependencies)
            {
                if (it == this)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddDependency(GXTableCreateQuery debency)
        {
            //Check that there are no cross references.
            foreach (var it in debency.Dependencies)
            {
                if (it == this)
                {
                    throw new ArgumentException("Cross reference between " + debency.Table.Name + " and " + this.Table.Name);
                }
                if (it == debency)
                {
                    throw new ArgumentException("Debency already added: " + debency.Table.Name);
                }
            }
            Dependencies.Add(debency);
        }
    }
}
