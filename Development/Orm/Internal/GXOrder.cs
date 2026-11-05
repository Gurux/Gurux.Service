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

namespace Gurux.Service.Orm.Internal
{
    internal class GXOrder
    {
        private Type? table;
        private string? column;

        public Type Table
        {
            get => table ?? throw new InvalidOperationException("Order table has not been initialized.");
            set => table = value;
        }

        public string Column
        {
            get => column ?? throw new InvalidOperationException("Order column has not been initialized.");
            set => column = value;
        }
        public bool? Descending;
    }
}
