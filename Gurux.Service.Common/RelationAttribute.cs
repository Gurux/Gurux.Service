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

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Identifies a relationship to another mapped entity.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a property that represents a relationship between tables.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the related entity type.
        /// </summary>
        public Type? Target
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationAttribute"/> class.
        /// </summary>
        public RelationAttribute()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationAttribute"/> class.
        /// </summary>
        /// <param name="target">The related entity type.</param>
        public RelationAttribute(Type target)
        {
            Target = target;
        }
    }
}
