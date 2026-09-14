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
//--------------------------------------------------------------------------

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
