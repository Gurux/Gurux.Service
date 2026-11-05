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

namespace Gurux.Service.Orm.Model
{
    /// <summary>
    /// Import settings.
    /// </summary>
    public class GXGeneratorSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXGeneratorSettings()
        {
            Serializable = true;
        }

        /// <summary>
        /// Directory where tables are generated.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Imported tables.
        /// </summary>
        /// <remarks>
        /// If tables is null all tables are imported.</remarks>
        public string[]? Tables { get; set; }

        /// <summary>
        /// Namespace
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Amount of spaces. Tab is used if zero. 
        /// </summary>
        public int Spaces { get; set; }

        /// <summary>
        /// Table prefix. 
        /// </summary>
        public string? TablePrefix { get; set; }

        /// <summary>
        /// Column prefix. 
        /// </summary>
        public string? ColumnPrefix { get; set; }

        /// <summary>
        /// Is Serializable attribute added.
        /// </summary>
        public bool Serializable { get; set; }


    }
}
