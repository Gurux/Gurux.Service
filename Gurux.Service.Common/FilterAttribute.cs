﻿//
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
    /// Filter attribute can be used to filter columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class FilterAttribute : Attribute
    {
        /// <summary>
        /// Filter type.
        /// </summary>
        public FilterType FilterType
        {
            get;
            set;
        }

        /// <summary>
        /// Default value when filter is ignored.
        /// </summary>
        public object DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterType">Filter type.</param>
        public FilterAttribute(FilterType filterType)
        {
            FilterType = filterType;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterType">Filter type.</param>
        /// <param name="defaultValue">Default value.</param>
        public FilterAttribute(FilterType filterType, object defaultValue)
        {
            FilterType = filterType;
            DefaultValue = defaultValue;
        }
    }
}
