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

using Gurux.Service.Orm.Enums;
using System;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Represents errors that occur during database operations, providing detailed information about the error type,
    /// SQL query, error code, SQL state, and whether the error is transient.
    /// </summary>
    /// <remarks>Use GXDatabaseException to capture and handle database-specific errors across different
    /// database providers. The exception includes properties to identify the nature of the error, the executed SQL
    /// statement, and additional diagnostic information to aid in troubleshooting and error handling
    /// strategies.</remarks>
    public class GXDatabaseException : Exception
    {
        /// <summary>
        /// Executed SQL query that caused the exception. 
        /// This property may be null if the SQL query is not available or applicable to the exception.
        /// </summary>
        public string Sql { get; init; }

        /// <summary>
        /// Type of the database error.
        /// </summary>
        public DatabaseErrorType ErrorType { get; }

        /// <summary>
        /// Indicates whether the error is transient and may be retried.
        /// </summary>  
        public bool IsTransient { get; }

        /// <summary>
        /// Gets the SQL state associated with the operation.
        /// </summary>
        public string SqlState { get; }

        /// <summary>
        /// Gets the database-specific error code associated with the exception. 
        /// This property may be null if the error code is not available or applicable to the exception.
        /// </summary>
        public int? ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the GXDatabaseException class with a specified error type, 
        /// message, and additional details about the database error.
        /// </summary>
        /// <param name="errorType">The type of the database error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="isTransient">Indicates whether the error is transient and may be retried.</param>
        /// <param name="errorCode">The database-specific error code associated with the exception.</param>
        /// <param name="sqlState">The SQL state associated with the operation.</param>
        /// <param name="sql">The executed SQL query that caused the exception.</param>
        /// <param name="innerException">The inner exception that caused the current exception.</param>
        public GXDatabaseException(
            DatabaseErrorType errorType,
            string message,
            bool isTransient = false,
            int? errorCode = null,
            string sqlState = null,
            string sql = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            ErrorType = errorType;
            IsTransient = isTransient;
            ErrorCode = errorCode;
            SqlState = sqlState;
            Sql = sql;
        }

        /// <summary>
        /// Returns a string that represents the current exception, including the error type, message, 
        /// error code, SQL state, and whether the error is transient. 
        /// This method provides a concise summary of the exception details for logging and debugging purposes.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            return $"{ErrorType}: {Message} " +
                   $"(Code={ErrorCode}, SqlState={SqlState}, Transient={IsTransient})";
        }

        /// <summary>
        /// Creates a GXDatabaseException from a given exception and SQL query by analyzing 
        /// the exception's type and properties to determine the appropriate error type, error code, 
        /// SQL state, and whether the error is transient. This method supports common database providers 
        /// such as Oracle, SQL Server, PostgreSQL, MySQL, DB2, SQLite, and SAP HANA 
        /// by inspecting known exception types and their properties to map them to the corresponding DatabaseErrorType. 
        /// If the exception does not match any known patterns, 
        /// it defaults to an Unknown error type while preserving the original message and inner exception details 
        /// for further analysis.
        /// </summary>
        /// <param name="ex">The exception to analyze.</param>
        /// <param name="sql">The SQL query that caused the exception.</param>
        /// <returns>A GXDatabaseException representing the analyzed exception.</returns>
        public static GXDatabaseException Create(Exception ex, string sql)
        {
            for (Exception current = ex; current != null; current = current.InnerException)
            {
                string typeName = current.GetType().FullName ?? "";

                int? number = GetIntProperty(current, "Number") ??
                    GetIntProperty(current, "ErrorCode") ??
                    GetIntProperty(current, "Code");
                string sqlState = GetStringProperty(current, "SqlState") ??
                    GetStringProperty(current, "SQLState");
                string message = current.Message;

                // Oracle
                if (typeName == "Oracle.ManagedDataAccess.Client.OracleException")
                {
                    return number switch
                    {
                        60 => new GXDatabaseException(
                            DatabaseErrorType.Deadlock,
                            message,
                            true,
                            number,
                            null,
                            sql,
                            ex),

                        1 => new GXDatabaseException(
                            DatabaseErrorType.UniqueConstraint,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        2291 or 2292 => new GXDatabaseException(
                            DatabaseErrorType.ForeignKeyConstraint,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        942 => new GXDatabaseException(
                            DatabaseErrorType.MissingTable,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        1017 => new GXDatabaseException(
                            DatabaseErrorType.AuthenticationFailed,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        _ => new GXDatabaseException(
                            DatabaseErrorType.Unknown,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex)
                    };
                }
                // SQL Server
                if (typeName == "Microsoft.Data.SqlClient.SqlException" ||
                    typeName == "System.Data.SqlClient.SqlException")
                {
                    return number switch
                    {
                        1205 => new GXDatabaseException(
                            DatabaseErrorType.Deadlock,
                            message,
                            true,
                            number,
                            null,
                            sql,
                            ex),

                        2627 or 2601 => new GXDatabaseException(
                            DatabaseErrorType.UniqueConstraint,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        547 => new GXDatabaseException(
                            DatabaseErrorType.ForeignKeyConstraint,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        208 => new GXDatabaseException(
                            DatabaseErrorType.MissingTable,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        -2 => new GXDatabaseException(
                            DatabaseErrorType.Timeout,
                            message,
                            true,
                            number,
                            null,
                            sql,
                            ex),

                        18456 => new GXDatabaseException(
                            DatabaseErrorType.AuthenticationFailed,
                            message,
                            false,
                            number,
                            null,
                            sql,
                            ex),

                        _ => new GXDatabaseException(
                            DatabaseErrorType.Unknown,
                            message, false, number, null, sql, ex)
                    };
                }
                // PostgreSQL
                if (typeName == "Npgsql.PostgresException")
                {
                    return sqlState switch
                    {
                        "40P01" => new GXDatabaseException(
                            DatabaseErrorType.Deadlock,
                            message,
                            true,
                            number,
                            sqlState,
                            sql,
                            ex),

                        "23505" => new GXDatabaseException(
                            DatabaseErrorType.UniqueConstraint,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        "23503" => new GXDatabaseException(
                            DatabaseErrorType.ForeignKeyConstraint,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        "42P01" => new GXDatabaseException(
                            DatabaseErrorType.MissingTable,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        "28P01" => new GXDatabaseException(
                            DatabaseErrorType.AuthenticationFailed,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        _ => new GXDatabaseException(
                            DatabaseErrorType.Unknown,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex)
                    };
                }
                // MySQL
                if (typeName == "MySql.Data.MySqlClient.MySqlException" ||
                    typeName == "MySqlConnector.MySqlException")
                {
                    return number switch
                    {
                        1213 => new GXDatabaseException(
                            DatabaseErrorType.Deadlock,
                            message,
                            true,
                            number,
                            sqlState,
                            sql,
                            ex),

                        1062 => new GXDatabaseException(
                            DatabaseErrorType.UniqueConstraint,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        1451 or 1452 => new GXDatabaseException(
                            DatabaseErrorType.ForeignKeyConstraint,
                            message,
                            false,
                            number,
                            sqlState,
                            sql,
                            ex),

                        1146 => new GXDatabaseException(
                            DatabaseErrorType.MissingTable,
                            message,
                            false,
                            number,
                            sqlState,
                            sql, ex),

                        1045 => new GXDatabaseException(
                            DatabaseErrorType.AuthenticationFailed,
                            message,
                            false,
                            number,
                            sqlState,
                            sql, ex),

                        _ => new GXDatabaseException(
                            DatabaseErrorType.Unknown,
                            message,
                            false,
                            number, sqlState, sql, ex)
                    };
                }
                // DB2
                if (typeName == "IBM.Data.DB2.Core.DB2Exception" ||
                    typeName == "IBM.Data.DB2.DB2Exception")
                {
                    DatabaseErrorType errorType = GetDb2ErrorType(sqlState, number, out bool isTransient);
                    return new GXDatabaseException(
                        errorType,
                        message,
                        isTransient,
                        number,
                        sqlState,
                        sql,
                        ex);
                }
                // SQLite
                if (typeName == "Microsoft.Data.Sqlite.SqliteException" ||
                    typeName == "System.Data.SQLite.SQLiteException")
                {
                    int? sqliteErrorCode = GetIntProperty(current, "SqliteErrorCode") ?? number;
                    int? sqliteExtendedErrorCode = GetIntProperty(current, "SqliteExtendedErrorCode");
                    DatabaseErrorType errorType = GetSqLiteErrorType(
                        message,
                        sqliteErrorCode,
                        sqliteExtendedErrorCode,
                        out bool isTransient);
                    return new GXDatabaseException(
                        errorType,
                        message,
                        isTransient,
                        sqliteExtendedErrorCode ?? sqliteErrorCode,
                        sqlState,
                        sql,
                        ex);
                }
                // SAP HANA
                if (typeName == "Sap.Data.Hana.HanaException")
                {
                    DatabaseErrorType errorType = GetSapHanaErrorType(sqlState, number, out bool isTransient);
                    return new GXDatabaseException(
                        errorType,
                        message,
                        isTransient,
                        number,
                        sqlState,
                        sql,
                        ex);
                }
            }

            return new GXDatabaseException(
                DatabaseErrorType.Unknown,
                ex.Message,
                false,
                null,
                null,
                sql, ex);
        }


        private static DatabaseErrorType GetDb2ErrorType(string sqlState, int? number, out bool isTransient)
        {
            isTransient = sqlState == "40001" || number == -911 || number == -913;
            return sqlState switch
            {
                "40001" => DatabaseErrorType.Deadlock,
                "23505" => DatabaseErrorType.UniqueConstraint,
                "23503" or "23504" => DatabaseErrorType.ForeignKeyConstraint,
                "23502" => DatabaseErrorType.NotNullConstraint,
                "23513" => DatabaseErrorType.CheckConstraint,
                "42704" or "42832" => DatabaseErrorType.MissingTable,
                "42703" => DatabaseErrorType.MissingColumn,
                "42601" => DatabaseErrorType.SyntaxError,
                "28000" => DatabaseErrorType.AuthenticationFailed,
                "42501" => DatabaseErrorType.PermissionDenied,
                _ => number switch
                {
                    -911 or -913 => DatabaseErrorType.Deadlock,
                    -803 => DatabaseErrorType.UniqueConstraint,
                    -530 or -531 or -532 => DatabaseErrorType.ForeignKeyConstraint,
                    -204 => DatabaseErrorType.MissingTable,
                    -206 => DatabaseErrorType.MissingColumn,
                    -104 => DatabaseErrorType.SyntaxError,
                    -30082 => DatabaseErrorType.AuthenticationFailed,
                    -551 => DatabaseErrorType.PermissionDenied,
                    _ => DatabaseErrorType.Unknown
                }
            };
        }

        private static DatabaseErrorType GetSqLiteErrorType(string message, int? errorCode, int? extendedErrorCode, out bool isTransient)
        {
            isTransient = errorCode == 5 || errorCode == 6 || errorCode == 14;
            if (errorCode == 5)
            {
                return DatabaseErrorType.Timeout;
            }
            if (errorCode == 6)
            {
                return DatabaseErrorType.LockTimeout;
            }
            if (errorCode == 14)
            {
                return DatabaseErrorType.ConnectionFailed;
            }
            if (errorCode == 19)
            {
                return (extendedErrorCode ?? errorCode) switch
                {
                    787 => DatabaseErrorType.ForeignKeyConstraint,
                    1299 => DatabaseErrorType.NotNullConstraint,
                    275 => DatabaseErrorType.CheckConstraint,
                    1555 or 2067 or 19 => DatabaseErrorType.UniqueConstraint,
                    _ => DatabaseErrorType.InvalidOperation
                };
            }
            if (errorCode == 1)
            {
                string lowerMessage = message.ToLowerInvariant();
                if (lowerMessage.Contains("no such table"))
                {
                    return DatabaseErrorType.MissingTable;
                }
                if (lowerMessage.Contains("no such column"))
                {
                    return DatabaseErrorType.MissingColumn;
                }
                return DatabaseErrorType.SyntaxError;
            }
            if (errorCode == 3 || errorCode == 23)
            {
                return DatabaseErrorType.PermissionDenied;
            }
            return DatabaseErrorType.Unknown;
        }

        private static DatabaseErrorType GetSapHanaErrorType(string sqlState, int? number, out bool isTransient)
        {
            isTransient = sqlState == "40001" || number == 133;
            return sqlState switch
            {
                "40001" => DatabaseErrorType.Deadlock,
                "23505" => DatabaseErrorType.UniqueConstraint,
                "23503" => DatabaseErrorType.ForeignKeyConstraint,
                "23502" => DatabaseErrorType.NotNullConstraint,
                "42S02" => DatabaseErrorType.MissingTable,
                "42S22" => DatabaseErrorType.MissingColumn,
                "42000" => DatabaseErrorType.SyntaxError,
                "28000" => DatabaseErrorType.AuthenticationFailed,
                _ => number switch
                {
                    133 => DatabaseErrorType.Deadlock,
                    301 => DatabaseErrorType.UniqueConstraint,
                    461 => DatabaseErrorType.ForeignKeyConstraint,
                    287 => DatabaseErrorType.NotNullConstraint,
                    259 => DatabaseErrorType.MissingTable,
                    260 => DatabaseErrorType.MissingColumn,
                    257 => DatabaseErrorType.SyntaxError,
                    10 => DatabaseErrorType.AuthenticationFailed,
                    258 => DatabaseErrorType.PermissionDenied,
                    _ => DatabaseErrorType.Unknown
                }
            };
        }
        private static int? GetIntProperty(object obj, string propertyName)
        {
            object value = obj.GetType()
                .GetProperty(propertyName)?
                .GetValue(obj);

            return value switch
            {
                int i => i,
                short s => s,
                long l => (int)l,
                uint ui => unchecked((int)ui),
                ushort us => us,
                ulong ul => (int)ul,
                byte b => b,
                Enum e => Convert.ToInt32(e),
                _ => null
            };
        }

        private static string GetStringProperty(object obj, string propertyName)
        {
            return obj.GetType()
                .GetProperty(propertyName)?
                .GetValue(obj) as string;
        }
    }
}
