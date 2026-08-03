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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Gurux.Service.Orm.Model
{
    public class GXSchemaManager
    {
        private GXSqlBuilder Builder;
        private DbConnection Connection;
        private SqlExecutedEventHandler sql;
        public GXSchemaManager(DbConnection connection, string tablePrefix)
        {
            Connection = connection ?? throw new ArgumentException(null, nameof(connection));
            Builder = new GXSqlBuilder(connection, tablePrefix);
            if (connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        public GXSchemaManager(GXDbConnection connection)
        {
            Connection = connection?.Connection ?? throw new ArgumentException(null, nameof(connection));
            Builder = new GXSqlBuilder(Connection, connection.Builder.Settings.TablePrefix);
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        /// <summary>
        /// Event hanler for column value conversion.
        /// </summary>
        public event EventHandler<GXColumnValueConvertingEventArgs> ColumnValueConverting;

        /// <summary>
        /// Event hanler for executed SQL.
        /// </summary>
        /// <remarks>
        /// This can be used for debugging executed SQLs.
        /// </remarks>
        public event SqlExecutedEventHandler OnSqlExecuted
        {
            add
            {
                sql += value;
            }
            remove
            {
                sql -= value;
            }
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
        public void CreateDatabase(IDbTransaction transaction, string databaseName)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string query;
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                var type = Connection.GetType();
                Connection = (DbConnection)Activator.CreateInstance(
                    type,
                    Connection.ConnectionString)!;
                Connection.Open();
                return;
            }
            if (Builder.Settings.Type == DatabaseType.DB2 ||
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
            ExecuteNonQuery(Connection, transaction, sql, query);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Database table names.</returns>
        public string[] GetTables(IDbTransaction transaction = null)
        {
            return GetTables(transaction, Connection.Database);
        }

        /// <summary>
        /// Returns table names in the current database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>Database table names.</returns>
        public string[] GetTables(IDbTransaction transaction, string databaseName)
        {
            return Builder.GetTables(Connection, transaction, databaseName);
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
            CreateTable<T>(true, true);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        public void CreateTable<T>(IDbTransaction transaction)
        {
            CreateTable(transaction, typeof(T), true, true);
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable<T>(IDbTransaction transaction, bool relations, bool overwrite)
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
        /// Check if table exists.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        public bool TableExist(IDbTransaction transaction, string tableName)
        {
            tableName = GetTableName(tableName);
            string query = Builder.Settings.TableExist(Connection.Database, tableName);
            return (int)ExecuteScalarInternal(Connection, transaction, query, typeof(int)) != 0;
        }

        internal static object ExecuteScalarInternal(IDbConnection connection, IDbTransaction transaction, string query, Type type)
        {
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    object value = com.ExecuteScalar();
                    if (type != null)
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
                if (TableExist(Builder.GetTableName(tmp, false)))
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
        public void CreateTable(IDbTransaction transaction, Type type, bool relations, bool overwrite)
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
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
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
            IDbTransaction transaction,
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
            IDbTransaction transaction, GXTableCreateQuery table, List<Type> created)
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
                //If table is not created yet.
                if (create)
                {
                    System.Diagnostics.Debug.WriteLine("Create table: " + table.Table.Name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Drop table: " + table.Table.Name);
                }
                foreach (string q in table.Queries)
                {
                    ExecuteNonQuery(connection, transaction, sql, q);
                }
            }
        }

        private void DropTable(IDbConnection connection, IDbTransaction transaction,
            Type type, Dictionary<Type, GXSerializedItem> tables)
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


        private List<(string ConstraintName, string TableName)> GetForeignKeys(IDbTransaction transaction, string tableName)
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
        public void UpdateTable<T>(IDbTransaction transaction = default)
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

        /// <summary>
        /// Update table.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="type">Table type.</param>
        public void UpdateTable(IDbTransaction transaction, Type type)
        {
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            string[] cols = GetColumns(type);
            string tableName = Builder.GetTableName(type, false);
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
            try
            {
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
                        sb.Append(GXDbHelpers.AddQuotes(tableName,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.TableNameQuoteCharacter));
                        sb.Append(" ADD ");
                        sb.Append(GXDbHelpers.AddQuotes(it.Key,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.ColumnNameQuoteCharacter));
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
                        ExecuteNonQuery(Connection, transaction, sql, sb.ToString());
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
                    Type oldType = Builder.GetColumnType(tableName, it.Key, connection, transaction,
                        out int length, out string databaseType);
                    object target = it.Value.Relation != null &&
                        it.Value.Relation.RelationType == RelationType.OneToOne
                        ? it.Value.Relation
                        : it.Value.Target;
                    string expectedType = Builder.GetDataBaseType(newType, target);
                    if (oldType != newType &&
                        !IsSameDatabaseType(databaseType, length, expectedType))
                    {
                        ChangeColumnType(connection, transaction, tableName, it.Key,
                            oldType, newType, it.Value);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Create indexes.
        /// </summary>
        /// <param name="type">Table where indexes are search.</param>
        /// <param name="tableItem"></param>
        /// <param name="tableName"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
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
                                if (Builder.Settings.UpperCase)
                                {
                                    name = it3.Key.ToUpper();
                                }
                                else
                                {
                                    name = it3.Key;
                                }
                                sb.Append(GXDbHelpers.AddQuotes(name,
                                    Builder.Settings.DataQuotaReplacement,
                                    Builder.Settings.ColumnNameQuoteCharacter));
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
            string tableName = Builder.GetTableName(type, false);
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
                            if (Builder.Settings.Type == DatabaseType.MySQL)
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
                                Connection.Database, tableName, null,
                                tableDescription.Description);
                            if (!string.IsNullOrEmpty(commentQuery))
                            {
                                tableItem.Queries.Add(commentQuery);
                            }
                        }
                        foreach (var property in GXSqlBuilder.GetProperties(type))
                        {
                            DescriptionAttribute columnDescription =
                                (property.Value.Target as MemberInfo)?
                                .GetCustomAttribute<DescriptionAttribute>(true);
                            if (string.IsNullOrEmpty(columnDescription?.Description))
                            {
                                continue;
                            }
                            string commentQuery = Builder.Settings.GetCommentQuery(
                                Connection.Database, tableName, property.Key,
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
                                string[] arr = Builder.Settings.CreateAutoIncrement(tableName, it.Key);
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
            sb.Append(" DEFAULT");
            string tmp = Builder.Settings.GetColumnDefaultValue(value, columnType);
            if (!string.IsNullOrEmpty(tmp))
            {
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
            string expectedType)
        {
            static string Normalize(string value)
            {
                return value.Replace(" ", "").ToUpperInvariant();
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

        private void ChangeColumnType(IDbConnection connection, IDbTransaction transaction,
            string tableName, string columnName, Type oldType, Type newType,
            GXSerializedItem item)
        {
            string temporaryName = columnName + "_GXConverted";
            HashSet<string> columns = new(Builder.GetColumns(tableName, connection, transaction),
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
            ExecuteNonQuery(Connection, transaction, sql, addConvertedColumn);
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
            ExecuteNonQuery(Connection, transaction, sql, dropOldColumn);
            ExecuteNonQuery(Connection, transaction, sql,
                Builder.Settings.GetRenameTableColumnQuery(tableName, temporaryName, columnName));
            if (Builder.Settings.Type == DatabaseType.DB2)
            {
                ExecuteNonQuery(Connection, transaction, sql,
                    $"CALL SYSPROC.ADMIN_CMD('REORG TABLE {tableName}')");
            }
        }

        internal static string[] ExecuteQuery(IDbConnection connection, IDbTransaction transaction,
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
        /// <param name="connection">Used DB connection.</param>
        /// <param name="transaction">Used transaction.</param>
        /// <param name="query">Query to execute.</param>
        internal static void ExecuteNonQuery(IDbConnection connection, IDbTransaction transaction,
            SqlExecutedEventHandler sql,
            string query)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (IDbCommand com = connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.Transaction = transaction;
                    com.CommandText = query;
                    com.ExecuteNonQuery();
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
                sql(null, query, (int)sw.ElapsedMilliseconds);
            }
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
                    byte[] bytes = Convert.FromHexString(guid.ToString("N"));
                    if (Builder.Settings.Type == DatabaseType.MySQL)
                    {
                        bytes =
                        [
                            bytes[6], bytes[7], bytes[4], bytes[5],
                            bytes[0], bytes[1], bytes[2], bytes[3],
                            bytes[8], bytes[9], bytes[10], bytes[11],
                            bytes[12], bytes[13], bytes[14], bytes[15]
                        ];
                    }
                    newValue = bytes;
                }
                else if (newValue is Guid oracleGuid &&
                    Builder.Settings.Type == DatabaseType.Oracle)
                {
                    newValue = oracleGuid.ToByteArray();
                }
                else if (newValue is Guid db2Guid &&
                    Builder.Settings.Type == DatabaseType.DB2)
                {
                    newValue = db2Guid.ToString();
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
        public void DropTable<T>(IDbTransaction transaction, bool relations)
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
        /// Drop database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        public void DropDatabase(IDbTransaction transaction, string databaseName)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Connection.Close();
                Connection.Dispose();
                File.Delete(databaseName + ".db");
                return;
            }
            string query;
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            if (Builder.Settings.Type == DatabaseType.DB2)
            {
                string old = Connection.Database;
                if (old != databaseName)
                {
                    Builder.ChangeDatabase(Connection, databaseName);
                }
                var tables = GetTables(transaction, databaseName);
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
                string old = Connection.Database;
                if (string.Equals(old, databaseName, StringComparison.OrdinalIgnoreCase))
                {
                    Builder.ChangeDatabase(Connection, "postgres");
                }
                string db = databaseName.Replace("'", "''");
                ExecuteNonQuery(Connection, transaction, sql,
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
            ExecuteNonQuery(Connection, transaction, sql, query);
        }

        /// <summary>
        /// Get list of databases.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Array of database names.</returns>
        public string[] GetDatabases(IDbTransaction transaction = null)
        {
            return Builder.GetDatabases(Connection, transaction);
        }

        /// <summary>
        /// Drop all database tables.
        /// </summary>
        public void DropAllTables(IDbTransaction transaction = null)
        {
            string query, tableName, name;
            var tables = GetTables();
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                query = $"PRAGMA foreign_keys = OFF";
                ExecuteNonQuery(Connection, transaction, sql, query);
            }
            try
            {
                foreach (var table in tables)
                {
                    if (Builder.Settings.Type != DatabaseType.SqLite)
                    {
                        List<(string ConstraintName, string TableName)> keys = GetForeignKeys(transaction, table);
                        foreach (var k in keys)
                        {
                            tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, k.TableName, null);
                            name = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, k.ConstraintName, null);
                            query = $"ALTER TABLE {tableName} DROP CONSTRAINT {name}";
                            ExecuteNonQuery(Connection, transaction, sql, query);
                        }
                    }
                    tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, table, null);
                    query = $"DROP TABLE {tableName}";
                    ExecuteNonQuery(Connection, transaction, sql, query);
                }
            }
            finally
            {
                if (Builder.Settings.Type == DatabaseType.SqLite)
                {
                    query = $"PRAGMA foreign_keys = ON";
                    ExecuteNonQuery(Connection, transaction, sql, query);
                }
            }
        }


        /// <summary>
        /// Force to drop all relation tables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ForceDropTable<T>()
        {
            ForceDropTable(typeof(T));
        }

        /// <summary>
        /// Force to drop all relation tables.
        /// </summary>
        public void ForceDropTable(Type type)
        {
            List<Type> failed = new List<Type>();
            Type[] list = GetRelationTables(type);
            list = list.Reverse().ToArray();
            foreach (Type it in list)
            {
                try
                {
                    try
                    {
                        string tableName = Builder.GetTableName(it, false);
                        tableName = GetTableName(tableName);
                        List<(string ConstraintName, string TableName)> keys = GetForeignKeys(null, tableName);
                        foreach (var k in keys)
                        {
                            tableName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, k.TableName, null);
                            string name = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column, null, k.ConstraintName, null);
                            string query = $"ALTER TABLE {tableName} DROP CONSTRAINT {name}";
                            ExecuteNonQuery(Connection, null, sql, query);
                        }
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails.
                    }
                    DropTable(it, false);
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
                    DropTable(it, false);
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
                ForceDropTable(type);
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
        /// <param name="transaction">DB transaction.</param>
        /// <param name="type">Table type to drop.</param>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable(IDbTransaction transaction, Type type, bool relations)
        {
            string table = Builder.GetTableName(type, false);
            table = GetTableName(table);
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
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
                                    ExecuteNonQuery(Connection, transaction, sql, it2);
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
            return TableExist(Builder.GetTableName(type, false));
        }

        private string GetTableName(string table)
        {
            if (Builder.Settings.Type == DatabaseType.Oracle ||
                Builder.Settings.Type == DatabaseType.DB2 ||
                Builder.Settings.Type == DatabaseType.SapHana)
            {
                table = table.ToUpper();
            }
            return table;
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
            return Builder.GetColumns(tableName, Connection);
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
        public void RenameTable<T>(IDbTransaction transaction, string newName)
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
        public void RenameTable(IDbTransaction transaction, Type type, string newName)
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
        public void RenameTable(IDbTransaction transaction, string oldName, string newName)
        {
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
            string query = Builder.Settings.GetRenameTableQuery(oldName, newName);
            ExecuteNonQuery(connection, transaction, sql, query);
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
        public void RenameTableColumn<T>(IDbTransaction transaction, string oldName, string newName)
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
        public void RenameTableColumn(IDbTransaction transaction, Type type, string oldName, string newName)
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
        public void RenameTableColumn(IDbTransaction transaction, string tableName, string oldName, string newName)
        {
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
            string query = Builder.Settings.GetRenameTableColumnQuery(tableName, oldName, newName);
            ExecuteNonQuery(Connection, transaction, sql, query);
        }

        /// <summary>
        /// Describe table schema.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <returns>Table description.</returns>
        public GXTableSchema Describe<T>()
        {
            return Describe(typeof(T));
        }

        /// <summary>
        /// Returns the schema information for the specified table type.    
        /// </summary>
        /// <param name="table">The type representing the table to describe.</param>
        /// <returns>A GXTableSchema instance containing the schema of the specified table.</returns>
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
        public GXTableSchema Describe(string tableName)
        {
            GXTableSchema table = new GXTableSchema()
            {
                Name = tableName
            };
            table.Comment = GetDescription(Connection, tableName, null);
            StringBuilder header = new StringBuilder();
            int len;

            foreach (string col in Builder.GetColumns(tableName, Connection))
            {
                GXColumnSchema column = new GXColumnSchema()
                {
                    Name = col
                };
                table.Columns.Add(column);
                column.Type = GetColumnType(tableName, col, Connection, out len);
                column.IsAutoIncrement = IsAutoIncrement(tableName, col, Connection);
                GetColumnDefaultValueQuery(tableName, column, Connection);
                column.MaxLength = len;
                column.IsNullable = GetColumnNullableQuery(tableName, col, Connection);
                column.Comment = GetDescription(Connection, tableName, col);
                column.Ordinal = GetOrdinal(Connection, tableName, col);
                if (GetPrimaryKeyQuery(tableName, col, Connection))
                {
                    column.IsUnique = true;
                }
                else
                {
                    string[] refs = GetReferenceTablesQuery(tableName, col, Connection);
                    if (refs.Length != 0)
                    {
                        ForeignKeyDelete onDelete;
                        ForeignKeyUpdate onUpdate;
                        string t = GetColumnConstraintsQuery(tableName, col, Connection, out onDelete, out onUpdate);
                        if (onUpdate == ForeignKeyUpdate.Restrict)
                        {
                            onUpdate = ForeignKeyUpdate.None;
                        }
                        //Only cascade is allowed on delete.
                        if (onDelete != ForeignKeyDelete.Cascade)
                        {
                            onDelete = ForeignKeyDelete.None;
                        }
                        if (onDelete == ForeignKeyDelete.None && onUpdate == ForeignKeyUpdate.None)
                        {
                            /*
                            foreach (string it in refs)
                            {
                                AddLine(data, settings, "[ForeignKey(typeof(" +
                                    GetName(settings, it) + "))]", 2);
                            }*/
                        }
                        else if (onDelete != ForeignKeyDelete.None && onUpdate != ForeignKeyUpdate.None)
                        {
                            /*
                            foreach (string it in refs)
                            {
                                AddLine(data, settings, "[ForeignKey(typeof(" +
                                GetName(settings, it) + "), OnDelete = ForeignKeyDelete." + onDelete +
                                ", OnUpdate = ForeignKeyUpdate." + onUpdate + ")]", 2);
                            }
                            */
                        }
                        else if (onDelete != ForeignKeyDelete.None)
                        {
                            /*
                            foreach (string it in refs)
                            {
                                AddLine(data, settings, "[ForeignKey(typeof(" +
                                GetName(settings, it) + "), OnDelete = ForeignKeyDelete." + onDelete + ")]", 2);
                            }
                            */
                        }
                        else if (onUpdate != ForeignKeyUpdate.None)
                        {
                            /*
                            foreach (string it in refs)
                            {
                                AddLine(data, settings, "[ForeignKey(typeof(" +
                                GetName(settings, it) + "), OnUpdate = ForeignKeyUpdate." + onUpdate + ")]", 2);
                            }
                            */
                        }
                    }
                }
            }
            return table;
        }

        private string GetColumnConstraintsQuery(string tableName, string columnName, IDbConnection connection, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            string targetTable = "";
            onDelete = ForeignKeyDelete.None;
            onUpdate = ForeignKeyUpdate.None;
            string query = Builder.Settings.GetColumnConstraintsQuery(connection.Database, tableName, columnName);
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
                            targetTable = Builder.Settings.GetColumnConstraints(new object[] { reader.GetString(0), reader.GetString(1), reader.GetString(2) }, out onDelete, out onUpdate);
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw GXDatabaseException.Create(ex, query);
            }
            return targetTable;
        }

        private string[] GetReferenceTablesQuery(string tableName, string columnName, IDbConnection connection)
        {
            List<string> list = new List<string>();
            string query = Builder.Settings.GetReferenceTablesQuery(connection.Database, tableName, columnName);
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
                            list.Add(reader.GetString(0));
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

        private void GetColumnDefaultValueQuery(string tableName,
            GXColumnSchema column,
            IDbConnection connection)
        {
            string query = Builder.Settings.GetColumnDefaultValueQuery(connection.Database, tableName, column.Name);
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
                            if (tmp == null || tmp is DBNull ||
                                (tmp is string s && s == "NULL"))
                            {
                                return;
                            }
                            string value = Convert.ToString(tmp,
                                CultureInfo.InvariantCulture).Trim();
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
                            upper.Contains("SYSDATETIMEOFFSET"))
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

        private string GetDescription(IDbConnection connection, string tableName, string columnName)
        {
            string query = Builder.Settings.GetDescriptionQuery(connection.Database, tableName, columnName);
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
            return "";
        }

        private int GetOrdinal(IDbConnection connection, string tableName, string columnName)
        {
            string query = Builder.Settings.GetOrdinalQuery(connection.Database, tableName, columnName);
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
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private bool GetPrimaryKeyQuery(string tableName, string columnName, IDbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetPrimaryKeyQuery(connection.Database, tableName, columnName);
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
            string query = Builder.Settings.GetColumnNullableQuery(connection.Database, tableName, columnName);
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
            return Builder.GetColumnType(tableName, columnName, connection, null, out len, out _);
        }

        private static string GetName(GXGeneratorSettings settings, string value)
        {
            return settings.TablePrefix + Char.ToUpper(value[0]) + value.Substring(1);
        }

        private static bool ContainsTable(string[] list, string value)
        {
            if (list == null)
            {
                return true;
            }
            foreach (string it in list)
            {
                if (string.Compare(it, value, true) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsAutoIncrement(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.GetAutoIncrementQuery(connection.Database, tableName, columnName);
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
    }
}
