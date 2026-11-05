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
    /// Utility class to handle reserved words in MS SQL Server.
    /// </summary>
    public static class GXMSSqlReservedWords
    {
        /// <summary>
        /// A set of reserved words in MS SQL Server. 
        /// This list is based on the official documentation and may not be exhaustive.
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "ADD",
            "ALL",
            "ALTER",
            "AND",
            "ANY",
            "AS",
            "ASC",
            "AUTHORIZATION",
            "BACKUP",
            "BEGIN",
            "BETWEEN",
            "BREAK",
            "BROWSE",
            "BULK",
            "BY",
            "CASCADE",
            "CASE",
            "CHECK",
            "CHECKPOINT",
            "CLOSE",
            "CLUSTERED",
            "COALESCE",
            "COLLATE",
            "COLUMN",
            "COMMIT",
            "COMPUTE",
            "CONSTRAINT",
            "CONTAINS",
            "CONTAINSTABLE",
            "CONTINUE",
            "CONVERT",
            "CREATE",
            "CROSS",
            "CURRENT",
            "CURRENT_DATE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_USER",
            "CURSOR",
            "DATABASE",
            "DBCC",
            "DEALLOCATE",
            "DECLARE",
            "DEFAULT",
            "DELETE",
            "DENY",
            "DESC",
            "DISK",
            "DISTINCT",
            "DISTRIBUTED",
            "DOUBLE",
            "DROP",
            "DUMP",
            "ELSE",
            "END",
            "ERRLVL",
            "ESCAPE",
            "EXCEPT",
            "EXEC",
            "EXECUTE",
            "EXISTS",
            "EXIT",
            "EXTERNAL",
            "FETCH",
            "FILE",
            "FILLFACTOR",
            "FOR",
            "FOREIGN",
            "FREETEXT",
            "FREETEXTTABLE",
            "FROM",
            "FULL",
            "FUNCTION",
            "GOTO",
            "GRANT",
            "GROUP",
            "HAVING",
            "HOLDLOCK",
            "IDENTITY",
            "IDENTITYCOL",
            "IDENTITY_INSERT",
            "IF",
            "IN",
            "INDEX",
            "INNER",
            "INSERT",
            "INTERSECT",
            "INTO",
            "IS",
            "JOIN",
            "KEY",
            "KILL",
            "LEFT",
            "LIKE",
            "LINENO",
            "LOAD",
            "MERGE",
            "NATIONAL",
            "NOCHECK",
            "NONCLUSTERED",
            "NOT",
            "NULL",
            "NULLIF",
            "OF",
            "OFF",
            "OFFSETS",
            "ON",
            "OPEN",
            "OPENDATASOURCE",
            "OPENQUERY",
            "OPENROWSET",
            "OPENXML",
            "OPTION",
            "OR",
            "ORDER",
            "OUTER",
            "OVER",
            "PERCENT",
            "PIVOT",
            "PLAN",
            "PRIMARY",
            "PRINT",
            "PROC",
            "PROCEDURE",
            "PUBLIC",
            "RAISERROR",
            "READ",
            "READTEXT",
            "RECONFIGURE",
            "REFERENCES",
            "REPLICATION",
            "RESTORE",
            "RESTRICT",
            "RETURN",
            "REVERT",
            "REVOKE",
            "RIGHT",
            "ROLLBACK",
            "ROWCOUNT",
            "ROWGUIDCOL",
            "RULE",
            "SAVE",
            "SCHEMA",
            "SECURITYAUDIT",
            "SELECT",
            "SESSION_USER",
            "SET",
            "SETUSER",
            "SHUTDOWN",
            "SOME",
            "STATISTICS",
            "SYSTEM_USER",
            "TABLE",
            "TABLESAMPLE",
            "TEXTSIZE",
            "THEN",
            "TO",
            "TOP",
            "TRAN",
            "TRANSACTION",
            "TRIGGER",
            "TRUNCATE",
            "TRY_CONVERT",
            "TSEQUAL",
            "UNION",
            "UNIQUE",
            "UNPIVOT",
            "UPDATE",
            "UPDATETEXT",
            "USE",
            "USER",
            "VALUES",
            "VARYING",
            "VIEW",
            "WAITFOR",
            "WHEN",
            "WHERE",
            "WHILE",
            "WITH",
            "WRITETEXT"
            };

        /// <summary>
        /// Checks if the given identifier is a reserved word in MS SQL Server.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public static bool IsReservedWord(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) &&
                   ReservedWords.Contains(identifier);
        }

        /// <summary>
        /// Escapes the given identifier if it is a reserved word in MSSQL.
        /// </summary>
        /// <param name="tablePrefix">The table prefix to use.</param>
        /// <param name="value">The identifier to escape.</param>
        /// <returns>The escaped identifier if it is a reserved word; otherwise, the original identifier.</returns>
        public static string EscapeIdentifier(string? tablePrefix, string value)
        {
            if (IsReservedWord(value))
            {
                return $"[{tablePrefix}{value}]";
            }
            return $"{tablePrefix}{value}";
        }
    }
}
