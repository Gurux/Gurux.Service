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
using Gurux.Common.Internal;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Internal;

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
        /// Generated insert SQL string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string sql;

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
                Parent.Updated = true;
                Parent.Settings = value;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(true);
        }

        /// <inheritdoc/>
        /// <param name="addExecutionTime"> is execution time added to string.</param>
        public string ToString(bool addExecutionTime)
        {
            if (Parent.Updated)
            {
                List<string> queries = new List<string>();
                GXDbHelpers.GetQueries(null, Parent.Settings, Values, Excluded, queries, null, insertedObjects);
                sql = string.Join(" ", queries.ToArray());
            }
            return sql;
        }

        public static GXInsertArgs Insert<T>(T value)
        {
            return Insert<T>(value, null);
        }

        /// <summary>
        /// Copy rows from the same table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <returns></returns>
        public static GXInsertArgs Insert<T>(GXSelectArgs select)
        {
            GXInsertArgs args = new GXInsertArgs();
            select.Parent.Settings = args.Parent.Settings;
            args.Parent.Updated = true;
            Expression<Func<T, object>> expression = _ => typeof(T);
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(select, expression));
            return args;
        }

        /// <summary>
        /// Copy rows from the table 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <returns></returns>
        public static GXInsertArgs Insert<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            GXInsertArgs args = new GXInsertArgs();
            select.Parent.Settings = args.Parent.Settings;
            args.Add(select, columns);
            return args;
        }

        /// <summary>
        /// Add updated column(s).
        /// </summary>
        /// <param name="columns">Updated columns.</param>
        public void Add<T>(T value, Expression<Func<T, object>> columns)
        {
            Parent.Updated = true;
            Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
        }

        public void Add<T>(GXSelectArgs select, Expression<Func<T, object>> columns)
        {
            Parent.Updated = true;
            Values.Add(new KeyValuePair<object, LambdaExpression>(select, columns));
        }

        public static GXInsertArgs Insert<T>(T[] value, Expression<Func<T, object>> columns)
        {
            return InsertRange<T>(value, columns);
        }

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
            args.Parent.Updated = true;
            args.Values.Add(new KeyValuePair<object, LambdaExpression>(value, columns));
            return args;
        }

        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection)
        {
            return InsertRange(collection, null);
        }

        public static GXInsertArgs InsertRange<T>(IEnumerable<T> collection, Expression<Func<T, object>> columns)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Inserted item can't be null.");
            }
            GXInsertArgs args = new GXInsertArgs();
            args.Parent.Updated = true;
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

        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination[] collections)
        {
            return Add<TItem, TDestination>(new TItem[] { item }, collections);
        }

        public static GXInsertArgs Add<TItem, TDestination>(TItem[] items, TDestination collection)
        {
            return Add<TItem, TDestination>(items, new TDestination[] { collection });
        }

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
                    args.Values.Add(new KeyValuePair<object, LambdaExpression>(target, null));
                }
            }
            return args;
        }

        /// <summary>
        /// Add given item to the n:n collection.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static GXInsertArgs Add<TItem, TDestination>(TItem item, TDestination collection)
        {
            return Add<TItem, TDestination>(new TItem[] { item }, new TDestination[] { collection });
        }

        /// <summary>
        /// Remove item from the n:n collection.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static GXInsertArgs Remove<TItem, TDestination>(TItem item, TDestination collection)
        {
            return null;
        }

        /// <summary>
        /// Exclude columns from the insert.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <returns>Created update attribute.</returns>
        public void Exclude<T>(Expression<Func<T, object>> columns)
        {
            Excluded.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), columns));
            Parent.Updated = true;
        }
    }
}
