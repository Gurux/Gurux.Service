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
        public virtual void BeforeAdd() { }

        /// <summary>
        /// Called after this entity is added to the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterAdd() { }

        /// <summary>
        /// Called before this entity is updated in the database. The default implementation does nothing.
        /// </summary>
        public virtual void BeforeUpdate() { }

        /// <summary>
        /// Called after this entity is updated in the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterUpdate() { }

        /// <summary>
        /// Called before this entity is removed from the database. The default implementation does nothing.
        /// </summary>
        public virtual void BeforeRemove() { }

        /// <summary>
        /// Called after this entity is removed from the database. The default implementation does nothing.
        /// </summary>
        public virtual void AfterRemove() { }
    }
}
