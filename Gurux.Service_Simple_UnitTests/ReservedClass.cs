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

using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Gurux.Service_Simple_Unit_Test
{
    [DataContract]
    class ReservedClass : IUnique<int>
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        [DataMember(Name = "ID"), Index]
        public int Id { get; set; }

        /// <summary>
        /// MySql , PostgreSQL, DB2, Oracle, MariaDB, SqLite and SapHana reserved word.
        /// </summary>
        [DataMember(Name = "ALL")]
        public string All { get; set; } = default!;
    }
}
