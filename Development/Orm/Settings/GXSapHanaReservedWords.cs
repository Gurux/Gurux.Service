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
    /// Utility class to handle reserved words in SAP HANA.
    /// </summary>
    public static class GXSapHanaReservedWords
    {
        /// <summary>
        /// A set of reserved words in HANA. 
        /// This list is based on the official documentation and may not be exhaustive.
        /// </summary>
        private static readonly HashSet<string> ReservedWords =
            new(StringComparer.OrdinalIgnoreCase)
           {
        "ALL", "ALTER", "AND", "ANY", "AS", "ASC",
        "BETWEEN", "BIGINT", "BINARY", "BLOB", "BOOLEAN", "BOTH",
        "CASE", "CHAR", "CHARACTER", "CLOB", "CONDITION",
        "CONNECT", "CONSTRAINT", "CONTINUE", "CORRESPONDING",
        "CREATE", "CROSS", "CURRENT", "CURRENT_CONNECTION",
        "CURRENT_DATE", "CURRENT_SCHEMA", "CURRENT_TIME",
        "CURRENT_TIMESTAMP", "CURRENT_TRANSACTION_ISOLATION_LEVEL",
        "CURRENT_USER", "CURSOR",
        "DATE", "DAY", "DEC", "DECIMAL", "DECLARE", "DEFAULT",
        "DELETE", "DESC", "DISTINCT", "DOUBLE", "DROP",
        "ELSE", "ELSEIF", "END", "ESCAPE", "EXCEPT", "EXEC",
        "EXECUTE", "EXISTS", "EXIT",
        "FALSE", "FETCH", "FOR", "FROM", "FULL",
        "GROUP",
        "HAVING",
        "IF", "IN", "INNER", "INOUT", "INSERT", "INT",
        "INTEGER", "INTERSECT", "INTO", "IS",
        "JOIN",
        "LEADING", "LEFT", "LIKE", "LIMIT", "LOCALTEMPORARY",
        "LONGDATE", "LONGVARCHAR", "LOOP",
        "MINUS",
        "NATURAL", "NCHAR", "NCLOB", "NEW", "NO", "NOT",
        "NULL", "NVARCHAR",
        "ON", "OR", "ORDER", "OUT", "OUTER",
        "PRIMARY", "PROCEDURE",
        "REAL", "RETURN", "RETURNS", "REVOKE", "RIGHT",
        "SELECT", "SESSION_USER", "SET", "SMALLDECIMAL",
        "SMALLINT", "SQL", "SQLSCRIPT",
        "TABLE", "THEN", "TIME", "TIMESTAMP", "TINYINT",
        "TO", "TOP", "TRAILING", "TRIGGER", "TRUE",
        "UNION", "UNIQUE", "UPDATE", "USER", "USING",
        "VALUES", "VARCHAR",
        "WHEN", "WHERE", "WHILE", "WITH",
        "COLUMN", "ROW", "SEQUENCE", "VIEW", "INDEX",
        "SCHEMA", "DATABASE", "SYNONYM",
        "DO", "BEGIN", "SIGNAL", "RESIGNAL", "CONDITION",
        "DECLARE", "HANDLER"
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
    }
}
