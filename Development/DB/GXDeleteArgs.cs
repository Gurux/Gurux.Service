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
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Gurux.Service.Orm.Settings;
using System.Collections;
using Gurux.Common.Internal;
using System.Diagnostics;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Common;
using Gurux.Service.DB;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Arguments for building a SQL DELETE statement.
    /// </summary>
    public class GXDeleteArgs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Type Table;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GXSettingsArgs Parent = new GXSettingsArgs();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        private GXDeleteArgs()
        {
            Where = new GXWhereCollection(Parent);
        }

        /// <summary>
        /// Clear all delete settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Table = null;
            Where.Clear();
        }

        /// <summary>
        /// Sets the query cache to use for this delete operation.
        /// </summary>
        /// <param name="queryCache">The query cache instance to use.</param>
        /// <returns>This <see cref="GXDeleteArgs"/> instance.</returns>
        public GXDeleteArgs UseQueryCache(GXQueryCache queryCache)
        {
            Parent.QueryCache = queryCache ?? Parent.QueryCache ?? new GXQueryCache();
            return this;
        }

        /// <summary>
        /// Last execution time in ms.
        /// </summary>
        public int ExecutionTime
        {
            get;
            internal set;
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Generates a SQL DELETE statement based on the current object's state.
        /// </summary>
        /// <param name="addExecutionTime">Unused parameter that indicates whether to include execution time in the output.</param>
        /// <returns>The constructed SQL DELETE statement as a string, or an empty string if no updates are necessary.</returns>
        public string ToString(bool addExecutionTime)
        {
            var sw = Stopwatch.StartNew();
            string cacheKey = Parent.QueryCache.BuildKey(
                "DeleteArgs",
                this,
                Parent.QueryCache.GetSettingsHash(Settings),
                Table,
                Where,
                Count);
            if (Parent.QueryCache.TryGet(cacheKey, out string cachedSql))
            {
                return cachedSql;
            }
            if (Parent.Updated || Updated)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("DELETE");
                if (Count != 0)
                {
                    sb.Append(" TOP (");
                    sb.Append(Count);
                    sb.Append(")");
                }
                sb.Append(" FROM ");
                sb.Append(GXDbHelpers.GetTableName(Table, true, Parent.Settings.TableQuotation, Parent.Settings.TablePrefix));
                string str = Where.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(" ");
                    sb.Append(str);
                }
                Updated = false;
                string sql = sb.ToString();
                Parent.QueryCache.Set(cacheKey, sql);
                sw.Stop();
                ExecutionTime = (int)sw.ElapsedMilliseconds;
                if (addExecutionTime && ExecutionTime != 0)
                {
                    sb.Clear();
                    sb.Append("Execution time: ");
                    sb.Append(ExecutionTime);
                    sb.Append(" ms. ");
                    sb.Append(Environment.NewLine);
                    sb.Append(sql);
                    sql = sb.ToString();
                }
                return sql;
            }
            return string.Empty;
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
        /// Where expression.
        /// </summary>
        public GXWhereCollection Where
        {
            get;
            private set;
        }

        /// <summary>
        /// Delete items from selected table.
        /// </summary>
        /// <typeparam name="T">Table where items are deleted.</typeparam>
        /// <returns></returns>
        public static GXDeleteArgs DeleteAll<T>()
        {
            return Delete(typeof(T));
        }

        /// <summary>
        /// Delete all items from the table and use the specified query cache.
        /// </summary>
        /// <typeparam name="T">Table where items are deleted.</typeparam>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXDeleteArgs DeleteAll<T>(GXQueryCache queryCache)
        {
            return DeleteAll<T>().UseQueryCache(queryCache);
        }

        internal static GXDeleteArgs Delete(Type type)
        {
            return new GXDeleteArgs() { Table = type, Updated = true };
        }

        /// <summary>
        /// Delete the specified item.
        /// </summary>
        /// <typeparam name="T">Type of the item to delete.</typeparam>
        /// <param name="item">Item to delete.</param>
        public static GXDeleteArgs Delete<T>(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Deleted item can't be null.");
            }
            if (item is GXTableBase tb)
            {
                tb.BeforeRemove();
            }
            GXDeleteArgs arg;
            if (item is IEnumerable)
            {
                arg = Delete(GXInternal.GetPropertyType(typeof(T)));
                foreach (var it in item as IEnumerable)
                {
                    arg.Where.Or<T>(q => it);
                }
                return arg;
            }
            GXSerializedItem si = GXSqlBuilder.FindUnique(typeof(T));
            if (si == null)
            {
                throw new ArgumentException("Delete by ID failed. Target class must be derived from IUnique.");
            }
            string name = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
            arg = DeleteAll<T>();
            arg.Where.Or<IUnique<T>>(_ => name.Equals(item));
            return arg;
        }

        /// <summary>
        /// Delete the specified item and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the item to delete.</typeparam>
        /// <param name="item">Item to delete.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXDeleteArgs Delete<T>(T item, GXQueryCache queryCache)
        {
            return Delete(item).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Delete items matching the where expression.
        /// </summary>
        /// <typeparam name="T">Type of the items to delete.</typeparam>
        /// <param name="where">Filter expression.</param>
        public static GXDeleteArgs Delete<T>(Expression<Func<T, object>> where)
        {
            GXDeleteArgs arg = DeleteAll<T>();
            if (where != null)
            {
                arg.Where.Or<T>(where);
            }
            return arg;
        }

        /// <summary>
        /// Delete items matching the where expression and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the items to delete.</typeparam>
        /// <param name="where">Filter expression.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXDeleteArgs Delete<T>(Expression<Func<T, object>> where, GXQueryCache queryCache)
        {
            return Delete(where).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Delete a range of items.
        /// </summary>
        /// <typeparam name="T">Type of the items to delete.</typeparam>
        /// <param name="collection">Collection of items to delete.</param>
        public static GXDeleteArgs DeleteRange<T>(IEnumerable<T> collection)
        {
            if (!collection.GetEnumerator().MoveNext())
            {
                throw new ArgumentOutOfRangeException("DeleteRange failed. Collection is empty.");
            }
            GXDeleteArgs args = Delete(typeof(T));
            args.Parent.Updated = true;
            args.Where.Or<T>(q => collection);
            return args;
        }

        /// <summary>
        /// Delete a range of items and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the items to delete.</typeparam>
        /// <param name="collection">Collection of items to delete.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXDeleteArgs DeleteRange<T>(IEnumerable<T> collection, GXQueryCache queryCache)
        {
            return DeleteRange(collection).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Delete item by ID.
        /// </summary>
        /// <typeparam name="T">Type of the item to delete.</typeparam>
        /// <param name="id">Primary key of the item to delete.</param>
        public static GXDeleteArgs DeleteById<T>(object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("Invalid Id.");
            }
            GXDeleteArgs arg = DeleteAll<T>();
            GXSerializedItem si = GXSqlBuilder.FindUnique(typeof(T));
            if (si == null)
            {
                throw new Exception("DeleteById failed. Class is not derived from IUnique.");
            }
            string name = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
            arg.Where.Or<IUnique<T>>(q => name.Equals(id));
            return arg;
        }

        /// <summary>
        /// Delete item by ID and use the query cache.
        /// </summary>
        /// <typeparam name="T">Type of the item to delete.</typeparam>
        /// <param name="id">Primary key of the item to delete.</param>
        /// <param name="queryCache">The query cache instance to use.</param>
        public static GXDeleteArgs DeleteById<T>(object id, GXQueryCache queryCache)
        {
            return DeleteById<T>(id).UseQueryCache(queryCache);
        }

        /// <summary>
        /// Remove the association between an item and a collection of destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source item.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="item">Source item.</param>
        /// <param name="collections">Destination items to disassociate.</param>
        public static GXDeleteArgs Remove<TItem, TDestination>(TItem item, TDestination[] collections)
        {
            return Remove<TItem, TDestination>(new TItem[] { item }, collections);
        }

        /// <summary>
        /// Remove the association between a collection of items and a destination item.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination item.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collection">Destination item to disassociate.</param>
        public static GXDeleteArgs Remove<TItem, TDestination>(TItem[] items, TDestination collection)
        {
            return Remove<TItem, TDestination>(items, new TDestination[] { collection });
        }

        /// <summary>
        /// Remove the associations between collections of items and destination items.
        /// </summary>
        /// <typeparam name="TItem">Type of the source items.</typeparam>
        /// <typeparam name="TDestination">Type of the destination items.</typeparam>
        /// <param name="items">Source items.</param>
        /// <param name="collections">Destination items to disassociate.</param>
        public static GXDeleteArgs Remove<TItem, TDestination>(TItem[] items, TDestination[] collections)
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
            GXDeleteArgs args = Delete(si.Relation.RelationMapTable.Relation.PrimaryTable);
            args.Parent.Updated = true;
            GXSerializedItem siItem = GXSqlBuilder.FindRelation(collectionType, itemType);
            foreach (TDestination c in collections)
            {
                //Get collection id.
                collectionId = si.Relation.RelationMapTable.Relation.ForeignId.Get(c);
                foreach (TItem it in items)
                {
                    object target = Activator.CreateInstance(si.Relation.RelationMapTable.Relation.PrimaryTable);
                    si.Relation.RelationMapTable.Relation.PrimaryId.Set(target, collectionId);
                    //Get item id.
                    id = siItem.Relation.RelationMapTable.Relation.ForeignId.Get(it);
                    siItem.Relation.RelationMapTable.Relation.PrimaryId.Set(target, id);
                    Expression<Func<object, object>> t = q => target;
                    args.Where.List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, t));
                }
            }
            return args;
        }

        /// <summary>
        /// Add given item to the n:n collection.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="item"></param>
        /// <param name="collection"></param>
        public static GXDeleteArgs Remove<TItem, TDestination>(TItem item, TDestination collection)
        {
            return Remove(new TItem[] { item }, new TDestination[] { collection });
        }

        /// <summary>
        /// The maximum number of items that can be deleted.
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
