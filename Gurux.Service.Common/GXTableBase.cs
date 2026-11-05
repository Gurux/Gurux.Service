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

namespace Gurux.Service.Orm.Common
{
    /// <summary>
    /// Provides overridable callbacks for database insert, update, and delete operations.
    /// </summary>
    public abstract class GXTableBase
    {
        /// <summary>
        /// Called before this entity is added to the database. The default implementation does nothing.
        /// </summary>
        public virtual void BeforeAdd()
        {
        }

        /// <summary>
        /// Called after this entity is added to the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterAdd()
        {
        }

        /// <summary>
        /// Called before this entity is updated in the database. The default implementation does nothing.
        /// </summary>
        public virtual void BeforeUpdate()
        {
        }

        /// <summary>
        /// Called after this entity is updated in the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterUpdate()
        {
        }

        /// <summary>
        /// Called before this entity is removed from the database. The default implementation does nothing.
        /// </summary>
        public virtual void BeforeRemove()
        {
        }

        /// <summary>
        /// Called after this entity is removed from the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterRemove()
        {
        }
    }
}
