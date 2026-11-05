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

namespace Gurux.Service.Orm.Common.Enums
{
    /// <summary>
    /// Defines the behavior of a foreign key when a referenced row is deleted.
    /// </summary>
    public enum ForeignKeyDelete
    {
        /// <summary>
        /// Leaves the delete action unspecified. This is the default value.
        /// </summary>
        None,
        /// <summary>
        /// Deletes referencing rows when the referenced row is deleted.
        /// </summary>
        Cascade,
        /// <summary>
        /// Rejects deletion of a referenced row if referencing rows exist, using the default database action.
        /// </summary>
        Empty,
        /// <summary>
        /// Explicitly restricts deletion of a row that is still referenced.
        /// </summary>
        Restrict
    }
}
