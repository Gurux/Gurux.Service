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
using System.Text;
using Gurux.Service.Orm.Settings;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Gurux.Common.Internal;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Common;
using Gurux.Service.DB;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Select arguments.
    /// </summary>
    public class GXSelectArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXSelectArgs()
        {
            Parent = new GXSettingsArgs();
            Joins = new GXJoinCollection(Parent);
            Columns = new GXColumnCollection(Parent);
            Columns.Joins = Joins;
            Where = new GXWhereCollection(Parent);
            OrderBy = new GXOrderByCollection(this);
            GroupBy = new GXGroupByCollection(this);
            Having = new GXHavingCollection(Parent);
        }

        internal string query;

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Convert selection clause to string.
        /// </summary>
        /// <param name="addExecutionTime">Is execution time added to the string.</param>
        /// <returns>Selection clause as a string.</returns>
        public string ToString(bool addExecutionTime)
        {
            var sw = Stopwatch.StartNew();
            StringBuilder sb = new StringBuilder();
            string post = null;
            sb.Append(Columns.ToString(ref post));

            string str, where = Where.ToString();
            if (!string.IsNullOrEmpty(where))
            {
                sb.Append(" ");
                sb.Append(where);
            }

            str = OrderBy.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                sb.Append(str);
            }

            str = GroupBy.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                sb.Append(str);
            }
            if (Settings.LimitType == LimitType.Fetch &&
                (Index != 0 || Count != 0))
            {
                sb.Append(" OFFSET ");
                sb.Append(Index);
                sb.Append(" ROWS");
                sb.Append(" FETCH NEXT ");
                sb.Append(Count);
                sb.Append(" ROWS ONLY");
            }

            str = Having.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                sb.Append(str);
            }
            if (Settings.Type == DatabaseType.Oracle)
            {
                str = Where.LimitToString();
                if (!string.IsNullOrEmpty(str))
                {
                    string tmp = sb.ToString();
                    str = string.Format(str, tmp);
                    sb.Length = 0;
                    sb.Append(str);
                }
            }

            if (Settings.Type != DatabaseType.Oracle)
            {
                str = Where.LimitToString();
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(" ");
                    sb.Append(str);
                }
            }
            if (post != null)
            {
                sb.Append(post);
            }
            query = sb.ToString();
            Parent.Updated = false;
            sw.Stop();
            ExecutionTime = (int)sw.ElapsedMilliseconds;
            if (addExecutionTime && ExecutionTime != 0)
            {
                sb.Clear();
                sb.Append("Execution time: ");
                sb.Append(ExecutionTime);
                sb.Append(" ms. ");
                sb.Append(Environment.NewLine);
                sb.Append(query);
                query = sb.ToString();
            }
            return query;
        }

        internal void Verify()
        {
            if (Descending && OrderBy.List.Count == 0)
            {
                throw new ArgumentException("Descending expects that OrderBy is used.");
            }
            if (Index != 0)
            {
                List<Type> tables = new List<Type>();
                foreach (var it in Columns.List)
                {
                    if (!tables.Contains(it.Key.Parameters[0].Type))
                    {
                        tables.Add(it.Key.Parameters[0].Type);
                    }
                }
                bool found = false;
                foreach (var it in tables)
                {
                    if (GXSqlBuilder.FindUnique(it) != null)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new ArgumentException("Index expects that class is derived from IQnique.");
                }
            }
        }

        /// <summary>
        /// Database settings.
        /// </summary>
        public GXDBSettings Settings
        {
            get
            {
                return Parent.Settings;
            }
            internal set
            {
                Parent.Updated = true;
                Parent.Settings = value;
            }
        }

        /// <summary>
        /// Clear all select settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Joins.List.Clear();
            Columns.Clear();
            Columns.Clear();
            Where.Clear();
            OrderBy.Clear();
            GroupBy.Clear();
            Having.Clear();
            ExecutionTime = 0;
        }

        /// <summary>
        /// Sets the query cache to use for this select operation.
        /// </summary>
        /// <param name="queryCache">The query cache instance to use.</param>
        /// <returns>This <see cref="GXSelectArgs"/> instance.</returns>
        public GXSelectArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
            return this;
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        public static GXSelectArgs SelectAll<T>()
        {
            return Select<T>((Expression<Func<T, object>>)null, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Select all columns from the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectAll<T>(GXQueryCache queryCache)
        {
            return Select<T>((Expression<Func<T, object>>)null, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Select all columns from rows matching the where expression.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="where">Filter expression.</param>
        public static GXSelectArgs SelectAll<T>(Expression<Func<T, object>> where)
        {
            return Select<T>((Expression<Func<T, object>>)null, where);
        }

        /// <summary>
        /// Select all columns from rows matching the where expression and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="where">Filter expression.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectAll<T>(Expression<Func<T, object>> where, GXQueryCache queryCache)
        {
            return Select<T>((Expression<Func<T, object>>)null, where).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Select specific columns from the table.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="columns">Columns to select.</param>
        public static GXSelectArgs Select<T>(Expression<Func<T, object>> columns)
        {
            GXSelectArgs arg = new GXSelectArgs();
            if (columns != null)
            {
                arg.Columns.Add<T>(columns);
            }
            else
            {
                arg.Columns.Add<T>(q => "*");
            }
            return arg;
        }

        /// <summary>
        /// Select specific columns from the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="columns">Columns to select.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs Select<T>(Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Select<T>(columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Select specific columns from rows matching the where expression.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="columns">Columns to select.</param>
        /// <param name="where">Filter expression.</param>
        public static GXSelectArgs Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, object>> where)
        {
            GXSelectArgs arg = Select<T>(columns);
            if (!IsNullOrEmptyWhereExpression(where))
            {
                arg.Where.Or<T>(where);
            }
            return arg;
        }

        /// <summary>
        /// Determines whether the specified expression is null or represents an empty value.
        /// </summary>
        /// <typeparam name="T">The type of the parameter used in the expression.</typeparam>
        /// <param name="where">The expression to evaluate for null or empty value.</param>
        /// <returns>true if the expression is null or empty; otherwise, false.</returns>
        private static bool IsNullOrEmptyWhereExpression<T>(Expression<Func<T, object>> where)
        {
            if (where == null)
            {
                return true;
            }

            Expression body = where.Body;
            while (body is UnaryExpression unaryExpression &&
                (body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.ConvertChecked))
            {
                body = unaryExpression.Operand;
            }

            if (body is ConstantExpression constantExpression)
            {
                if (constantExpression.Value == null)
                {
                    return true;
                }
                if (constantExpression.Value is string value && string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }
            }

            if (body is NewExpression newExpression && newExpression.Arguments.Count == 0)
            {
                return true;
            }

            if (body is NewArrayExpression newArrayExpression && newArrayExpression.Expressions.Count == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Select specific columns from rows matching the where expression and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="columns">Columns to select.</param>
        /// <param name="where">Filter expression.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, object>> where, GXQueryCache queryCache)
        {
            return Select<T>(columns, where).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Check if the table is empty.
        /// </summary>
        public static GXSelectArgs IsEmpty<T>()
        {
            return IsEmpty<T>(null);
        }

        /// <summary>
        /// Check if there are no items that match the where clause.
        /// </summary>
        public static GXSelectArgs IsEmpty<T>(Expression<Func<T, object>> where)
        {
            return Select(q => GXSql.IsEmpty(q), where);
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(string id)
        {
            return SelectById<T, string>(id, q => "*");
        }

        /// <summary>
        /// Select item by string ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(string id, GXQueryCache queryCache)
        {
            return SelectById<T, string>(id, q => "*", queryCache);
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(Guid id)
        {
            return SelectById<T, Guid>(id, q => "*");
        }

        /// <summary>
        /// Select item by GUID ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(Guid id, GXQueryCache queryCache)
        {
            return SelectById<T, Guid>(id, q => "*", queryCache);
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id)
        {
            return SelectById<T, ulong>(id, q => "*");
        }

        /// <summary>
        /// Select item by unsigned 64-bit integer ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id, GXQueryCache queryCache)
        {
            return SelectById<T, ulong>(id, q => "*", queryCache);
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(long id)
        {
            return SelectById<T, long>(id, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Select item by 64-bit integer ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(long id, GXQueryCache queryCache)
        {
            return SelectById<T, long>(id, (Expression<Func<T, object>>)null, queryCache);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(Guid id, Expression<Func<T, object>> columns)
        {
            return SelectById<T, Guid>(id, columns);
        }

        /// <summary>
        /// Select item's columns by GUID ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(Guid id, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return SelectById<T, Guid>(id, columns, queryCache);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(string id, Expression<Func<T, object>> columns)
        {
            return SelectById<T, string>(id, columns);
        }

        /// <summary>
        /// Select item's columns by string ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(string id, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return SelectById<T, string>(id, columns, queryCache);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(long id, Expression<Func<T, object>> columns)
        {
            return SelectById<T, long>(id, columns);
        }

        /// <summary>
        /// Select item's columns by 64-bit integer ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(long id, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return SelectById<T, long>(id, columns, queryCache);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id, Expression<Func<T, object>> columns)
        {
            return SelectById<T, UInt64>(id, columns);
        }

        /// <summary>
        /// Select item's columns by unsigned 64-bit integer ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return SelectById<T, UInt64>(id, columns, queryCache);
        }

        /// <summary>
        /// Select item's columns by ID using a generic ID type.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <typeparam name="IDTYPE">Type of the ID.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T, IDTYPE>(IDTYPE id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs arg = GXSelectArgs.Select<T>(columns);
            GXSerializedItem si = GXSqlBuilder.FindUnique(typeof(T));
            if (si == null)
            {
                throw new ArgumentException("Select by ID failed. Target class must be derived from IUnique.");
            }
            string name = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
            arg.Where.Or<IUnique<T>>(q => name.Equals((IDTYPE)id));
            return arg;
        }

        /// <summary>
        /// Select item's columns by ID using a generic ID type and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to select from.</typeparam>
        /// <typeparam name="IDTYPE">Type of the ID.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXSelectArgs SelectById<T, IDTYPE>(IDTYPE id, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return SelectById<T, IDTYPE>(id, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Selected columns.
        /// </summary>
        public GXColumnCollection Columns
        {
            get;
            private set;
        }

        /// <summary>
        /// Last execution time in ms.
        /// </summary>
        public int ExecutionTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Order items.
        /// </summary>
        public GXOrderByCollection OrderBy
        {
            get;
            private set;
        }

        /// <summary>
        /// Group items.
        /// </summary>
        public GXGroupByCollection GroupBy
        {
            get;
            private set;
        }

        /// <summary>
        /// Get rows that have the same value on a column.
        /// </summary>
        public GXHavingCollection Having
        {
            get;
            private set;
        }

        /// <summary>
        /// Where expression.
        /// </summary>
        public GXWhereCollection Where
        {
            get;
            private set;
        }

        /// <summary>
        /// Where expression.
        /// </summary>
        public GXJoinCollection Joins
        {
            get;
            private set;
        }

        /// <summary>
        /// Is select distinct.
        /// </summary>
        public bool Distinct
        {
            get
            {
                return Parent.Distinct;
            }
            set
            {
                Parent.Distinct = value;
            }
        }

        /// <summary>
        /// Is select made by Ascending (default) or Descending.
        /// </summary>
        public bool Descending
        {
            get
            {
                return Parent.Descending;
            }
            set
            {
                Parent.Descending = value;
            }
        }

        /// <summary>
        /// Start index.
        /// </summary>
        public UInt32 Index
        {
            get
            {
                return Parent.Index;
            }
            set
            {
                Parent.Index = value;
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
                return Parent.Count;
            }
            set
            {
                Parent.Count = value;
            }
        }
    }
}
