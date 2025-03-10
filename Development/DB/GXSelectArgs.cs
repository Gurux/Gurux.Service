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
            return ToString(true);
        }

        /// <summary>
        /// Convert selection clause to string.
        /// </summary>
        /// <param name="addExecutionTime">Is execution time added to the string.</param>
        /// <returns>Selection clause as a string.</returns>
        public string ToString(bool addExecutionTime)
        {
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
            if (addExecutionTime && ExecutionTime != 0)
            {
                sb.Clear();
                sb.Append("Execution time: ");
                sb.Append(ExecutionTime);
                sb.Append(" ms. ");
                sb.Append(Environment.NewLine);
                sb.Append(query);
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

        public static GXSelectArgs SelectAll<T>()
        {
            return Select<T>(null, null);
        }

        public static GXSelectArgs SelectAll<T>(Expression<Func<T, object>> where)
        {
            return Select<T>(null, where);
        }

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

        public static GXSelectArgs Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, object>> where)
        {
            GXSelectArgs arg = Select<T>(columns);
            if (where != null)
            {
                arg.Where.Or<T>(where);
            }
            return arg;
        }

        public static GXSelectArgs IsEmpty<T>(Expression<Func<T, object>> where)
        {
            GXSelectArgs arg = Select<T>(null);
            if (where != null)
            {
                arg.Where.Or<T>(where);
            }
            return arg;
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
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(Guid id)
        {
            return SelectById<T, Guid>(id, q => "*");
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
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(long id)
        {
            return SelectById<T, long>(id, null);
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
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id, Expression<Func<T, object>> columns)
        {
            return SelectById<T, UInt64>(id, columns);
        }

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
