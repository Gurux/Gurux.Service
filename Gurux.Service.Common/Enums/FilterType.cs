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

using System.Runtime.Serialization;

namespace Gurux.Service.Orm.Common.Enums
{
    /// <summary>
    /// Defines the comparison used to filter a mapped property.
    /// </summary>
    public enum FilterType : int
    {
        /// <summary>
        /// Matches values exactly.
        /// </summary>
        /// <remarks>
        /// String comparisons are case-sensitive.
        /// </remarks>
        [EnumMember(Value = "0")]
        Exact = 0,
        /// <summary>
        /// Matches values equal to the filter value.
        /// </summary>
        /// <remarks>
        /// String comparisons are case-insensitive.
        /// </remarks>
        [EnumMember(Value = "1")]
        Equals = 1,
        /// <summary>
        /// Matches values greater than the filter value.
        /// </summary>
        [EnumMember(Value = "2")]
        Greater = 2,
        /// <summary>
        /// Matches values less than the filter value.
        /// </summary>
        [EnumMember(Value = "3")]
        Less = 3,
        /// <summary>
        /// Matches values greater than or equal to the filter value.
        /// </summary>
        [EnumMember(Value = "4")]
        GreaterOrEqual = 4,
        /// <summary>
        /// Matches values less than or equal to the filter value.
        /// </summary>
        [EnumMember(Value = "5")]
        LessOrEqual = 5,
        /// <summary>
        /// String value starts with the filtered value.
        /// </summary>
        [EnumMember(Value = "6")]
        StartsWith = 6,
        /// <summary>
        /// String value ends with the filtered value.
        /// </summary>
        [EnumMember(Value = "7")]
        EndsWith = 7,
        /// <summary>
        /// String value contains the filtered value.
        /// </summary>
        [EnumMember(Value = "8")]
        Contains = 8,
        /// <summary>
        /// Matches values that differ from the filter value.
        /// </summary>
        [EnumMember(Value = "9")]
        NotEqual = 9,
        /// <summary>
        /// Matches null values when the filter is enabled.
        /// </summary>
        [EnumMember(Value = "10")]
        Null = 10,
        /// <summary>
        /// Matches non-null values when the filter is enabled.
        /// </summary>
        [EnumMember(Value = "11")]
        NotNull = 11,
    }
}
