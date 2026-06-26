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
                List,
                Parent.Joins != null ? Parent.Joins.GetItemHash() : 0,
                Parent.Count);
            if (Parent.Parent.QueryCache.TryGet(cacheKey, out string cached))
            {
                Debug.WriteLine("Cached SQL: " + cached);
                return cached;
            }
            List<GXJoin> joinList = new List<GXJoin>();
            List<GXOrder> orderList = new List<GXOrder>();
            UpdateJoins(Parent.Settings, Parent.Joins, joinList);
            foreach (var it in List)
            {
                GroupBy(Parent.Settings, joinList, it, orderList);
            }
            StringBuilder sb = new StringBuilder();
            GroupByToString(Parent, sb, orderList, joinList);
            string sql = sb.ToString();
            if (sql != string.Empty)
            {
                Parent.Parent.QueryCache.Set(cacheKey, sql);
                Debug.WriteLine("New SQL: " + sql);
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
            string[] list = GXDbHelpers.GetMemberList(args);
            foreach (string it in list)
            {
                GXOrder o = new GXOrder();
                o.Table = sourceColumn.Parameters[0].Type;
                o.Column = it;
                OrderList.Add(o);
            }
        }

        internal static MemberExpression GetMemberExpression(Expression expression, out bool allowNull)
        {
            if (expression is MemberExpression me)
            {
                MemberInfo m = me.Member;
                Type tp = (m as PropertyInfo).PropertyType;
                allowNull = tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>);
                return (expression as MemberExpression);
            }
            if (expression is UnaryExpression ue)
            {
                MemberExpression e = GetMemberExpression(ue.Operand, out allowNull);
                //If value is nullable.
                if (e.Expression.Type.IsGenericType && e.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
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

        internal static void UpdateJoins(GXDBSettings settings, GXJoinCollection list, List<GXJoin> joins)
        {
            char separtor = settings.ColumnNameQuoteCharacter;
            bool allowNull;
            MemberExpression me;
            foreach (KeyValuePair<JoinType, BinaryExpression> it in list.List)
            {
                GXJoin join = new GXJoin();
                join.Type = it.Key;
                me = GetMemberExpression(it.Value.Left, out allowNull);
                MemberInfo m = me.Member;
                Expression e = me.Expression;
                join.Column1 = GXDbHelpers.ConvertToString(settings, TargetType.Column, null, m, null);
                join.AllowNull1 = allowNull;
                m = GetMemberExpression(it.Value.Right, out allowNull).Member;
                join.Column2 = GXDbHelpers.ConvertToString(settings, TargetType.Column, null, m, null);
                join.AllowNull2 = allowNull;
                join.UpdateTables(e.Type, m.DeclaringType);
                joins.Add(join);
            }
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
                    string tableName = null;
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
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
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
