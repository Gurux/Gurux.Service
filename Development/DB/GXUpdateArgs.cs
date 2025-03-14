﻿//
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
using Gurux.Service.Orm.Settings;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Common;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Select arguments.
    /// </summary>
    public class GXUpdateArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent = new GXSettingsArgs();

        /// <summary>
        /// List of values to update.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<object, LambdaExpression>> Values = new List<KeyValuePair<object, LambdaExpression>>();

        /// <summary>
        /// List of values to exlude from update.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, LambdaExpression>> Excluded = new List<KeyValuePair<Type, LambdaExpression>>();

        /// <summary>
        /// Generated update SQL string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string sql;

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXUpdateArgs()
        {
            Joins = new GXJoinCollection(Parent);
            Where = new GXWhereCollection(Parent);
        }

        /// <summary>
        /// Clear all update settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Values.Clear();
            Joins.List.Clear();
            Where.Clear();
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Update argument as SQL string.
        /// </summary>
        /// <param name="addExecutionTime">Is execution time included to the string.</param>
        /// <returns></returns>
        public string ToString(bool addExecutionTime)
        {
            if (Parent.Updated)
            {
                List<string> queries = new List<string>();
                if (Where.List.Count == 0)
                {
                    //Get inserted items.
                    GXDbHelpers.GetQueries(null, Parent.Settings, Values, Excluded, queries, Where, null);
                }

                //Get updated items.
                GXDbHelpers.GetQueries(this, Parent.Settings, Values, Excluded, queries, Where, null);
                sql = string.Join(" ", queries.ToArray());
            }
            return sql;
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value)
        {
            return Update<T>(value, null);
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, Expression<Func<T, object>> columns)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Invalid value");
            }
            if (value is IEnumerable)
            {
                throw new ArgumentException("Use UpdateRange to update a collection.");
            }
            if (value is GXTableBase tb)
            {
                tb.BeforeUpdate();
            }
            GXUpdateArgs args = new GXUpdateArgs();
            args.Parent.Updated = true;
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
            args.Where.And<T>(q => value);
            return args;
        }

        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection)
        {
            return UpdateRange(collection, null);
        }

        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns)
        {
            GXUpdateArgs args = new GXUpdateArgs();
            args.Parent.Updated = true;
            foreach (var it in collection)
            {
                if (it is GXTableBase tb)
                {
                    tb.BeforeUpdate();
                }
                args.Values.Add(new KeyValuePair<object, LambdaExpression>(it, columns));
            }
            return args;
        }

        /// <summary>
        /// Add new item to update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
            Parent.Updated = true;
            //Clear previous values if values collection is empty.
            if (Values.Count == 1 && Values[0].Value == null)
            {
                Values.Clear();
            }
            Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
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
        /// Exclude columns from the update.
        /// </summary>
        /// <param name="columns">Excluded columns.</param>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
            Parent.Updated = true;
        }

        /// <summary>
        /// The maximum number of items that can be updated.
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
