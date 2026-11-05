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

using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Represents a collection of group by expressions used to construct SQL queries.
    /// </summary>
    public class GXGroupByCollection
    {
        internal List<LambdaExpression> List = new List<LambdaExpression>();
        GXSelectArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXGroupByCollection(GXSelectArgs parent)
        {
            Parent = parent;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string cacheKey = Parent.Parent.QueryCache.BuildKey(
                Parent.Settings.Type,
                List,
                Parent.Joins != null ? Parent.Joins.GetItemHash() : 0,
                Parent.Count);
            if (Parent.Parent.QueryCache.TryGet(cacheKey, out string? cached, out int generationTime))
            {
                return cached!;
            }
            List<GXJoin> joinList = new List<GXJoin>();
            List<GXOrder> orderList = new List<GXOrder>();
            GXOrderByCollection.UpdateJoins(Parent.Settings, Parent.Joins, joinList);
            foreach (var it in List)
            {
                GroupBy(Parent.Settings, joinList, it, orderList);
            }
            StringBuilder sb = new StringBuilder();
            GroupByToString(Parent, sb, orderList, joinList);
            string sql = sb.ToString();
            if (sql != string.Empty)
            {
                Parent.Parent.QueryCache.Set(cacheKey, sql, 0);
            }
            return sql;
        }

        internal int GetItemHash()
        {
            return Parent.Parent.QueryCache.GetHash(List);
        }

        /// <summary>
        /// Group values by.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="joinList">List of joins.</param>
        /// <param name="sourceColumn">Columns order by.</param>
        /// <param name="OrderList">List of orders.</param>
        internal static void GroupBy(GXDBSettings settings,
            List<GXJoin> joinList,
            LambdaExpression sourceColumn,
            List<GXOrder> OrderList)
        {
            GXGetMembersArgs args = new GXGetMembersArgs(settings, TargetType.Column)
            {
                SingleTable = !joinList.Any(),
                Expression = sourceColumn.Body,
            };
            string[] list = GXDbHelpers.GetMemberList(args) ?? throw new InvalidOperationException("Column expression did not identify any members.");
            foreach (string it in list)
            {
                GXOrder o = new GXOrder();
                o.Table = sourceColumn.Parameters[0].Type;
                o.Column = it;
                OrderList.Add(o);
            }
        }

        internal static MemberExpression GetMemberExpression(Expression? expression, out bool allowNull)
        {
            if (expression is MemberExpression me)
            {
                MemberInfo m = me.Member;
                Type tp = m is PropertyInfo property ? property.PropertyType : throw new ArgumentException("Join member must be a property.");
                allowNull = tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>);
                return me;
            }
            if (expression is UnaryExpression ue)
            {
                MemberExpression e = GetMemberExpression(ue.Operand, out allowNull);
                //If value is nullable.
                if (e.Expression != null && e.Expression.Type.IsGenericType && e.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    e = GetMemberExpression(e.Expression, out allowNull);
                    if (e.NodeType == ExpressionType.MemberAccess)
                    {
                        allowNull = false;
                    }
                }
                return e;
            }
            if (expression is ConstantExpression ce)
            {
                return GetMemberExpression(ce.Value as Expression, out allowNull);
            }
            throw new ArgumentOutOfRangeException("Invalid join.");
        }

        internal static void GroupByToString(GXSelectArgs parent, StringBuilder sb, List<GXOrder> groupList, List<GXJoin> joinList)
        {
            if (groupList.Count != 0)
            {
                sb.Append(" GROUP BY ");
                bool first = true;
                foreach (GXOrder it in groupList)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    //Table name is not added if there is only one table.
                    string? tableName = null;
                    if (joinList.Any())
                    {
                        tableName = GXDbHelpers.ConvertToString(parent.Settings, TargetType.Column, null, it.Table, null);
                    }
                    sb.Append(GXDbHelpers.ConvertToString(parent.Settings, TargetType.Column, tableName, it.Column, null));
                }
            }
        }

        /// <summary>
        /// Clear where expressions.
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        /// Add new group by expression.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="expression">The expression identifying the selected members.</param>
        public void Add<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(expression);
        }
    }
}
