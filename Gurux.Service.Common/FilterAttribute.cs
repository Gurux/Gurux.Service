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

using Gurux.Service.Orm.Common.Enums;
using System;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Specifies how a mapped value is compared when building a filter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class FilterAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the comparison used by the filter.
        /// </summary>
        public FilterType FilterType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value for which the filter is omitted.
        /// </summary>
        public object? DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class.
        /// </summary>
        /// <param name="filterType">The comparison used by the filter.</param>
        public FilterAttribute(FilterType filterType)
        {
            FilterType = filterType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterAttribute"/> class.
        /// </summary>
        /// <param name="filterType">The comparison used by the filter.</param>
        /// <param name="defaultValue">The value for which the filter is omitted.</param>
        public FilterAttribute(FilterType filterType, object defaultValue)
        {
            FilterType = filterType;
            DefaultValue = defaultValue;
        }
    }
}
