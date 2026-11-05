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
    /// Utility class to handle reserved words in DB2.
    /// </summary>
    static class GXDB2ReservedWords
    {
        /// <summary>
        /// A set of reserved words in DB2. 
        /// This list is based on the official documentation and may not be exhaustive.
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
           {
        "ADD", "AFTER", "ALL", "ALLOCATE", "ALLOW", "ALTER", "AND",
        "ANY", "AS", "ASC", "ASSOCIATE", "ASUTIME", "AUDIT",
        "AUTHORIZATION", "AUX", "AUXILIARY",
        "BEFORE", "BEGIN", "BETWEEN", "BUFFERPOOL", "BY",
        "CALL", "CAPTURE", "CASCADED", "CASE", "CAST", "CCSID",
        "CHAR", "CHARACTER", "CHECK", "CLONE", "CLOSE", "CLUSTER",
        "COLLECTION", "COLLID", "COLUMN", "COMMENT", "COMMIT",
        "CONCAT", "CONDITION", "CONNECT", "CONNECTION", "CONSTRAINT",
        "CONTAINS", "CONTINUE", "CREATE", "CURRENT", "CURRENT_DATE",
        "CURRENT_LC_CTYPE", "CURRENT_PATH", "CURRENT_SCHEMA",
        "CURRENT_SERVER", "CURRENT_TIME", "CURRENT_TIMESTAMP",
        "CURRENT_TIMEZONE", "CURRENT_USER", "CURSOR",
        "DATABASE", "DAY", "DAYS", "DBINFO", "DECLARE", "DEFAULT",
        "DELETE", "DESCRIPTOR", "DETERMINISTIC", "DISALLOW",
        "DISTINCT", "DO", "DOUBLE", "DROP", "DYNAMIC",
        "EDITPROC", "ELSE", "ELSEIF", "ENCODING", "END", "ENDING",
        "ERASE", "ESCAPE", "EXCEPT", "EXCEPTION", "EXECUTE", "EXISTS",
        "EXIT", "EXTERNAL",
        "FETCH", "FIELDPROC", "FINAL", "FOR", "FOREIGN", "FREE",
        "FROM", "FULL", "FUNCTION",
        "GENERAL", "GENERATED", "GET", "GLOBAL", "GO", "GOTO",
        "GRANT", "GROUP",
        "HANDLER", "HAVING", "HOLD", "HOUR", "HOURS",
        "IF", "IMMEDIATE", "IN", "INCLUDING", "INDEX", "INHERIT",
        "INNER", "INOUT", "INSENSITIVE", "INSERT", "INTERSECT",
        "INTO", "IS", "ISOBID", "ITERATE",
        "JAR", "JOIN",
        "KEEP", "KEY",
        "LABEL", "LANGUAGE", "LEAVE", "LEFT", "LIKE", "LOCAL",
        "LOCALE", "LOCATOR", "LOCATORS", "LOCK", "LOCKMAX",
        "LOCKSIZE", "LONG", "LOOP",
        "MAINTAINED", "MATERIALIZED", "MICROSECOND", "MICROSECONDS",
        "MINUTE", "MINUTES", "MODIFIES", "MONTH", "MONTHS",
        "NEW", "NEW_TABLE", "NO", "NONE", "NOT", "NULL", "NULLS",
        "OBID", "OF", "OLD", "OLD_TABLE", "ON", "OPEN", "OPTIMIZATION",
        "OPTIMIZE", "OR", "ORDER", "OUT", "OUTER",
        "PACKAGE", "PARAMETER", "PART", "PARTITION", "PATH", "PIECESIZE",
        "PLAN", "PRECISION", "PREPARE", "PRIMARY", "PRIQTY",
        "PRIVILEGES", "PROCEDURE", "PROGRAM",
        "QUERY", "QUERYNO",
        "READS", "REFERENCES", "REFRESH", "RELEASE", "RENAME",
        "RESIGNAL", "REPEAT", "RESTRICT", "RESULT", "RESULT_SET_LOCATOR",
        "RETURN", "RETURNS", "REVOKE", "RIGHT", "ROLE", "ROLLBACK",
        "ROLLUP", "ROW", "ROWSET", "RUN",
        "SAVEPOINT", "SCHEMA", "SCRATCHPAD", "SECOND", "SECONDS",
        "SECQTY", "SECURITY", "SEQUENCE", "SELECT", "SENSITIVE",
        "SESSION_USER", "SET", "SIGNAL", "SIMPLE", "SOME", "SOURCE",
        "SPECIFIC", "STANDARD", "STATIC", "STATEMENT", "STAY",
        "STOGROUP", "STORES", "STYLE", "SUMMARY", "SYNONYM",
        "SYSDATE", "SYSTEM", "SYSTIMESTAMP",
        "TABLE", "TABLESPACE", "THEN", "TO", "TRIGGER", "TRUNCATE",
        "TYPE",
        "UNDO", "UNION", "UNIQUE", "UNTIL", "UPDATE", "USER", "USING",
        "VALIDPROC", "VALUE", "VALUES", "VARIABLE", "VARIANT",
        "VCAT", "VERSIONING", "VIEW", "VOLATILE", "VOLUMES",
        "WHEN", "WHENEVER", "WHERE", "WHILE", "WITH", "WLM",
        "XMLCAST", "XMLEXISTS", "XMLNAMESPACES",
        "YEAR", "YEARS", "ZONE"
    };

        /// <summary>
        /// Checks if the given identifier is a reserved word in DB2.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public static bool IsReservedWord(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) &&
                   ReservedWords.Contains(identifier);
        }

        /// <summary>
        /// Escapes the given identifier if it is a reserved word in DB2.
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
