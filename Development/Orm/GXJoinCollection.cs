// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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

        /// <summary>Add a query source join.</summary>
        /// <param name="source">The source query.</param>
        /// <param name="type">The join type.</param>
        /// <param name="on">The optional join condition.</param>
        public void Add(GXSelectArgs source, JoinType type, Expression? on = null) => List.Add((type, on, source));

        /// <summary>Append joins from another collection.</summary>
        /// <param name="source">The joins to append.</param>
        public void AddRange(GXJoinCollection source) => Append(source);


        internal int GetItemHash()
        {
            return Parent.QueryCache.GetHash(List);
        }
    }
}
