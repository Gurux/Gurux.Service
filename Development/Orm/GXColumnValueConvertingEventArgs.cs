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

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Arguments for column value conversion.
    /// </summary>
    public sealed class GXColumnValueConvertingEventArgs : EventArgs
    {
        /// <summary>
        /// Table name where column is located.
        /// </summary>
        public required string TableName
        {
            get; init;
        }
        /// <summary>
        /// Column name where value is located.
        /// </summary>
        public required string ColumnName
        {
            get; init;
        }

        /// <summary>
        /// Old column type. This is used to convert value to new type.
        /// </summary>
        public required Type OldType
        {
            get; init;
        }
        /// <summary>
        /// New column type. This is used to convert value to new type.
        /// </summary>
        public required Type NewType
        {
            get; init;
        }

        /// <summary>
        /// Current column value. This can be changed to new value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Indicates whether value is converted to new type. 
        /// If this is false, value is not converted and framework converts the value.
        /// </summary>
        public bool IsConverted { get; set; }

    }
}
