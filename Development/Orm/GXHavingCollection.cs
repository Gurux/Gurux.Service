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

using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Collection of having expressions.
    /// </summary>
    public class GXHavingCollection
    {
        internal List<KeyValuePair<WhereType, LambdaExpression>> List = new List<KeyValuePair<WhereType, LambdaExpression>>();
        GXSettingsArgs Parent;
        private GXJoinCollection _joins;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXHavingCollection(GXSettingsArgs parent, GXJoinCollection joins)
        {
            Parent = parent;
            _joins = joins;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string cacheKey = Parent.QueryCache.BuildKey(List);
            if (Parent.QueryCache.TryGet(cacheKey, out string cached))
            {
                Debug.WriteLine("Cached SQL: " + cached);
                return cached;
            }
            string sql = HavingToString(Parent.Settings, List, !_joins.List.Any());
            if (!string.IsNullOrEmpty(sql))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(" HAVING ");
                sb.Append(sql);
                sql = sb.ToString();
                Parent.QueryCache.Set(cacheKey, sql);
                Debug.WriteLine("New SQL: " + sql);
            }
            return sql;
        }

        internal int GetItemHash()
        {
            return Parent.QueryCache.GetHash(List);
        }

        /// <summary>
        /// Clear where expressions.
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        /// Add And expression to having.
        /// </summary>
        public void And<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.And, expression));
        }

        /// <summary>
        /// Add or expression to having.
        /// </summary>
        public void Or<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, expression));
        }

        internal static string HavingToString(GXDBSettings settings,
            List<KeyValuePair<WhereType, LambdaExpression>> list, bool singleTable)
        {
            if (list.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                bool first = true;
                if (list.Count > 1)
                {
                    sb.Append('(');
                }
                foreach (var it in list)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        switch (it.Key)
                        {
                            case WhereType.And:
                                sb.Append(" AND ");
                                break;
                            case WhereType.Or:
                                sb.Append(" OR ");
                                break;
                            default:
                                throw new ArgumentException("Invalid where argument: " + it.Key.ToString());
                        }
                    }
                    sb.Append(Having(settings, it.Value, singleTable));
                }
                if (list.Count > 1)
                {
                    sb.Append(')');
                }
                return sb.ToString();
            }
            return null;
        }

        internal static string Having(GXDBSettings Settings, LambdaExpression value, bool singleTable)
        {
            if (value != null)
            {
                GXGetMembersArgs args = new GXGetMembersArgs(Settings, TargetType.Column)
                {
                    StringBuilder = new StringBuilder(),
                    Expression = value.Body,
                    SingleTable = singleTable,
                };
                GXDbHelpers.GetMembers(args);
                return args.StringBuilder.ToString();
            }
            return null;
        }
    }
}
