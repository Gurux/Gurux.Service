//
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

namespace Gurux.Service.Orm.Enums
{
    /// <summary>
    /// Database error type.
    /// </summary>
    public enum DatabaseErrorType
    {
        /// <summary>
        /// Unknown error type. This value is used when the specific error type cannot be determined.
        /// </summary>
        Unknown,
        /// <summary>
        /// Deadlock errors occur when two or more transactions are waiting for each other to release resources, 
        /// causing a cycle of dependencies that prevents any of the transactions from proceeding. 
        /// Deadlocks typically require intervention to resolve, such as retrying the transactions.
        /// </summary>
        Deadlock,
        /// <summary>
        /// Timeout errors occur when a database operation exceeds the allotted time for completion. This can happen due to long-running queries, network issues, or resource contention. 
        /// Timeouts can lead to incomplete transactions and may require retrying the operation or optimizing the query for better performance.
        /// </summary>
        Timeout,
        /// <summary>
        /// Lock timeout errors occur when a database operation cannot acquire the necessary locks within the allotted time. This can happen due to high contention for resources or long-running transactions.
        /// </summary>
        LockTimeout,
        /// <summary>
        /// Serialization failure errors occur when a transaction cannot be serialized due to concurrent modifications. This typically happens in high-concurrency environments and may require retrying the transaction.
        /// </summary>
        SerializationFailure,
        /// <summary>
        /// Unique constraint errors occur when a value being inserted or updated violates a unique constraint in the database.
        /// </summary>  
        UniqueConstraint,
        /// <summary>
        /// Foreign key constraint errors occur when a value being inserted or updated violates a foreign key constraint in the database.
        /// </summary>
        ForeignKeyConstraint,
        /// <summary>
        /// Not null constraint errors occur when a null value is inserted into a column that does not allow nulls.
        /// </summary>
        NotNullConstraint,
        /// <summary>
        /// Check constraint errors occur when a value being inserted or updated violates a check constraint in the database.
        /// </summary>
        CheckConstraint,
        /// <summary>
        /// Connection failed errors occur when the database connection cannot be established.
        /// </summary>
        ConnectionFailed,
        /// <summary>
        /// Connection lost errors occur when the database connection is unexpectedly lost.
        /// </summary>
        ConnectionLost,
        /// <summary>
        /// Authentication failed errors occur when the database authentication process fails.
        /// </summary>
        AuthenticationFailed,
        /// <summary>
        /// Transaction aborted errors occur when a transaction is aborted due to various reasons, such as deadlocks or conflicts.
        /// </summary>
        TransactionAborted,
        /// <summary>
        /// Transaction conflict errors occur when a transaction conflicts with another transaction, typically due to concurrent modifications.
        /// </summary>
        TransactionConflict,
        /// <summary>
        /// Syntax errors occur when a database query contains invalid syntax.
        /// </summary>
        SyntaxError,
        /// <summary>
        /// Missing table errors occur when a referenced table does not exist in the database.
        /// </summary>
        MissingTable,
        /// <summary>
        /// Missing column errors occur when a referenced column does not exist in the database.
        /// </summary>
        MissingColumn,
        /// <summary>
        /// Invalid operation errors occur when the database rejects the requested operation.
        /// </summary>
        InvalidOperation,

        /// <summary>
        /// Data too long errors occur when a value being inserted or updated exceeds the maximum allowed length for a column.
        /// </summary>
        DataTooLong,
        /// <summary>
        /// Numeric overflow errors occur when a numeric value exceeds the allowable range for a column.
        /// </summary>
        NumericOverflow,
        /// <summary>
        /// Invalid cast errors occur when a value cannot be converted to the expected data type.
        /// </summary>
        InvalidCast,

        /// <summary>
        /// Permission denied errors occur when the database operation is not allowed due to insufficient permissions.
        /// </summary>
        PermissionDenied
    }
}
