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

using Gurux.Service.Orm.Common.Model;
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
    /// Collection of ORDER BY expressions for a SQL query.
    /// </summary>
    public class GXOrderByCollection
    {
        internal readonly List<(LambdaExpression Expression, bool? Descending)> List = new();
        GXSelectArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXOrderByCollection(GXSelectArgs parent)
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
                Parent.Count,
                Parent.Descending);
            if (Parent.Parent.QueryCache.TryGet(cacheKey, out string? cached, out int generationTime))
            {
                Debug.WriteLine("Cached SQL: " + cached);
                return cached!;
            }
            List<GXJoin> joinList = new List<GXJoin>();
            List<GXOrder> orderList = new List<GXOrder>();
            UpdateJoins(Parent.Settings, Parent.Joins, joinList);
            foreach (var it in List)
            {
                OrderBy(Parent.Settings, it.Expression, joinList, orderList);
            }
            StringBuilder sb = new StringBuilder();
            OrderByToString(Parent, sb, orderList, joinList);
            string sql = sb.ToString();
            if (sql != string.Empty)
            {
                Parent.Parent.QueryCache.Set(cacheKey, sql, 0);
                Debug.WriteLine("New SQL: " + sql);
            }
            return sql;
        }

        internal int GetItemHash()
        {
            return Parent.Parent.QueryCache.GetHash(List);
        }

        /// <summary>
        /// Order values by.
        /// </summary>
        /// <param name="settings">DB settings.</param>
        /// <param name="sourceColumn">Columns order by.</param>
        /// <param name="joinList">Join list.</param>
        /// <param name="OrderList">Order list.</param>
        internal static void OrderBy(GXDBSettings settings, LambdaExpression sourceColumn,
            List<GXJoin> joinList,
            List<GXOrder> OrderList)
        {
            GXGetMembersArgs args = new GXGetMembersArgs(settings, TargetType.Column | TargetType.Plain)
            {
                SingleTable = !joinList.Any(),
                Expression = sourceColumn.Body,
            };
            string[] list = GXDbHelpers.GetMemberList(args);
            foreach (string it in list)
            {
                GXOrder o = new GXOrder();
                if (sourceColumn.Parameters.Count == 0)
                {
                    o.Table = sourceColumn.ReturnType;
                }
                else
                {
                    o.Table = sourceColumn.Parameters[0].Type;
                }
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
                if (ce.Value is GXColumnSchema)
                {
                    throw new NotSupportedException("Column metadata joins require a schema SELECT.");
                }
                return GetMemberExpression(ce.Value as Expression, out allowNull);
            }
            throw new ArgumentOutOfRangeException("Invalid join.");
        }

        internal static (GXColumnSchema Column, string? Alias)? GetSchemaJoinColumn(
            GXDBSettings settings, Expression expression)
        {
            while (true)
            {
                if (expression is UnaryExpression conversion &&
                    (conversion.NodeType == ExpressionType.Convert || conversion.NodeType == ExpressionType.ConvertChecked))
                    expression = conversion.Operand;
                else if (expression is ConstantExpression wrapper && wrapper.Value is Expression inner)
                    expression = inner;
                else
                    break;
            }
            if (expression is ConstantExpression constant && constant.Value is GXColumnSchema column)
                return (column, null);

            Expression? collection = null;
            Expression? index = null;
            if (expression is MethodCallExpression item && item.Method.Name == "get_Item" && item.Arguments.Count == 1)
            {
                collection = item.Object;
                index = item.Arguments[0];
            }
            else if (expression is IndexExpression indexed && indexed.Arguments.Count == 1)
            {
                collection = indexed.Object;
                index = indexed.Arguments[0];
            }
            if (collection is MemberExpression columns && columns.Member.Name == nameof(GXTableSchema.Columns) &&
                columns.Member.DeclaringType == typeof(GXTableSchema) &&
                columns.Expression is MethodCallExpression aliasCall &&
                aliasCall.Method.DeclaringType == typeof(GXSql) && aliasCall.Method.Name == nameof(GXSql.As))
            {
                // Evaluate only captured metadata and the index, never the SQL marker itself.
                var table = Expression.Lambda<Func<GXTableSchema>>(aliasCall.Arguments[0]).Compile()();
                ArgumentNullException.ThrowIfNull(table);
                int position = Expression.Lambda<Func<int>>(index!).Compile()();
                return (table.Columns[position], GXDbHelpers.GetTableAlias(settings, aliasCall));
            }
            return null;
        }

        internal static void UpdateJoins(GXDBSettings settings, GXJoinCollection list, List<GXJoin> joins)
        {
            bool allowNull;
            MemberExpression me;
            foreach (var it in list.List)
            {
                if (it.Source != null || it.On is not BinaryExpression predicate)
                    throw new NotSupportedException("Query-source joins require a schema SELECT.");
                GXJoin join = new GXJoin();
                join.Type = it.Type;
                var source = GetSchemaJoinColumn(settings, predicate.Left);
                var destination = GetSchemaJoinColumn(settings, predicate.Right);
                if (source.HasValue && destination.HasValue)
                {
                    var s = source.Value.Column;
                    var t = destination.Value.Column;
                    join.Alias1 = source.Value.Alias;
                    join.Alias2 = destination.Value.Alias;
                    join.AllowNull1 = s.IsNullable;
                    join.AllowNull2 = t.IsNullable;
                    join.Column1 = GXDbHelpers.ConvertToString(settings, TargetType.Column | TargetType.Plain, null, s.Name, null);
                    join.Column2 = GXDbHelpers.ConvertToString(settings, TargetType.Column | TargetType.Plain, null, t.Name, null);
                    join.UpdateTables(settings, s.Parent, t.Parent);
                    joins.Add(join);
                }
                else
                {
                    me = GetMemberExpression(predicate.Left, out allowNull);
                    MemberInfo m = me.Member;
                    Expression e = me.Expression;
                    join.Alias1 = GXDbHelpers.GetTableAlias(settings, e);
                    join.Column1 = GXDbHelpers.ConvertToString(settings, TargetType.Column | TargetType.Plain, null, m, null);
                    join.AllowNull1 = allowNull;
                    var rightMember = GetMemberExpression(predicate.Right, out allowNull);
                    join.Alias2 = GXDbHelpers.GetTableAlias(settings, rightMember.Expression);
                    m = rightMember.Member;
                    join.Column2 = GXDbHelpers.ConvertToString(settings, TargetType.Column | TargetType.Plain, null, m, null);
                    join.AllowNull2 = allowNull;
                    join.UpdateTables(settings, e.Type, m.DeclaringType);
                    joins.Add(join);
                }
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
                    string tableName = null;
                    if (joinList.Any())
                    {
                        tableName = it.Table.Name;
                    }
                    sb.Append(GXDbHelpers.ConvertToString(parent.Settings, TargetType.Column, tableName, it.Column, null));
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
            List.Clear();
        }

        /// <summary>
        /// Add new order by expression.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="expression">LINQ expression.</param>
        public void Add<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add((expression, null));
        }

        public void Add(Expression expression, bool descending = false)
        {
            var lambda = Expression.Lambda(expression);
            List.Add((lambda, descending));
        }
        public void AddRange(IEnumerable<(Expression Expression, bool Descending)> columns)
        {
            foreach (var column in columns) Add(column.Expression, column.Descending);
        }

        private bool Find(Type type, List<string> path, int index)
        {
            bool found = false;
            foreach (var it in GXSqlBuilder.GetProperties(type))
            {
                if (string.Compare(((PropertyInfo)it.Value.Target).Name, path[index], true) == 0)
                {
                    if (path.Count != 1 + index)
                    {
                        found = Find((it.Value.Target as PropertyInfo).PropertyType, path, ++index);
                    }
                    else
                    {
                        found = true;
                        var expression = MethodCallExpression.Parameter(type, path[index]);
                        List.Add((Expression.Lambda(expression), null));
                    }
                    break;
                }
            }
            if (!found)
            {
                throw new ArgumentException(string.Format("Order by failed. Unknown property {0}", path[index]));
            }
            return found;
        }

        /// <summary>
        /// Add new order by expression.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="name">Column name.</param>
        public void Add<T>(string name)
        {
            if (name == null)
            {
                throw new ArgumentException(nameof(name));
            }
            List<string> path = new List<string>(name.Split('.'));
            Find(typeof(T), path, 0);
        }
    }
}
