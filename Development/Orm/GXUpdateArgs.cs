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
using Gurux.Service.Orm.Settings;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Common;
using Gurux.Service.DB;
using System.Text;

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
        /// Constructor.
        /// </summary>
        private GXUpdateArgs()
        {
            Joins = new GXJoinCollection(Parent);
            Where = new GXWhereCollection(Parent, Joins);
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
        /// <param name="addGenerationTime">Is SQL generation time included in the output.</param>
        /// <returns></returns>
        public string ToString(bool addGenerationTime)
        {
            string sql;
            var sw = Stopwatch.StartNew();
            string cacheKey = Parent.QueryCache.BuildKey(
                Values,
                Excluded,
                Where != null ? Where.GetItemHash() : 0,
                Joins != null ? Joins.GetItemHash() : 0,
                Count);
            if (Parent.QueryCache.TryGet(cacheKey, out string cachedSql))
            {
                sw.Stop();
                GenerationTime = (int)sw.ElapsedMilliseconds;
                Debug.WriteLine($"Cached SQL: {GenerationTime} ms {cachedSql}");
                sql = cachedSql;
            }
            else
            {
                GXGetMembersArgs args = new GXGetMembersArgs(Parent.Settings, TargetType.Table)
                {
                    SingleTable = true
                };
                List<string> queries = new List<string>();
                if (Where.List.Count == 0)
                {
                    //Get inserted items.
                    GXDbHelpers.GetQueries(args, null, Values, Excluded, queries, Where, null);
                }
                //Get updated items.
                GXDbHelpers.GetQueries(args, this, Values, Excluded, queries, Where, null);
                sql = string.Join(" ", queries.ToArray());
                Parent.QueryCache.Set(cacheKey, sql);
                sw.Stop();
                GenerationTime = (int)sw.ElapsedMilliseconds;
                Debug.WriteLine($"New SQL: {GenerationTime} ms {sql}");
            }
            if (addGenerationTime)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Generation time: ");
                sb.Append(GenerationTime);
                sb.Append(" ms. ");
                sb.Append(Environment.NewLine);
                sb.Append(sql);
                sql = sb.ToString();
            }
            return sql;
        }

        /// <summary>
        /// Set query cache.
        /// </summary>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Update arguments.</returns>
        public GXUpdateArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.Settings = GXSqlBuilder.CreateSettings(queryCache.DatabaseType);
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
            return this;
        }

        /// <summary>
        /// SQL generation time in ms.
        /// </summary>
        public int GenerationTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value)
        {
            return Update<T>(value, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, GXQueryCache queryCache)
        {
            return Update<T>(value, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
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
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
            args.Where.And<T>(q => value);
            return args;
        }

        /// <summary>
        /// Create new update expression.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs Update<T>(T value, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Update<T>(value, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection)
        {
            return UpdateRange(collection, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, GXQueryCache queryCache)
        {
            return UpdateRange(collection, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="columns">Updated columns.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns)
        {
            GXUpdateArgs args = new GXUpdateArgs();
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
        /// Create new update expression for a collection.
        /// </summary>
        /// <param name="collection">Updated values.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="queryCache">Query cache to use.</param>
        /// <returns>Created update attribute.</returns>
        public static GXUpdateArgs UpdateRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return UpdateRange(collection, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add new item to update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
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
