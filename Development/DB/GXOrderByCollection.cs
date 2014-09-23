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
using Gurux.Service.Db.Settings;
using System.Reflection;

namespace Gurux.Service.Db
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
                OrderByToString(sb, orderList, Parent.Descending, joinList);
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
            string table = GXDbHelpers.GetTableName(sourceColumn.Parameters[0].Type, true, settings.ColumnQuotation, settings.TablePrefix);
            foreach (string it in list)
            {
                GXOrder o = new GXOrder();
                o.Table = table;
                o.Column = it;
                OrderList.Add(o);
            }
        }

        internal static void UpdateJoins(GXDBSettings settings, GXJoinCollection list, List<GXJoin> joins)
        {
            char separtor = settings.ColumnQuotation;
            string prefix = settings.TablePrefix;
            foreach (var it in list.List)
            {
                GXJoin join = new GXJoin();
                join.Type = it.Key;
                MemberInfo m = (it.Value.Left as MemberExpression).Member;
                join.Table1 = GXDbHelpers.GetTableName(m.DeclaringType, false, separtor, prefix);
                join.Column1 = GXDbHelpers.GetColumnName(m, separtor);
                m = (it.Value.Right as MemberExpression).Member;
                join.Table2 = GXDbHelpers.GetTableName(m.DeclaringType, false, separtor, prefix);
                join.Column2 = GXDbHelpers.GetColumnName(m, separtor);
                joins.Add(join);
            }
        }

        internal static void OrderByToString(StringBuilder sb, List<GXOrder> OrderList, bool OrderByDesc, List<GXJoin> joinList)
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
                    //Add table name always until there is a way to check are multible tables used. if (joinList.Count != 0)
                    {
                        sb.Append(it.Table);
                        sb.Append('.');
                    }
                    sb.Append(it.Column);
                }
                if (OrderByDesc)
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
