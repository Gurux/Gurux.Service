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
namespace Gurux.Service.Orm.Model
{
    /// <summary>Contains the name and source code of a generated class.</summary>
    public sealed class GXClassGeneratedEventArgs : EventArgs
    {
        /// <summary>Initializes the generated class information.</summary>
        /// <param name="className">Name of the generated class.</param>
        /// <param name="source">Generated source code.</param>
        public GXClassGeneratedEventArgs(string className, string source)
        {
            ClassName = className;
            Source = source;
        }

        /// <summary>
        /// Class name of the generated class.
        /// </summary>
        public string ClassName
        {
            get;
        }
        /// <summary>
        /// Generated source code of the class.
        /// </summary>
        public string Source
        {
            get;
        }
    }
}
