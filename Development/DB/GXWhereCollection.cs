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
using System.Text;
using System.Linq.Expressions;
using Gurux.Service.Orm.Settings;

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
        string sql;
        GXSettingsArgs Parent;
        bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXWhereCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

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
            Updated = true;
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.And, expression));
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
            Updated = true;
            List.Add(new KeyValuePair<WhereType, LambdaExpression>(WhereType.Or, expression));
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
                if (Settings.UseQuotationWhereColumns)
                {
                    str = GXDbHelpers.GetMembers(Settings, value.Body, Settings.ColumnQuotation, true)[0];
                }
                else
                {
                    str = GXDbHelpers.GetMembers(Settings, value.Body, '\0', true)[0];
                }
                //Remove brackets.
                if (removebrackets)
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
    }
}
