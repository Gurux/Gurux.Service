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
    /// Utility class to handle reserved words in MariaDB.
    /// </summary>
    public static class GXMariaDBReservedWords
    {
        /// <summary>
        /// A set of reserved words in MariaDB. 
        /// This list is based on the official documentation and may not be exhaustive.
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "ACCESSIBLE",
            "ADD",
            "ALL",
            "ALTER",
            "ANALYZE",
            "AND",
            "AS",
            "ASC",
            "ASENSITIVE",
            "BEFORE",
            "BETWEEN",
            "BIGINT",
            "BINARY",
            "BLOB",
            "BOTH",
            "BY",
            "CALL",
            "CASCADE",
            "CASE",
            "CHANGE",
            "CHAR",
            "CHARACTER",
            "CHECK",
            "COLLATE",
            "COLUMN",
            "CONDITION",
            "CONSTRAINT",
            "CONTINUE",
            "CONVERT",
            "CREATE",
            "CROSS",
            "CURRENT_DATE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_USER",
            "CURSOR",
            "DATABASE",
            "DATABASES",
            "DAY_HOUR",
            "DAY_MICROSECOND",
            "DAY_MINUTE",
            "DAY_SECOND",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DELAYED",
            "DELETE",
            "DESC",
            "DESCRIBE",
            "DETERMINISTIC",
            "DISTINCT",
            "DISTINCTROW",
            "DIV",
            "DOUBLE",
            "DROP",
            "DUAL",
            "EACH",
            "ELSE",
            "ELSEIF",
            "ENCLOSED",
            "ESCAPED",
            "EXISTS",
            "EXIT",
            "EXPLAIN",
            "FALSE",
            "FETCH",
            "FLOAT",
            "FLOAT4",
            "FLOAT8",
            "FOR",
            "FORCE",
            "FOREIGN",
            "FROM",
            "FULLTEXT",
            "GENERATED",
            "GET",
            "GRANT",
            "GROUP",
            "HAVING",
            "HIGH_PRIORITY",
            "IF",
            "IGNORE",
            "IN",
            "INDEX",
            "INFILE",
            "INNER",
            "INOUT",
            "INSENSITIVE",
            "INSERT",
            "INT",
            "INT1",
            "INT2",
            "INT3",
            "INT4",
            "INT8",
            "INTEGER",
            "INTERVAL",
            "INTO",
            "IS",
            "ITERATE",
            "JOIN",
            "KEY",
            "KEYS",
            "KILL",
            "LEADING",
            "LEAVE",
            "LEFT",
            "LIKE",
            "LIMIT",
            "LINEAR",
            "LINES",
            "LOAD",
            "LOCALTIME",
            "LOCALTIMESTAMP",
            "LOCK",
            "LONG",
            "LONGBLOB",
            "LONGTEXT",
            "LOOP",
            "LOW_PRIORITY",
            "MASTER_SSL_VERIFY_SERVER_CERT",
            "MATCH",
            "MAXVALUE",
            "MEDIUMBLOB",
            "MEDIUMINT",
            "MEDIUMTEXT",
            "MIDDLEINT",
            "MINUTE_MICROSECOND",
            "MINUTE_SECOND",
            "MOD",
            "MODIFIES",
            "NATURAL",
            "NOT",
            "NO_WRITE_TO_BINLOG",
            "NULL",
            "NUMERIC",
            "ON",
            "OPTIMIZE",
            "OPTION",
            "OPTIONALLY",
            "OR",
            "ORDER",
            "OUT",
            "OUTER",
            "OUTFILE",
            "PARTITION",
            "PRECISION",
            "PRIMARY",
            "PROCEDURE",
            "PURGE",
            "RANGE",
            "READ",
            "READS",
            "READ_WRITE",
            "REAL",
            "REFERENCES",
            "REGEXP",
            "RELEASE",
            "RENAME",
            "REPEAT",
            "REPLACE",
            "REQUIRE",
            "RESIGNAL",
            "RESTRICT",
            "RETURN",
            "REVOKE",
            "RIGHT",
            "RLIKE",
            "SCHEMA",
            "SCHEMAS",
            "SECOND_MICROSECOND",
            "SELECT",
            "SENSITIVE",
            "SEPARATOR",
            "SET",
            "SHOW",
            "SIGNAL",
            "SMALLINT",
            "SPATIAL",
            "SPECIFIC",
            "SQL",
            "SQLEXCEPTION",
            "SQLSTATE",
            "SQLWARNING",
            "SQL_BIG_RESULT",
            "SQL_CALC_FOUND_ROWS",
            "SQL_SMALL_RESULT",
            "SSL",
            "STARTING",
            "STRAIGHT_JOIN",
            "TABLE",
            "TERMINATED",
            "THEN",
            "TINYBLOB",
            "TINYINT",
            "TINYTEXT",
            "TO",
            "TRAILING",
            "TRIGGER",
            "TRUE",
            "UNDO",
            "UNION",
            "UNIQUE",
            "UNLOCK",
            "UNSIGNED",
            "UPDATE",
            "USAGE",
            "USE",
            "USING",
            "UTC_DATE",
            "UTC_TIME",
            "UTC_TIMESTAMP",
            "VALUES",
            "VARBINARY",
            "VARCHAR",
            "VARCHARACTER",
            "VARYING",
            "WHEN",
            "WHERE",
            "WHILE",
            "WITH",
            "WRITE",
            "XOR",
            "YEAR_MONTH",
            "ZEROFILL",
            "SEQUENCE",
            "RETURNING",
            "SYSTEM",
            "OVER",
            "WINDOW",
            "RECURSIVE"
        };

        /// <summary>
        /// Checks if the given identifier is a reserved word in MySQL.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public static bool IsReservedWord(string identifier)
        {
            return !string.IsNullOrWhiteSpace(identifier) &&
                   ReservedWords.Contains(identifier);
        }

        /// <summary>
        /// Gets the collection of reserved words in MySQL.
        /// </summary>
        /// <returns>A read-only collection of reserved words.</returns>
        public static IReadOnlyCollection<string> GetReservedWords()
        {
            return ReservedWords;
        }

        /// <summary>
        /// Escapes the given identifier if it is a reserved word in MariaDB.
        /// </summary>
        /// <param name="tablePrefix">The table prefix to use.</param>
        /// <param name="value">The identifier to escape.</param>
        /// <returns>The escaped identifier if it is a reserved word; otherwise, the original identifier.</returns>
        public static string EscapeIdentifier(string? tablePrefix, string value)
        {
            if (IsReservedWord(value))
            {
                return $"`{tablePrefix}{value}`";
            }
            return $"{tablePrefix}{value}";
        }
    }
}
