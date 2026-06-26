//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Utility class to handle reserved words in DB2.
    /// </summary>
    public static class GXDB2ReservedWords
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
    }
}
