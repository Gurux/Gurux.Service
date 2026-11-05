// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
            string cacheKey = Parent.QueryCache.BuildKey(
                Parent.Settings.Type,
                List);
            if (Parent.QueryCache.TryGet(cacheKey, out string? cached, out int generationTime))
            {
                return cached!;
            }
            string? sql = HavingToString(Parent.Settings, List, !_joins.List.Any());
            if (!string.IsNullOrEmpty(sql))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(" HAVING ");
                sb.Append(sql);
                sql = sb.ToString();
                if (!string.IsNullOrEmpty(sql))
                {
                    Parent.QueryCache.Set(cacheKey, sql, 0);
                }
            }
            return sql ?? string.Empty;
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

        internal static string? HavingToString(GXDBSettings settings,
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

        internal static string? Having(GXDBSettings Settings, LambdaExpression value, bool singleTable)
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
