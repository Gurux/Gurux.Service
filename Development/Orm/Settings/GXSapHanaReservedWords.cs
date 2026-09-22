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
