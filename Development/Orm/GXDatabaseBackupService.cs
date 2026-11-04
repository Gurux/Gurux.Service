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

using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm
{
    /// <summary>Exports and imports logical table backups on the supplied connection.</summary>
    /// <remarks>
    /// Streams and the connection remain owned by the caller. Do not use the connection concurrently.
    /// Backups contain user tables, not views, users, routines or physical recovery information.
    /// Export streams rows; JSON import buffers its document. Concurrent source writes require
    /// caller-managed database locking for a consistent multi-table backup.
    /// </remarks>
    public sealed partial class GXDatabaseBackupService
    {
        private readonly GXDbConnection database;
        private GXSchemaManager Schema => new(database);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            Converters = { new JsonStringEnumConverter() }
        };
        private static readonly Dictionary<string, Type> Types = new[]
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
            typeof(double), typeof(decimal), typeof(string), typeof(char), typeof(byte[]),
            typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
            typeof(DateOnly), typeof(TimeOnly)
        }.ToDictionary(t => t.FullName!, StringComparer.Ordinal);

        /// <summary>Creates a service targeting the database selected by the connection.</summary>
        public GXDatabaseBackupService(GXDbConnection connection)
        {
            database = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>Exports all discovered user tables. The destination remains open.</summary>
        public Task BackupDatabaseAsync(Stream destination,
            DatabaseBackupFormat format = DatabaseBackupFormat.Json,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ExportTablesAsync(destination, Schema.GetTables(), format, cancellationToken);
        }

        /// <summary>Exports named tables with schema, indexes, foreign keys and rows.</summary>
        public async Task ExportTablesAsync(Stream destination, IEnumerable<string> tables,
            DatabaseBackupFormat format = DatabaseBackupFormat.Json,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(destination);
            if (!destination.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(destination));
            ValidateFormat(format);
            EnsureProvider();
            cancellationToken.ThrowIfCancellationRequested();
            var names = GetNames(tables);
            var schemas = OrderTables(names.Select(name => Schema.Describe(name)).ToList());
            foreach (var schema in schemas)
            {
                await EnrichSchemaAsync(schema, cancellationToken).ConfigureAwait(false);
                ValidateSchema(schema);
            }
            if (format == DatabaseBackupFormat.Sql)
            {
                await ExportSqlAsync(destination, schemas, cancellationToken).ConfigureAwait(false);
                return;
            }
            using var writer = new Utf8JsonWriter(destination);
            writer.WriteStartObject();
            writer.WriteNumber("formatVersion", 1);
            writer.WriteString("databaseType", database.DatabaseType.ToString());
            writer.WriteStartArray("tables");
            foreach (var schema in schemas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteStartObject();
                writer.WritePropertyName("schema");
                JsonSerializer.Serialize(writer, schema, JsonOptions);
                writer.WriteStartArray("rows");
                using var command = CreateCommand("SELECT " + string.Join(",", schema.Columns.Select(c => Quote(c.Name))) + " FROM " + TableName(schema));
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    writer.WriteStartArray();
                    for (int pos = 0; pos != reader.FieldCount; ++pos)
                    {
                        var value = reader.GetValue(pos);
                        if (value is DBNull)
                        { writer.WriteNullValue(); continue; }
                        var type = value.GetType();
                        RequireType(type.FullName);
                        writer.WriteStartObject();
                        writer.WriteString("type", type.FullName);
                        writer.WritePropertyName("value");
                        JsonSerializer.Serialize(writer, value, type, JsonOptions);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Imports every table in a backup into this service's database.</summary>
        /// <remarks>SQL accepts only this service's scripts for the same database product and Append mode.</remarks>
        public Task RestoreDatabaseAsync(Stream source,
            DatabaseBackupFormat format = DatabaseBackupFormat.Json,
            DatabaseImportMode mode = DatabaseImportMode.Append,
            CancellationToken cancellationToken = default) =>
            ImportCoreAsync(source, null, format, mode, cancellationToken);

        /// <summary>Imports only the named backup tables. Referenced tables must be included or already exist.</summary>
        public Task ImportTablesAsync(Stream source, IEnumerable<string> tables,
            DatabaseBackupFormat format = DatabaseBackupFormat.Json,
            DatabaseImportMode mode = DatabaseImportMode.Append,
            CancellationToken cancellationToken = default) =>
            ImportCoreAsync(source, GetNames(tables), format, mode, cancellationToken);

        private async Task ImportCoreAsync(Stream source, HashSet<string>? selected,
            DatabaseBackupFormat format, DatabaseImportMode mode, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!source.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(source));
            ValidateFormat(format);
            EnsureProvider();
            if (!Enum.IsDefined(mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            token.ThrowIfCancellationRequested();
            if (format == DatabaseBackupFormat.Sql)
            {
                if (mode != DatabaseImportMode.Append)
                    throw new NotSupportedException("SQL scripts define their own DDL. Replacement modes apply to JSON only.");
                await ImportSqlAsync(source, selected, token).ConfigureAwait(false);
                return;
            }
            using var document = await JsonDocument.ParseAsync(source, cancellationToken: token).ConfigureAwait(false);
            var root = document.RootElement;
            if (!root.TryGetProperty("formatVersion", out var version) || version.GetInt32() != 1)
                throw new InvalidDataException("Unsupported backup format version.");
            if (!Enum.TryParse<DatabaseType>(root.GetProperty("databaseType").GetString(), out var sourceType) || !Enum.IsDefined(sourceType))
                throw new InvalidDataException("Unknown source database type.");
            var imports = new List<ImportTable>();
            foreach (var element in root.GetProperty("tables").EnumerateArray())
            {
                var metadata = element.GetProperty("schema");
                // Validate type names before GXColumnSchema.TypeName resolves any types.
                foreach (var column in metadata.GetProperty("columns").EnumerateArray())
                    RequireType(column.GetProperty("typeName").GetString());
                var schema = metadata.Deserialize<GXTableSchema>(JsonOptions) ?? throw new InvalidDataException("Missing schema.");
                ValidateSchema(schema);
                if (selected != null && !selected.Contains(schema.Name))
                    continue;
                var rows = new List<object?[]>();
                foreach (var row in element.GetProperty("rows").EnumerateArray())
                {
                    if (row.GetArrayLength() != schema.Columns.Count)
                        throw new InvalidDataException("Row width differs from schema.");
                    rows.Add(row.EnumerateArray().Select(ReadValue).ToArray());
                }
                PrepareSchema(schema, sourceType);
                imports.Add(new ImportTable(schema, rows));
            }
            if (imports.Select(t => t.Schema.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != imports.Count)
                throw new InvalidDataException("Duplicate table names.");
            CheckSelection(selected, imports.Select(t => t.Schema.Name));
            var ordered = OrderTables(imports.Select(t => t.Schema).ToList());
            // Generate all DDL before any destructive actions, including provider validation.
            foreach (var schema in ordered)
                Schema.GetCreateTableStatements(schema);
            await using var transaction = await database.Connection.BeginTransactionAsync(token).ConfigureAwait(false);
            try
            {
                foreach (var schema in ordered.AsEnumerable().Reverse())
                {
                    token.ThrowIfCancellationRequested();
                    if (Schema.TableExist(transaction, schema.Name))
                    {
                        if (mode == DatabaseImportMode.ReplaceTables)
                            await ExecuteAsync("DROP TABLE " + TableName(schema), transaction, token).ConfigureAwait(false);
                        else if (mode == DatabaseImportMode.ReplaceData)
                            await ExecuteAsync("DELETE FROM " + TableName(schema), transaction, token).ConfigureAwait(false);
                    }
                }
                foreach (var schema in ordered)
                {
                    token.ThrowIfCancellationRequested();
                    if (!Schema.TableExist(transaction, schema.Name))
                        foreach (var sql in Schema.GetCreateTableStatements(schema))
                            await ExecuteAsync(sql, transaction, token).ConfigureAwait(false);
                    var data = imports.Single(t => ReferenceEquals(t.Schema, schema));
                    await InsertRowsAsync(data, transaction, token).ConfigureAwait(false);
                }
                await transaction.CommitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            finally { database.SchemaCache.Clear(); }
        }

        private sealed record ImportTable(GXTableSchema Schema, List<object?[]> Rows);

        private async Task EnrichSchemaAsync(GXTableSchema schema, CancellationToken token)
        {
            using var command = CreateCommand("SELECT * FROM " + TableName(schema) + " WHERE 1=0");
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var columns = reader.GetColumnSchema();
            foreach (var column in schema.Columns)
            {
                var metadata = columns.Single(c => c.ColumnName == column.Name);
                if (database.DatabaseType != DatabaseType.SqLite && metadata.DataType != null)
                    column.Type = metadata.DataType;
                column.IsAutoIncrement |= metadata.IsAutoIncrement == true;
                column.IsComputed |= metadata.IsExpression == true;
                column.Precision = metadata.NumericPrecision;
                column.Scale = metadata.NumericScale;
                if (metadata.ColumnSize.HasValue && (column.Type == typeof(string) || column.Type == typeof(byte[])))
                    column.MaxLength = metadata.ColumnSize;
                string native = metadata.DataTypeName ?? "";
                if (native.Length != 0 && !native.Contains('('))
                {
                    if (column.Type == typeof(decimal) && column.Precision > 0 && column.Scale >= 0)
                        native += $"({column.Precision},{column.Scale})";
                    else if (native.Equals("varchar", StringComparison.OrdinalIgnoreCase) || native.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                        native.Equals("char", StringComparison.OrdinalIgnoreCase) || native.Equals("nchar", StringComparison.OrdinalIgnoreCase) ||
                        native.Equals("binary", StringComparison.OrdinalIgnoreCase) || native.Equals("varbinary", StringComparison.OrdinalIgnoreCase) ||
                        native.Equals("character varying", StringComparison.OrdinalIgnoreCase))
                        native += column.MaxLength > 0 && column.MaxLength < int.MaxValue ? $"({column.MaxLength})" :
                            database.DatabaseType == DatabaseType.MSSQL ? "(max)" : "";
                }
                column.DbType = native;
                if (native.Equals("date", StringComparison.OrdinalIgnoreCase))
                    column.Type = typeof(DateOnly);
                if (native.StartsWith("time", StringComparison.OrdinalIgnoreCase) && !native.StartsWith("timestamp", StringComparison.OrdinalIgnoreCase))
                    column.Type = database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB ? typeof(TimeSpan) : typeof(TimeOnly);
                if (native.Contains("with time zone", StringComparison.OrdinalIgnoreCase) || native.StartsWith("timestamptz", StringComparison.OrdinalIgnoreCase) || native.StartsWith("datetimeoffset", StringComparison.OrdinalIgnoreCase))
                    column.Type = typeof(DateTimeOffset);
            }
            if (database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB)
            {
                // Reader metadata omits fractional temporal precision; COLUMN_TYPE retains it.
                using var types = CreateCommand("SELECT COLUMN_NAME,COLUMN_TYPE,DATETIME_PRECISION FROM information_schema.columns WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@table");
                var parameter = types.CreateParameter();
                parameter.ParameterName = "@table";
                parameter.Value = schema.Name;
                types.Parameters.Add(parameter);
                await reader.DisposeAsync().ConfigureAwait(false);
                await using var typeReader = await types.ExecuteReaderAsync(token).ConfigureAwait(false);
                while (await typeReader.ReadAsync(token).ConfigureAwait(false))
                {
                    var column = schema.Columns.Single(c => c.Name == typeReader.GetString(0));
                    column.DbType = typeReader.GetString(1);
                    if (!typeReader.IsDBNull(2))
                        column.DateTimePrecision = Convert.ToInt32(typeReader.GetValue(2), CultureInfo.InvariantCulture);
                }
            }
        }

        private static object? ReadValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
                return null;
            var type = RequireType(value.GetProperty("type").GetString());
            return value.GetProperty("value").Deserialize(type, JsonOptions);
        }

        private void PrepareSchema(GXTableSchema schema, DatabaseType sourceType)
        {
            // Always target the connection's default schema, never the source catalog/owner.
            schema.Catalog = null;
            schema.Schema = null;
            foreach (var key in schema.ForeignKeys)
                key.ReferencedSchema = "";
            foreach (var column in schema.Columns)
            {
                if (column.IsComputed || column.ComputedExpression != null)
                    throw new NotSupportedException("Computed columns cannot be restored: " + column.Name);
                if (column.DefaultValue is JsonElement json)
                    column.DefaultValue = json.ValueKind == JsonValueKind.Null ? null : json.ToString();
                if (sourceType != database.DatabaseType)
                {
                    if (column.DefaultValue != null && !column.IsAutoIncrement && !column.IsIdentity)
                        throw new NotSupportedException("Cross-product default expressions require explicit mapping: " + column.Name);
                    column.DbType = "";
                    if (column.Type == typeof(decimal) && database.DatabaseType == DatabaseType.SqLite)
                        column.DbType = "TEXT"; // SQLite NUMERIC affinity would round through IEEE double.
                    if (column.Type == typeof(DateTime))
                        column.DbType = database.DatabaseType switch
                        {
                            DatabaseType.MySQL or DatabaseType.MariaDB => "DATETIME(6)",
                            DatabaseType.MSSQL => "DATETIME2(7)",
                            DatabaseType.PostgreSQL => "TIMESTAMP(6) WITHOUT TIME ZONE",
                            _ => ""
                        };
                    if (column.Type == typeof(DateTimeOffset))
                        column.DbType = database.DatabaseType switch
                        {
                            DatabaseType.PostgreSQL => "TIMESTAMP(6) WITH TIME ZONE",
                            DatabaseType.MSSQL => "DATETIMEOFFSET(7)",
                            _ => "VARCHAR(40)"
                        };
                    if (column.Type == typeof(TimeSpan))
                        column.DbType = database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB ? "TIME(6)" : "VARCHAR(40)";
                }
                if (column.IsAutoIncrement || column.IsIdentity)
                    column.DefaultValue = null;
            }
        }

        private async Task InsertRowsAsync(ImportTable table, DbTransaction transaction, CancellationToken token)
        {
            var schema = table.Schema;
            bool identity = database.DatabaseType == DatabaseType.MSSQL && schema.Columns.Any(c => c.IsAutoIncrement || c.IsIdentity);
            if (identity)
                await ExecuteAsync("SET IDENTITY_INSERT " + TableName(schema) + " ON", transaction, token).ConfigureAwait(false);
            try
            {
                foreach (var row in table.Rows)
                {
                    using var command = CreateCommand(InsertPrefix(schema) + "(" + string.Join(",", row.Select((_, i) => "@p" + i)) + ")", transaction);
                    for (int i = 0; i != row.Length; ++i)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@p" + i;
                        parameter.Value = ConvertValue(row[i], schema.Columns[i].Type) ?? DBNull.Value;
                        if (parameter.Value is DBNull)
                            parameter.DbType = GetParameterType(schema.Columns[i].Type);
                        else if (parameter.Value is DateTime && database.DatabaseType == DatabaseType.MSSQL)
                            parameter.DbType = DbType.DateTime2;
                        command.Parameters.Add(parameter);
                    }
                    await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
                foreach (var sql in ReseedStatements(schema))
                    await ExecuteAsync(sql, transaction, token).ConfigureAwait(false);
            }
            finally
            {
                if (identity)
                    await ExecuteAsync("SET IDENTITY_INSERT " + TableName(schema) + " OFF", transaction, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private object? ConvertValue(object? value, Type columnType)
        {
            if (value == null)
                return null;
            if (columnType == typeof(TimeSpan) && value is TimeSpan duration &&
                database.DatabaseType is not (DatabaseType.MySQL or DatabaseType.MariaDB))
                return duration.ToString("c", CultureInfo.InvariantCulture);
            if (columnType == typeof(DateOnly) && value is DateTime day)
                value = DateOnly.FromDateTime(day);
            if (columnType == typeof(TimeOnly) && value is TimeSpan time)
                value = TimeOnly.FromTimeSpan(time);
            if (columnType == typeof(DateTimeOffset))
            {
                if (value is DateTime dateValue)
                    value = new DateTimeOffset(DateTime.SpecifyKind(dateValue, DateTimeKind.Utc));
                if (value is DateTimeOffset offset)
                    value = database.DatabaseType switch
                    {
                        DatabaseType.PostgreSQL => offset.ToUniversalTime(),
                        DatabaseType.MySQL or DatabaseType.MariaDB => offset.ToString("O", CultureInfo.InvariantCulture),
                        _ => offset
                    };
            }
            if (database.DatabaseType == DatabaseType.PostgreSQL)
            {
                if (columnType == typeof(bool) && value is not bool)
                    return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
                if (columnType == typeof(Guid) && value is string text)
                    return Guid.Parse(text);
                if (value is DateTime date)
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
            }
            if (value is Guid guid && database.DatabaseType is DatabaseType.MySQL or DatabaseType.MariaDB)
                return guid.ToByteArray();
            return value;
        }

        private static DbType GetParameterType(Type type)
        {
            if (type == typeof(byte[]))
                return DbType.Binary;
            if (type == typeof(Guid))
                return DbType.Guid;
            if (type == typeof(bool))
                return DbType.Boolean;
            if (type == typeof(DateTime))
                return DbType.DateTime2;
            if (type == typeof(DateTimeOffset))
                return DbType.DateTimeOffset;
            if (type == typeof(DateOnly))
                return DbType.Date;
            if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
                return DbType.Time;
            if (type == typeof(decimal))
                return DbType.Decimal;
            if (type == typeof(float))
                return DbType.Single;
            if (type == typeof(double))
                return DbType.Double;
            if (type == typeof(long) || type == typeof(ulong))
                return DbType.Int64;
            if (type == typeof(int) || type == typeof(uint) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
                return DbType.Int32;
            return DbType.String;
        }

        private IEnumerable<string> ReseedStatements(GXTableSchema schema)
        {
            if (database.DatabaseType == DatabaseType.PostgreSQL)
                foreach (var column in schema.Columns.Where(c => c.IsAutoIncrement || c.IsIdentity))
                    yield return "SELECT setval(pg_get_serial_sequence(" + StringLiteral(TableName(schema)) + "," + StringLiteral(column.Name) + "), COALESCE(MAX(" + Quote(column.Name) + "),1), MAX(" + Quote(column.Name) + ") IS NOT NULL) FROM " + TableName(schema);
        }

        private string InsertPrefix(GXTableSchema schema) => "INSERT INTO " + TableName(schema) + " (" + string.Join(",", schema.Columns.Select(c => Quote(c.Name))) + ") VALUES ";
        private string TableName(GXTableSchema schema) => string.IsNullOrEmpty(schema.Schema) ? Quote(schema.Name) : Quote(schema.Schema) + "." + Quote(schema.Name);
        private string Quote(string name)
        {
            char open = database.Builder.Settings.ColumnNameQuoteCharacter;
            if (open == '\0')
                open = '"';
            char close = open == '[' ? ']' : open;
            return open + name.Replace(close.ToString(), new string(close, 2)) + close;
        }
        private DbCommand CreateCommand(string sql, DbTransaction? transaction = null)
        {
            var command = database.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandTimeout = database.CommandTimeout;
            return command;
        }
        private async Task ExecuteAsync(string sql, DbTransaction? transaction, CancellationToken token)
        {
            using var command = CreateCommand(sql, transaction);
            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
        private static void ValidateFormat(DatabaseBackupFormat format)
        {
            if (!Enum.IsDefined(format))
                throw new ArgumentOutOfRangeException(nameof(format));
        }
        private void EnsureProvider()
        {
            if (database.DatabaseType is not (DatabaseType.SqLite or DatabaseType.MSSQL or
                DatabaseType.PostgreSQL or DatabaseType.MySQL or DatabaseType.MariaDB))
                throw new NotSupportedException("Logical backup supports SQLite, SQL Server, PostgreSQL, MySQL and MariaDB.");
        }
        private static Type RequireType(string? name) => name != null && Types.TryGetValue(name, out var type)
            ? type : throw new NotSupportedException("Unsupported backup type: " + name);
        private static HashSet<string> GetNames(IEnumerable<string> tables)
        {
            ArgumentNullException.ThrowIfNull(tables);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in tables)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Table name is empty.", nameof(tables));
                if (!names.Add(name))
                    throw new ArgumentException("Duplicate table: " + name, nameof(tables));
            }
            return names;
        }
        private static void CheckSelection(HashSet<string>? selected, IEnumerable<string> available)
        {
            if (selected != null && !selected.IsSubsetOf(available))
                throw new InvalidDataException("Selected table is absent from backup.");
        }
        private static void ValidateSchema(GXTableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Name) || schema.Columns.Count == 0 ||
                schema.Columns.Any(c => string.IsNullOrWhiteSpace(c.Name)) ||
                schema.Columns.Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != schema.Columns.Count)
                throw new InvalidDataException("Invalid table or column names.");
            foreach (var column in schema.Columns)
                RequireType(column.TypeName);
            if (schema.Columns.Any(c => c.IsComputed))
                throw new NotSupportedException("Computed columns cannot be backed up.");
        }
        private static List<GXTableSchema> OrderTables(List<GXTableSchema> tables)
        {
            var result = new List<GXTableSchema>();
            var visiting = new HashSet<GXTableSchema>();
            void Visit(GXTableSchema table)
            {
                if (result.Contains(table))
                    return;
                if (!visiting.Add(table))
                    throw new NotSupportedException("Cyclic foreign keys require a provider-specific restore strategy.");
                foreach (var key in table.ForeignKeys)
                {
                    var parent = tables.SingleOrDefault(t => string.Equals(t.Name, key.ReferencedTable, StringComparison.OrdinalIgnoreCase));
                    if (parent == table)
                        throw new NotSupportedException("Self-referencing tables require an explicit row ordering strategy.");
                    if (parent != null)
                        Visit(parent);
                }
                visiting.Remove(table);
                result.Add(table);
            }
            foreach (var table in tables)
                Visit(table);
            return result;
        }
    }
}
