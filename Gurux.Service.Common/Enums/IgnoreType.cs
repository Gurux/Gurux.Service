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

namespace Gurux.Service.Orm.Common.Enums
{
    /// <summary>
    /// Specifies operations that exclude a mapped member. Values can be combined.
    /// </summary>
    [Flags]
    public enum IgnoreType : int
    {
        /// <summary>
        /// Does not exclude the member from any operation.
        /// </summary>
        None = 0,
        /// <summary>
        /// Excludes the member from database mapping.
        /// </summary>
        Db = 1,
        /// <summary>
        /// Excludes the member from JSON serialization by formatters that honor this flag.
        /// </summary>
        Json = 2,
        /// <summary>
        /// Excludes the member from all supported operations.
        /// </summary>
        All = -1
    }
}
