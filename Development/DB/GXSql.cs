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
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    public static class GXSql
    {
        /// <summary>
        /// Return count of selected objects.
        /// </summary>
        /// <example>
        /// <code>
        /// GXSelectArgs arg = new GXSelectArgs();        
        /// arg.Columns.Add<TestClass>(q => GXSql.Count(q));
        /// //or arg.Columns.Add<TestClass>(q => GXSql.Count(null));
        /// //or arg.Columns.Add<TestClass>(q => GXSql.Count("*"));
        /// //or arg.Columns.Add<TestClass>(q => GXSql.Count("column name"));
        /// parser.Select<TestClass>(arg);
        /// </code>
        /// </example>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Count(object expression)
        {
            return true;
        }

        /// <summary>
        /// Count sum of selected objects.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Sum(object expression)
        {
            return true;
        }

        /// <summary>
        /// Get minimum value from selected objects.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Min(object expression)
        {
            return true;
        }

        /// <summary>
        /// Get maximum value from selected objects.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Max(object expression)
        {
            return true;
        }

        /// <summary>
        /// Get maximum value from selected objects.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Avg(object expression)
        {
            return true;
        }

        public static bool In<T>(T value, params T[] collection)
        {
            return true;
        }

        public static bool In(object value, GXSelectArgs expression)
        {
            return true;
        }

        /// <summary>
        /// Is value exists in the table. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression">Value to search.</param>
        /// <returns>True, if value exists.</returns>
        public static bool Exists<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn, GXSelectArgs expression)
        {
            return true;
        }
    }
}
