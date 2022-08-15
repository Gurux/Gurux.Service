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

namespace Gurux.Service.Orm
{
    public class GXHavingCollection
    {
        internal List<KeyValuePair<WhereType, LambdaExpression>> List = new List<KeyValuePair<WhereType, LambdaExpression>>();
        string sql;
        GXSettingsArgs Parent;
        internal bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXHavingCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Parent.Updated || Updated)
            {
                StringBuilder sb = new StringBuilder();
                string str = HavingToString(Parent.Settings, List);
                if (!string.IsNullOrEmpty(str))
                {
                    sb.Append(" HAVING ");
                    sb.Append(str);
                }
                sql = sb.ToString();
                Updated = false;
            }
            return sql;
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
        /// Add And expression to having.
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
        /// Add or expression to having.
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

        internal static string HavingToString(GXDBSettings settings, List<KeyValuePair<WhereType, LambdaExpression>> list)
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
                    sb.Append(Having(settings, it.Value, list.Count == 1));
                }
                if (list.Count > 1)
                {
                    sb.Append(')');
                }
                return sb.ToString();
            }
            return null;
        }

        internal static string Having(GXDBSettings Settings, LambdaExpression value, bool removebrackets)
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
