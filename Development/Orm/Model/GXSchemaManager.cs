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

using Gurux.Common.Internal;
using Gurux.Orm.Internal.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Common.Model;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm.Model
{
    /// <summary>
    /// GXSchemaManager is used to create and update database tables.
    /// </summary>
    public partial class GXSchemaManager
    {
        private GXSqlBuilder Builder;
        private DbConnection Connection;

        /// <summary>Gets the table-description cache shared by managers on this connection.</summary>
        /// <remarks>Set its CacheTime to zero to disable caching. Clear it after external schema changes.</remarks>
        public Gurux.Service.DB.GXSchemaCache SchemaCache => Gurux.Service.DB.GXSchemaCache.ForConnection(Connection);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        public GXSchemaManager(DbConnection connection,
            string? tablePrefix = null)
        {
            Connection = connection ?? throw new ArgumentException(null, nameof(connection));
            Builder = new GXSqlBuilder(connection, tablePrefix);
            if (connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
            if (Builder.Settings.Type == DatabaseType.DB2 &&
                !string.IsNullOrEmpty(connection.Database))
            {
                ChangeDatabase(connection.Database);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <exception cref="ArgumentException">The connection is null.</exception>
        public GXSchemaManager(GXDbConnection connection)
        {
            Connection = connection?.Connection ?? throw new ArgumentException(null, nameof(connection));
            Builder = new GXSqlBuilder(Connection, connection.Builder.Settings.TablePrefix);
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
            if (Builder.Settings.Type == DatabaseType.DB2 &&
                !string.IsNullOrEmpty(connection.Connection.Database))
            {
                ChangeDatabase(connection.Connection.Database);
            }
        }

        /// <summary>
        /// Event handler for column value conversion.
        /// </summary>
        public event EventHandler<GXColumnValueConvertingEventArgs>? ColumnValueConverting;

        /// <summary>
        /// Event handler for executed SQL.
        /// </summary>
        /// <remarks>
        /// This can be used for debugging executed SQL statements.
        /// </remarks>
        public event EventHandler<GXSqlExecutedEventArgs>? OnSqlExecuted;

        /// <summary>
        /// Used database type.
        /// </summary>
        public DatabaseType DatabaseType
        {
            get
            {
                return Builder.Settings.Type;
            }
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction()
        {
            IDbTransaction transaction = Connection.BeginTransaction();
            return transaction;
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            IDbTransaction transaction = Connection.BeginTransaction(isolationLevel);
            return transaction;
        }

        /// <summary>
        /// Create database with given name.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        public void CreateDatabase(string databaseName)
        {
            CreateDatabase(null, databaseName);
        }

        /// <summary>
        /// Create database with given name.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        public void CreateDatabase(IDbTransaction? transaction, string databaseName)
        {
            databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
            string query;
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                return;
            }
            else if (Builder.Settings.Type == DatabaseType.DB2 ||
                  Builder.Settings.Type == DatabaseType.SapHana)
            {
                query = "CREATE SCHEMA " + databaseName;
            }
            else if (Builder.Settings.Type == DatabaseType.Oracle)
            {
                throw new ArgumentException("Oracle database cannot be created from the application. Please create it manually.");
            }
            else
            {
                query = "CREATE DATABASE " + databaseName;
            }
            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Database table names.</returns>
        public string[] GetTables(IDbTransaction? transaction = null)
        {
            return GetTables(transaction, Builder.Database);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        public string[] GetTables(IDbTransaction? transaction, string databaseName)
        {
            return Builder.GetTables(this, Connection, transaction, OnSqlExecuted, databaseName);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        public string[] GetTables(string databaseName)
        {
            return GetTables(null, databaseName);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        public void CreateTable<T>()
        {
            CreateTable<T>(true, false);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable<T>(bool relations, bool overwrite)
        {
            CreateTable(typeof(T), relations, overwrite);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        public void CreateTable<T>(IDbTransaction? transaction)
        {
            CreateTable(transaction, typeof(T), true, true);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable<T>(IDbTransaction? transaction, bool relations, bool overwrite)
        {
            CreateTable(transaction, typeof(T), relations, overwrite);
        }

        /// <summary>
        /// Check if table exists.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        public bool TableExist(string tableName)
        {
            return TableExist(null, tableName);
        }

        /// <summary>
        /// Create new table from <paramref name="schema"/>.
        /// </summary>
        public void CreateTable(GXTableSchema schema)
        {
            CreateTable(null, schema);
        }

        /// <summary>
        /// Create new table from <paramref name="schema"/>.
        /// </summary>
        public void CreateTable(IDbTransaction? transaction, GXTableSchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }
            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                throw new ArgumentException("Table name is empty.", nameof(schema));
            }
            if (schema.Columns.Count == 0)
            {
                throw new ArgumentException("Table schema does not contain columns.", nameof(schema));
            }

            IDbConnection connection = transaction?.Connection ?? Connection;
            string tableName = GetSchemaTableName(schema);
            StringBuilder sb = new();
            sb.Append("CREATE TABLE ");
            sb.Append(tableName);
            sb.Append('(');
            var primaryKeys = schema.Columns.Where(c => c.IsPrimaryKey).ToArray();
            bool first = true;
            foreach (GXColumnSchema column in schema.Columns.OrderBy(c => c.Ordinal == 0 ? int.MaxValue : c.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(column.Name))
                {
                    throw new ArgumentException("Column name is empty.", nameof(schema));
                }
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                AppendSchemaColumnDefinition(sb, column, primaryKeys.Length > 1);
            }
            if (primaryKeys.Length > 1)
            {
                sb.Append(", PRIMARY KEY (");
                sb.Append(string.Join(", ", primaryKeys.Select(column => GXDbHelpers.ConvertToString(
                    Builder.Settings, TargetType.Column, null,
                    Builder.Settings.UpperCase ? column.Name.ToUpperInvariant() : column.Name, null))));
                sb.Append(')');
            }
            foreach (var key in schema.ForeignKeys)
            {
                if (key.Columns.Count == 0 || string.IsNullOrWhiteSpace(key.ReferencedTable))
                    throw new ArgumentException("Foreign keys require columns and a referenced table.");
                sb.Append(", ");
                if (!string.IsNullOrEmpty(key.Name)) sb.Append("CONSTRAINT ").Append(QuoteSchemaIdentifier(key.Name)).Append(' ');
                sb.Append("FOREIGN KEY (")
                    .Append(string.Join(", ", key.Columns.OrderBy(c => c.Position).Select(c => QuoteSchemaIdentifier(c.Column))))
                    .Append(") REFERENCES ")
                    .Append(GetSchemaTableName(new GXTableSchema
                    {
                        Name = key.ReferencedTable,
                        Schema = Builder.Settings.Type == DatabaseType.SqLite ? null : key.ReferencedSchema
                    }))
                    .Append(" (")
                    .Append(string.Join(", ", key.Columns.OrderBy(c => c.Position).Select(c => QuoteSchemaIdentifier(c.ReferencedColumn))))
                    .Append(')');
                AppendSchemaForeignKeyAction(sb, "DELETE", key.OnDelete);
                AppendSchemaForeignKeyAction(sb, "UPDATE", key.OnUpdate);
            }
            sb.Append(')');
            ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, sb.ToString());

            var primaryKeyNames = primaryKeys.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var index in schema.Indexes)
            {
                if (index.Columns.Count == 0 || string.IsNullOrWhiteSpace(index.Name))
                    throw new ArgumentException("Indexes require a name and columns.");
                // These constraints were already emitted in the table definition.
                if (index.Unique && (primaryKeyNames.SetEquals(index.Columns.Select(c => c.Name)) ||
                    (index.Columns.Count == 1 && schema.Columns.Any(c => c.IsUnique &&
                        string.Equals(c.Name, index.Columns[0].Name, StringComparison.OrdinalIgnoreCase))))) continue;
                string columns = string.Join(", ", index.Columns.OrderBy(c => c.Position).Select(c =>
                    QuoteSchemaIdentifier(c.Name) + (c.Order == Gurux.Service.Orm.Common.Enums.IndexOrder.Descending ? " DESC" : " ASC")));
                string indexName = QuoteSchemaIdentifier(index.Name);
                string indexTable = tableName;
                if (Builder.Settings.Type == DatabaseType.SqLite && !string.IsNullOrEmpty(schema.Schema))
                {
                    indexName = QuoteSchemaIdentifier(schema.Schema) + "." + indexName;
                    indexTable = QuoteSchemaIdentifier(schema.Name);
                }
                ExecuteNonQuery(this, connection, transaction, OnSqlExecuted,
                    $"CREATE {(index.Unique ? "UNIQUE " : "")}INDEX {indexName} ON {indexTable} ({columns})");
            }

            foreach (GXColumnSchema column in schema.Columns.Where(c => c.IsAutoIncrement))
            {
                string[]? queries = Builder.Settings.CreateAutoIncrement(tableName, column.Name);
                if (queries == null)
                {
                    continue;
                }
                foreach (string query in queries)
                {
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                }
            }

            AddSchemaComments(connection, transaction, tableName, schema);
        }

        private string QuoteSchemaIdentifier(string name)
        {
            char open = Builder.Settings.ColumnNameQuoteCharacter;
            if (open == '\0') open = '"';
            char close = open == '[' ? ']' : open;
            if (Builder.Settings.UpperCase) name = name.ToUpperInvariant();
            return open + name.Replace(close.ToString(), new string(close, 2)) + close;
        }

        private void AppendSchemaForeignKeyAction(StringBuilder sql, string operation, Gurux.Service.Orm.Common.Enums.ForeignKeyAction action)
        {
            if (action is Gurux.Service.Orm.Common.Enums.ForeignKeyAction.None or Gurux.Service.Orm.Common.Enums.ForeignKeyAction.NoAction) return;
            if (Builder.Settings.Type == DatabaseType.Oracle && operation == "UPDATE")
                throw new ArgumentException("Oracle does not support ON UPDATE foreign key actions.");
            string value = action switch
            {
                Gurux.Service.Orm.Common.Enums.ForeignKeyAction.Restrict => Builder.Settings.Type == DatabaseType.MSSQL ? "NO ACTION" : "RESTRICT",
                Gurux.Service.Orm.Common.Enums.ForeignKeyAction.Cascade => "CASCADE",
                Gurux.Service.Orm.Common.Enums.ForeignKeyAction.SetNull => "SET NULL",
                Gurux.Service.Orm.Common.Enums.ForeignKeyAction.SetDefault => "SET DEFAULT",
                _ => throw new ArgumentException("Unsupported foreign key action.")
            };
            sql.Append(" ON ").Append(operation).Append(' ').Append(value);
        }

        private string GetSchemaTableName(GXTableSchema schema)
        {
            string tableName = schema.Name;
            if (!string.IsNullOrEmpty(schema.Schema) &&
                !tableName.Contains('.'))
            {
                tableName = schema.Schema + "." + tableName;
            }
            return GetTableName(tableName);
        }

        private void AppendSchemaColumnDefinition(StringBuilder sb, GXColumnSchema column, bool compositePrimaryKey = false)
        {
            string columnName = Builder.Settings.UpperCase
                ? column.Name.ToUpperInvariant()
                : column.Name;
            sb.Append(GXDbHelpers.ConvertToString(Builder.Settings,
                TargetType.Column, null, columnName, null));
            sb.Append(' ');
            sb.Append(GetSchemaColumnType(column));

            if (!(Builder.Settings.Type == DatabaseType.Oracle &&
                (column.DefaultValue != null ||
                column.IsAutoIncrement ||
                column.IsIdentity ||
                column.IsPrimaryKey)))
            {
                sb.Append(column.IsNullable && !column.IsPrimaryKey
                    ? " NULL "
                    : " NOT NULL ");
            }

            if (column.DefaultValue != null)
            {
                GetSchemaDefaultValue(sb, column.DefaultValue, column.Type);
            }

            if (column.IsPrimaryKey && !compositePrimaryKey)
            {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                if (Builder.Settings.Type == DatabaseType.Oracle ||
                    Builder.Settings.Type == DatabaseType.SapHana)
                {
                    if (column.IsAutoIncrement || column.IsIdentity)
                    {
                        sb.Append(Builder.Settings.AutoIncrementDefinition);
                    }
                    sb.Append(" PRIMARY KEY ");
                }
                else
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                {
                    sb.Append(" PRIMARY KEY ");
                    if (column.IsAutoIncrement || column.IsIdentity)
                    {
                        sb.Append(Builder.Settings.AutoIncrementDefinition);
                    }
                }
            }
            else if (column.IsUnique)
            {
                sb.Append(" UNIQUE ");
            }

            if ((Builder.Settings.Type == DatabaseType.MySQL ||
                Builder.Settings.Type == DatabaseType.MariaDB) &&
                !string.IsNullOrEmpty(column.Comment))
            {
                sb.Append(" COMMENT '");
                sb.Append(column.Comment.Replace("'", "''"));
                sb.Append("' ");
            }
        }

        private string GetSchemaColumnType(GXColumnSchema column)
        {
            if (!string.IsNullOrEmpty(column.DbType))
            {
                return column.DbType;
            }
            Type type = Nullable.GetUnderlyingType(column.Type) ?? column.Type;
            if ((type == typeof(string) || type == typeof(char[]) ||
                type == typeof(object)) &&
                column.MaxLength.HasValue)
            {
                return Builder.Settings.StringColumnDefinition(
                    GetSchemaColumnLength(column.MaxLength.Value));
            }
            if (type == typeof(byte[]) &&
                column.MaxLength.HasValue)
            {
                return Builder.Settings.ByteArrayColumnDefinition(
                    GetSchemaColumnLength(column.MaxLength.Value));
            }
            if ((column.IsAutoIncrement || column.IsIdentity) &&
                Builder.Settings.Type == DatabaseType.SqLite)
            {
                type = typeof(int);
            }
            else if ((column.IsAutoIncrement || column.IsIdentity) &&
                Builder.Settings.Type == DatabaseType.PostgreSQL &&
                type == typeof(ulong))
            {
                type = typeof(long);
            }
            return Builder.GetDataBaseType(type, null);
        }

        private static int GetSchemaColumnLength(long maxLength)
        {
            if (maxLength <= 0 || maxLength > 8000 || maxLength > int.MaxValue)
            {
                return 0;
            }
            return (int)maxLength;
        }

        private void GetSchemaDefaultValue(StringBuilder sb,
            object value,
            Type columnType)
        {
            string tmp = Builder.Settings.GetColumnDefaultValue(value, columnType);
            if (string.IsNullOrEmpty(tmp))
            {
                return;
            }
            sb.Append(" DEFAULT");
            if (Builder.Settings.Type == DatabaseType.SqLite ||
                Builder.Settings.Type == DatabaseType.MSSQL)
            {
                sb.Append('(');
                sb.Append(tmp);
                sb.Append(')');
            }
            else
            {
                sb.Append(' ');
                sb.Append(tmp);
            }
        }

        private void AddSchemaComments(IDbConnection connection,
            IDbTransaction? transaction,
            string tableName,
            GXTableSchema schema)
        {
            if (!string.IsNullOrEmpty(schema.Comment))
            {
                string query = Builder.Settings.GetCommentQuery(
                    Builder.Database, tableName, null, schema.Comment);
                if (!string.IsNullOrEmpty(query))
                {
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                }
            }
            foreach (GXColumnSchema column in schema.Columns)
            {
                if (string.IsNullOrEmpty(column.Comment))
                {
                    continue;
                }
                string query = Builder.Settings.GetCommentQuery(
                    Builder.Database,
                    tableName,
                    Builder.Settings.EscapeIdentifier(null, column.Name),
                    column.Comment);
                if (!string.IsNullOrEmpty(query))
                {
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                }
            }
        }

        /// <summary>
        /// Check if table exists.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        public bool TableExist(IDbTransaction? transaction, string tableName)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            tableName = GetTableName(tableName);
            string query = Builder.Settings.TableExist(Builder.Database, tableName);
            return (int?)ExecuteScalarInternal(connection, transaction, query, typeof(int)) != 0;
        }

        internal static object? ExecuteScalarInternal(IDbConnection connection,
            IDbTransaction? transaction,
            string query,
            Type? type)
        {
            if (transaction?.Connection != null)
            {
                connection = transaction.Connection;
            }
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    object? value = com.ExecuteScalar();
                    if (value is DBNull)
                    {
                        value = null;
                    }
                    else if (type != null)
                    {
                        value = Convert.ChangeType(value, type);
                    }
                    return value;
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        internal static async Task<object?> ExecuteScalarInternalAsync(DbConnection connection,
            string query,
            Type? type,
            CancellationToken cancellationToken)
        {
            try
            {
                await using DbCommand com = connection.CreateCommand();
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    object? value = await com.ExecuteScalarAsync(cancellationToken);
                    if (value is DBNull)
                    {
                        value = null;
                    }
                    else if (type != null)
                    {
                        value = Convert.ChangeType(value, type);
                    }
                    return value;
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        /// <summary>
        /// Returns existing relation tables.
        /// </summary>
        public Type[] GetRelationTables<T>()
        {
            return GetRelationTables(typeof(T));
        }

        /// <summary>
        /// Returns existing relation tables.
        /// </summary>
        public Type[] GetRelationTables(Type type)
        {
            return GetRelationTables(null, type);
        }

        /// <summary>
        /// Returns existing relation tables.
        /// </summary>
        public Type[] GetRelationTables(IDbTransaction? transaction, Type type)
        {
            List<Type> list = [];
            Dictionary<Type, GXSerializedItem> tables = [];
            GXSqlBuilder.GetTables(type, tables);
            if (!tables.ContainsKey(type))
            {
                tables.Add(type, null);
            }
            //Find existing tables.
            foreach (var it in tables)
            {
                Type tmp = it.Key;
                if (GXDbHelpers.IsSharedTable(tmp))
                {
                    tmp = tmp.BaseType;
                }
                if (TableExist(transaction, Builder.GetTableName(tmp, false)))
                {
                    list.Add(it.Key);
                }
            }
            return list.ToArray();
        }


        /// <summary>
        /// Create new table.
        /// </summary>
        /// <param name="type">Type of the table.</param>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable(Type type, bool relations, bool overwrite)
        {
            CreateTable(null, type, relations, overwrite);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="type">Type of the table.</param>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable(IDbTransaction? transaction, Type type, bool relations, bool overwrite)
        {
            Dictionary<Type, GXSerializedItem> tables = [];
            if (relations)
            {
                GXSqlBuilder.GetTables(type, tables);
            }
            if (!tables.ContainsKey(type))
            {
                tables.Add(type, null);
            }
            if (relations && !overwrite)
            {
                //Existing tables are not created.
                List<Type> removed = [];
                foreach (var it in tables)
                {
                    var tableName = Builder.GetTableName(it.Key, false);
                    tableName = GetTableName(tableName);
                    if (TableExist(transaction, tableName))
                    {
                        removed.Add(it.Key);
                    }
                }
                foreach (var it in removed)
                {
                    tables.Remove(it);
                }
            }
            IDbConnection connection = transaction?.Connection ?? Connection;
            try
            {
                //Find dropped tables.
                Dictionary<Type, GXSerializedItem> dropTables = [];
                foreach (var it in tables)
                {
                    Type tmp = it.Key;
                    var tableName = Builder.GetTableName(it.Key, false);
                    tableName = GetTableName(tableName);
                    if (TableExist(transaction, tableName))
                    {
                        if (!overwrite)
                        {
                            continue;
                        }
                        dropTables[tmp] = it.Value;
                    }
                    if (tmp.BaseType != typeof(object) && tmp.BaseType != typeof(GXTableBase))
                    {
                        tmp = tmp.BaseType;
                        if (TableExist(Builder.GetTableName(tmp, false)))
                        {
                            if (!overwrite)
                            {
                                continue;
                            }
                            dropTables[tmp] = it.Value;
                        }
                    }
                }
                foreach (var it in tables)
                {
                    //If table do not have relations.
                    if (it.Value == null)
                    {
                        DropTable(connection, transaction, type, dropTables);
                    }
                    else
                    {
                        DropTable(connection, transaction, it.Key, dropTables);
                    }
                }

                CreateTable(connection, transaction, type, tables);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void CreateTable(IDbConnection connection,
            IDbTransaction? transaction,
            Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(true, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(connection, true, transaction, it.Value, created);
            }
        }

        /// <summary>
        /// Create or drop selected table and it's dependencies.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="create">Indicates whether to create or drop the table.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="table">Table creation query.</param>
        /// <param name="created">List of created tables.</param>
        private void TableCreation(IDbConnection connection, bool create,
            IDbTransaction? transaction, GXTableCreateQuery table, List<Type> created)
        {
            Type type;
            //Create and drop depended tables first.
            foreach (var t in table.Dependencies)
            {
                TableCreation(connection, create, transaction, t, created);
            }
            if (GXDbHelpers.IsSharedTable(table.Table))
            {
                type = table.Table.BaseType;
            }
            else
            {
                type = table.Table;
            }
            if (!created.Contains(type))
            {
                created.Add(type);
                foreach (string q in table.Queries)
                {
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, q);
                }
            }
        }

        private void DropTable(IDbConnection connection,
            IDbTransaction? transaction,
            Type type,
            Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(false, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(connection, false, transaction, it.Value, created);
            }
        }

        private string GetIndexName(string table, string column)
        {
            string value = string.Format("{0}_{1}", table, column).ToLower();
            if (value.Length > Builder.Settings.MaximumIndexNameLength)
            {
                value = value.Substring(0, Builder.Settings.MaximumIndexNameLength);
            }
            return value;
        }


        private List<(string ConstraintName, string TableName)> GetForeignKeys(IDbTransaction? transaction, string tableName)
        {
            string query = Builder.Settings.GetForeignKeysQuery(tableName);
            List<object[]> columns = new List<object[]>();
            ExecuteQuery(Connection, transaction, query, 2, columns);
            return columns.Select(c => (c[0].ToString(), c[1].ToString())).ToList();
        }


        /// <summary>
        /// Update table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        public void UpdateTable<T>(IDbTransaction? transaction = default)
        {
            UpdateTable(transaction, typeof(T));
        }

        /// <summary>
        /// Update table.
        /// </summary>
        /// <param name="type">Table type.</param>
        public void UpdateTable(Type type)
        {
            UpdateTable(null, type);
        }

        /// <summary>Updates a table, optionally leaving foreign keys unchanged for partial upgrade models.</summary>
        public void UpdateTable(Type type, bool updateForeignKeys)
        {
            UpdateTable(null, type, updateForeignKeys);
        }

        /// <summary>Updates a complete model, optionally deleting database-only columns and their foreign keys.</summary>
        public void UpdateTable(Type type, bool updateForeignKeys, bool removeUnusedColumns)
        {
            UpdateTable(null, type, updateForeignKeys, removeUnusedColumns);
        }

        /// <summary>
        /// Update table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="type">Table type.</param>
        /// <remarks>Invalidates cached descriptions so the next Describe call reads the current schema, including after partial updates.</remarks>
        public void UpdateTable(IDbTransaction? transaction, Type type)
        {
            UpdateTable(transaction, type, updateForeignKeys: true);
        }

        /// <summary>Updates a table, optionally leaving foreign keys unchanged for partial upgrade models.</summary>
        public void UpdateTable(IDbTransaction? transaction, Type type, bool updateForeignKeys)
        {
            UpdateTable(transaction, type, updateForeignKeys, removeUnusedColumns: false);
        }

        private void UpdateTable(IDbTransaction? transaction, Type type, bool updateForeignKeys, bool removeUnusedColumns)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            string tableName = Builder.GetTableName(type, true);
            string[] cols = Builder.GetColumns(this, GetTableName(Builder.GetTableName(type, false)),
                connection, transaction, OnSqlExecuted);
            try
            {
                if (removeUnusedColumns)
                {
                    RemoveUnusedColumns(transaction, type, cols);
                }
                //Add new columns.
                foreach (var it in GXSqlBuilder.GetProperties(type))
                {
                    if (!cols.Contains(it.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
                        {
                            if (it.Value.Relation.RelationType == RelationType.OneToMany ||
                                it.Value.Relation.RelationType == RelationType.ManyToMany)
                            {
                                continue;
                            }
                        }
                        StringBuilder sb = new();
                        sb.Append("ALTER TABLE ");
                        sb.Append(tableName);
                        sb.Append(" ADD ");
                        sb.Append(Builder.Settings.EscapeIdentifier(null, it.Key));
                        sb.Append(" ");
                        if (it.Value.Relation != null &&
                            it.Value.Relation.RelationType == RelationType.OneToOne)
                        {
                            //If 1:1 ralation
                            sb.Append(Builder.GetDataBaseType(it.Value.Relation.ForeignId.Type, it.Value.Relation));
                        }
                        else
                        {
                            sb.Append(Builder.GetDataBaseType(it.Value.Type, it.Value.Target));
                        }
                        sb.Append(' ');
                        //If nullable.
                        if ((it.Value.Attributes & (Attributes.AllowNull)) != 0)
                        {
                            sb.Append(" NULL");
                        }
                        else
                        {
                            sb.Append(" NOT NULL");
                        }
                        if ((it.Value.Attributes & (Attributes.DefaultValue)) != 0 && it.Value.DefaultValue != null)
                        {
                            GetDefaultValue(sb, it.Value.DefaultValue, it.Value.Type);
                        }
                        ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, sb.ToString());
                    }
                }
                //Change columns whose database type no longer matches the model.
                foreach (var it in GXSqlBuilder.GetProperties(type))
                {
                    if (!cols.Contains(it.Key, StringComparer.OrdinalIgnoreCase) ||
                        (it.Value.Relation != null &&
                        it.Value.Relation.ForeignTable != type &&
                        (it.Value.Relation.RelationType == RelationType.OneToMany ||
                        it.Value.Relation.RelationType == RelationType.ManyToMany)))
                    {
                        continue;
                    }
                    Type newType = GetColumnDataType(type, it.Value);
                    Type oldType = Builder.GetColumnType(this, tableName, it.Key,
                        connection, transaction, OnSqlExecuted,
                        out int length, out string databaseType);
                    object target = it.Value.Relation != null &&
                        it.Value.Relation.RelationType == RelationType.OneToOne
                        ? it.Value.Relation
                        : it.Value.Target;
                    string expectedType = Builder.GetDataBaseType(newType, target);
                    if (oldType != newType &&
                        !IsSameDatabaseType(databaseType, length, expectedType, Builder.Settings.Type))
                    {
                        ChangeColumnType(connection, transaction, tableName, it.Key,
                            oldType, newType, it.Value);
                    }
                }
                if (updateForeignKeys) UpdateForeignKeys(transaction, type);
            }
            finally
            {
                // Refresh even when the database already matches the model or an update partially fails.
                SchemaCache.Invalidate(transaction);
                if (connection is DbConnection schemaConnection && !ReferenceEquals(schemaConnection, Connection))
                    Gurux.Service.DB.GXSchemaCache.ForConnection(schemaConnection).Invalidate(transaction);
            }
        }

        private HashSet<string> GetModelColumnNames(Type type)
            => new(GXSqlBuilder.GetProperties(type).Where(p => p.Value.Relation == null ||
                p.Value.Relation.ForeignTable == type ||
                (p.Value.Relation.RelationType != RelationType.OneToMany &&
                 p.Value.Relation.RelationType != RelationType.ManyToMany)).Select(p => p.Key), StringComparer.OrdinalIgnoreCase);

        private void RemoveUnusedColumns(IDbTransaction? transaction, Type type, string[] columns)
        {
            var expected = GetModelColumnNames(type);
            var removed = columns.Where(c => !expected.Contains(c)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (removed.Count == 0) return;
            var connection = transaction?.Connection ?? Connection;
            string table = Builder.GetTableName(type, true);
            var keys = ReadForeignKeys(type, transaction);
            var obsoleteKeys = keys.Where(k => k.Columns.Any(c => removed.Contains(c.Column))).ToList();
            if (Builder.Settings.Type == DatabaseType.SqLite && obsoleteKeys.Count != 0)
                UpdateSqliteForeignKeys(transaction, type, keys.Except(obsoleteKeys).ToList());
            else
                foreach (var key in obsoleteKeys)
                {
                    string drop = Builder.Settings.Type is DatabaseType.MySQL or DatabaseType.MariaDB ? "DROP FOREIGN KEY " : "DROP CONSTRAINT ";
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted,
                        $"ALTER TABLE {table} {drop}{QuoteSchemaIdentifier(key.Name)}");
                }
            foreach (string column in removed)
            {
                // SQL Server default constraints must be removed explicitly first.
                if (Builder.Settings.Type == DatabaseType.MSSQL)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = "SELECT dc.name FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id WHERE dc.parent_object_id=OBJECT_ID(@table) AND c.name=@column";
                    var tableParameter = command.CreateParameter();
                    tableParameter.ParameterName = "@table";
                    tableParameter.Value = table;
                    command.Parameters.Add(tableParameter);
                    var columnParameter = command.CreateParameter();
                    columnParameter.ParameterName = "@column";
                    columnParameter.Value = column;
                    command.Parameters.Add(columnParameter);
                    if (command.ExecuteScalar() is string constraint)
                        ExecuteNonQuery(this, connection, transaction, OnSqlExecuted,
                            $"ALTER TABLE {table} DROP CONSTRAINT {QuoteSchemaIdentifier(constraint)}");
                }
                string quoted = QuoteSchemaIdentifier(column);
                string sql = Builder.Settings.Type == DatabaseType.SapHana
                    ? $"ALTER TABLE {table} DROP ({quoted})"
                    : $"ALTER TABLE {table} DROP COLUMN {quoted}";
                ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, sql);
            }
            if (Builder.Settings.Type == DatabaseType.DB2)
                ExecuteNonQuery(this, connection, transaction, OnSqlExecuted,
                    $"CALL SYSPROC.ADMIN_CMD('REORG TABLE {table.Replace("'", "''")}')");
        }

        /// <summary>Returns missing and removed columns, column type changes and foreign key changes without modifying the database.</summary>
        /// <param name="type">The table model to compare with the database.</param>
        /// <returns>Descriptions of pending model changes, or an empty array when no update is required.</returns>
        public string[] GetTableChanges(Type type)
        {
            if (!TableExist(type)) return new[] { "Table is missing" };
            string tableName = Builder.GetTableName(type, true);
            var columns = GetColumns(type);
            var changes = new List<string>();
            var modelColumns = GetModelColumnNames(type);
            changes.AddRange(columns.Where(c => !modelColumns.Contains(c)).Select(c => "Column removed from model: " + c));
            foreach (var item in GXSqlBuilder.GetProperties(type))
            {
                if (item.Value.Relation != null && item.Value.Relation.ForeignTable != type &&
                    (item.Value.Relation.RelationType == RelationType.OneToMany || item.Value.Relation.RelationType == RelationType.ManyToMany)) continue;
                if (!columns.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                {
                    changes.Add("Missing column: " + item.Key);
                    continue;
                }
                Type expected = GetColumnDataType(type, item.Value);
                Type actual = Builder.GetColumnType(this, tableName, item.Key, Connection, null, OnSqlExecuted, out int length, out string databaseType);
                object target = item.Value.Relation != null && item.Value.Relation.RelationType == RelationType.OneToOne ? item.Value.Relation : item.Value.Target;
                if (actual != expected && !IsSameDatabaseType(databaseType, length, Builder.GetDataBaseType(expected, target), Builder.Settings.Type))
                    changes.Add("Column type changed: " + item.Key);
            }
            changes.AddRange(GetForeignKeyChanges(type));
            return changes.ToArray();
        }

        private IEnumerable<string> GetForeignKeyChanges(Type type)
        {
            var actual = ReadForeignKeys(type, null);
            var properties = GXSqlBuilder.GetProperties(type);
            var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var expected in GetExpectedForeignKeys(type))
            {
                string column = expected.Columns[0].Column;
                expectedColumns.Add(column);
                var keys = actual.Where(k => k.Columns.Any(c =>
                    string.Equals(c.Column, column, StringComparison.OrdinalIgnoreCase))).ToList();
                if (keys.Count == 0)
                    yield return "Foreign key missing: " + column;
                else if (keys.Count != 1 || !SameForeignKey(keys[0], expected))
                    yield return "Foreign key changed: " + column;
            }
            foreach (var key in actual)
                if (key.Columns.Count != 0 && key.Columns.All(c => properties.Keys.Contains(c.Column, StringComparer.OrdinalIgnoreCase)) &&
                    !key.Columns.Any(c => expectedColumns.Contains(c.Column)))
                    yield return "Foreign key removed from model: " + string.Join(", ", key.Columns.Select(c => c.Column));
        }

        private List<GXForeignKeySchema> ReadForeignKeys(Type type, IDbTransaction? transaction)
        {
            var schema = new GXTableSchema { Name = GXSqlBuilder.UnescapeIdentifier(Builder.GetTableName(type, true)) };
            GetTableForeignKeys(schema, transaction?.Connection ?? Connection, transaction);
            return schema.ForeignKeys.ToList();
        }

        private List<GXForeignKeySchema> GetExpectedForeignKeys(Type type, IDbTransaction? transaction = null)
        {
            var result = new List<GXForeignKeySchema>();
            foreach (var item in GXSqlBuilder.GetProperties(type))
            {
                var relation = item.Value.Relation;
                var attribute = (item.Value.Target as PropertyInfo)?.GetCustomAttribute<ForeignKeyAttribute>(true);
                if (attribute == null || relation == null || relation.RelationType != RelationType.OneToOne) continue;
                string targetTable = GXSqlBuilder.UnescapeIdentifier(Builder.GetTableName(relation.ForeignTable, true));
                string targetColumn = GXSqlBuilder.GetProperties(relation.ForeignTable)
                    .First(p => Equals(p.Value.Target, relation.ForeignId.Target)).Key;
                ForeignKeyAction delete = attribute.OnDelete switch
                {
                    ForeignKeyDelete.Cascade => ForeignKeyAction.Cascade,
                    ForeignKeyDelete.Restrict => ForeignKeyAction.Restrict,
                    _ => ForeignKeyAction.NoAction
                };
                ForeignKeyAction update = attribute.OnUpdate switch
                {
                    ForeignKeyUpdate.Cascade => ForeignKeyAction.Cascade,
                    ForeignKeyUpdate.Restrict => ForeignKeyAction.Restrict,
                    ForeignKeyUpdate.Null => ForeignKeyAction.SetNull,
                    _ => ForeignKeyAction.NoAction
                };
                var key = new GXForeignKeySchema { ReferencedTable = targetTable, OnDelete = delete, OnUpdate = update };
                key.Columns.Add(new GXForeignKeyColumnSchema
                {
                    Column = NormalizeForeignKeyIdentifier(item.Key),
                    ReferencedColumn = NormalizeForeignKeyIdentifier(targetColumn),
                    Position = 0
                });
                result.Add(key);
            }
            if (result.Count != 0)
            {
                string? query = Builder.Settings.Type switch
                {
                    DatabaseType.MySQL or DatabaseType.MariaDB => "SELECT DATABASE()",
                    DatabaseType.MSSQL => "SELECT SCHEMA_NAME()",
                    DatabaseType.PostgreSQL => "SELECT current_schema()",
                    DatabaseType.Oracle => "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL",
                    DatabaseType.DB2 => "VALUES CURRENT SCHEMA",
                    DatabaseType.SapHana => "SELECT CURRENT_SCHEMA FROM DUMMY",
                    _ => null
                };
                if (query != null)
                {
                    string schema = Convert.ToString(ExecuteScalarInternal(transaction?.Connection ?? Connection, transaction, query, typeof(string)))!;
                    foreach (var key in result)
                    {
                        string name = QuoteSchemaIdentifier(key.ReferencedTable).Replace("'", "''");
                        string? resolve = Builder.Settings.Type switch
                        {
                            DatabaseType.MSSQL => $"SELECT OBJECT_SCHEMA_NAME(OBJECT_ID(N'{name}'))",
                            DatabaseType.PostgreSQL => $"SELECT n.nspname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE c.oid=to_regclass('{name}')",
                            _ => null
                        };
                        key.ReferencedSchema = resolve == null ? schema :
                            Convert.ToString(ExecuteScalarInternal(transaction?.Connection ?? Connection, transaction, resolve, typeof(string))) ?? schema;
                    }
                }
            }
            return result;
        }

        private string NormalizeForeignKeyIdentifier(string name)
            => GXSqlBuilder.UnescapeIdentifier(Builder.Settings.EscapeIdentifier(null, name));

        private bool SameForeignKey(GXForeignKeySchema actual, GXForeignKeySchema expected)
            => actual.Columns.Count == expected.Columns.Count &&
                string.Equals(actual.ReferencedTable, expected.ReferencedTable, StringComparison.OrdinalIgnoreCase) &&
                (Builder.Settings.Type == DatabaseType.SqLite ||
                    string.Equals(actual.ReferencedSchema, expected.ReferencedSchema, StringComparison.OrdinalIgnoreCase)) &&
                actual.Columns.OrderBy(c => c.Position).Zip(expected.Columns.OrderBy(c => c.Position))
                    .All(pair => string.Equals(pair.First.Column, pair.Second.Column, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pair.First.ReferencedColumn, pair.Second.ReferencedColumn, StringComparison.OrdinalIgnoreCase)) &&
                NormalizeForeignKeyAction(actual.OnDelete) == NormalizeForeignKeyAction(expected.OnDelete) &&
                NormalizeForeignKeyAction(actual.OnUpdate) == NormalizeForeignKeyAction(expected.OnUpdate);

        private string BuildForeignKeyClause(GXForeignKeySchema key)
        {
            if (Builder.Settings.Type == DatabaseType.Oracle && key.OnDelete == ForeignKeyAction.Restrict)
                throw new ArgumentException("Oracle does not support ON DELETE RESTRICT; use the default NO ACTION behavior.");
            var sql = new StringBuilder("FOREIGN KEY (");
            sql.Append(string.Join(", ", key.Columns.OrderBy(c => c.Position).Select(c => QuoteSchemaIdentifier(c.Column))))
                .Append(") REFERENCES ").Append(QuoteForeignKeyTable(key))
                .Append(" (").Append(string.Join(", ", key.Columns.OrderBy(c => c.Position)
                    .Select(c => QuoteSchemaIdentifier(c.ReferencedColumn)))).Append(')');
            AppendSchemaForeignKeyAction(sql, "DELETE", key.OnDelete);
            AppendSchemaForeignKeyAction(sql, "UPDATE", key.OnUpdate);
            return sql.ToString();
        }

        private string QuoteForeignKeyTable(GXForeignKeySchema key)
            => (Builder.Settings.Type != DatabaseType.SqLite && !string.IsNullOrEmpty(key.ReferencedSchema)
                ? QuoteSchemaIdentifier(key.ReferencedSchema) + "." : "") + QuoteSchemaIdentifier(key.ReferencedTable);

        private void UpdateForeignKeys(IDbTransaction? transaction, Type type)
        {
            var actual = ReadForeignKeys(type, transaction);
            var expected = GetExpectedForeignKeys(type, transaction);
            var properties = GXSqlBuilder.GetProperties(type);
            // Keep database-only constraints on columns outside this model.
            var removed = actual.Where(k => k.Columns.Count != 0 &&
                k.Columns.All(c => properties.Keys.Contains(c.Column, StringComparer.OrdinalIgnoreCase)) &&
                !expected.Any(e => SameForeignKey(k, e))).ToList();
            var added = expected.Where(e => !actual.Any(k => SameForeignKey(k, e))).ToList();
            if (removed.Count == 0 && added.Count == 0) return;
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                UpdateSqliteForeignKeys(transaction, type, actual.Except(removed).Concat(added).ToList());
                return;
            }
            var connection = transaction?.Connection ?? Connection;
            string table = Builder.GetTableName(type, true);
            // Reject unsupported provider actions before dropping any existing constraint.
            var clauses = added.Select(BuildForeignKeyClause).ToList();
            using IDbTransaction? owned = transaction == null &&
                Builder.Settings.Type is DatabaseType.MSSQL or DatabaseType.PostgreSQL
                ? connection.BeginTransaction() : null;
            transaction ??= owned;
            // Validate existing rows before removing an old constraint, including on engines with implicit DDL commits.
            foreach (var key in added)
            {
                string parent = QuoteForeignKeyTable(key);
                string joins = string.Join(" AND ", key.Columns.Select(c =>
                    $"c.{QuoteSchemaIdentifier(c.Column)} = p.{QuoteSchemaIdentifier(c.ReferencedColumn)}"));
                string present = string.Join(" AND ", key.Columns.Select(c => $"c.{QuoteSchemaIdentifier(c.Column)} IS NOT NULL"));
                string query = $"SELECT COUNT(*) FROM {table} c LEFT JOIN {parent} p ON {joins} WHERE {present} AND p.{QuoteSchemaIdentifier(key.Columns[0].ReferencedColumn)} IS NULL";
                if (Convert.ToInt64(ExecuteScalarInternal(connection, transaction, query, typeof(long))) != 0)
                    throw new InvalidOperationException($"Cannot add foreign key on {table}: existing rows reference missing {key.ReferencedTable} records.");
            }
            foreach (var key in removed)
            {
                string drop = Builder.Settings.Type is DatabaseType.MySQL or DatabaseType.MariaDB ? "DROP FOREIGN KEY " : "DROP CONSTRAINT ";
                ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, $"ALTER TABLE {table} {drop}{QuoteSchemaIdentifier(key.Name)}");
            }
            foreach (var clause in clauses)
                ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, $"ALTER TABLE {table} ADD {clause}");
            owned?.Commit();
        }

        private ForeignKeyAction NormalizeForeignKeyAction(ForeignKeyAction action)
        {
            if (action == ForeignKeyAction.None) return ForeignKeyAction.NoAction;
            // These providers report the default NO ACTION behavior as RESTRICT.
            if (action == ForeignKeyAction.Restrict &&
                Builder.Settings.Type is DatabaseType.MySQL or DatabaseType.MariaDB or DatabaseType.MSSQL or DatabaseType.SapHana)
                return ForeignKeyAction.NoAction;
            return action;
        }

        /// <summary>
        /// Create indexes.
        /// </summary>
        /// <param name="type">Table where indexes are search.</param>
        /// <param name="tableItem">The table creation plan to receive the generated index statements.</param>
        /// <param name="tableName">The database table name.</param>
        /// <param name="sb">The buffer used to assemble each index statement.</param>
        private void CreateIndex(Type type, GXTableCreateQuery tableItem, string tableName, StringBuilder sb)
        {
            string name;
            Dictionary<string, GXSerializedItem> list = GXSqlBuilder.GetProperties(type);
            foreach (var it in list)
            {
                if ((it.Value.Attributes & Attributes.Index) != 0)
                {
                    //Oracle will fail if we try to create index for primary key. Skip it.
                    if (!(Builder.Settings.Type == DatabaseType.Oracle &&
                        (it.Value.Attributes & (Attributes.AutoIncrement | Attributes.PrimaryKey)) != 0))
                    {
                        IndexAttribute index = GXInternal.GetAttribute<IndexAttribute>(it.Value.Target);
                        sb.Length = 0;
                        sb.Append("CREATE ");
                        if (index.Unique)
                        {
                            sb.Append("UNIQUE ");
                        }
                        if (index.Clustered && (
                            Builder.Settings.Type == DatabaseType.MSSQL ||
                            Builder.Settings.Type == DatabaseType.MySQL))
                        {
                            //Create clustered index for MSSQL or MySQL.
                            sb.Append("CLUSTERED ");
                        }
                        sb.Append("INDEX ");
                        if (Builder.Settings.UpperCase)
                        {
                            name = it.Key.ToUpper();
                            sb.Append(GetIndexName(tableName, name).ToUpper());
                        }
                        else
                        {
                            name = it.Key;
                            sb.Append(GetIndexName(tableName, name));
                        }
                        //Index name.
                        sb.Append(" ON ");
                        sb.Append(Builder.GetTableName(type, true));
                        sb.Append("(");
                        sb.Append(GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, name, null));
                        if (index.Descend)
                        {
                            sb.Append(" DESC");
                        }
                        sb.Append(")");
                        if (index.IncludeOnlyNull && index.ExcludeNull)
                        {
                            throw new Exception("IncludeOnlyNull and ExcludeNull are both set.");
                        }
                        else if (index.IncludeOnlyNull)
                        {
                            sb.Append(" WHERE( ");
                            sb.Append(GXDbHelpers.AddQuotes(name,
                                Builder.Settings.DataQuotaReplacement,
                                Builder.Settings.ColumnNameQuoteCharacter));
                            sb.Append(" IS NULL)");
                        }
                        else if (index.ExcludeNull)
                        {
                            sb.Append(" WHERE( ");
                            sb.Append(GXDbHelpers.AddQuotes(name,
                                Builder.Settings.DataQuotaReplacement,
                                Builder.Settings.ColumnNameQuoteCharacter));
                            sb.Append(" IS NOT NULL)");
                        }
                        tableItem.Queries.Add(sb.ToString());
                    }
                }
            }
            if (Builder.Settings.Type != DatabaseType.Oracle)
            {
                IndexCollectionAttribute coll = GXInternal.GetAttribute<IndexCollectionAttribute>(type);
                if (coll != null)
                {
                    bool first = true;
                    sb.Length = 0;
                    sb.Append("CREATE ");
                    if (coll.Unique)
                    {
                        sb.Append("UNIQUE ");
                    }
                    if (coll.Clustered &&
                        (Builder.Settings.Type == DatabaseType.MSSQL))
                    {
                        //Create clustered index for MSSQL or MySQL.
                        sb.Append("CLUSTERED ");
                    }
                    sb.Append("INDEX ");
                    name = coll.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        foreach (string it2 in coll.Columns)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                name += "_";
                            }
                            name += it2;
                        }
                    }
                    name = GetIndexName(tableName, name);
                    sb.Append(name);
                    sb.Append(" ON ");
                    sb.Append(Builder.GetTableName(type, true));
                    sb.Append(" (");
                    first = true;
                    foreach (string it2 in coll.Columns)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        name = null;
                        //Find correct name.
                        foreach (var it3 in list)
                        {
                            if (((PropertyInfo)it3.Value.Target).Name == it2)
                            {
                                name = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, it3.Key, null);
                                sb.Append(name);
                                break;
                            }
                        }
                        if (name == null)
                        {
                            throw new Exception("Unknown index name: " + it2);
                        }
                    }
                    sb.Append(")");
                    tableItem.Queries.Add(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Get all tables that need to create in relation tree.
        /// </summary>
        /// <param name="create">Indicates whether to create the tables.</param>
        /// <param name="type">The type of the table.</param>
        /// <param name="parent">The parent table in the relation tree.</param>
        /// <param name="tables">Dictionary of tables.</param>
        /// <param name="tablesCreationQueries">Dictionary of table creation queries.</param>
        /// <param name="isForeignKey">Is FK to relation. This is used when tables are created or dropped.</param>
        GXTableCreateQuery GetCreateTableQueries(bool create, Type type, GXTableCreateQuery parent,
            Dictionary<Type, GXSerializedItem> tables,
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries, bool isForeignKey)
        {
            GXTableCreateQuery t, m;
            GXTableCreateQuery tableItem;
            Type tp;
            //Check that table is not created yet.
            bool first = !tablesCreationQueries.ContainsKey(type);
            string str, name;
            string tableName = Builder.GetTableName(type, true);
            Dictionary<Type, GXSerializedItem> relationTables = null;
            if (!first)
            {
                tableItem = tablesCreationQueries[type];
                if (isForeignKey && parent != null)
                {
                    parent.AddDependency(tableItem);
                }
            }
            else
            {
                tableItem = new GXTableCreateQuery
                {
                    Table = type
                };
                StringBuilder sb = new();
                if (isForeignKey && parent != null)
                {
                    parent.AddDependency(tableItem);
                }
                //Remove table if exists.
                if (create || (!create && tables.ContainsKey(type)))
                {
                    tablesCreationQueries.Add(type, tableItem);
                }
                if (tables.ContainsKey(type) || tables.ContainsKey(type.BaseType))
                {
                    if (tables.ContainsKey(type))
                    {
                        tables.Remove(type);
                    }
                    else
                    {
                        tables.Remove(type.BaseType);
                    }
                    name = Builder.GetTableName(type, true);
                    if (!create)//Drop table.
                    {
                        sb.Append("DROP TABLE ");
                        sb.Append(name);
                        tableItem.Queries.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else//Create table.
                    {
                        sb.Append("CREATE TABLE ");
                        sb.Append(name);
                        sb.Append('(');
                        //Get relation tables and remove them.
                        if (GXDbHelpers.IsSharedTable(type))
                        {
                            relationTables = new Dictionary<Type, GXSerializedItem>();
                            for (int pos = 0; pos != tables.Count; ++pos)
                            {
                                KeyValuePair<Type, GXSerializedItem> it = tables.ElementAt(pos);
                                if (GXDbHelpers.IsSharedTable(it.Key) && it.Key.BaseType.IsAssignableFrom(type.BaseType))
                                {
                                    tables.Remove(it.Key);
                                    relationTables.Add(it.Key, it.Value);
                                    --pos;
                                }
                            }
                        }
                        else
                        {
                            relationTables = [];
                            for (int pos = 0; pos != tables.Count; ++pos)
                            {
                                KeyValuePair<Type, GXSerializedItem> it = tables.ElementAt(pos);
                                if (type.IsAssignableFrom(it.Key))
                                {
                                    tables.Remove(it.Key);
                                    relationTables.Add(it.Key, it.Value);
                                    --pos;
                                }
                            }
                        }
                    }
                    Type original = type;
                    List<string> serialized = [];
                    StringBuilder fkStr = new StringBuilder();
                    do
                    {
                        foreach (var it in GXSqlBuilder.GetProperties(type))
                        {
                            if (serialized.Contains(it.Key))
                            {
                                continue;
                            }
                            serialized.Add(it.Key);
                            tp = it.Value.Type;
                            //Create relations.
                            if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
                            {
                                if (it.Value.Relation.RelationType == RelationType.OneToMany)
                                {
                                    if (create)
                                    {
                                        t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, null, tables,
                                            tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);
                                    }
                                    else
                                    {
                                        t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, tableItem, tables,
                                            tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);
                                    }
                                    continue;
                                }
                                else if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, null, tables,
                                            tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);

                                    m = GetCreateTableQueries(create, it.Value.Relation.RelationMapTable.Relation.PrimaryTable, null, tables,
                                            tablesCreationQueries, it.Value.Relation.RelationMapTable.Relation.RelationType != RelationType.Relation);
                                    if (!create)//Drop table.
                                    {
                                        tableItem.Dependencies.Add(m);
                                    }
                                    continue;
                                }
                                else
                                {
                                    //If 1:1
                                    if (it.Value.Relation.RelationMapTable == null)
                                    {
                                        if (create)
                                        {
                                            t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, tableItem, tables,
                                                tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);
                                        }
                                        else
                                        {
                                            t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, null, tables,
                                                 tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);
                                        }
                                    }
                                    else //If relation map table.
                                    {
                                        t = GetCreateTableQueries(create, it.Value.Relation.ForeignTable, null, tables,
                                            tablesCreationQueries, it.Value.Relation.RelationType != RelationType.Relation);
                                    }
                                }
                                tp = it.Value.Relation.ForeignId.Type;
                                //If array.
                                if (tp != typeof(string) && tp != typeof(byte[]) && tp != typeof(char[]) &&
                                    (tp.IsArray || typeof(IList).IsAssignableFrom(tp)))
                                {
                                    tp = GXInternal.GetPropertyType(tp);
                                }
                            }
                            if (!create)
                            {
                                continue;
                            }
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                sb.Append(", ");
                            }
                            if (Builder.Settings.UpperCase)
                            {
                                name = it.Key.ToUpper();
                            }
                            else
                            {
                                name = it.Key;
                            }
                            sb.Append(GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, name, null));
                            sb.Append(" ");
                            if (!((it.Value.Attributes & (Attributes.AutoIncrement)) != 0 &&
                                (Builder.Settings.Type == DatabaseType.SqLite)))
                            {
                                try
                                {
                                    str = null;
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                    if ((it.Value.Attributes & (Attributes.PrimaryKey | Attributes.ForeignKey)) != 0)
                                    {
                                        if (it.Value.Relation == null)
                                        {
                                            tp = it.Value.Type;
                                        }
                                        else if ((it.Value.Relation.ForeignId.Attributes & Attributes.AutoIncrement) == 0)
                                        {
                                            str = Builder.GetDataBaseType(tp, it.Value.Relation.ForeignId.Target);
                                        }
                                        else
                                        {
                                            tp = typeof(int);
                                        }
                                    }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                    if ((it.Value.Attributes & Attributes.AutoIncrement) != 0 &&
                                        Builder.Settings.Type == DatabaseType.PostgreSQL &&
                                        tp == typeof(ulong))
                                    {
                                        tp = typeof(long);
                                    }
                                    if (str == null)
                                    {
                                        str = Builder.GetDataBaseType(tp, it.Value.Target);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Failed to create table '" + tableName + "'." + Environment.NewLine + ex.Message);
                                }
                                System.Diagnostics.Debug.Assert(str.Length != 0);
                                sb.Append(str);
                            }
                            //SQLite allows only int as auto increment type.
                            else if (Builder.Settings.Type == DatabaseType.SqLite)
                            {
                                str = Builder.GetDataBaseType(typeof(int), it.Value);
                                System.Diagnostics.Debug.Assert(str.Length != 0);
                                sb.Append(str);
                            }
                            bool required = false;
                            if (it.Value.Target is PropertyInfo)
                            {
                                DataMemberAttribute[] attr = (DataMemberAttribute[])(it.Value.Target as PropertyInfo).GetCustomAttributes(typeof(DataMemberAttribute), true);
                                if (attr.Length != 0)
                                {
                                    required = attr[0].IsRequired;
                                }
                            }
                            if (!required && it.Value.Type.IsGenericType && it.Value.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                required = (it.Value.Attributes & Attributes.AllowNull) == 0;
                            }
                            if (!(Builder.Settings.Type == DatabaseType.Oracle &&
                                (it.Value.DefaultValue != null || (it.Value.Attributes & (Attributes.AutoIncrement | Attributes.PrimaryKey)) != 0)))
                            {
                                if ((it.Value.Attributes & Attributes.PrimaryKey) == 0 &&
                                    it.Value.DefaultValue == null &&
                                    (!required ||
                                    it.Value.Type.IsArray))
                                {
                                    //If nullable.
                                    sb.Append(" NULL ");
                                }
                                else
                                {
                                    sb.Append(" NOT NULL ");
                                }
                            }
                            //If field is marked as auto increment or primary key.
                            if ((it.Value.Attributes & (Attributes.AutoIncrement | Attributes.PrimaryKey)) != 0)
                            {
                                if (it.Value.DefaultValue != null)
                                {
                                    //Set default values.
                                    GetDefaultValue(sb, it.Value.DefaultValue, it.Value.Type);
                                }
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                if (Builder.Settings.Type == DatabaseType.Oracle ||
                                    Builder.Settings.Type == DatabaseType.SapHana)
                                {
                                    if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                                    {
                                        sb.Append(Builder.Settings.AutoIncrementDefinition);
                                    }
                                    sb.Append(" PRIMARY KEY ");
                                }
                                else
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                {
                                    sb.Append(" PRIMARY KEY ");
                                    if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                                    {
                                        sb.Append(Builder.Settings.AutoIncrementDefinition);
                                    }
                                }
                                if (it.Value.DefaultValue == null && Builder.Settings.Type == DatabaseType.Oracle)
                                {
                                    if ((it.Value.Attributes & Attributes.PrimaryKey) == 0 && (!required || (it.Value.Type.IsGenericType && it.Value.Type.GetGenericTypeDefinition() == typeof(Nullable<>)) || it.Value.Type.IsArray))
                                    {
                                        //If nullable.
                                        sb.Append(" NULL ");
                                    }
                                    else
                                    {
                                        sb.Append(" NOT NULL ");
                                    }
                                }
                            }
                            else if (it.Value.DefaultValue != null)
                            {
                                //Set default values.
                                GetDefaultValue(sb, it.Value.DefaultValue, it.Value.Type);
                            }
                            //MySQL requires the complete column definition when
                            // setting a column comment. Add it while the CREATE
                            // TABLE definition is being generated.
                            if (Builder.Settings.Type == DatabaseType.MySQL ||
                                Builder.Settings.Type == DatabaseType.MariaDB)
                            {
                                DescriptionAttribute description =
                                    (it.Value.Target as MemberInfo)?
                                    .GetCustomAttribute<DescriptionAttribute>(true);
                                if (!string.IsNullOrEmpty(description?.Description))
                                {
                                    sb.Append(" COMMENT '");
                                    sb.Append(description.Description.Replace("'", "''"));
                                    sb.Append("' ");
                                }
                            }
                            if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type &&
                                    it.Value.Relation.RelationType == RelationType.OneToOne)
                            {
                                fkStr.Append(", ");
                                string pk;
                                if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    GXSerializedItem u = it.Value.Relation.RelationMapTable.Relation.PrimaryId;
                                    pk = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, u.Target, null);
                                }
                                else
                                {
                                    pk = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, it.Value.Relation.ForeignId.Target, null);
                                }
                                string table = Builder.GetTableName(it.Value.Relation.PrimaryTable, false);
                                name = it.Key;
                                if (pk == null)
                                {
                                    throw new ArgumentOutOfRangeException(string.Format("Table {0} do not have primary key.",
                                            table));
                                }
                                if (Builder.Settings.UpperCase)
                                {
                                    table = table.ToUpper();
                                    name = name.ToUpper();
                                    pk = pk.ToUpper();
                                }
                                string table2;
                                if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    table2 = Builder.GetTableName(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, false);
                                }
                                else
                                {
                                    table2 = Builder.GetTableName(it.Value.Relation.ForeignTable, false);
                                }
                                ForeignKeyAttribute fk = ((ForeignKeyAttribute[])(it.Value.Target as PropertyInfo).GetCustomAttributes(typeof(ForeignKeyAttribute), true))[0];

                                //Name is generated automatically at the moment. Use CONSTRAINT to give name to the Foreign key.
                                fkStr.Append(" FOREIGN KEY (");
                                fkStr.Append(GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, name, null));
                                fkStr.Append(") REFERENCES ");
                                fkStr.Append(GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, table2, null));
                                fkStr.Append("(");
                                fkStr.Append(pk);
                                fkStr.Append(")");
                                switch (fk.OnDelete)
                                {
                                    case ForeignKeyDelete.None:
                                        //Foreign key on delete is not used.
                                        break;
                                    case ForeignKeyDelete.Cascade:
                                        fkStr.Append(" ON DELETE CASCADE");
                                        break;
                                    case ForeignKeyDelete.Empty:
                                        //Emit will cause this.
                                        break;
                                    case ForeignKeyDelete.Restrict:
                                        //ON DELETE NO ACTION will also work.
                                        fkStr.Append(" ON DELETE RESTRICT");
                                        break;
                                    default:
                                        break;
                                }
                                switch (fk.OnUpdate)
                                {
                                    case ForeignKeyUpdate.None:
                                        //Foreign key on update is not used.
                                        break;
                                    case ForeignKeyUpdate.Cascade:
                                        fkStr.Append(" ON UPDATE CASCADE");
                                        break;
                                    case ForeignKeyUpdate.Reject:
                                        //Emit will cause this.
                                        break;
                                    case ForeignKeyUpdate.Restrict:
                                        //ON UPDATE NO ACTION will also work.
                                        fkStr.Append(" ON UPDATE RESTRICT");
                                        break;
                                    case ForeignKeyUpdate.Null:
                                        fkStr.Append(" ON UPDATE SET NULL");
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        if (relationTables != null && relationTables.Count != 0)
                        {
                            KeyValuePair<Type, GXSerializedItem> it = relationTables.ElementAt(0);
                            type = it.Key;
                            relationTables.Remove(type);
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (true);
                    type = original;
                    sb.Append(fkStr);
                    if (create)
                    {
                        sb.Append(')');
                        tableItem.Queries.Add(sb.ToString());

                        DescriptionAttribute tableDescription =
                            type.GetCustomAttribute<DescriptionAttribute>(true);
                        if (!string.IsNullOrEmpty(tableDescription?.Description))
                        {
                            string commentQuery = Builder.Settings.GetCommentQuery(
                                Builder.Database, tableName, null,
                                tableDescription.Description);
                            if (!string.IsNullOrEmpty(commentQuery))
                            {
                                tableItem.Queries.Add(commentQuery);
                            }
                        }
                        foreach (var property in GXSqlBuilder.GetProperties(type))
                        {
                            if (property.Value.Attributes.HasFlag(Attributes.ForeignKey))
                            {
                                continue;
                            }
                            DescriptionAttribute columnDescription =
                                (property.Value.Target as MemberInfo)?
                                .GetCustomAttribute<DescriptionAttribute>(true);
                            if (string.IsNullOrEmpty(columnDescription?.Description))
                            {
                                continue;
                            }

                            string commentQuery = Builder.Settings.GetCommentQuery(
                                Builder.Database, tableName,
                                Builder.Settings.EscapeIdentifier(null, property.Key),
                                columnDescription.Description);
                            if (!string.IsNullOrEmpty(commentQuery))
                            {
                                tableItem.Queries.Add(commentQuery);
                            }
                        }
                    }
                    if (create)
                    {
                        //Create auto increments that are not supported by DB.
                        foreach (var it in GXSqlBuilder.GetProperties(type))
                        {
                            //If field is marked as an auto increment.
                            if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                            {
                                string[]? arr = Builder.Settings.CreateAutoIncrement(tableName, it.Key);
                                if (arr != null)
                                {
                                    foreach (string it2 in arr)
                                    {
                                        try
                                        {
                                            tableItem.Queries.Add(it2);
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine(ex.Message);
                                        }
                                    }
                                }
                            }
                        }
                        //Create indexes.
                        CreateIndex(type, tableItem, tableName, sb);
                    }
                }
            }
            return tableItem;
        }

        private void GetDefaultValue(StringBuilder sb, object value, Type columnType)
        {
            string tmp = Builder.Settings.GetColumnDefaultValue(value, columnType);
            if (!string.IsNullOrEmpty(tmp))
            {
                sb.Append(" DEFAULT");
                if (Builder.Settings.Type == DatabaseType.SqLite)
                {
                    sb.Append('(');
                    sb.Append(tmp);
                    sb.Append(')');
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(tmp);
                }
            }
        }

        private Type GetColumnDataType(Type tableType, GXSerializedItem item)
        {
            Type valueType = item.Type;
            if (item.Relation != null &&
                item.Relation.RelationType == RelationType.OneToOne &&
                item.Relation.ForeignTable != tableType)
            {
                valueType = item.Relation.ForeignId.Type;
            }
            valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            if (valueType.IsEnum)
            {
                return Builder.Settings.UseEnumStringValue
                    ? typeof(string)
                    : Enum.GetUnderlyingType(valueType);
            }
            if (valueType == typeof(Type) || valueType == typeof(char[]) ||
                valueType == typeof(object))
            {
                return typeof(string);
            }
            if (valueType.IsArray && valueType != typeof(byte[]))
            {
                return GXInternal.GetPropertyType(valueType);
            }
            return valueType;
        }

        private static bool IsSameDatabaseType(string databaseType, int length,
            string expectedType, DatabaseType provider)
        {
            string Normalize(string value)
            {
                string normalized = value.Replace(" ", "").ToUpperInvariant();
                // MySQL omits zero fractional-second precision in column metadata.
                // Other providers have different default temporal precisions.
                if ((provider == DatabaseType.MySQL || provider == DatabaseType.MariaDB) &&
                    (normalized == "DATETIME(0)" || normalized == "TIMESTAMP(0)" || normalized == "TIME(0)"))
                {
                    return normalized.Substring(0, normalized.Length - 3);
                }
                return normalized;
            }
            string actual = Normalize(databaseType);
            string expected = Normalize(expectedType);
            if (actual == expected)
            {
                return true;
            }
            if (length > 0)
            {
                actual += "(" + length + ")";
            }
            else if (length == -1)
            {
                actual += "(MAX)";
            }
            return actual == expected;
        }

        private void ChangeColumnType(IDbConnection connection,
            IDbTransaction? transaction,
            string tableName,
            string columnName,
            Type oldType,
            Type newType,
            GXSerializedItem item)
        {
            columnName = Builder.Settings.EscapeIdentifier(null, columnName);
            string temporaryName = columnName + "_GXConverted";
            HashSet<string> columns = new(Builder.GetColumns(this, tableName,
                connection, transaction, OnSqlExecuted),
                StringComparer.OrdinalIgnoreCase);
            for (int index = 2; columns.Contains(temporaryName); ++index)
            {
                temporaryName = columnName + "_GXConverted" + index;
            }

            string quotedTableName = tableName;
            string quotedColumnName = columnName;
            string quotedTemporaryName = temporaryName;
            if (Builder.Settings.Type == DatabaseType.Oracle ||
                Builder.Settings.Type == DatabaseType.SapHana ||
                Builder.Settings.Type == DatabaseType.DB2)
            {
                quotedTableName = quotedTableName.ToUpperInvariant();
                quotedColumnName = quotedColumnName.ToUpperInvariant();
                quotedTemporaryName = quotedTemporaryName.ToUpperInvariant();
            }

            string quotedTable = GXDbHelpers.AddQuotes(quotedTableName,
                Builder.Settings.DataQuotaReplacement,
                Builder.Settings.TableNameQuoteCharacter);
            string quotedColumn = GXDbHelpers.AddQuotes(quotedColumnName,
                Builder.Settings.DataQuotaReplacement,
                Builder.Settings.ColumnNameQuoteCharacter);
            string quotedTemporary = GXDbHelpers.AddQuotes(quotedTemporaryName,
                Builder.Settings.DataQuotaReplacement,
                Builder.Settings.ColumnNameQuoteCharacter);

            string convertedType = Builder.GetDataBaseType(newType, item.Relation != null &&
                item.Relation.RelationType == RelationType.OneToOne
                ? item.Relation
                : item.Target);
            string addConvertedColumn = Builder.Settings.Type == DatabaseType.SapHana
                ? $"ALTER TABLE {quotedTable} ADD ({quotedTemporary} {convertedType} NULL)"
                : $"ALTER TABLE {quotedTable} ADD {quotedTemporary} {convertedType} NULL";
            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, addConvertedColumn);
            List<object> values = [];
            string selectedColumn = Builder.Settings.Type switch
            {
                DatabaseType.SapHana => $"TO_NVARCHAR({quotedColumn})",
                DatabaseType.DB2 => $"CAST({quotedColumn} AS VARCHAR(32672))",
                _ => quotedColumn
            };
            string select = $"SELECT DISTINCT {selectedColumn} FROM {quotedTable} " +
                $"WHERE {quotedColumn} IS NOT NULL";
            try
            {
                using IDbCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandText = select;
                using IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    values.Add(reader.GetValue(0));
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, select);
            }

            foreach (object oldValue in values)
            {
                GXColumnValueConvertingEventArgs args = new()
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    OldType = oldType,
                    NewType = newType,
                    Value = oldValue
                };
                ColumnValueConverting?.Invoke(this, args);
                object newValue = args.IsConverted
                    ? args.Value
                    : Builder.Settings.ChangeType(args.Value, newType);
                UpdateConvertedColumnValue(connection, transaction, quotedTable,
                    quotedColumn, quotedTemporary, oldValue, newValue);
            }

            string dropOldColumn = Builder.Settings.Type == DatabaseType.SapHana
                ? $"ALTER TABLE {quotedTable} DROP ({quotedColumn})"
                : $"ALTER TABLE {quotedTable} DROP COLUMN {quotedColumn}";
            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, dropOldColumn);
            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted,
                Builder.Settings.GetRenameTableColumnQuery(tableName, temporaryName, columnName));
            if (Builder.Settings.Type == DatabaseType.DB2)
            {
                ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted,
                    $"CALL SYSPROC.ADMIN_CMD('REORG TABLE {tableName}')");
            }
        }

        internal static string[] ExecuteQuery(IDbConnection connection,
            IDbTransaction? transaction,
        string query, int index = 0)
        {
            List<string> list = new List<string>();
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    com.Transaction = transaction;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.GetString(index));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return list.ToArray();
        }

        internal static void ExecuteQuery(IDbConnection connection, IDbTransaction transaction,
           string query, int count, List<object[]> list)
        {
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    com.Transaction = transaction;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] values = new object[count];
                            reader.GetValues(values);
                            list.Add(values);
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        /// <summary>
        /// Execute given SQL query that does not return any result.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="connection">Used DB connection.</param>
        /// <param name="transaction">Used transaction.</param>
        /// <param name="sql">SQL executed event handler.</param>
        /// <param name="query">Query to execute.</param>
        internal static int ExecuteNonQuery(
            object sender,
            IDbConnection connection,
            IDbTransaction? transaction,
            EventHandler<GXSqlExecutedEventArgs>? sql,
            string query)
        {
            int count = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.Transaction = transaction;
                    com.CommandText = query;
                    count = com.ExecuteNonQuery();
                    if (sender is GXSchemaManager && connection is DbConnection schemaConnection)
                        Gurux.Service.DB.GXSchemaCache.ForConnection(schemaConnection).Invalidate(transaction);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                throw GXDatabaseException.Create(ex, query);
            }
            if (sql != null)
            {
                sw.Stop();
                sql(sender, new GXSqlExecutedEventArgs()
                {
                    Sql = query,
                    Elapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)
                });
            }
            return count;
        }

        /// <summary>
        /// Change database.
        /// </summary>
        /// <param name="databaseName">Name of the database to switch to.</param>
        public void ChangeDatabase(string databaseName)
        {
            Connection = Builder.ChangeDatabaseAsync(Connection, databaseName).Result;
        }

        /// <summary>
        /// Change database asynchronously.
        /// </summary>
        /// <param name="databaseName">Name of the database to switch to.</param>
        public async Task ChangeDatabaseAsync(string databaseName)
        {
            Connection = await Builder.ChangeDatabaseAsync(Connection, databaseName);
        }

        private void UpdateConvertedColumnValue(IDbConnection connection,
            IDbTransaction transaction, string quotedTable, string quotedColumn,
            string quotedTemporary, object oldValue, object newValue)
        {
            string prefix = Builder.Settings.Type == DatabaseType.Oracle ||
                Builder.Settings.Type == DatabaseType.SapHana ? ":" : "@";
            string comparedColumn = Builder.Settings.Type switch
            {
                DatabaseType.SapHana => $"TO_NVARCHAR({quotedColumn})",
                DatabaseType.DB2 => $"CAST({quotedColumn} AS VARCHAR(32672))",
                _ => quotedColumn
            };
            string query = $"UPDATE {quotedTable} SET {quotedTemporary} = {prefix}newValue " +
                $"WHERE {comparedColumn} = {prefix}oldValue";
            try
            {
                using IDbCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandText = query;
                if (newValue is Guid guid &&
                    (Builder.Settings.Type == DatabaseType.MariaDB ||
                    Builder.Settings.Type == DatabaseType.MySQL))
                {
                    byte[] bytes = guid.ToByteArray();
                    newValue = bytes;
                }
                else if (newValue is Guid sqliteGuid && Builder.Settings.Type == DatabaseType.SqLite)
                {
                    // Match the lowercase text used by ORM inserts. The provider's Guid binding
                    // otherwise emits uppercase text, which fails SQLite foreign key equality.
                    newValue = sqliteGuid.ToString();
                }
                else if (newValue is Guid oracleGuid &&
                    Builder.Settings.Type == DatabaseType.Oracle)
                {
                    newValue = oracleGuid.ToByteArray();
                }
                else if (newValue is Guid db2Guid &&
                    Builder.Settings.Type == DatabaseType.DB2)
                {
                    newValue = db2Guid.ToByteArray();
                }
                else if (newValue is Guid hanaGuid &&
                    Builder.Settings.Type == DatabaseType.SapHana)
                {
                    newValue = hanaGuid.ToByteArray();
                }
                if (newValue is bool boolValue &&
                    Builder.Settings.Type == DatabaseType.Oracle)
                {
                    newValue = boolValue ? 1 : 0;
                }
                newValue = newValue switch
                {
                    byte value when Builder.Settings.Type == DatabaseType.DB2 => (short)value,
                    bool value when Builder.Settings.Type == DatabaseType.DB2 => value ? 1 : 0,
                    sbyte value => (short)value,
                    ushort value => (int)value,
                    uint value => (long)value,
                    ulong value => (decimal)value,
                    _ => newValue
                };
                IDbDataParameter parameter = command.CreateParameter();
                parameter.ParameterName = "newValue";
                parameter.Value = newValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
                parameter = command.CreateParameter();
                parameter.ParameterName = "oldValue";
                parameter.Value = oldValue;
                command.Parameters.Add(parameter);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <typeparam name="T">Table type to drop.</typeparam>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable<T>(bool relations)
        {
            DropTable(typeof(T), relations);
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <typeparam name="T">Table type to drop.</typeparam>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable<T>(IDbTransaction? transaction, bool relations)
        {
            DropTable(transaction, typeof(T), relations);
        }

        /// <summary>
        /// Drop database.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        public void DropDatabase(string databaseName)
        {
            DropDatabase(null, databaseName);
        }

        /// <summary>
        /// Check if database exists.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        /// <returns>True if the database exists, otherwise false.</returns>
        public bool DatabaseExists(string databaseName)
        {
            return DatabaseExists(null, databaseName);
        }

        /// <summary>
        /// Check if database exists.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>True if the database exists, otherwise false.</returns>
        public bool DatabaseExists(IDbTransaction? transaction, string databaseName)
        {
            databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
            string[] databases = GetDatabases(transaction);
            return databases.Contains(databaseName);
        }

        /// <summary>
        /// Drop database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        public void DropDatabase(IDbTransaction? transaction, string databaseName)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Connection.Close();
                Connection.Dispose();
                File.Delete(databaseName + ".db");
                return;
            }
            string query;
            databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
            if (Builder.Settings.Type == DatabaseType.DB2)
            {
                DropAllTables(transaction, databaseName);
                query = "DROP SCHEMA " + databaseName + " RESTRICT";
            }
            else if (Builder.Settings.Type == DatabaseType.SapHana)
            {
                query = "DROP SCHEMA " + databaseName + " CASCADE";
            }
            else if (Builder.Settings.Type == DatabaseType.Oracle)
            {
                query = "DROP USER " + databaseName + " CASCADE";
            }
            else if (Builder.Settings.Type == DatabaseType.PostgreSQL)
            {
                string old = Builder.Database;
                if (string.Equals(old, databaseName, StringComparison.OrdinalIgnoreCase))
                {
                    Builder.ChangeDatabase(Connection, "postgres");
                }
                string db = databaseName.Replace("'", "''");
                ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted,
                    $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{db}' AND pid <> pg_backend_pid()");
                query = "DROP DATABASE " + databaseName;
            }
            else
            {
                query = "DROP DATABASE " + databaseName;
                if (Builder.Settings.Type == DatabaseType.MSSQL)
                {
                    Builder.ChangeDatabase(Connection, GetDatabases().First());
                }
            }
            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
        }

        /// <summary>
        /// Get list of databases.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Array of database names.</returns>
        public string[] GetDatabases(IDbTransaction? transaction = null)
        {
            return Builder.GetDatabases(this, Connection, transaction, OnSqlExecuted);
        }

        /// <summary>
        /// Drop all database tables.
        /// </summary>
        public void DropAllTables(IDbTransaction? transaction = null, string? databaseName = null)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite &&
                transaction != null)
            {
                throw new ArgumentException("SQLite does not support transactions for dropping all tables.");
            }
            string query, tableName, name;
            string[] tables;
            if (databaseName == null)
            {
                tables = GetTables(transaction);
            }
            else
            {
                tables = GetTables(transaction, databaseName);
            }
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                query = $"PRAGMA foreign_keys = OFF";
                ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
            }
            try
            {
                foreach (var table in tables)
                {
                    if (Builder.Settings.Type != DatabaseType.SqLite &&
                        Builder.Settings.Type != DatabaseType.DB2 &&
                        Builder.Settings.Type != DatabaseType.SapHana)
                    {
                        List<(string ConstraintName, string TableName)> keys = GetForeignKeys(transaction, table);
                        foreach (var k in keys)
                        {
                            tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, k.TableName, null);
                            name = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, k.ConstraintName, null);
                            if (Builder.Settings.Type == DatabaseType.Oracle)
                            {
                                if (!string.IsNullOrEmpty(Builder.Database))
                                {
                                    query = $"ALTER TABLE {Builder.Database}.{tableName} DROP CONSTRAINT {name}";
                                }
                                else
                                {
                                    query = $"ALTER TABLE {tableName} DROP CONSTRAINT {name}";
                                }
                            }
                            else if (Builder.Settings.Type == DatabaseType.MySQL)
                            {
                                query = $"ALTER TABLE {tableName} DROP FOREIGN KEY {name}";
                            }
                            else
                            {
                                query = $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {name}";
                            }
                            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
                        }
                    }
                    tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, table, null);
                    if (Builder.Settings.Type == DatabaseType.DB2 && !string.IsNullOrEmpty(Builder.Database))
                    {
                        query = $"DROP TABLE {Builder.Database}.{tableName}";
                    }
                    else if (Builder.Settings.Type == DatabaseType.SqLite ||
                        Builder.Settings.Type == DatabaseType.Oracle ||
                        Builder.Settings.Type == DatabaseType.MSSQL)
                    {
                        query = $"DROP TABLE {tableName}";
                    }
                    else
                    {
                        query = $"DROP TABLE {tableName} CASCADE";
                    }
                    ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
                }
            }
            finally
            {
                if (Builder.Settings.Type == DatabaseType.SqLite)
                {
                    query = $"PRAGMA foreign_keys = ON";
                    ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
                }
            }
        }


        /// <summary>
        /// Force to drop all relation tables.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        public void ForceDropTable<T>(IDbTransaction? transaction = null)
        {
            ForceDropTable(transaction, typeof(T));
        }

        /// <summary>
        /// Force to drop all relation tables.
        /// </summary>
        public void ForceDropTable(Type type)
        {
            ForceDropTable(null, type);
        }

        /// <summary>
        /// Force to drop all relation tables.
        /// </summary>
        public void ForceDropTable(IDbTransaction? transaction, Type type)
        {
            List<Type> failed = new List<Type>();
            Type[] list = GetRelationTables(transaction, type);
            list = list.Reverse().ToArray();
            foreach (Type it in list)
            {
                try
                {
                    try
                    {
                        string tableName = Builder.GetTableName(it, false);
                        tableName = GetTableName(tableName);
                        List<(string ConstraintName, string TableName)> keys = GetForeignKeys(transaction, tableName);
                        foreach (var k in keys)
                        {
                            tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, k.TableName, null);
                            string name = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, k.ConstraintName, null);
                            string query = $"ALTER TABLE {tableName} DROP CONSTRAINT {name}";
                            ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
                        }
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails.
                    }
                    DropTable(transaction, it, false);
                }
                catch (Exception)
                {
                    //It's OK if this fails.
                    failed.Add(it);
                    continue;
                }
            }
            foreach (Type it in failed)
            {
                try
                {
                    DropTable(transaction, it, false);
                }
                catch (Exception)
                {
                    //It's OK if this fails.
                    continue;
                }
            }
            //Try to drop failed tables again.
            if (failed.Count != 0 && list.Length != failed.Count)
            {
                ForceDropTable(transaction, type);
            }
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="type">Type of the table to drop.</param>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable(Type type, bool relations)
        {
            DropTable(null, type, relations);
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="tableName">Table name to drop.</param>
        public void DropTable(string tableName)
        {
            DropTable(null, tableName);
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="tableName">Table name to drop.</param>
        public void DropTable(IDbTransaction? transaction, string tableName)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            try
            {
                if (TableExist(transaction, tableName))
                {
                    string query = "DROP TABLE " + tableName;
                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="type">Table type to drop.</param>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable(IDbTransaction? transaction, Type type, bool relations)
        {
            string table = Builder.GetTableName(type, false);
            table = GetTableName(table);
            IDbConnection connection = transaction?.Connection ?? Connection;
            try
            {
                if (TableExist(transaction, table))
                {
                    Dictionary<Type, GXSerializedItem> tables = new Dictionary<Type, GXSerializedItem>();
                    if (relations)
                    {
                        GXSqlBuilder.GetTables(type, tables);
                    }
                    if (!tables.ContainsKey(type))
                    {
                        tables.Add(type, null);
                    }
                    for (int pos = 0; pos != tables.Count; ++pos)
                    {
                        Type it = tables.Keys.ElementAt(pos);
                        table = Builder.GetTableName(it, false);
                        table = GetTableName(table);
                        if (!TableExist(transaction, table))
                        {
                            tables.Remove(it);
                            --pos;
                        }
                    }
                    DropTable(connection, transaction, type, tables);

                    //Drop auto increments that are not supported by DB.
                    foreach (var it in GXSqlBuilder.GetProperties(type))
                    {
                        //If field is marked as an auto increment.
                        if ((it.Value.Attributes & Attributes.AutoIncrement) != 0)
                        {
                            string[] arr = Builder.Settings.DropAutoIncrement(table, it.Key);
                            if (arr != null)
                            {
                                foreach (string it2 in arr)
                                {
                                    ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, it2);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Return true if table exists in the database.
        /// </summary>
        public bool TableExist<T>()
        {
            return TableExist(typeof(T));
        }

        /// <summary>
        /// Return true if table exists in the database.
        /// </summary>
        public bool TableExist(Type type)
        {
            return TableExist(Builder.GetTableName(type, true));
        }

        private string GetTableName(string table)
        {
            return Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, table);
        }

        private string GetColumnName(string table)
        {
            return GXSqlBuilder.UnescapeIdentifier(table);
        }

        /// <summary>
        /// Returns list of table columns.
        /// </summary>
        /// <returns>The list of table columns.</returns>
        /// <remarks>
        /// Column names are returned as they are in the database, so they may be different than property names.
        /// </remarks>
        public string[] GetColumns<T>()
        {
            return GetColumns(typeof(T));
        }

        /// <summary>
        /// Returns list of table columns. 
        /// </summary>
        /// <returns>The list of table columns.</returns>
        /// <remarks>
        /// Column names are returned as they are in the database, so they may be different than property names.
        /// </remarks>
        public string[] GetColumns(Type type)
        {
            string tableName = Builder.GetTableName(type, false);
            return GetColumns(tableName);
        }

        /// <summary>
        /// Returns list of table columns. 
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>The list of table columns.</returns>
        /// <remarks>
        /// Column names are returned as they are in the database, so they may be different than property names.
        /// </remarks>
        public string[] GetColumns(string tableName)
        {
            tableName = GetTableName(tableName);
            return Builder.GetColumns(this, tableName, Connection, null, OnSqlExecuted);
        }

        /// <summary>
        /// Get available permissions for the current database.
        /// </summary>
        public DatabasePermission[] AvailablePermissions()
        {
            return Builder.Settings.AvailablePermissions();
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="newName">New table name.</param>
        public void RenameTable<T>(string newName)
        {
            RenameTable(null, typeof(T), newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTable<T>(IDbTransaction? transaction, string newName)
        {
            RenameTable(transaction, typeof(T), newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="type">Table type.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTable(Type type, string newName)
        {
            RenameTable(null, type, newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="type">Table type.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTable(IDbTransaction? transaction, Type type, string newName)
        {
            string tableName = Builder.GetTableName(type, false);
            RenameTable(transaction, tableName, newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="oldName">Old table name.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTable(IDbTransaction? transaction, string oldName, string newName)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            oldName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, oldName);
            newName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, newName);
            string query = Builder.Settings.GetRenameTableQuery(oldName, newName);
            ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="oldName">Old table name.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTable(string oldName, string newName)
        {
            RenameTable(null, oldName, newName);
        }

        /// <summary>
        /// Rename table column.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="column">Old column name.</param>
        /// <param name="newName">New column name.</param>
        public void RenameTableColumn<T>(Expression<Func<T, object>> column, string newName)
        {
            string columnName = GXDbHelpers.ConvertToString(null, TargetType.Column, null, column, null);
            RenameTableColumn(null, typeof(T), columnName, newName);
        }

        /// <summary>
        /// Rename table column.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="oldName">Old column name.</param>
        /// <param name="newName">New column name.</param>
        public void RenameTableColumn<T>(string oldName, string newName)
        {
            RenameTableColumn(null, typeof(T), oldName, newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="oldName">Old column name.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTableColumn<T>(IDbTransaction? transaction, string oldName, string newName)
        {
            RenameTableColumn(transaction, typeof(T), oldName, newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="type">Table type.</param>
        /// <param name="oldName">Old column name.</param>
        /// <param name="newName">New table name.</param>
        public void RenameTableColumn(Type type, string oldName, string newName)
        {
            RenameTableColumn(null, type, oldName, newName);
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="type">Table type.</param>
        /// <param name="oldName">Old column name.</param>
        /// <param name="newName">New column name.</param>
        public void RenameTableColumn(IDbTransaction? transaction, Type type, string oldName, string newName)
        {
            string tableName = Builder.GetTableName(type, false);
            RenameTableColumn(transaction, tableName, oldName, newName);
        }

        /// <summary>
        /// Create new view.
        /// </summary>
        /// <param name="map">How columns are mapped between select and view.</param>
        /// <param name="select">How data is retreaved from the tables.</param>
        /// <param name="overwrite">Old view is dropped if exists.</param>
        public void CreateView(GXCreateViewArgs map, GXSelectArgs select, bool overwrite)
        {

        }

        /// <summary>
        /// Rename table column.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="oldName">Old column name.</param>
        /// <param name="newName">New column name.</param>
        public void RenameTableColumn(IDbTransaction? transaction, string tableName, string oldName, string newName)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            string query = Builder.Settings.GetRenameTableColumnQuery(tableName, oldName, newName);
            ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
        }

        /// <summary>
        /// Returns the current database schema as a Mermaid ER diagram.
        /// </summary>
        /// <remarks>
        /// Calls Describe for each database table. Index definitions and column
        /// metadata are shown as attribute comments. The result contains Mermaid
        /// source without Markdown fences.
        /// </remarks>
        /// <returns>Mermaid erDiagram source.</returns>
        public string ExportMermaid()
        {
            var tables = GetTables().OrderBy(it => it, StringComparer.Ordinal)
                .Select(it => Describe(it)).ToList();
            StringBuilder sb = new StringBuilder("erDiagram\n");
            var entities = new Dictionary<string, string>(StringComparer.Ordinal);
            string Entity(string name)
            {
                if (!entities.TryGetValue(name, out string? id))
                {
                    id = "table" + entities.Count.ToString(CultureInfo.InvariantCulture);
                    entities.Add(name, id);
                    sb.Append("    ").Append(id).Append("[\"")
                        .Append(MermaidText(name)).Append("\"]\n");
                }
                return id;
            }
            foreach (GXTableSchema table in tables)
            {
                Entity(table.ToString());
            }
            foreach (GXTableSchema table in tables)
            {
                sb.Append("    ").Append(entities[table.ToString()]).Append(" {\n");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (GXColumnSchema column in table.Columns.OrderBy(it => it.Ordinal))
                {
                    var keys = new List<string>();
                    if (column.IsPrimaryKey)
                    {
                        keys.Add("PK");
                    }
                    if (table.ForeignKeys.Any(key => key.Columns.Any(it => it.Column == column.Name)))
                    {
                        keys.Add("FK");
                    }
                    if (!column.IsPrimaryKey && IsMermaidUnique(table, column))
                    {
                        keys.Add("UK");
                    }
                    string type = string.IsNullOrEmpty(column.DbType)
                        ? column.Type?.Name ?? "object" : column.DbType;
                    string name = MermaidIdentifier(column.Name);
                    string uniqueName = name;
                    int suffix = 2;
                    while (!names.Add(uniqueName))
                    {
                        uniqueName = name + "_" + (suffix++).ToString(CultureInfo.InvariantCulture);
                    }
                    var comments = new List<string>();
                    if (uniqueName != column.Name) comments.Add("Column: " + column.Name);
                    if (MermaidIdentifier(type) != type) comments.Add("Type: " + type);
                    comments.Add(column.IsNullable && !column.IsPrimaryKey ? "NULL" : "NOT NULL");
                    if (column.IsAutoIncrement) comments.Add("AUTO_INCREMENT");
                    if (column.IsIdentity) comments.Add("IDENTITY");
                    if (column.IsGenerated) comments.Add("GENERATED");
                    if (column.IsComputed) comments.Add("COMPUTED " + column.ComputedExpression);
                    if (column.MaxLength.HasValue && column.MaxLength.Value > 0)
                        comments.Add("Length: " + column.MaxLength.Value.ToString(CultureInfo.InvariantCulture));
                    if (column.Precision.HasValue)
                        comments.Add("Precision: " + column.Precision.Value.ToString(CultureInfo.InvariantCulture));
                    if (column.Scale.HasValue)
                        comments.Add("Scale: " + column.Scale.Value.ToString(CultureInfo.InvariantCulture));
                    if (column.DefaultValue != null)
                        comments.Add("DEFAULT " + Convert.ToString(column.DefaultValue, CultureInfo.InvariantCulture));
                    if (!string.IsNullOrEmpty(column.Comment)) comments.Add(column.Comment);
                    foreach (GXIndex index in table.Indexes.OrderBy(it => it.Name, StringComparer.Ordinal))
                    {
                        if (index.Columns.Any(it => it.Name == column.Name))
                        {
                            comments.Add((index.Unique ? "UNIQUE INDEX " : "INDEX ") + index.Name +
                                " (" + string.Join(", ", index.Columns.OrderBy(it => it.Position).Select(it =>
                                    it.Name + (it.Order == IndexOrder.Descending ? " DESC" : " ASC"))) + ")");
                        }
                    }
                    sb.Append("        ").Append(MermaidIdentifier(type)).Append(' ').Append(uniqueName);
                    if (keys.Count != 0) sb.Append(' ').Append(string.Join(", ", keys));
                    sb.Append(" \"").Append(MermaidText(string.Join("; ", comments))).Append("\"\n");
                }
                sb.Append("    }\n");
            }
            foreach (GXTableSchema table in tables)
            {
                foreach (GXForeignKeySchema key in table.ForeignKeys.OrderBy(it => it.Name, StringComparer.Ordinal))
                {
                    string referencedName = string.IsNullOrEmpty(key.ReferencedSchema)
                        ? key.ReferencedTable : key.ReferencedSchema + "." + key.ReferencedTable;
                    // Resolve unqualified references only when the target is unambiguous.
                    if (!entities.ContainsKey(referencedName))
                    {
                        var matches = tables.Where(it => it.Name == key.ReferencedTable &&
                            (string.IsNullOrEmpty(key.ReferencedSchema) || string.IsNullOrEmpty(it.Schema) ||
                             it.Schema == key.ReferencedSchema)).ToList();
                        if (matches.Count == 1) referencedName = matches[0].ToString();
                    }
                    string parent = Entity(referencedName);
                    var columns = table.Columns.Where(it => key.Columns.Any(c => c.Column == it.Name)).ToList();
                    var foreignColumns = new HashSet<string>(key.Columns.Select(it => it.Column), StringComparer.Ordinal);
                    var primaryColumns = table.Columns.Where(it => it.IsPrimaryKey).Select(it => it.Name).ToList();
                    bool unique = (primaryColumns.Count != 0 && primaryColumns.All(foreignColumns.Contains)) ||
                        columns.Any(it => !it.IsPrimaryKey && IsMermaidUnique(table, it)) ||
                        table.Indexes.Any(it => it.Unique && it.Columns.Count != 0 &&
                            it.Columns.All(c => foreignColumns.Contains(c.Name)));
                    bool identifying = columns.Count != 0 && columns.Count == key.Columns.Count &&
                        columns.All(it => it.IsPrimaryKey);
                    string label = key.Name + ": " + string.Join(", ", key.Columns.OrderBy(it => it.Position)
                        .Select(it => it.Column + " -> " + it.ReferencedColumn));
                    label += "; DELETE " + key.OnDelete + "; UPDATE " + key.OnUpdate;
                    sb.Append("    ").Append(parent).Append(columns.Any(it => it.IsNullable && !it.IsPrimaryKey) ? " |o" : " ||")
                        .Append(identifying ? "--" : "..").Append(unique ? "o| " : "o{ ")
                        .Append(entities[table.ToString()]).Append(" : \"").Append(MermaidText(label)).Append("\"\n");
                }
            }
            return sb.ToString();
        }

        private static bool IsMermaidUnique(GXTableSchema table, GXColumnSchema column)
        {
            // Some providers mark every member of a composite unique index as unique.
            var indexes = table.Indexes.Where(it => it.Unique &&
                it.Columns.Any(c => c.Name == column.Name)).ToList();
            return indexes.Count == 0 ? column.IsUnique : indexes.Any(it => it.Columns.Count == 1);
        }

        private static string MermaidText(string value)
        {
            return value.Replace("&", "&amp;").Replace("\"", "&quot;")
                .Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\r", " ").Replace("\n", " ");
        }

        private static string MermaidIdentifier(string value)
        {
            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(value) || !char.IsLetter(value[0])) sb.Append('c');
            foreach (char ch in value)
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Describe table schema.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <returns>Table description.</returns>
        /// <remarks>Uses <see cref="SchemaCache"/> and returns an independently editable schema.</remarks>
        public GXTableSchema Describe<T>()
        {
            return Describe(typeof(T));
        }

        /// <summary>
        /// Returns the schema information for the specified table type.    
        /// </summary>
        /// <param name="table">The type representing the table to describe.</param>
        /// <returns>A GXTableSchema instance containing the schema of the specified table.</returns>
        /// <remarks>Uses <see cref="SchemaCache"/> and returns an independently editable schema.</remarks>
        public GXTableSchema Describe(Type table)
        {
            string name = Builder.GetTableName(table, false);
            return Describe(name);
        }

        /// <summary>
        /// Describe table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>Table description.</returns>
        /// <remarks>
        /// Uses the connection's <see cref="SchemaCache"/>. Each result has independent columns,
        /// indexes, and foreign keys. Schema-manager DDL invalidates the cache; call
        /// <see cref="Gurux.Service.DB.GXSchemaCache.Clear"/> after external schema changes.
        /// </remarks>
        public GXTableSchema Describe(string tableName)
        {
            tableName = GetTableName(tableName);
            // Length prefixes keep catalog/schema/table combinations unambiguous.
            string database = Connection.Database ?? string.Empty;
            string builderDatabase = Builder.Database ?? string.Empty;
            string key = database.Length + ":" + database + builderDatabase.Length + ":" + builderDatabase + tableName;
            return SchemaCache.GetOrAdd(key, () => DescribeCore(tableName));
        }

        private GXTableSchema DescribeCore(string tableName)
        {
            GXTableSchema table = new GXTableSchema()
            {
                Name = tableName
            };

            if (!TableExist(tableName))
            {
                throw new ArgumentException("Table '" + tableName + "' does not exist.");
            }

            table.Comment = GetDescription(Connection, tableName, null);
            StringBuilder header = new StringBuilder();
            int len;
            var cols = Builder.GetColumns(this, tableName, Connection, null, OnSqlExecuted);
            foreach (string col in cols)
            {
                GXColumnSchema column = new GXColumnSchema()
                {
                    Name = GetColumnName(col)
                };
                table.Columns.Add(column);
                column.Parent = table;
                column.Type = GetColumnType(tableName, column.Name, Connection, out len);
                column.IsAutoIncrement = IsAutoIncrement(tableName, column.Name, Connection);
                column.IsUnique = IsUnique(tableName, column.Name, Connection);
                column.IsPrimaryKey = GetPrimaryKey(tableName, column.Name, Connection);
                column.IsIdentity = IsIdentity(tableName, column.Name, Connection);
                GetColumnDefaultValueQuery(tableName, column, Connection);
                column.MaxLength = len;
                column.IsNullable = GetColumnNullableQuery(tableName, column.Name, Connection);
                column.Comment = GetDescription(Connection, tableName, column.Name);
                column.Ordinal = GetOrdinal(Connection, tableName, column.Name);
                if (GetPrimaryKeyQuery(tableName, column.Name, Connection))
                {
                    //TODO: Is this needed because IsUnique is already set above?
                    //Check if this is redundant. 
                    column.IsUnique = true;
                }
            }
            //Arrange columns by ordinal.
            table.Columns.Sort(static (left, right) => left.Ordinal.CompareTo(right.Ordinal));
            GetTableIndexes(table, Connection);
            GetTableForeignKeys(table, Connection);
            return table;
        }

        private void GetColumnDefaultValueQuery(string tableName,
            GXColumnSchema column,
            IDbConnection connection)
        {
            string query = Builder.Settings.GetColumnDefaultValueQuery(Builder.Database, tableName, column.Name);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    if (Builder.Settings.Type == DatabaseType.Oracle)
                    {
                        SetInitialLongFetchSize(com);
                    }
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp == null || tmp is DBNull ||
                                (tmp is string s && (s == "NULL" || s == "")))
                            {
                                return;
                            }
                            string value = Convert.ToString(tmp,
                                CultureInfo.InvariantCulture)!.Trim();
                            string upper = value.ToUpperInvariant();
                            if (upper.Contains("UUID()") ||
                                upper.Contains("NEWID()") ||
                                upper.Contains("SYS_GUID") ||
                                upper.Contains("SYSUUID") ||
                                upper.Contains("GEN_RANDOM_UUID()"))
                            {
                                column.DefaultValue = DefaultValueKind.NewGuid;
                                return;
                            }
                            if (upper.Contains("UTC_TIMESTAMP") ||
                                upper.Contains("UTC_DATE") ||
                                upper.Contains("UTC_TIME") ||
                                upper.Contains("SYSUTCDATETIME") ||
                                upper.Contains("SYS_EXTRACT_UTC") ||
                                upper.Contains("AT TIME ZONE 'UTC'"))
                            {
                                column.DefaultValue = DefaultValueKind.UtcNow;
                                if (column.Type == typeof(string))
                                {
                                    column.Type = typeof(DateTime);
                                }
                                return;
                            }
                            if (upper.Contains("CURRENT_TIMESTAMP") ||
                                upper.Contains("CURRENT_DATE") ||
                                upper.Contains("CURRENT_TIME") ||
                                upper.Contains("CURDATE()") ||
                                upper.Contains("CURTIME()") ||
                                upper.Contains("NOW()") ||
                                upper.Contains("GETDATE") ||
                                upper.Contains("SYSDATETIMEOFFSET") ||
                                upper.Contains("SYSTIMESTAMP") ||
                                upper.Contains("SYSDATE") ||
                                upper.Contains("CURRENT TIMESTAMP") ||
                                upper.Contains("CURRENT DATE") ||
                                upper.Contains("CURRENT TIME"))
                            {
                                column.DefaultValue = DefaultValueKind.Now;
                                if (column.Type == typeof(string))
                                {
                                    column.Type = typeof(DateTime);
                                    column.DefaultValue = DefaultValueKind.UtcNow;
                                }
                                return;
                            }
                            while (value.Length > 1 && value[0] == '(' &&
                                value[^1] == ')')
                            {
                                value = value[1..^1].Trim();
                            }
                            if (column.Type == typeof(Guid))
                            {
                                for (int pos = 0; pos + 36 <= value.Length; ++pos)
                                {
                                    if (Guid.TryParse(value.Substring(pos, 36),
                                        out Guid guid))
                                    {
                                        column.DefaultValue = guid;
                                        return;
                                    }
                                }
                                throw new FormatException(
                                    "Unrecognized Guid default value: " + value);
                            }
                            int quote = value.IndexOf('\'');
                            if (quote >= 0 && value.EndsWith('\''))
                            {
                                value = value[(quote + 1)..^1]
                                    .Replace("''", "'");
                            }
                            if (column.Type == typeof(bool) &&
                                (value == "0" || value == "1"))
                            {
                                column.DefaultValue = value == "1";
                                return;
                            }
                            column.DefaultValue = Builder.Settings.ChangeType(value, column.Type);
                            if (column.DefaultValue is DefaultValueKind df)
                            {
                                if (df == DefaultValueKind.NewGuid && column.Type == typeof(byte[]))
                                {
                                    column.Type = typeof(Guid);
                                }
                                else if ((df == DefaultValueKind.Now | df == DefaultValueKind.UtcNow) &&
                                    column.Type == typeof(string))
                                {
                                    //SQ Lite uses string for date time, so we change it to DateTime.
                                    column.Type = typeof(DateTime);
                                }
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        private static void SetInitialLongFetchSize(IDbCommand command)
        {
            PropertyInfo? property = command.GetType().GetProperty("InitialLONGFetchSize");
            if (property != null &&
                property.PropertyType == typeof(int) &&
                property.CanWrite)
            {
                property.SetValue(command, -1);
            }
        }

        private string? GetDescription(IDbConnection connection,
            string tableName,
            string? columnName)
        {
            string query = Builder.Settings.GetDescriptionQuery(Builder.Database, tableName, columnName);
            if (string.IsNullOrEmpty(query))
            {
                throw new NotImplementedException();
            }
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Convert.ToString(tmp);
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return null;
        }

        private int GetOrdinal(IDbConnection connection, string tableName, string columnName)
        {
            string query = Builder.Settings.GetOrdinalQuery(Builder.Database, tableName, columnName);
            if (string.IsNullOrEmpty(query))
            {
                return 0;
            }
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Convert.ToInt32(tmp);
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return 0;
        }

        /// <summary>
        /// Check is column primary key.
        /// </summary>
        /// <param name="tableName">The database table name.</param>
        /// <param name="columnName">The database column name.</param>
        /// <param name="connection">The database connection used to query metadata.</param>
        /// <returns>True when the column participates in the primary key; otherwise, false.</returns>
        private bool GetPrimaryKeyQuery(string tableName, string columnName, IDbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetPrimaryKeyQuery(Builder.Database, tableName, columnName);
            try
            {
                using IDbCommand com = connection.CreateCommand();
                com.CommandType = CommandType.Text;
                com.CommandText = query;
                using IDataReader reader = com.ExecuteReader();
                while (reader.Read())
                {
                    object tmp = reader.GetValue(0);
                    if (tmp != null && !(tmp is DBNull))
                    {
                        ret = Builder.Settings.IsPrimaryKey(tmp);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return ret;
        }

        private bool GetColumnNullableQuery(string tableName, string columnName, IDbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetColumnNullableQuery(Builder.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ret = Builder.Settings.IsNullable(reader.GetValue(0));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return ret;
        }
        private Type GetColumnType(string tableName,
            string columnName,
            IDbConnection connection,
            out int len)
        {
            return Builder.GetColumnType(this, tableName, columnName, connection,
                null, OnSqlExecuted, out len, out _);
        }

        private bool IsAutoIncrement(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.GetAutoIncrementQuery(Builder.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Builder.Settings.IsAutoIncrement(reader.GetValue(0));
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return false;
        }
        private void GetTableForeignKeys(GXTableSchema schema, IDbConnection connection, IDbTransaction? transaction = null)
        {
            string query = Builder.Settings.GetColumnConstraintsQuery(Builder.Database, schema.Name);
            try
            {
                schema.ForeignKeys.Clear();
                List<object[]> keys = new List<object[]>();
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] key = new object[reader.FieldCount];
                            reader.GetValues(key);
                            keys.Add(key);
                        }
                        reader.Close();
                    }
                }
                foreach (object[] row in keys)
                {
                    if (row.Length < 8 ||
                        row[0] == null ||
                        row[0] is DBNull)
                    {
                        continue;
                    }
                    string name = Convert.ToString(row[0], CultureInfo.InvariantCulture)!;
                    GXForeignKeySchema? key = schema.ForeignKeys
                        .SingleOrDefault(it => string.Equals(it.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (key == null)
                    {
                        key = new GXForeignKeySchema()
                        {
                            Name = name,
                            ReferencedSchema = ToSchemaString(row[1]),
                            ReferencedTable = ToSchemaString(row[2]),
                            OnDelete = ToForeignKeyAction(row[6]),
                            OnUpdate = ToForeignKeyAction(row[7])
                        };
                        schema.ForeignKeys.Add(key);
                    }
                    key.Columns.Add(new GXForeignKeyColumnSchema()
                    {
                        Column = ToSchemaString(row[3]),
                        ReferencedColumn = ToSchemaString(row[4]),
                        Position = Math.Max(0, Convert.ToInt32(row[5], CultureInfo.InvariantCulture) - 1)
                    });
                }
                foreach (GXForeignKeySchema key in schema.ForeignKeys)
                {
                    key.Columns.Sort((a, b) => a.Position.CompareTo(b.Position));
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        private static string ToSchemaString(object value)
        {
            if (value == null || value is DBNull)
            {
                return string.Empty;
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static ForeignKeyAction ToForeignKeyAction(object value)
        {
            string str = ToSchemaString(value).Replace("_", " ").Trim().ToUpperInvariant();
            return str switch
            {
                "C" or "CASCADE" => ForeignKeyAction.Cascade,
                "R" or "RESTRICT" => ForeignKeyAction.Restrict,
                "N" or "SET NULL" => ForeignKeyAction.SetNull,
                "D" or "SET DEFAULT" => ForeignKeyAction.SetDefault,
                "A" or "NO ACTION" or "NOACTION" => ForeignKeyAction.NoAction,
                _ => ForeignKeyAction.None
            };
        }

        private void GetTableIndexes(GXTableSchema schema, IDbConnection connection)
        {
            string query = Builder.Settings.TableIndexesQuery(Builder.Database, schema.Name);
            try
            {
                List<object[]> indexes = new List<object[]>();
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] index = new object[reader.FieldCount];
                            reader.GetValues(index);
                            if (index != null)
                            {
                                indexes.Add(index);
                            }
                        }
                        reader.Close();
                    }
                }
                if (indexes.Any())
                {
                    Builder.Settings.UpdateTableIndexes(schema, indexes);
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
        }

        private bool IsUnique(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.UniqueQuery(Builder.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Builder.Settings.IsUnique(reader.GetValue(0));
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return false;
        }

        private bool IsIdentity(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.IsIdentityQuery(Builder.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Builder.Settings.IsIdentity(reader.GetValue(0));
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return false;
        }


        private bool GetPrimaryKey(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.GetPrimaryKeyQuery(Builder.Database, tableName, columnName);
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object tmp = reader.GetValue(0);
                            if (tmp != null && !(tmp is DBNull))
                            {
                                return Builder.Settings.IsPrimaryKey(reader.GetValue(0));
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return false;
        }
    }
}
