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
using System.Collections.Generic;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Utility class to handle reserved words in SAP HANA.
    /// </summary>
    public static class GXSapHanaReservedWords
    {
        /// <summary>
        /// A set of reserved words in SAP HANA. 
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
        "ALL",
        "ALTER",
        "AS",
        "BEFORE",
        "BEGIN",
        "BOTH",
        "CASE",
        "CHAR",
        "CONDITION",
        "CONNECT",
        "CROSS",
        "CUBE",
        "CURRENT_CONNECTION",
        "CURRENT_DATE",
        "CURRENT_SCHEMA",
        "CURRENT_TIME",
        "CURRENT_TIMESTAMP",
        "CURRENT_TRANSACTION_ISOLATION_LEVEL",
        "CURRENT_USER",
        "CURRENT_UTCDATE",
        "CURRENT_UTCTIME",
        "CURRENT_UTCTIMESTAMP",
        "DEALLOCATE",
        "DISTINCT",
        "ELSE",
        "ELSEIF",
        "END",
        "EXCEPT",
        "EXCEPTION",
        "EXEC",
        "FOR",
        "FROM",
        "FULL",
        "GROUP",
        "HAVING",
        "IF",
        "IN",
        "INNER",
        "INOUT",
        "INTERSECT",
        "INTO",
        "IS",
        "JOIN",
        "LEADING",
        "LEFT",
        "LOOP",
        "MINUS",
        "NATURAL",
        "NULL",
        "ON",
        "ORDER",
        "OUT",
        "PRIOR",
        "RETURN",
        "RETURNS",
        "REVERSE",
        "RIGHT",
        "ROLLUP",
        "ROWID",
        "SELECT",
        "SET",
        "SQL",
        "START",
        "SYSDATE",
        "SYSTIME",
        "SYSTIMESTAMP",
        "SYSUUID",
        "TRAILING",
        "UNION",
        "USING",
        "UTCDATE",
        "UTCTIME",
        "UTCTIMESTAMP",
        "VALUES",
        "WHEN",
        "WHERE",
        "WHILE",
        "WITH"
    };


        /// <summary>
        /// Checks if the given identifier is a reserved word in HANA.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public static bool IsReservedWord(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) &&
                   ReservedWords.Contains(identifier);
        }

        /// <summary>
        /// Escapes the given identifier if it is a reserved word in SAP Hana.
        /// </summary>
        /// <param name="tablePrefix">The table prefix to use.</param>
        /// <param name="value">The identifier to escape.</param>
        /// <returns>The escaped identifier if it is a reserved word; otherwise, the original identifier.</returns>
        public static string EscapeIdentifier(string? tablePrefix, string value)
        {
            if (IsReservedWord(value))
            {
                return $"\"{tablePrefix}{value}\"";
            }
            return $"{tablePrefix}{value.ToUpperInvariant()}";
        }
    }
}
