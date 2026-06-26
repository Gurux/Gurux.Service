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

using Gurux.Common.Internal;
using Gurux.Service.DB;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace Gurux.Service.Orm
{
    class GXUpdateItem
    {
        /// <summary>
        /// List of columns.
        /// </summary>
        public List<string> Columns;

        /// <summary>
        /// List of columns objects and serialized items.
        /// </summary>
        public List<List<KeyValuePair<object, GXSerializedItem>>> Rows;

        /// <summary>
        /// List of inset where items.
        /// </summary>
        internal List<string> Where = new List<string>();

        /// <summary>
        /// Is item inserted.
        /// </summary>
        /// <remarks>
        /// This is used in update.
        /// </remarks>
        public bool Inserted;

        public GXUpdateItem()
        {
            Columns = new List<string>();
            Rows = new List<List<KeyValuePair<object, GXSerializedItem>>>();
        }
    }

    /// <summary>
    /// Select arguments.
    /// </summary>
    public class GXInsertArgs
    {
        /// <summary>
        /// List of values to insert.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<object, LambdaExpression>> Values = new List<KeyValuePair<object, LambdaExpression>>();

        /// <summary>
        /// List of values to exlude from insert.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<KeyValuePair<Type, LambdaExpression>> Excluded = new List<KeyValuePair<Type, LambdaExpression>>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal GXSettingsArgs Parent = new GXSettingsArgs();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal List<object> insertedObjects = new List<object>();

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXInsertArgs()
        {
        }

        /// <summary>
        /// Clear all insert settings.
        /// </summary>
        public void Clear()
        {
            Values.Clear();
            Parent.Clear();
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

        /// <inheritdoc/>
        /// <param name="addGenerationTime">Is SQL generation time included in the output.</param>
        public string ToString(bool addGenerationTime)
        {
            var sw = Stopwatch.StartNew();
            string sql;
            string cacheKey = Parent.QueryCache.BuildKey(Values, Excluded);
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
                GXDbHelpers.GetQueries(args, null, Values, Excluded, queries, null, insertedObjects);
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
        /// Sets the query cache to use for this insert operation.
        /// </summary>
        /// <param name="queryCache">The query cache instance to use.</param>
        /// <returns>This <see cref="GXInsertArgs"/> instance.</returns>
        public GXInsertArgs UseQueryCache(GXQueryCache queryCache)
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
        /// Insert a value into the table.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        public static GXInsertArgs Insert<T>(T value)
        {
            return Insert<T>(value, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Insert a value into the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T value, GXQueryCache queryCache)
        {
            return Insert<T>(value, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Copy rows from the same table.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select)
        {
            GXInsertArgs args = new GXInsertArgs();
            select.Parent.Settings = args.Parent.Settings;
            Expression<Func<T, object>> expression = _ => typeof(T);
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(select, expression));
            return args;
        }

        /// <summary>
        /// Copy rows from the same table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, GXQueryCache queryCache)
        {
            return Insert<T>(select).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Copy rows from the table into selected columns.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            GXInsertArgs args = new GXInsertArgs();
            select.Parent.Settings = args.Parent.Settings;
            args.Add(select, columns);
            return args;
        }

        /// <summary>
        /// Copy rows from the table into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Table type to insert into.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Insert<T>(select, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add a value with specific columns to insert.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
            Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
        }

        /// <summary>
        /// Add select-based rows with specific columns to insert.
        /// </summary>
        /// <typeparam name="T">Type of the target table.</typeparam>
        /// <param name="select">Select arguments defining the rows to copy.</param>
        /// <param name="columns">Columns to insert into.</param>
        public void Add<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            Values.Add(new KeyValuePair<object, LambdaExpression>(select, columns));
        }

        /// <summary>
        /// Insert an array of values into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="value">Array of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(T[] value, Expression<Func<T, object>> columns)
        {
            return InsertRange<T>(value, columns);
        }

        /// <summary>
        /// Insert an array of values into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="value">Array of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T[] value, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return InsertRange<T>(value, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a value into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs Insert<T>(T value, Expression<Func<T, object>> columns)
        {
            if (value is GXSelectArgs)
            {
                return Insert<T>(value as GXSelectArgs);
            }
            if (value is GXInsertArgs)
            {
                throw new ArgumentException("Can't insert GXInsertArgs.");
            }
            if (value == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            if (value is IEnumerable)
            {
                throw new ArgumentException("Use InsertRange to add a collection.");
            }
            if (value is GXTableBase tb)
            {
                tb.BeforeAdd();
            }
            GXInsertArgs args = new GXInsertArgs();
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
            return args;
        }

        /// <summary>
        /// Insert a value into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the value to insert.</typeparam>
        /// <param name="value">Value to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs Insert<T>(T value, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return Insert<T>(value, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a collection of values into the table.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection)
        {
            return InsertRange(collection, (Expression<Func<T, object>>)null);
        }

        /// <summary>
        /// Insert a collection of values into the table and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection, GXQueryCache queryCache)
        {
            return InsertRange(collection, (Expression<Func<T, object>>)null).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Insert a collection of values into selected columns.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            GXInsertArgs args = new GXInsertArgs();
            foreach (var it in collection)
            {
                if (it is GXTableBase tb)
                {
                    tb.BeforeAdd();
                }
                args.Values.Add(new KeyValuePair<object, LambdaExpression>(it, columns));
            }
            return args;
        }

        /// <summary>
        /// Insert a collection of values into selected columns and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the values to insert.</typeparam>
        /// <param name="collection">Collection of values to insert.</param>
        /// <param name="columns">Columns to insert into.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns, GXQueryCache queryCache)
        {
            return InsertRange(collection, columns).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Add the association between an item and a collection of destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collections">Destination items to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination[] collections)
        {
            return Add([item], collections);
        }

        /// <summary>
        /// Add the association between a collection of items and a destination item.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collection">Destination item to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem[] items, TDestination collection)
        {
            return Add(items, [collection]);
        }

        /// <summary>
        /// Add the associations between collections of items and destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collections">Destination items to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem[] items, TDestination[] collections)
        {
            object collectionId, id;
            if (items == null || collections == null || items.Length == 0 || collections.Length == 0)
            {
                throw new ArgumentNullException("Invalid value");
            }
            Type itemType = typeof(TItem);
            Type collectionType = typeof(TDestination);
            GXSerializedItem si = GXSqlBuilder.FindRelation(itemType, collectionType);
            if (si.Relation == null || si.Relation.RelationMapTable == null)
            {
                throw new ArgumentNullException("Invalid collection");
            }
            GXInsertArgs args = new GXInsertArgs();
            GXSerializedItem siItem = GXSqlBuilder.FindRelation(collectionType, itemType);
            foreach (TDestination c in collections)
            {
                //Get collection id.
                collectionId = si.Relation.RelationMapTable.Relation.ForeignId.Get(c);
                foreach (TItem it in items)
                {
                    object target = GXInternal.CreateClass(si.Relation.RelationMapTable.Relation.PrimaryTable);
                    si.Relation.RelationMapTable.Relation.PrimaryId.Set(target, collectionId);
                    //Get item id.
                    id = siItem.Relation.RelationMapTable.Relation.ForeignId.Get(it);
                    siItem.Relation.RelationMapTable.Relation.PrimaryId.Set(target, id);
                    args.Values.Add(new KeyValuePair<object, LambdaExpression>(target, null));
                }
            }
            return args;
        }

        /// <summary>
        /// Add given item to the n:n collection.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collection">Destination item to associate.</param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination collection)
        {
            return Add(new TItem[] { item }, new TDestination[] { collection });
        }

        /// <summary>
        /// Remove item from the n:n collection.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collection">Destination item to disassociate.</param>
        public static GXInsertArgs Remove<TItem, TDestination>(TItem item, TDestination collection)
        {
            return null;
        }

        /// <summary>
        /// Exclude columns from the insert.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="columns">Columns to exclude.</param>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
        }
    }
}
