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

using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm
{
    public sealed partial class GXDatabaseBackupService
    {
        private const string SqlHeader = "-- GXDatabaseBackupService 1 ";

        private void EnsureSqlProvider()
        {
            if (database.DatabaseType is not (DatabaseType.SqLite or DatabaseType.MSSQL or
                DatabaseType.PostgreSQL or DatabaseType.MySQL or DatabaseType.MariaDB))
            {
                throw new NotSupportedException("SQL backup is supported for SQLite, SQL Server, PostgreSQL, MySQL and MariaDB.");
            }
        }

        private async Task ExportSqlAsync(Stream destination, List<GXTableSchema> schemas, CancellationToken token)
        {
            EnsureSqlProvider();
            // Validate all table DDL before emitting a script.
            var ddl = schemas.ToDictionary(s => s, s => Schema.GetCreateTableStatements(s));
            using var writer = new StreamWriter(destination, new UTF8Encoding(false), 4096, leaveOpen: true);
            await writer.WriteLineAsync((SqlHeader + database.DatabaseType).AsMemory(), token).ConfigureAwait(false);
            async Task Write(string table, string statement)
            {
                // Metadata is a SQL comment; statements remain executable by native SQL clients.
                await writer.WriteLineAsync(("-- GXTable " + Convert.ToBase64String(Encoding.UTF8.GetBytes(table))).AsMemory(), token).ConfigureAwait(false);
                await writer.WriteLineAsync((statement.TrimEnd().TrimEnd(';') + ";").AsMemory(), token).ConfigureAwait(false);
            }
            foreach (var schema in schemas)
            {
                foreach (var sql in ddl[schema])
                {
                    await Write(schema.Name, sql).ConfigureAwait(false);
                }

                bool identity = database.DatabaseType == DatabaseType.MSSQL && schema.Columns.Any(c => c.IsAutoIncrement || c.IsIdentity);
                if (identity)
                {
                    await Write(schema.Name, "SET IDENTITY_INSERT " + TableName(schema) + " ON").ConfigureAwait(false);
                }

                using (var command = CreateCommand("SELECT " + string.Join(",", schema.Columns.Select(c => Quote(c.Name))) + " FROM " + TableName(schema)))
                await using (var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(token).ConfigureAwait(false))
                    {
                        var values = new string[reader.FieldCount];
                        for (int i = 0; i != values.Length; ++i)
                        {
                            object value = reader.GetValue(i);
                            if (schema.Columns[i].Type == typeof(DateTimeOffset) && value is DateTime utc)
                            {
                                value = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
                            }

                            values[i] = SqlLiteral(value);
                        }
                        await Write(schema.Name, InsertPrefix(schema) + "(" + string.Join(",", values) + ")").ConfigureAwait(false);
                    }
                }
                if (identity)
                {
                    await Write(schema.Name, "SET IDENTITY_INSERT " + TableName(schema) + " OFF").ConfigureAwait(false);
                }

                foreach (var sql in ReseedStatements(schema))
                {
                    await Write(schema.Name, sql).ConfigureAwait(false);
                }
            }
            await writer.FlushAsync(token).ConfigureAwait(false);
        }

        private string StringLiteral(string value)
        {
            // MySQL backslash handling depends on sql_mode. Hex-encoded UTF-8 is unambiguous.
            if (database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB)
            {
                return "CONVERT(X'" + Convert.ToHexString(Encoding.UTF8.GetBytes(value)) + "' USING utf8mb4)";
            }

            if (database.DatabaseType == DatabaseType.PostgreSQL)
            {
                return "E'" + value.Replace("\\", "\\\\").Replace("'", "''") + "'";
            }

            return (database.DatabaseType == DatabaseType.MSSQL ? "N" : "") + "'" + value.Replace("'", "''") + "'";
        }

        private string SqlLiteral(object value)
        {
            if (value is DBNull)
            {
                return "NULL";
            }

            if (value is byte[] bytes)
            {
                var hex = Convert.ToHexString(bytes);
                return database.DatabaseType switch
                {
                    DatabaseType.MSSQL => "0x" + hex,
                    DatabaseType.PostgreSQL => "decode('" + hex + "','hex')",
                    _ => "X'" + hex + "'"
                };
            }
            if (value is bool flag)
            {
                return database.DatabaseType == DatabaseType.PostgreSQL ? (flag ? "TRUE" : "FALSE") : (flag ? "1" : "0");
            }

            if (value is string or char or Guid)
            {
                return StringLiteral(value.ToString()!);
            }

            if (value is DateTime date)
            {
                return StringLiteral(date.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
            }

            if (value is DateTimeOffset offset)
            {
                return StringLiteral(offset.ToString("O", CultureInfo.InvariantCulture));
            }

            if (value is DateOnly day)
            {
                return StringLiteral(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            if (value is TimeOnly time)
            {
                return StringLiteral(time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
            }

            if (value is TimeSpan span)
            {
                if (database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB)
                {
                    var absolute = span.Duration();
                    return StringLiteral((span < TimeSpan.Zero ? "-" : "") + ((long)absolute.TotalHours).ToString(CultureInfo.InvariantCulture) +
                        absolute.ToString(@"\:mm\:ss\.ffffff", CultureInfo.InvariantCulture));
                }
                return StringLiteral(span.ToString("c", CultureInfo.InvariantCulture));
            }
            RequireType(value.GetType().FullName);
            if (value is double d && !double.IsFinite(d) || value is float f && !float.IsFinite(f))
            {
                throw new NotSupportedException("Non-finite SQL numeric literals are unsupported.");
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture)!;
        }

        private async Task ImportSqlAsync(Stream source, HashSet<string>? selected, CancellationToken token)
        {
            EnsureSqlProvider();
            using var reader = new StreamReader(source, Encoding.UTF8, true, 4096, leaveOpen: true);
            string? header = await reader.ReadLineAsync(token).ConfigureAwait(false);
            if (header != SqlHeader + database.DatabaseType)
            {
                throw new InvalidDataException("SQL backup version or source database product does not match the target.");
            }

            var text = await reader.ReadToEndAsync(token).ConfigureAwait(false);
            var statements = ParseSql(text);
            CheckSelection(selected, statements.Select(s => s.Table));
            await using var transaction = await database.Connection.BeginTransactionAsync(token).ConfigureAwait(false);
            var identities = new HashSet<string>();
            try
            {
                foreach (var statement in statements)
                {
                    if (selected != null && !selected.Contains(statement.Table))
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();
                    await ExecuteAsync(statement.Sql, transaction, token).ConfigureAwait(false);
                    if (statement.Sql.StartsWith("SET IDENTITY_INSERT ", StringComparison.OrdinalIgnoreCase))
                    {
                        var table = statement.Sql[20..];
                        if (table.EndsWith(" ON", StringComparison.OrdinalIgnoreCase))
                        {
                            identities.Add(table[..^3]);
                        }

                        if (table.EndsWith(" OFF", StringComparison.OrdinalIgnoreCase))
                        {
                            identities.Remove(table[..^4]);
                        }
                    }
                }
                await transaction.CommitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                // IDENTITY_INSERT is session scoped and can survive a rollback.
                foreach (var table in identities)
                {
                    await ExecuteAsync("SET IDENTITY_INSERT " + table + " OFF", null, CancellationToken.None).ConfigureAwait(false);
                }

                throw;
            }
            finally { database.SchemaCache.Clear(); }
        }

        private sealed record SqlStatement(string Table, string Sql);

        // Parse only the service's SQL dialect subset: quotes, escaped quotes, identifiers,
        // comments, and semicolons. Never split string values on embedded semicolons.
        private List<SqlStatement> ParseSql(string script)
        {
            var result = new List<SqlStatement>();
            int position = 0;
            while (position < script.Length)
            {
                while (position < script.Length && char.IsWhiteSpace(script[position]))
                {
                    ++position;
                }

                if (position == script.Length)
                {
                    break;
                }

                int lineEnd = script.IndexOf('\n', position);
                if (lineEnd < 0 || !script.AsSpan(position).StartsWith("-- GXTable "))
                {
                    throw new InvalidDataException("SQL statement is missing table metadata.");
                }

                string table;
                try
                { table = Encoding.UTF8.GetString(Convert.FromBase64String(script[(position + 11)..lineEnd].Trim())); }
                catch (FormatException ex) { throw new InvalidDataException("Invalid SQL table metadata.", ex); }
                position = lineEnd + 1;
                int start = position;
                char quote = '\0';
                bool backslashEscapes = false;
                bool complete = false;
                for (; position < script.Length; ++position)
                {
                    char c = script[position];
                    if (quote != '\0')
                    {
                        // PostgreSQL E strings use backslash escaping. Generated MySQL strings use hex.
                        if (c == '\\' && quote == '\'' && backslashEscapes)
                        { ++position; continue; }
                        if (c == quote)
                        {
                            if (position + 1 < script.Length && script[position + 1] == quote)
                            { ++position; continue; }
                            quote = '\0';
                        }
                        continue;
                    }
                    if (c is '\'' or '"' or '`' or '[')
                    {
                        backslashEscapes = database.DatabaseType == DatabaseType.PostgreSQL && c == '\'' && position > start && script[position - 1] == 'E';
                        quote = c == '[' ? ']' : c;
                        continue;
                    }
                    if (c == ';')
                    {
                        result.Add(new SqlStatement(table, script[start..position].Trim()));
                        ++position;
                        complete = true;
                        break;
                    }
                }
                if (!complete)
                {
                    throw new InvalidDataException("Incomplete SQL statement.");
                }
            }
            return result;
        }
    }
}
