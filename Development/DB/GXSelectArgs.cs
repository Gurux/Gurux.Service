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
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.ExecutionTime != 0)
            {
                sb.Append("Execution time: ");
                sb.Append(ExecutionTime);
                sb.Append(" ms. ");
                sb.Append(Environment.NewLine);
            }
            sb.Append(Columns.ToString());
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
            if (this.Settings.Type == DatabaseType.Oracle)
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

            if (this.Settings.Type != DatabaseType.Oracle)
            {
                str = Where.LimitToString();
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(" ");
                    sb.Append(str);
                }
            }
            return sb.ToString();
        }

        internal void Verify()
        {
            if (this.Descending && OrderBy.List.Count == 0)
            {
                throw new ArgumentException("Descending expects that OrderBy is used.");
            }
            if (this.Index != 0)
            {
                List<Type> tables = new List<Type>();
                foreach (var it in Columns.List)
                {
                    if (!tables.Contains(it.Parameters[0].Type))
                    {
                        tables.Add(it.Parameters[0].Type);
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

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id)
        {
            return SelectByIdInternal<T, ulong>(id, q => "*");
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public static GXSelectArgs SelectById<T>(long id)
        {
            return SelectById<T>(id, q => "*");
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(long id, Expression<Func<T, object>> columns)
        {
            return SelectByIdInternal<T, long>(id, columns);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public static GXSelectArgs SelectById<T>(UInt64 id, Expression<Func<T, object>> columns)
        {
            return SelectByIdInternal<T, UInt64>(id, columns);
        }

        private static GXSelectArgs SelectByIdInternal<T, IDTYPE>(IDTYPE id, Expression<Func<T, object>> columns)
        {
            if (typeof(IUnique<>).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException("Select by ID failed. Target class must be derived from IUnique.");
            }
            GXSelectArgs arg = GXSelectArgs.Select<T>(columns);
            arg.Where.And<IUnique<T>>(q => q.Id.Equals(id));
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
        /// Order items by.
        /// </summary>
        public GXOrderByCollection OrderBy
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
