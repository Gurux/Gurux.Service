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
    /// Specifies whether a mapped database column requires a non-null value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IsRequiredAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether null values are prohibited.
        /// </summary>
        public bool IsRequired
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes an attribute that prohibits null column values.
        /// </summary>
        public IsRequiredAttribute()
        {
            IsRequired = true;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsRequiredAttribute"/> class.
        /// </summary>
        /// <param name="required"><see langword="true"/> to prohibit null values; otherwise, <see langword="false"/>.</param>
        public IsRequiredAttribute(bool required)
        {
            IsRequired = required;
        }
    }
}
