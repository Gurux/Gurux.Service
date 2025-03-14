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
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using Gurux.Service.Orm.Settings;
using Gurux.Common.Internal;
using System.Reflection;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Internal;

namespace Gurux.Service.Orm
{
    internal enum WhereType
    {
        And,
        Or
    }

    public class GXWhereCollection
    {
        internal List<KeyValuePair<WhereType, LambdaExpression>> List = new List<KeyValuePair<WhereType, LambdaExpression>>();
        internal string sql;
        internal GXSettingsArgs Parent;
        internal bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXWhereCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Parent.Updated || Updated)
            {
                StringBuilder sb = new StringBuilder();
                string str = WhereToString(Parent.Settings, List);
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append("WHERE ");
                    sb.Append(str);
                }
                sql = sb.ToString();
                Updated = false;
            }
            return sql;
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
            Updated = true;
            List.Clear();
        }

        /// <summary>
        /// Add And expression to where.
        /// </summary>
        public void And<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.And, expression));
            Updated = true;
        }

        /// <summary>
        /// Add or expression to where.
        /// </summary>
        public void Or<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, expression));
            Updated = true;
        }

        internal static string WhereToString(GXDBSettings settings, List<KeyValuePair<WhereType, LambdaExpression>> list)
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
                    sb.Append(Where(settings, it.Value, list.Count == 1));
                }
                if (list.Count > 1)
                {
                    sb.Append(')');
                }
                return sb.ToString();
            }
            return null;
        }

        internal static string LimitToString(GXDBSettings settings, UInt32 index, UInt32 count)
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

        internal static string Where(GXDBSettings Settings, LambdaExpression value, bool removebrackets)
        {
            if (value != null)
            {
                string str;
                string post = null;
                if (Settings.UseQuotationWhereColumns)
                {
                    str = GXDbHelpers.GetMembers(Settings, value.Body, Settings.ColumnQuotation, true, ref post)[0];
                }
                else
                {
                    str = GXDbHelpers.GetMembers(Settings, value.Body, '\'', true, ref post)[0];
                }
                //Remove brackets.
                if (removebrackets && str.Length > 2)
                {
                    return str.Substring(1, str.Length - 2);
                }
                else
                {
                    return str;
                }
            }
            return null;
        }

        public void Append(GXWhereCollection where)
        {
            List.AddRange(where.List);
        }

        /// <summary>
        /// Update where condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
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
                                    foreach(var e1 in (System.Collections.IEnumerable) actual)
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
                                    int val = b ? 1 : 0;
                                    And<T>(q => it.Value.Target.Equals(val));
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
