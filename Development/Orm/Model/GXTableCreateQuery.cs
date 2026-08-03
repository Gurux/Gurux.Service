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

using Gurux.Service.Orm.Internal;
using System;
using System.Collections.Generic;

namespace Gurux.Service.Orm.Model
{
    class GXTableCreateQuery
    {
        public Type Table;
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
            string str = null;
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
