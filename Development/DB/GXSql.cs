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
        public static int Count(object expression)
        {
            return 0;
        }
        public static int DistinctCount(object expression)
        {
            return 0;
        }

        /// <summary>
        /// Are there any rows.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>True, if there are no rows.</returns>
        public static bool IsEmpty(object expression)
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
        /// Get average value from selected objects.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Avg(object expression)
        {
            return true;
        }

        /// <summary>
        /// Select 1 FROM table.
        /// </summary>
        /// <returns></returns>
        public static bool One
        {
            get
            {
                return true;
            }
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
        /// <typeparam name="TSourceTable">Source table</typeparam>
        /// <typeparam name="TDestinationTable">Destination table</typeparam>
        /// <param name="sourceColumn">Source column.</param>
        /// <param name="destinationColumn">Destination column.</param>
        /// <param name="expression">Value to search.</param>
        /// <returns>True, if value exists.</returns>
        public static bool Exists<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn, GXSelectArgs expression)
        {
            return true;
        }

        /// <summary>
        /// Is value exists in the table.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool Exists(GXSelectArgs args)
        {
            return true;
        }

        /// <summary>
        /// Is value containing the expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static bool Contains<T>(T value, object expression)
        {
            return true;
        }

        /// <summary>
        /// Is value starts with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool StartsWith<T>(T value, object expression)
        {
            return true;
        }

        /// <summary>
        /// Is value ends with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool EndsWith<T>(T value, object expression)
        {
            return true;
        }

        /// <summary>
        /// Is value ends with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool Greater<T>(T value, object expression)
        {
            return true;
        }
        /// <summary>
        /// Is value ends with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool Less<T>(T value, object expression)
        {
            return true;
        }
        /// <summary>
        /// Is value ends with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool GreaterOrEqual<T>(T value, object expression)
        {
            return true;
        }
        /// <summary>
        /// Is value ends with expression.
        /// </summary>
        /// <param name="args">Selection arguments.</param>
        /// <returns>True, if value exists.</returns>
        public static bool LessOrEqual<T>(T value, object expression)
        {
            return true;
        }
    }
}
