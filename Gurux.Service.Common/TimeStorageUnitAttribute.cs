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

using Gurux.Service.Orm.Common.Enums;
using System;

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Specifies the unit used when storing a time value in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TimeStorageUnitAttribute : Attribute
    {
        /// <summary>
        /// Gets the unit used to store the time value in the database.
        /// </summary>
        public TimeStorageUnit Unit
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeStorageUnitAttribute"/> class.
        /// </summary>
        /// <param name="unit">The unit used to store the time value.</param>
        public TimeStorageUnitAttribute(TimeStorageUnit unit)
        {
            Unit = unit;
        }
    }
}
