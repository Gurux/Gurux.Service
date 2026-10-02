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
using Gurux.Service.Orm.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Collection of JOIN expressions for a SQL query.
    /// </summary>
    public class GXJoinCollection
    {
        internal readonly List<(JoinType Type, Expression? On, GXSelectArgs? Source)> List = new();
        readonly GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXJoinCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Add join.
        /// </summary>
        internal void AddJoin<TSourceTable, TDestinationTable>(JoinType type, Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(type, (LambdaExpression)sourceColumn, (LambdaExpression)destinationColumn);
        }

        /// <summary>
        /// Add join.
        /// </summary>
        internal void AddJoin(JoinType type, LambdaExpression sourceColumn, LambdaExpression destinationColumn)
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
            List.Add((type, Expression.Equal(s, d), null));
        }

        /// <summary>
        /// Add join.
        /// </summary>
        private void AddJoin(JoinType type, GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            if (sourceColumn == null)
            {
                throw new ArgumentNullException("sourceColumn");
            }
            if (destinationColumn == null)
            {
                throw new ArgumentNullException("destinationColumn");
            }
            Expression s = Expression.Constant(sourceColumn), d = Expression.Constant(destinationColumn);
            List.Add((type, Expression.Equal(s, d), null));
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
        /// Add cross join.
        /// </summary>
        public void AddCrossJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn)
        {
            AddJoin(JoinType.Cross, sourceColumn, destinationColumn);
        }


        /// <summary>
        /// Append joins.
        /// </summary>
        /// <param name="joins">The joins to append to this collection.</param>
        public void Append(GXJoinCollection joins)
        {
            List.AddRange(joins.List);
        }

        /// <summary>
        /// Add inner join.
        /// </summary>
        public void AddInnerJoin(GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            AddJoin(JoinType.Inner, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add left join.
        /// </summary>
        public void AddLeftJoin(GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            AddJoin(JoinType.Left, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add right join.
        /// </summary>
        public void AddRightJoin(GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            AddJoin(JoinType.Right, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add full join.
        /// </summary>
        public void AddFullJoin(GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            AddJoin(JoinType.Full, sourceColumn, destinationColumn);
        }

        /// <summary>
        /// Add cross join.
        /// </summary>
        public void AddCrossJoin(GXColumnSchema sourceColumn, GXColumnSchema destinationColumn)
        {
            AddJoin(JoinType.Cross, sourceColumn, destinationColumn);
        }

        public void Add(GXSelectArgs source, JoinType type, Expression? on = null) => List.Add((type, on, source));

        public void AddRange(GXJoinCollection source) => Append(source);


        internal int GetItemHash()
        {
            return Parent.QueryCache.GetHash(List);
        }
    }
}
