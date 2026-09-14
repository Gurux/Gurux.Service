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
    /// Specifies index options for a mapped type or member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the index requires unique values.
        /// </summary>
        public bool Unique
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether index values are sorted in descending order.
        /// </summary>
        public bool Descend
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the index includes only null values.
        /// </summary>
        public bool IncludeOnlyNull
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether null values are excluded from the index.
        /// </summary>
        public bool ExcludeNull
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the index is clustered.
        /// </summary>
        public bool Clustered
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a unique index with ascending sort order.
        /// </summary>
        public IndexAttribute()
        {
            Unique = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        public IndexAttribute(bool unique)
        {
            Unique = unique;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="descend"><see langword="true"/> to sort in descending order; otherwise, <see langword="false"/>.</param>
        public IndexAttribute(bool unique, bool descend)
        {
            Unique = unique;
            Descend = descend;
        }
    }
}
