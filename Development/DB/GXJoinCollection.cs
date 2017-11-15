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

namespace Gurux.Service.Orm
{
    public class GXJoinCollection
    {
        internal List<KeyValuePair<JoinType, BinaryExpression>> List = new List<KeyValuePair<JoinType, BinaryExpression>>();
        GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXJoinCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        private static UnaryExpression GetExpression(Expression e)
        {
            if (e is UnaryExpression)
            {
                return (UnaryExpression)e;
            }
            if (e is MemberExpression)
            {
                MemberExpression m = (MemberExpression)e;
                object u = Expression.Convert(m, typeof(UnaryExpression));
                return (UnaryExpression)u;
            }
            if (e is ParameterExpression)
            {
                throw new Exception("Invalid expression.");
            }
            throw new Exception("Invalid expression.");
        }

        /// <summary>
        /// Add inner join.
        /// </summary>
        private void AddJoin<TSourceTable, TDestinationTable>(JoinType type, Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            if (sourceColumn == null)
            {
                throw new ArgumentNullException("sourceColumn");
            }
            if (sourceColumn == null)
            {
                throw new ArgumentNullException("destinationColumn");
            }
            Parent.Updated = true;
            Expression t = Expression.Equal(sourceColumn.Body, destinationColumn.Body);
            List.Add(new KeyValuePair<JoinType, BinaryExpression>(type, t as BinaryExpression));
        }

        /// <summary>
        /// Add inner join.
        /// </summary>
        public void AddInnerJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(JoinType.Inner, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add left join.
        /// </summary>
        public void AddLeftJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(JoinType.Left, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add right join.
        /// </summary>
        public void AddRightJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(JoinType.Right, sourceColumn, destinationColumn);
        }
    }
}
