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
    /// Oracle reserved words.
    /// </summary>
    public static class GXOracleReservedWords
    {
        /// <summary>
        /// A set of reserved words in Oracle. This list is not exhaustive and may need to be updated based on the specific version of Oracle being used.
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "ACCESS",
            "ADD",
            "ALL",
            "ALTER",
            "AND",
            "ANY",
            "AS",
            "ASC",
            "AUDIT",
            "BETWEEN",
            "BY",
            "CHAR",
            "CHECK",
            "CLUSTER",
            "COLUMN",
            "COMMENT",
            "COMPRESS",
            "CONNECT",
            "CREATE",
            "CURRENT",
            "DATE",
            "DECIMAL",
            "DEFAULT",
            "DELETE",
            "DESC",
            "DISTINCT",
            "DROP",
            "ELSE",
            "EXCLUSIVE",
            "EXISTS",
            "FILE",
            "FLOAT",
            "FOR",
            "FROM",
            "GRANT",
            "GROUP",
            "HAVING",
            "IDENTIFIED",
            "IMMEDIATE",
            "IN",
            "INCREMENT",
            "INDEX",
            "INITIAL",
            "INSERT",
            "INTEGER",
            "INTERSECT",
            "INTO",
            "IS",
            "LEVEL",
            "LIKE",
            "LOCK",
            "LONG",
            "MAXEXTENTS",
            "MINUS",
            "MLSLABEL",
            "MODE",
            "MODIFY",
            "NOAUDIT",
            "NOCOMPRESS",
            "NOT",
            "NOWAIT",
            "NULL",
            "NUMBER",
            "OF",
            "OFFLINE",
            "ON",
            "ONLINE",
            "OPTION",
            "OR",
            "ORDER",
            "PCTFREE",
            "PRIOR",
            "PRIVILEGES",
            "PUBLIC",
            "RAW",
            "RENAME",
            "RESOURCE",
            "REVOKE",
            "ROW",
            "ROWID",
            "ROWNUM",
            "ROWS",
            "SELECT",
            "SESSION",
            "SET",
            "SHARE",
            "SIZE",
            "SMALLINT",
            "START",
            "SUCCESSFUL",
            "SYNONYM",
            "SYSDATE",
            "TABLE",
            "THEN",
            "TO",
            "TRIGGER",
            "UID",
            "UNION",
            "UNIQUE",
            "UPDATE",
            "USER",
            "VALIDATE",
            "VALUES",
            "VARCHAR",
            "VARCHAR2",
            "VIEW",
            "WHENEVER",
            "WHERE",
            "WITH"
            };

        /// <summary>
        /// Checks if the given identifier is a reserved word in Oracle.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public static bool IsReservedWord(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) &&
                   ReservedWords.Contains(identifier);
        }

        /// <summary>
        /// Returns a read-only collection of reserved words in Oracle.
        /// </summary>
        /// <returns>A read-only collection of reserved words in Oracle.</returns>
        public static IReadOnlyCollection<string> GetReservedWords()
        {
            return ReservedWords;
        }

        /// <summary>
        /// Escapes the given identifier if it is a reserved word in Oracle.
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
