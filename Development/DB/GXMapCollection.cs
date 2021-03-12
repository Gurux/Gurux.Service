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
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    public class GXMapCollection
    {
        internal List<BinaryExpression> List = new List<BinaryExpression>();
        GXSettingsArgs Parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal GXMapCollection(GXSettingsArgs parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Add map.
        /// </summary>
        public void AddMap<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> destinationColumn,
            Expression<Func<TDestinationTable, object>> sourceColumn)
        {
            if (destinationColumn == null)
            {
                throw new ArgumentNullException("destinationColumn");
            }
            if (sourceColumn == null)
            {
                throw new ArgumentNullException("sourceColumn");
            }
            Parent.Updated = true;
            Expression t = Expression.Equal(destinationColumn.Body, sourceColumn.Body);
            List.Add(t as BinaryExpression);
        }
    }
}
