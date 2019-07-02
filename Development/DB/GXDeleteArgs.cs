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
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Gurux.Service.Orm.Settings;
using System.Collections;
using Gurux.Common.Internal;
using System.Diagnostics;
using Gurux.Common.JSon;

namespace Gurux.Service.Orm
{
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
            Joins = new GXJoinCollection(Parent);
            Where = new GXWhereCollection(Parent);
        }

        /// <summary>
        /// Clear all delete settings.
        /// </summary>
        public void Clear()
        {
            Parent.Clear();
            Table = null;
            Joins.List.Clear();
            Where.Clear();
        }

        public override string ToString()
        {
            if (Parent.Updated || Updated)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("DELETE FROM ");
                sb.Append(GXDbHelpers.GetTableName(Table, true, Parent.Settings.TableQuotation, Parent.Settings.TablePrefix));
                string str = Where.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(" ");
                    sb.Append(str);
                }
                Updated = false;
                return sb.ToString();
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
        /// Where expression.
        /// </summary>
        public GXJoinCollection Joins
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

        internal static GXDeleteArgs Delete(Type type)
        {
            return new GXDeleteArgs() { Table = type, Updated = true };
        }

        public static GXDeleteArgs Delete<T>(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Removed item can't be null.");
            }
            if (item is IEnumerable)
            {
                GXDeleteArgs arg = Delete(GXInternal.GetPropertyType(typeof(T)));
                foreach (var it in item as IEnumerable)
                {
                    arg.Where.Or<T>(q => it);
                }
                return arg;
            }
            return Delete<T>(q => item);
        }

        public static GXDeleteArgs Delete<T>(Expression<Func<T, object>> where)
        {
            GXDeleteArgs arg = DeleteAll<T>();
            if (where != null)
            {
                arg.Where.Or<T>(where);
            }
            return arg;
        }

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

        public static GXDeleteArgs Remove<TItem, TDestination>(TItem item, TDestination[] collections)
        {
            return Remove<TItem, TDestination>(new TItem[] { item }, collections);
        }

        public static GXDeleteArgs Remove<TItem, TDestination>(TItem[] items, TDestination collection)
        {
            return Remove<TItem, TDestination>(items, new TDestination[] { collection });
        }

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
                    object target = GXJsonParser.CreateInstance(si.Relation.RelationMapTable.Relation.PrimaryTable);
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
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static GXDeleteArgs Remove<TItem, TDestination>(TItem item, TDestination collection)
        {
            return Remove<TItem, TDestination>(new TItem[] { item }, new TDestination[] { collection });
        }
    }
}
