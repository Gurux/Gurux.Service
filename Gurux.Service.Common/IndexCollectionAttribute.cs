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

using System;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Defines a named or unnamed index over one or more table columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class IndexCollectionAttribute
        : Attribute
    {
        /// <summary>
        /// Gets or sets whether the combination of indexed values must be unique.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets the column names in index order.
        /// </summary>
        public string[] Columns { get; private set; }

        /// <summary>
        /// Gets or sets whether the index is clustered.
        /// </summary>
        public bool Clustered { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="column">The column to include in the index.</param>
        public IndexCollectionAttribute(string column)
        {
            Columns = new string[] { column };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column">The column to include in the index.</param>
        public IndexCollectionAttribute(bool unique, string column)
        {
            Unique = unique;
            Columns = new string[] { column };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column1">The first index column.</param>
        /// <param name="colum2">The second index column.</param>
        public IndexCollectionAttribute(bool unique, string column1, string colum2)
        {
            Unique = unique;
            Columns = new string[] { column1, colum2 };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column1">The first index column.</param>
        /// <param name="colum2">The second index column.</param>
        /// <param name="column3">The third index column.</param>
        public IndexCollectionAttribute(bool unique, string column1, string colum2, string column3)
        {
            Unique = unique;
            Columns = new string[] { column1, colum2, column3 };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column1">The first index column.</param>
        /// <param name="colum2">The second index column.</param>
        /// <param name="column3">The third index column.</param>
        /// <param name="name4">The fourth index column.</param>
        public IndexCollectionAttribute(bool unique, string column1, string colum2, string column3, string name4)
        {
            Unique = unique;
            Columns = new string[] { column1, colum2, column3, name4 };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column1">The first index column.</param>
        /// <param name="colum2">The second index column.</param>
        /// <param name="column3">The third index column.</param>
        /// <param name="name4">The fourth index column.</param>
        /// <param name="column5">The fifth index column.</param>
        public IndexCollectionAttribute(bool unique, string column1, string colum2, string column3, string name4, string column5)
        {
            Unique = unique;
            Columns = new string[] { column1, colum2, column3, name4, column5 };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexCollectionAttribute"/> class.
        /// </summary>
        /// <param name="unique"><see langword="true"/> to require unique index values; otherwise, <see langword="false"/>.</param>
        /// <param name="column1">The first index column.</param>
        /// <param name="colum2">The second index column.</param>
        /// <param name="column3">The third index column.</param>
        /// <param name="name4">The fourth index column.</param>
        /// <param name="column5">The fifth index column.</param>
        /// <param name="column6">The sixth index column.</param>
        public IndexCollectionAttribute(bool unique, string column1, string colum2, string column3, string name4, string column5, string column6)
        {
            Unique = unique;
            Columns = new string[] { column1, colum2, column3, name4, column5, column6 };
        }
    }
}