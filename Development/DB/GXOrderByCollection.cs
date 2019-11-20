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
using System.Linq.Expressions;
using System.Text;
using Gurux.Service.Orm.Settings;
using System.Reflection;

namespace Gurux.Service.Orm
{
    public class GXOrderByCollection
    {
        internal List<LambdaExpression> List = new List<LambdaExpression>();
        GXSelectArgs Parent;
        internal bool Updated;
        string sql;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXOrderByCollection(GXSelectArgs parent)
        {
            Parent = parent;
        }


        public override string ToString()
        {
            if (Updated)
            {
                List<GXJoin> joinList = new List<GXJoin>();
                List<GXOrder> orderList = new List<GXOrder>();
                UpdateJoins(Parent.Settings, Parent.Joins, joinList);
                foreach (var it in List)
                {
                    OrderBy(Parent.Settings, it, orderList);
                }
                StringBuilder sb = new StringBuilder();
                OrderByToString(Parent, sb, orderList, joinList);
                sql = sb.ToString();
                Updated = false;
            }
            return sql;
        }

        /// <summary>
        /// Order values by.
        /// </summary>
        /// <param name="sourceColumn">Columns order by.</param>
        internal static void OrderBy(GXDBSettings settings, LambdaExpression sourceColumn, List<GXOrder> OrderList)
        {
            string[] list = GXDbHelpers.GetMembers(settings, sourceColumn.Body, '\0', false);
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
            if (expression is MemberExpression)
            {
                MemberInfo m = (expression as MemberExpression).Member;
                Type tp = (m as PropertyInfo).PropertyType;
                allowNull = tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>);
                return (expression as MemberExpression);
            }
            if (expression is UnaryExpression)
            {
                MemberExpression e = GetMemberExpression((expression as UnaryExpression).Operand, out allowNull);
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
            throw new ArgumentOutOfRangeException("Invalid join.");
        }

        internal static void UpdateJoins(GXDBSettings settings, GXJoinCollection list, List<GXJoin> joins)
        {
            char separtor = settings.ColumnQuotation;
            string prefix = settings.TablePrefix;
            bool allowNull;
            MemberExpression me;
            foreach (KeyValuePair<JoinType, BinaryExpression> it in list.List)
            {
                GXJoin join = new GXJoin();
                join.Type = it.Key;
                me = GetMemberExpression(it.Value.Left, out allowNull);
                MemberInfo m = me.Member;
                Expression e = me.Expression;
                join.Column1 = GXDbHelpers.GetColumnName(m, separtor);
                join.AllowNull1 = allowNull;
                m = GetMemberExpression(it.Value.Right, out allowNull).Member;
                join.Column2 = GXDbHelpers.GetColumnName(m, separtor);
                join.AllowNull2 = allowNull;
                join.UpdateTables(e.Type, m.DeclaringType);
                joins.Add(join);
            }
        }

        internal static void OrderByToString(GXSelectArgs parent, StringBuilder sb, List<GXOrder> OrderList, List<GXJoin> joinList)
        {
            bool first = true;
            if (OrderList.Count != 0)
            {
                sb.Append(" ORDER BY ");
                first = true;
                foreach (GXOrder it in OrderList)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    if (parent.Settings.Type == DatabaseType.MSSQL && parent.Count != 0)
                    {
                        string table = GXDbHelpers.GetTableName(it.Table, true, '\0', parent.Settings.TablePrefix);
                        sb.Append(GXDbHelpers.AddQuotes(table + '.' + it.Column, parent.Settings.ColumnQuotation));
                        continue;
                    }
                    //Add table name always until there is a way to check are multiple tables used. if (joinList.Count != 0)
                    {
                        string table = GXDbHelpers.GetTableName(it.Table, true, parent.Settings.TableQuotation, parent.Settings.TablePrefix);
                        sb.Append(table);
                        sb.Append('.');
                    }
                    sb.Append(GXDbHelpers.AddQuotes(it.Column, parent.Settings.ColumnQuotation));
                }
                if (parent.Descending)
                {
                    sb.Append(" DESC");
                }
            }
        }

        /// <summary>
        /// Clear where expressions.
        /// </summary>
        public void Clear()
        {
            Updated = true;
            List.Clear();
        }

        /// <summary>
        /// Add new order by expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        public void Add<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            Updated = true;
            List.Add(expression);
        }
    }
}
