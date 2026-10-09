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
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
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
    internal enum WhereType
    {
        And,
        Or
    }

    /// <summary>
    /// Collection of WHERE conditions for a SQL query.
    /// </summary>
    public class GXWhereCollection
    {
        internal List<KeyValuePair<WhereType, LambdaExpression>> List = new List<KeyValuePair<WhereType, LambdaExpression>>();
        internal GXSettingsArgs Parent;
        internal GXJoinCollection? Joins;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXWhereCollection(GXSettingsArgs parent, GXJoinCollection? joins)
        {
            Parent = parent;
            Joins = joins;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string cacheKey = Parent.QueryCache.BuildKey(Parent.Settings.Type,
                List,
                Joins != null ? Joins.GetItemHash() : 0);
            if (Parent.QueryCache.TryGet(cacheKey, out string? cached, out int generationTime))
            {
                return cached!;
            }
            string sql = string.Empty;
            GXGetMembersArgs args = new GXGetMembersArgs(Parent.Settings, TargetType.Where)
            {
                StringBuilder = new StringBuilder(),
                SingleTable = Joins?.List.Any() != true,
            };
            WhereToString(args, List, args.SingleTable);
            if (args.StringBuilder.Length > 0)
            {
                sql = "WHERE " + args.StringBuilder.ToString();
            }
            if (sql != string.Empty)
            {
                Parent.QueryCache.Set(cacheKey, sql, 0);
            }
            return sql;
        }

        internal int GetItemHash()
        {
            return Parent.QueryCache.GetHash(List);
        }

        internal string LimitToString()
        {
            return LimitToString(Parent.Settings, Parent.Index, Parent.Count);
        }

        /// <summary>
        /// Clear where expressions.
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        /// Add And expression to where.
        /// </summary>
        public void And<T>(Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.And, expression));
        }

        /// <summary>Adds a predicate for a physical column.</summary>
        public void And(Expression<Func<GXColumnSchema, bool>> predicate)
        {
            And<GXColumnSchema>(predicate);
        }

        /// <summary>
        /// Add And expression to where using <see cref="GXSelectArgs"/>. 
        /// This allows for more complex conditions to be added to the WHERE clause.
        /// </summary>
        /// <param name="args"></param>
        public void And(GXSelectArgs args)
        {
            var constant = Expression.Constant(args, typeof(GXSelectArgs));
            var expression = Expression.Lambda(constant);
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.And, expression));
        }

        /// <summary>
        /// Add And expression to where using <see cref="GXSelectArgs"/>. 
        /// This allows for more complex conditions to be added to the WHERE clause.
        /// </summary>
        /// <param name="args"></param>
        public void Or(GXSelectArgs args)
        {
            var constant = Expression.Constant(args, typeof(GXSelectArgs));
            var expression = Expression.Lambda(constant);
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, expression));
        }

        /// <summary>
        /// Add or expression to where.
        /// </summary>
        public void Or<T>(Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, expression));
        }

        // Entity expressions are translated into key predicates by the ORM.
        internal void AndEntity<T>(Expression<Func<T, object>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            List.Add(new(WhereType.And, expression));
        }

        internal void OrEntity<T>(Expression<Func<T, object>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            List.Add(new(WhereType.Or, expression));
        }

        private sealed class BooleanConstantSimplifier : ExpressionVisitor
        {
            protected override Expression VisitBinary(BinaryExpression node)
            {
                var visited = (BinaryExpression)base.VisitBinary(node);
                if (visited.Type != typeof(bool))
                {
                    return visited;
                }

                bool and = visited.NodeType == ExpressionType.AndAlso || visited.NodeType == ExpressionType.And;
                bool or = visited.NodeType == ExpressionType.OrElse || visited.NodeType == ExpressionType.Or;
                if (!and && !or)
                {
                    return visited;
                }

                if (visited.Left is ConstantExpression { Value: bool left })
                {
                    return and ? left ? visited.Right : Expression.Constant(false)
                    : left ? Expression.Constant(true) : visited.Right;
                }

                if (visited.Right is ConstantExpression { Value: bool right })
                {
                    return and ? right ? visited.Left : Expression.Constant(false)
                    : right ? Expression.Constant(true) : visited.Left;
                }

                return visited;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                var visited = (UnaryExpression)base.VisitUnary(node);
                return visited.Type == typeof(bool) && visited.NodeType == ExpressionType.Not && visited.Operand is ConstantExpression { Value: bool value }
                    ? Expression.Constant(!value) : visited;
            }
        }

        // SQL AND binds more tightly than OR. Simplify constants within each AND group,
        // then combine the remaining groups without changing the original precedence.
        private static List<KeyValuePair<WhereType, LambdaExpression>> NormalizeBooleanConstants(
            List<KeyValuePair<WhereType, LambdaExpression>> list)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var simplifier = new BooleanConstantSimplifier();
            var normalized = new List<KeyValuePair<WhereType, LambdaExpression>>(list.Count);
            bool hasConstant = false;
            foreach (var item in list)
            {
                var body = simplifier.Visit(item.Value.Body)!;
                var lambda = ReferenceEquals(body, item.Value.Body) ? item.Value : Expression.Lambda(body, item.Value.Parameters);
                hasConstant |= body is ConstantExpression { Value: bool };
                normalized.Add(new(item.Key, lambda));
            }
            if (!hasConstant)
            {
                return normalized;
            }

            var groups = new List<List<LambdaExpression>>();
            foreach (var item in normalized)
            {
                if (groups.Count == 0 || item.Key == WhereType.Or)
                {
                    groups.Add(new());
                }

                groups[groups.Count - 1].Add(item.Value);
            }
            var result = new List<KeyValuePair<WhereType, LambdaExpression>>();
            foreach (var group in groups)
            {
                if (group.Any(predicate => predicate.Body is ConstantExpression { Value: false }))
                {
                    continue;
                }

                var predicates = group.Where(predicate => predicate.Body is not ConstantExpression { Value: true }).ToArray();
                // A true OR group makes the whole WHERE unconditional.
                if (predicates.Length == 0)
                {
                    return new();
                }

                for (int index = 0; index != predicates.Length; ++index)
                {
                    result.Add(new(index == 0 && result.Count != 0 ? WhereType.Or : WhereType.And, predicates[index]));
                }
            }
            if (result.Count == 0)
            {
                result.Add(new(WhereType.And, Expression.Lambda(Expression.Constant(false), list[0].Value.Parameters)));
            }

            return result;
        }

        private static void WhereToString(GXGetMembersArgs args,
            List<KeyValuePair<WhereType, LambdaExpression>> list,
            bool singleTable)
        {
            if (args.StringBuilder == null)
            {
                throw new ArgumentNullException("args.StringBuilder");
            }
            list = NormalizeBooleanConstants(list);
            if (list.Count != 0)
            {
                bool emptyId = false;
                bool first = true;
                if (list.Count != 1)
                {
                    args.StringBuilder.Append('(');
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
                                args.StringBuilder.Append(") AND (");
                                break;
                            case WhereType.Or:
                                args.StringBuilder.Append(") OR (");
                                break;
                            default:
                                throw new ArgumentException("Invalid where argument: " + it.Key.ToString());
                        }
                    }
                    args.Expression = it.Value;
                    if (it.Value.Body is ConstantExpression { Value: false })
                    {
                        args.StringBuilder.Append("1 = 0");
                    }
                    else
                    {
                        GXDbHelpers.GetMembers(args);
                    }

                    if (args.StringBuilder.Length == 0)
                    {
                        first = emptyId = true;
                    }
                }
                if (list.Count != 1 && !emptyId)
                {
                    args.StringBuilder.Append(')');
                }
            }
        }

        internal static string LimitToString(GXDBSettings settings, long index, long count)
        {
            StringBuilder sb;
            if ((index != 0 || count != 0))
            {
                if (settings.LimitType == LimitType.Limit)
                {
                    sb = new StringBuilder();
                    sb.Append("LIMIT ");
                    sb.Append(index);
                    sb.Append(",");
                    sb.Append(count);
                    return sb.ToString();
                }
                else if (settings.LimitType == LimitType.Oracle)
                {
                    sb = new StringBuilder();
                    if (index == 0) //Top
                    {
                        sb.Append("SELECT * FROM ({0}) WHERE ROWNUM < ");
                        sb.Append(count + 1);
                        return sb.ToString();
                    }
                    else //Limit
                    {
                        sb.Append("SELECT * FROM (SELECT GX.*, ROWNUM rnum FROM ({0}) GX WHERE ROWNUM < ");
                        sb.Append(index + count + 1);
                        sb.Append(") WHERE rnum > ");
                        sb.Append(index);
                        return sb.ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Append all conditions from another <see cref="GXWhereCollection"/> to this collection.
        /// </summary>
        /// <param name="where">The collection whose conditions are appended.</param>
        public void Append(GXWhereCollection where)
        {
            List.AddRange(where.List);
        }

        /// <summary>
        /// Update where condition.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="filters">The object whose mapped values provide the filter.</param>
        public void FilterBy(IEnumerable<(GXColumnSchema Column, object? value)> filters)
        {
            foreach (var it in filters)
            {
                if (it.value != null)
                {
                    And<GXColumnSchema>(q => it.Column == it.value);
                }
            }
        }

        /// <summary>
        /// Update where condition.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="target">The object whose mapped values provide the filter.</param>
        public void FilterBy<T>(T target)
        {
            if (target != null)
            {
                Dictionary<string, GXSerializedItem> properties = GXSqlBuilder.GetProperties(GXInternal.GetPropertyType(target.GetType()));
                foreach (var it in properties)
                {
                    if ((it.Value.Attributes & Attributes.Filter) != 0 && it.Value.Get != null)
                    {
                        object actual = it.Value.Get(target);
                        if (actual != null && it.Value.FilterValue == null)
                        {
                            if (actual is DateTime d)
                            {
                                if (d == DateTime.MinValue)
                                {
                                    continue;
                                }
                            }
                            else if (actual is DateTimeOffset dto)
                            {
                                if (dto == DateTimeOffset.MinValue)
                                {
                                    continue;
                                }
                            }
                            else if (actual is Guid q)
                            {
                                if (q == Guid.Empty)
                                {
                                    continue;
                                }
                            }
                            else if (!(actual is string))
                            {
                                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(actual.GetType()))
                                {
                                    foreach (var e1 in (System.Collections.IEnumerable)actual)
                                    {
                                        FilterBy(e1);
                                    }
                                    continue;
                                }
                                else if (actual.GetType().IsClass)
                                {
                                    FilterBy(actual);
                                    continue;
                                }
                            }
                        }
                        if (Convert.ToString(it.Value.FilterValue) != Convert.ToString(actual))
                        {
                            if (actual != null)
                            {
                                if (actual.GetType().IsEnum)
                                {
                                    actual = Convert.ToInt64(actual);
                                }
                                if (actual is bool b)
                                {
                                    if (this.Parent.Settings.Type == DatabaseType.PostgreSQL)
                                    {
                                        And<T>(q => it.Value.Target.Equals(b));
                                    }
                                    else
                                    {
                                        int val = b ? 1 : 0;
                                        And<T>(q => it.Value.Target.Equals(val));
                                    }
                                }
                                else if (actual is Guid)
                                {
                                    And<T>(q => it.Value.Target == actual);
                                }
                                else
                                {
                                    switch (it.Value.FilterType)
                                    {
                                        case FilterType.Exact:
                                            And<T>(q => it.Value.Target == actual);
                                            break;
                                        case FilterType.Equals:
                                            And<T>(q => it.Value.Target.Equals(actual));
                                            break;
                                        case FilterType.Greater:
                                            And<T>(q => GXSql.Greater(it.Value.Target, actual));
                                            break;
                                        case FilterType.Less:
                                            And<T>(q => GXSql.Less(it.Value.Target, actual));
                                            break;
                                        case FilterType.GreaterOrEqual:
                                            And<T>(q => GXSql.GreaterOrEqual(it.Value.Target, actual));
                                            break;
                                        case FilterType.LessOrEqual:
                                            And<T>(q => GXSql.LessOrEqual(it.Value.Target, actual));
                                            break;
                                        case FilterType.StartsWith:
                                            And<T>(q => GXSql.StartsWith(it.Value.Target, actual));
                                            break;
                                        case FilterType.EndsWith:
                                            And<T>(q => GXSql.EndsWith(it.Value.Target, actual));
                                            break;
                                        case FilterType.Contains:
                                            And<T>(q => GXSql.Contains(it.Value.Target, actual));
                                            break;
                                        case FilterType.Null:
                                            //Value is not null if filter value is given.
                                            //This can be used with remove time.
                                            And<T>(q => it.Value.Target != null);
                                            break;
                                        case FilterType.NotNull:
                                            //Value must be null if filter value is given.
                                            //This can be used with remove time.
                                            And<T>(q => it.Value.Target == null);
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException(nameof(it.Value.FilterType));
                                    }
                                }
                            }
                        }
                        else if (it.Value.FilterType == FilterType.Null)
                        {
                            //If value must be null. This can be used with remove time.
                            And<T>(q => it.Value.Target == null);
                        }
                    }
                }
            }
        }
    }
}
