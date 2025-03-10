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
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    public class GXJoinCollection
    {
        internal List<KeyValuePair<JoinType, BinaryExpression>> List = new List<KeyValuePair<JoinType, BinaryExpression>>();
        internal bool Updated;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXJoinCollection(GXSettingsArgs parent)
        {
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
            if (destinationColumn == null)
            {
                throw new ArgumentNullException("destinationColumn");
            }
            Expression s, d;
            if (sourceColumn.Body is UnaryExpression)
            {
                s = sourceColumn.Body;
            }
            else
            {
                s = Expression.Constant(sourceColumn.Body);
            }
            if (destinationColumn.Body is UnaryExpression)
            {
                d = destinationColumn.Body;
            }
            else
            {
                d = Expression.Constant(destinationColumn.Body);
            }
            List.Add(new KeyValuePair<JoinType, BinaryExpression>(type, BinaryExpression.Equal(s, d)));
            Updated = true;
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

        /// <summary>
        /// Add full join.
        /// </summary>
        public void AddFullJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(JoinType.Full, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Append joins.
        /// </summary>
        /// <param name="joins"></param>
        public void Append(GXJoinCollection joins)
        {
            List.AddRange(joins.List);
        }
    }
}
