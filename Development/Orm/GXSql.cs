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
using System.Linq.Expressions;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Provides expression-tree markers for SQL aggregates, predicates, subqueries, aliases, and window functions.
    /// </summary>
    /// <remarks>
    /// These members are markers for the ORM expression translator. Direct invocation throws
    /// <see cref="InvalidOperationException"/>; they do not execute SQL or calculate aggregates in memory. Use them
    /// inside query expressions passed to the ORM. CLR return types allow expression composition and do not necessarily describe the SQL result type.
    /// </remarks>
    public static class GXSql
    {
        /// <summary>
        /// Represents a SQL COUNT aggregate over rows or non-null operand values.
        /// </summary>
        /// <param name="expression">The column or SQL operand to count; use the row parameter, null, or "*" to count rows.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static int Count(object expression) =>
            throw new InvalidOperationException("Use Count inside a SQL expression.");
        /// <summary>
        /// Represents a SQL COUNT(DISTINCT ...) aggregate over distinct non-null values.
        /// </summary>
        /// <param name="expression">The column or SQL operand whose distinct values are counted.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static int DistinctCount(object expression) =>
            throw new InvalidOperationException("Use DistinctCount inside a SQL expression.");

        /// <summary>
        /// Represents an emptiness check that yields 1 when the query has no rows and 0 otherwise.
        /// </summary>
        /// <param name="expression">The SQL operand used to construct the query being checked.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool IsEmpty(object expression) =>
            throw new InvalidOperationException("Use IsEmpty inside a SQL expression.");

        /// <summary>
        /// Represents a SQL SUM aggregate over a numeric operand.
        /// </summary>
        /// <param name="expression">The numeric column or SQL expression to sum.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Sum(object expression) =>
            throw new InvalidOperationException("Use Sum inside a SQL expression.");

        /// <summary>
        /// Assigns an alias to an expression in the SQL projection.
        /// </summary>
        /// <typeparam name="T">The expression result type.</typeparam>
        /// <param name="expression">The column or SQL expression to alias.</param>
        /// <param name="alias">The alias used in the generated SQL.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static T As<T>(T expression, string alias) =>
            throw new InvalidOperationException("Use As inside a SQL expression.");

        /// <summary>
        /// Represents a SQL MIN aggregate that selects the smallest non-null value.
        /// </summary>
        /// <param name="expression">The column or SQL expression to aggregate.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Min(object expression) =>
            throw new InvalidOperationException("Use Min inside a SQL expression.");

        /// <summary>
        /// Represents a SQL MAX aggregate that selects the largest non-null value.
        /// </summary>
        /// <param name="expression">The column or SQL expression to aggregate.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Max(object expression) =>
            throw new InvalidOperationException("Use Max inside a SQL expression.");

        /// <summary>
        /// Represents a SQL AVG aggregate over non-null numeric values.
        /// </summary>
        /// <param name="expression">The numeric column or SQL expression to average.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Avg(object expression) =>
            throw new InvalidOperationException("Use Avg inside a SQL expression.");

        /// <summary>
        /// Represents the constant 1 in a SQL projection.
        /// </summary>
        /// <value>The SQL constant 1; direct access does not return a value.</value>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool One
        {
            get
            {
                throw new InvalidOperationException("Use One inside a SQL expression.");
            }
        }


        /// <summary>
        /// Represents a SQL IN predicate that tests membership in a collection.
        /// </summary>
        /// <typeparam name="T">The operand and collection element type.</typeparam>
        /// <param name="value">The column or SQL operand to test.</param>
        /// <param name="collection">The values included in the SQL IN list.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool In<T>(T value, params IEnumerable<T> collection) =>
            throw new InvalidOperationException("Use In inside a SQL expression.");

        /// <summary>
        /// Represents a SQL IN predicate that tests membership in a subquery result.
        /// </summary>
        /// <param name="value">The column or SQL operand to test.</param>
        /// <param name="expression">The query selecting the values to compare against.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool In(object value, GXSelectArgs expression) =>
            throw new InvalidOperationException("Use In inside a SQL expression.");

        /// <summary>
        /// Represents a correlated SQL EXISTS predicate matching source and destination columns.
        /// </summary>
        /// <typeparam name="TSourceTable">The outer query table type.</typeparam>
        /// <typeparam name="TDestinationTable">The subquery table type.</typeparam>
        /// <param name="sourceColumn">The column from the outer query used for correlation.</param>
        /// <param name="destinationColumn">The column in the subquery compared with the source column.</param>
        /// <param name="expression">The subquery whose matching rows are tested.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Exists<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn,
            Expression<Func<TDestinationTable, object>> destinationColumn, GXSelectArgs expression) =>
            throw new InvalidOperationException("Use Exists inside a SQL expression.");

        /// <summary>
        /// Represents a SQL EXISTS predicate that tests whether a subquery returns any rows.
        /// </summary>
        /// <param name="args">The subquery whose row existence is tested.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Exists(GXSelectArgs args) =>
            throw new InvalidOperationException("Use Exists inside a SQL expression.");

        /// <summary>
        /// Embeds a query as a parenthesized subquery in a SQL expression.
        /// </summary>
        /// <typeparam name="T">The result type used to compose the surrounding expression.</typeparam>
        /// <param name="args">The query to embed.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static T Subquery<T>(GXSelectArgs args) =>
            throw new InvalidOperationException("Use Subquery inside a SQL expression.");

        /// <summary>
        /// Embeds a query as a scalar subquery in a SQL expression.
        /// </summary>
        /// <typeparam name="T">The scalar result type used in the surrounding expression.</typeparam>
        /// <param name="args">The query selecting a single column and at most one row.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static T Scalar<T>(GXSelectArgs args) =>
            throw new InvalidOperationException("Use Scalar inside a SQL expression.");

        /// <summary>
        /// Represents a case-insensitive literal substring search on a metadata column.
        /// </summary>
        /// <param name="column">The physical column to search after conversion to text.</param>
        /// <param name="value">The literal substring to find; SQL wildcard characters are escaped.</param>
        /// <param name="comparison">Must be StringComparison.OrdinalIgnoreCase; translated using SQL UPPER and LIKE.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Contains(Gurux.Service.Orm.Common.Model.GXColumnSchema column, string value, StringComparison comparison) =>
            throw new InvalidOperationException("Use Contains inside a SQL expression.");

        /// <summary>
        /// Represents a SQL LIKE predicate that searches for text within an operand.
        /// </summary>
        /// <typeparam name="T">The searched operand type.</typeparam>
        /// <param name="value">The column or SQL operand to search.</param>
        /// <param name="expression">The search text placed between SQL wildcard characters.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Contains<T>(T value, object expression) =>
            throw new InvalidOperationException("Use Contains inside a SQL expression.");

        /// <summary>
        /// Marks a prefix comparison in a SQL expression.
        /// </summary>
        /// <typeparam name="T">The searched operand type.</typeparam>
        /// <param name="value">The column or SQL operand to search.</param>
        /// <param name="expression">The prefix to match.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool StartsWith<T>(T value, object expression) =>
            throw new InvalidOperationException("Use StartsWith inside a SQL expression.");

        /// <summary>
        /// Marks a suffix comparison in a SQL expression.
        /// </summary>
        /// <typeparam name="T">The searched operand type.</typeparam>
        /// <param name="value">The column or SQL operand to search.</param>
        /// <param name="expression">The suffix to match.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool EndsWith<T>(T value, object expression) =>
            throw new InvalidOperationException("Use EndsWith inside a SQL expression.");

        /// <summary>
        /// Represents a SQL &gt; predicate testing whether the left operand is greater than the right operand.
        /// </summary>
        /// <typeparam name="T">The left operand type.</typeparam>
        /// <param name="value">The left column or SQL operand.</param>
        /// <param name="expression">The right value or SQL expression.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Greater<T>(T value, object expression) =>
            throw new InvalidOperationException("Use Greater inside a SQL expression.");
        /// <summary>
        /// Represents a SQL &lt; predicate testing whether the left operand is less than the right operand.
        /// </summary>
        /// <typeparam name="T">The left operand type.</typeparam>
        /// <param name="value">The left column or SQL operand.</param>
        /// <param name="expression">The right value or SQL expression.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool Less<T>(T value, object expression) =>
            throw new InvalidOperationException("Use Less inside a SQL expression.");
        /// <summary>
        /// Represents a SQL &gt;= predicate testing whether the left operand is greater than or equal to the right operand.
        /// </summary>
        /// <typeparam name="T">The left operand type.</typeparam>
        /// <param name="value">The left column or SQL operand.</param>
        /// <param name="expression">The right value or SQL expression.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool GreaterOrEqual<T>(T value, object expression) =>
            throw new InvalidOperationException("Use GreaterOrEqual inside a SQL expression.");
        /// <summary>
        /// Represents a SQL &lt;= predicate testing whether the left operand is less than or equal to the right operand.
        /// </summary>
        /// <typeparam name="T">The left operand type.</typeparam>
        /// <param name="value">The left column or SQL operand.</param>
        /// <param name="expression">The right value or SQL expression.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool LessOrEqual<T>(T value, object expression) =>
            throw new InvalidOperationException("Use LessOrEqual inside a SQL expression.");

        /// <summary>
        /// Defines the PARTITION BY columns of a SQL window function.
        /// </summary>
        /// <param name="columns">The columns or SQL expressions that group rows into partitions.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static object[] PartitionBy(params object[] columns) =>
            throw new InvalidOperationException("Use PartitionBy inside a SQL expression.");

        /// <summary>
        /// Defines ascending ordering within a SQL window function.
        /// </summary>
        /// <typeparam name="T">The ordering operand type.</typeparam>
        /// <param name="column">The column or SQL expression to sort in ascending order.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool OrderBy<T>(T column) =>
            throw new InvalidOperationException("Use OrderBy inside a SQL expression.");

        /// <summary>
        /// Defines descending ordering within a SQL window function.
        /// </summary>
        /// <typeparam name="T">The ordering operand type.</typeparam>
        /// <param name="column">The column or SQL expression to sort in descending order.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool OrderByDescending<T>(T column) =>
            throw new InvalidOperationException("Use OrderByDescending inside a SQL expression.");

        /// <summary>
        /// Represents ROW_NUMBER() OVER (...) with partitioning and ordering.
        /// </summary>
        /// <typeparam name="T">The partition operand type.</typeparam>
        /// <param name="partition">The partition expressions, typically supplied using PartitionBy.</param>
        /// <param name="order">One or more ordering expressions supplied using OrderBy or OrderByDescending.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool RowNumber<T>(T[] partition, params object[] order) =>
            throw new InvalidOperationException("Use RowNumber inside a SQL expression.");

        /// <summary>
        /// Represents ROW_NUMBER() OVER (...) with ordering across all selected rows.
        /// </summary>
        /// <typeparam name="T">The ordering expression type.</typeparam>
        /// <param name="order">The ordering expression, supplied using OrderBy or OrderByDescending.</param>
        /// <returns>An expression marker for SQL translation; direct invocation does not return a value.</returns>
        /// <exception cref="InvalidOperationException">Always thrown when invoked directly instead of translated as part of a SQL expression tree.</exception>
        public static bool RowNumber<T>(T order) =>
            throw new InvalidOperationException("Use RowNumber inside a SQL expression.");
    }
}
