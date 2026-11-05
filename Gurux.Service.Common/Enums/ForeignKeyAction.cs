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
    /// Defines referential actions reported by a database foreign key constraint.
    /// </summary>
    public enum ForeignKeyAction
    {
        /// <summary>
        /// No action has been specified or the action is unknown.
        /// </summary>
        None,
        /// <summary>
        /// Applies NO ACTION: rejects a change if the foreign key constraint is violated when checked.
        /// </summary>
        NoAction,
        /// <summary>
        /// Rejects deleting or changing a referenced key while referencing rows exist.
        /// </summary>
        Restrict,
        /// <summary>
        /// Changes made to the referenced row are cascaded to the referencing rows.
        /// </summary>
        Cascade,
        /// <summary>
        /// Sets the foreign key column to NULL when the referenced row is deleted or updated.
        /// </summary>
        SetNull,
        /// <summary>
        /// Sets the foreign key column to its default value when the referenced row is deleted or updated.
        /// </summary>
        SetDefault
    }
}
