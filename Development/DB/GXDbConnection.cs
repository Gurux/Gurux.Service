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
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Collections;
using Gurux.Common.Internal;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Settings;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading;
using Gurux.Common.Db;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Event hanler for executed SQL.
    /// </summary>
    /// <param name="instance">Sender.</param>
    /// <param name="sql">Executed SQL.</param>
    /// <param name="executionTime">Execution time.</param>
    public delegate void SqlExecutedEventHandler(object instance, string sql, int executionTime);

    /// <summary>
    /// This class is used to communicate with database.
    /// </summary>
    public class GXDbConnection : IDisposable
    {
        /// <summary>
        /// Default database type.
        /// </summary>
        /// <remarks>
        /// When this is changed generated SQL are shown using this database format.
        /// MySql settings are default settings because of MariaDB (https://mariadb.org/).
        /// </remarks>
        public static DatabaseType DefaultDatabaseType = DatabaseType.MySQL;
        private SqlExecutedEventHandler sql;

        /// <summary>
        /// Used database.
        /// </summary>
        public DatabaseType DatabaseType
        {
            get
            {
                return Builder.Settings.Type;
            }
        }
        internal GXSqlBuilder Builder
        {
            get;
            private set;
        }

        private IDbConnection[] Connections
        {
            get;
            set;
        }

        /// <summary>
        /// Null string is handled as empty string.
        /// </summary>
        /// <remarks>
        /// NULL string is saved as empty string or convert to empty string when null string is read from the DB.
        /// </remarks>
        [DefaultValue(false)]
        public bool UseEmptyString
        {
            get;
            set;
        }

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
        /// Is transaction used automatically.
        /// </summary>
        public bool AutoTransaction
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of available connections.
        /// </summary>
        int _connections = 0;
        /// <summary>
        /// connection is returned to the pool
        /// </summary>
        internal Semaphore ConnectionReleased;

        internal readonly object sync = new object();

        /// <summary>
        /// Get next connection from the pool.
        /// </summary>
        public IDbConnection GetConnection()
        {
            if (!ConnectionReleased.WaitOne(20000))
            {
                throw new Exception("Failed to get connection from the pool.");
            }
            IDbConnection con;
            lock (sync)
            {
                --_connections;
                con = Connections[_connections];
            }
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
            return con;
        }

        /// <summary>
        /// Get next connection from the pool.
        /// </summary>
        public IDbConnection TryGetConnection(int waitTime)
        {
            if (!ConnectionReleased.WaitOne(waitTime))
            {
                return null;
            }
            IDbConnection con;
            lock (sync)
            {
                --_connections;
                con = Connections[_connections];
            }
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
            return con;
        }

        /// <summary>
        /// connection is returned to the pool after use.
        /// </summary>
        /// <param name="connection"></param>
        private void ReleaseConnection(IDbConnection connection)
        {
            lock (sync)
            {
                Connections[_connections] = connection;
                ++_connections;
                if (!(DatabaseType == DatabaseType.SqLite &&
                    connection.ConnectionString == "Data Source=:memory:"))
                {
                    //SQLite will destroy all tables when the connection is closed.
                    connection.Close();
                }
            }
            ConnectionReleased.Release();
        }

        /// <summary>
        /// Used connection string.
        /// </summary>
        public string ConnectionString
        {
            get;
            private set;
        }

        /// <summary>
        /// Used connection string.
        /// </summary>
        public ConnectionState State
        {
            get
            {
                lock (Connections)
                {
                    if (_connections == 0)
                    {
                        return ConnectionState.Open;
                    }
                    return Connections[0].State;
                }
            }
        }

        /// <inheritdoc>
        public IDbTransaction BeginTransaction()
        {
            IDbConnection connection = GetConnection();
            IDbTransaction transaction = connection.BeginTransaction();
            return transaction;
        }

        /// <inheritdoc>
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            IDbConnection connection = GetConnection();
            IDbTransaction transaction = connection.BeginTransaction(isolationLevel);
            return transaction;
        }

        /// <summary>
        /// Accept transaction.
        /// </summary>
        /// <param name="transaction"></param>
        public void CommitTransaction(IDbTransaction transaction)
        {
            //Commit will clear connection
            IDbConnection connection = transaction.Connection;
            transaction.Commit();
            ReleaseConnection(connection);
        }

        /// <summary>
        /// Rollback transaction.
        /// </summary>
        /// <param name="transaction"></param>
        public void RollbackTransaction(IDbTransaction transaction)
        {
            //Commit will clear connection
            IDbConnection connection = transaction.Connection;
            transaction.Rollback();
            ReleaseConnection(connection);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection"></param>
        public GXDbConnection(DbConnection connection, string tablePrefix) :
            this(new DbConnection[] { connection }, tablePrefix)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connections"></param>
        public GXDbConnection(DbConnection[] connections, string tablePrefix)
        {
            if (connections == null || connections.Length == 0)
            {
                throw new ArgumentException(nameof(connections));
            }
            Type tp = connections[0].GetType();
            foreach (DbConnection it in connections)
            {
                if (it.GetType() != tp)
                {
                    throw new ArgumentException("All database connnections must be the same type.");
                }
            }
            ConnectionString = connections[0].ConnectionString;
            if (connections[0].State != ConnectionState.Open)
            {
                connections[0].Open();
            }
            AutoTransaction = true;
            string name = tp.Name;
            DatabaseType type;
            if (name == "SQLiteConnection")
            {
                type = DatabaseType.SqLite;
            }
            else if (name == "MySqlConnection")
            {
                type = DatabaseType.MySQL;
            }
            else if (name == "SqlConnection")
            {
                type = DatabaseType.MSSQL;
            }
            else if (name == "OracleConnection")
            {
                type = DatabaseType.Oracle;
            }
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
            else if (name == "OdbcConnection")
            {
                name = tp.GetProperty("DataSource").GetValue(connections[0], null).ToString();
                if (name == "ACCESS")
                {
                    type = DatabaseType.Access;
                }
                else
                {
                    if (connections[0].ServerVersion.Contains("Oracle"))
                    {
                        type = DatabaseType.Oracle;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Unknown connection.");
                    }
                }
            }
            else if (name == "OleDbConnection")
            {
                name = tp.GetProperty("DataSource").GetValue(connections[0], null).ToString();
                if (string.Compare(Path.GetExtension(name), ".mdb", true) == 0)
                {
                    type = DatabaseType.Access;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Unknown connection.");
                }
            }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
            else
            {
                throw new ArgumentOutOfRangeException("Unknown connection.");
            }
            Connections = connections;
            _connections = Connections.Length;
            ConnectionReleased = new Semaphore(_connections, _connections);
            Builder = new GXSqlBuilder(type, tablePrefix);
            Builder.Settings.ServerVersion = connections[0].ServerVersion;
        }

        /// <summary>
        /// Is datetime saved in universal time.
        /// </summary>
        [DefaultValue(false)]
        public bool UniversalTime
        {
            get
            {
                return Builder.Settings.UniversalTime;
            }
            set
            {
                Builder.Settings.UniversalTime = value;
            }
        }

        /// <summary>
        /// Get tables to create.
        /// </summary>
        /// <param name="type">Parent table type</param>
        /// <param name="tables">Collection of tables to create.</param>
        internal static void GetTables(Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            if (!tables.ContainsKey(type))
            {
                foreach (var it in GXSqlBuilder.GetProperties(type))
                {
                    if (it.Value.Relation != null)
                    {
                        //Add primary table.
                        if (!tables.ContainsKey(type))
                        {
                            tables.Add(type, it.Value);
                        }
                        //Add 1:n and n:n relation tables.
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(it.Value.Type))
                        {
                            GetTables(GXInternal.GetPropertyType(it.Value.Type), tables);
                        }
                        else if (!GXInternal.IsGenericDataType(it.Value.Type))
                        {
                            GetTables(it.Value.Type, tables);
                        }
                        //Add 1:1 relation table.
                        if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                        {
                            GetTables(it.Value.Relation.ForeignTable, tables);
                            if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                            {
                                tables.Add(it.Value.Relation.ForeignTable, it.Value.Relation.ForeignId);
                            }
                        }
                        //Add relation map table.
                        if (it.Value.Relation.RelationMapTable != null)
                        {
                            if (!tables.ContainsKey(it.Value.Relation.RelationMapTable.Relation.PrimaryTable))
                            {
                                tables.Add(it.Value.Relation.RelationMapTable.Relation.PrimaryTable, it.Value.Relation.RelationMapTable.Relation.PrimaryId);
                            }
                        }
                    }
                }
            }
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
        /// Returns existing relation tables.
        /// </summary>
        public Type[] GetRelationTables<T>()
        {
            List<Type> list = new List<Type>();
            Type type = typeof(T);
            Dictionary<Type, GXSerializedItem> tables = new Dictionary<Type, GXSerializedItem>();
            GetTables(type, tables);
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
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable(IDbTransaction transaction, Type type, bool relations, bool overwrite)
        {
            Dictionary<Type, GXSerializedItem> tables = new Dictionary<Type, GXSerializedItem>();
            if (relations)
            {
                GetTables(type, tables);
            }
            if (!tables.ContainsKey(type))
            {
                tables.Add(type, null);
            }
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = GetConnection();
                if (AutoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
            }
            try
            {
                //Find dropped tables.
                Dictionary<Type, GXSerializedItem> dropTables = new Dictionary<Type, GXSerializedItem>();
                foreach (var it in tables)
                {
                    Type tmp = it.Key;
                    if (TableExist(connection, transaction, Builder.GetTableName(tmp, false)))
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
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UpdateTable<T>()
        {
            UpdateTable(typeof(T));
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
                connection = GetConnection();
                if (AutoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
            }
            try
            {
                //Add new columns.
                foreach (var it in GXSqlBuilder.GetProperties(type))
                {
                    if (!cols.Contains(it.Key))
                    {
                        if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
                        {
                            if (it.Value.Relation.RelationType == RelationType.OneToOne ||
                                it.Value.Relation.RelationType == RelationType.OneToMany ||
                                it.Value.Relation.RelationType == RelationType.ManyToMany)
                            {
                                continue;
                            }
                        }
                        StringBuilder sb = new StringBuilder();
                        sb.Append("ALTER TABLE ");
                        sb.Append(GXDbHelpers.AddQuotes(tableName,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.TableQuotation));
                        sb.Append(" ADD ");
                        sb.Append(GXDbHelpers.AddQuotes(it.Key,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.ColumnQuotation));
                        sb.Append(" ");
                        sb.Append(GetDataBaseType(it.Value.Type, it.Value.Target));
                        sb.Append(" ");
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
                            GetDefaultValue(sb, it.Value.DefaultValue);
                        }
                        ExecuteNonQuery(transaction, sb.ToString());
                    }
                }
                //TODO: Check is column type changed.
                /*
                int len;
                foreach (string col in cols)
                {
                    Type colType = GetColumnType(tableName, col, connection, out len);
                }
                */
                if (AutoTransaction && transaction != null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                if (AutoTransaction && transaction != null)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Create new view.
        /// </summary>
        /// <param name="type">View type.</param>
        /// <param name="map">How columns are mapped between select and view.</param>
        /// <param name="select">How data is retreaved from the tables.</param>
        /// <param name="overwrite">Old view is dropped if exists.</param>
        public void CreateView(GXCreateViewArgs map, GXSelectArgs select, bool overwrite)
        {

        }

        class GXTableCreateQuery
        {
            public Type Table;
            /// <summary>
            /// List of queries to execute.
            /// </summary>
            public List<string> Queries = new List<string>();
            /// <summary>
            /// List of tables that must create first.
            /// </summary>
            public List<GXTableCreateQuery> Dependencies = new List<GXTableCreateQuery>();

            /// <inheritdoc/>
            public override string ToString()
            {
                string str = null;
                foreach (var it in Dependencies)
                {
                    str += GXDbHelpers.GetTableName(it.Table, false, null) + ", ";
                }
                return GXDbHelpers.GetTableName(Table, false, null) + " depends from : " + str;
            }

            public bool CheckDependency(GXTableCreateQuery debency)
            {
                //Check that there is not cross reference.
                foreach (var it in debency.Dependencies)
                {
                    if (it == this)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void AddDependency(GXTableCreateQuery debency)
            {
                //Check that there are no cross references.
                foreach (var it in debency.Dependencies)
                {
                    if (it == this)
                    {
                        throw new ArgumentException("Cross reference between " + debency.Table.Name + " and " + this.Table.Name);
                    }
                    if (it == debency)
                    {
                        throw new ArgumentException("Debency already added: " + debency.Table.Name);
                    }
                }
                Dependencies.Add(debency);
            }
        }

        /// <summary>
        /// Get all tables that need to create in relation tree.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tables"></param>
        /// <param name="tablesCreationQueries"></param>
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
                tableItem = new GXTableCreateQuery();
                tableItem.Table = type;
                StringBuilder sb = new StringBuilder();
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
                    if (!create)//Drop table.
                    {
                        sb.Append("DROP TABLE ");
                        sb.Append(GXDbHelpers.AddQuotes(tableName,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.TableQuotation));
                        tableItem.Queries.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else//Create table.
                    {
                        sb.Append("CREATE TABLE ");
                        sb.Append(GXDbHelpers.AddQuotes(tableName,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.TableQuotation));
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
                            relationTables = new Dictionary<Type, GXSerializedItem>();
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
                    List<string> serialized = new List<string>();
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
                            sb.Append(GXDbHelpers.AddQuotes(name,
                                Builder.Settings.DataQuotaReplacement,
                                Builder.Settings.ColumnQuotation));
                            sb.Append(" ");
                            if (!((it.Value.Attributes & (Attributes.AutoIncrement)) != 0 &&
                                (
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                Builder.Settings.Type == DatabaseType.Access ||
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                Builder.Settings.Type == DatabaseType.SqLite)))
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
                                            str = GetDataBaseType(tp, it.Value.Relation.ForeignId.Target);
                                        }
                                        else
                                        {
                                            tp = typeof(int);
                                        }
                                    }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                    if (str == null)
                                    {
                                        str = GetDataBaseType(tp, it.Value.Target);
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
                                str = GetDataBaseType(typeof(int), it.Value);
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
                                if (Builder.Settings.Type == DatabaseType.Access || Builder.Settings.Type == DatabaseType.Oracle)
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
                                GetDefaultValue(sb, it.Value.DefaultValue);
                            }
                            if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type &&
                                    it.Value.Relation.RelationType == RelationType.OneToOne)
                            {
                                fkStr.Append(", ");
                                string pk;
                                if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    GXSerializedItem u = it.Value.Relation.RelationMapTable.Relation.PrimaryId;
                                    pk = GXDbHelpers.GetColumnName(u.Target as PropertyInfo, Builder.Settings.ColumnQuotation);
                                }
                                else
                                {
                                    pk = GXDbHelpers.GetColumnName(it.Value.Relation.ForeignId.Target as PropertyInfo, Builder.Settings.ColumnQuotation);
                                }
                                string table = Builder.GetTableName(it.Value.Relation.PrimaryTable, false);
                                name = it.Key;
                                if (pk == null)
                                {
                                    throw new ArgumentOutOfRangeException(string.Format("Table {0} do not have primary key.",
                                            table));
                                }
                                if (this.Builder.Settings.UpperCase)
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
                                fkStr.Append(GXDbHelpers.AddQuotes(name,
                                    Builder.Settings.DataQuotaReplacement,
                                    Builder.Settings.ColumnQuotation));
                                fkStr.Append(") REFERENCES ");
                                fkStr.Append(GXDbHelpers.AddQuotes(table2,
                                    Builder.Settings.DataQuotaReplacement,
                                    Builder.Settings.TableQuotation));
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

        private void GetDefaultValue(StringBuilder sb, object value)
        {
            sb.Append(" DEFAULT ");
            if (value is Enum)
            {
                if (Builder.Settings.UseEnumStringValue)
                {
                    sb.Append("'" + Convert.ToString(value) + "'");
                }
                else
                {
                    Type type2 = Enum.GetUnderlyingType(value.GetType());
                    sb.Append(Convert.ToString(Convert.ChangeType(value, type2)));
                }
            }
            else if (value is string s)
            {
                sb.Append("'" + s + "'");
            }
            else if (value is bool b)
            {
                sb.Append(b ? 1 : 0);
            }
            else
            {
                sb.Append(Convert.ToString(value));
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
                        sb.Append(GXDbHelpers.AddQuotes(name,
                            Builder.Settings.DataQuotaReplacement,
                            Builder.Settings.ColumnQuotation));
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
                                Builder.Settings.ColumnQuotation));
                            sb.Append(" IS NULL)");
                        }
                        else if (index.ExcludeNull)
                        {
                            sb.Append(" WHERE( ");
                            sb.Append(GXDbHelpers.AddQuotes(name,
                                Builder.Settings.DataQuotaReplacement,
                                Builder.Settings.ColumnQuotation));
                            sb.Append(" IS NOT NULL)");
                        }
                        tableItem.Queries.Add(sb.ToString());
                    }
                }
            }
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
                if (coll.Clustered && Builder.Settings.Type == DatabaseType.MSSQL)
                {
                    //Create clustered index for MSSQL.
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
                sb.Append(GXDbHelpers.AddQuotes(name,
                    Builder.Settings.DataQuotaReplacement,
                    Builder.Settings.ColumnQuotation));
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

        /// <summary>
        /// Create or drop selected table and it's dependencies.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="table"></param>
        private void TableCreation(IDbConnection connection, bool create, IDbTransaction transaction, GXTableCreateQuery table, List<Type> created)
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
                    ExecuteNonQuery(connection, transaction, q);
                }
            }
        }

        private void CreateTable(IDbConnection connection, IDbTransaction transaction, Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(true, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(connection, true, transaction, it.Value, created);
            }
        }

        private void DropTable(IDbConnection connection, IDbTransaction transaction, Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(false, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(connection, false, transaction, it.Value, created);
            }
        }

        static private string GetIndexName(string table, string column)
        {
            return string.Format("{0}{1}", table, column).ToLower();
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
        /// Force to drop all relation tables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ForceDropTable<T>()
        {
            List<Type> failed = new List<Type>();
            Type[] list = GetRelationTables<T>();
            foreach (Type it in list)
            {
                try
                {
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
                ForceDropTable<T>();
            }
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable(Type type, bool relations)
        {
            DropTable(null, type, relations);
        }

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="relations">Are relation tables dropped also.</param>
        public void DropTable(IDbTransaction transaction, Type type, bool relations)
        {
            string table = Builder.GetTableName(type, false);
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = GetConnection();
                if (AutoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
            }
            try
            {
                if (TableExist(connection, transaction, table))
                {
                    Dictionary<Type, GXSerializedItem> tables = new Dictionary<Type, GXSerializedItem>();
                    if (relations)
                    {
                        GetTables(type, tables);
                    }
                    if (!tables.ContainsKey(type))
                    {
                        tables.Add(type, null);
                    }
                    for (int pos = 0; pos != tables.Count; ++pos)
                    {
                        Type it = tables.Keys.ElementAt(pos);
                        if (!TableExist(connection, transaction, Builder.GetTableName(it, false)))
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
                                    ExecuteNonQuery(transaction, it2);
                                }
                            }
                        }
                    }
                }
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        public T ExecuteScalar<T>(string query)
        {
            IDbConnection connection = GetConnection();
            try
            {
                return (T)ExecuteScalarInternal(connection, null, query, typeof(T));
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        private static object ExecuteScalarInternal(IDbConnection connection, IDbTransaction transaction, string query, Type type)
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
                ex.Data.Add("SQL", query);
                throw;
            }
        }

        /// <summary>
        /// Execute given query.
        /// </summary>
        /// <param name="query">Query to execute.</param>
        public void ExecuteNonQuery(string query)
        {
            ExecuteNonQuery(null, query);
        }

        /// <summary>
        /// Execute given query.
        /// </summary>
        /// <param name="transaction">Used transaction.</param>
        /// <param name="query">Query to execute.</param>
        public void ExecuteNonQuery(IDbTransaction transaction, string query)
        {
            IDbConnection connection;
            if (transaction == null)
            {
                connection = GetConnection();
            }
            else
            {
                connection = transaction.Connection;
            }
            try
            {
                ExecuteNonQuery(connection, transaction, query);
            }
            finally
            {
                if (transaction == null)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Execute given query.
        /// </summary>
        /// <param name="transaction">Used transaction.</param>
        /// <param name="query">Query to execute.</param>
        private void ExecuteNonQuery(IDbConnection connection, IDbTransaction transaction, string query)
        {
            DateTime now = DateTime.Now;
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
                ex.Data.Add("SQL", query);
                throw;
            }
            if (sql != null)
            {
                sql(this, query, (int)(DateTime.Now - now).TotalMilliseconds);
            }
        }
        /// <summary>
        /// Returns last inserted ID.
        /// </summary>
        /// <returns>Last inserted row ID.</returns>
        private object GetLastInsertId(IDbConnection connection, IDbTransaction transaction, Type valueType, string columnName, Type tableType)
        {
            string name = null, table = null;
            if (tableType != null)
            {
                if (Builder.Settings.Type == DatabaseType.Oracle)
                {
                    table = Builder.GetTableName(tableType, false);
                }
                else
                {
                    table = Builder.GetTableName(tableType, true);
                }
                name = " FROM " + table;
            }
            switch (Builder.Settings.Type)
            {
                case DatabaseType.MySQL:
                    return ExecuteScalarInternal(connection, transaction, "SELECT LAST_INSERT_ID()" + name, valueType);
                case DatabaseType.Oracle:
                    return ExecuteScalarInternal(connection, transaction, "SELECT " + Gurux.Service.Orm.Settings.GXOracleSqlSettings.GetSequenceName(table, columnName) + ".CURRVAL FROM dual", valueType);
                case DatabaseType.MSSQL:
                    return ExecuteScalarInternal(connection, transaction, "SELECT @@IDENTITY" + name, valueType);
                case DatabaseType.SqLite:
                    return ExecuteScalarInternal(connection, transaction, "SELECT last_insert_rowid()" + name, valueType);
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                case DatabaseType.Access:
                    return ExecuteScalarInternal(connection, transaction, "SELECT @@IDENTITY" + name, valueType);
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                default:
                    throw new ArgumentOutOfRangeException("GetLastInsertId failed. Unknown database connection.");
            }
        }

        public bool TableExist<T>()
        {
            return TableExist(typeof(T));
        }

        public bool TableExist(Type type)
        {
            return TableExist(Builder.GetTableName(type, false));
        }

        public string[] GetColumns<T>()
        {
            return GetColumns(typeof(T));
        }

        public string[] GetColumns(Type type)
        {
            string tableName = Builder.GetTableName(type, false);
            return GetColumns(tableName);
        }

        public string[] GetColumns(string tableName)
        {
            IDbConnection connection = GetConnection();
            try
            {
                return GetColumnsInteral(tableName, Builder.Settings.Type, connection);
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        private string[] GetColumnsInteral(string tableName, DatabaseType type, IDbConnection connection)
        {
            string query;
            int index = 0;
            List<string> list;
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
            if (type == DatabaseType.Access)
            {
                DataTable dt;
                if (connection as System.Data.OleDb.OleDbConnection != null)
                {
                    dt = (connection as System.Data.OleDb.OleDbConnection).GetSchema("Columns", new string[] { null, null, tableName });
                }
                else
                {
                    dt = (connection as System.Data.Odbc.OdbcConnection).GetSchema("Columns", new string[] { null, null, tableName });
                }
                list = new List<string>(dt.Rows.Count);
                foreach (DataRow it in dt.Rows)
                {
                    list.Add(it[3].ToString());
                }
                return list.ToArray();
            }
            else
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0
            {
                query = Builder.Settings.GetColumnsQuery(connection.Database, tableName, out index);
            }
            list = new List<string>();
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
                            list.Add(reader.GetString(index));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("SQL", query);
                throw;
            }
            return list.ToArray();
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
                ex.Data.Add("SQL", query);
                throw;
            }
            return false;
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
                ex.Data.Add("SQL", query);
                throw;
            }
            return targetTable;
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
                                ret = Builder.Settings.IsPrimaryKey(tmp);
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("SQL", query);
                throw;
            }
            return ret;
        }

        /// <summary>
        /// Check are there any relations for this column.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private bool GetRelationsQuery(string tableName, string columnName, DbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetPrimaryKeyQuery(connection.Database, tableName, columnName);
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
                                ret = Builder.Settings.IsPrimaryKey(tmp);
                            }
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("SQL", query);
                throw;
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
                ex.Data.Add("SQL", query);
                throw;
            }
            return ret;
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
                ex.Data.Add("SQL", query);
                throw;
            }
            return list.ToArray();
        }

        private string GetColumnDefaultValueQuery(string tableName, string columnName, IDbConnection connection)
        {
            string query = Builder.Settings.GetColumnDefaultValueQuery(connection.Database, tableName, columnName);
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
                ex.Data.Add("SQL", query);
                throw;
            }
            return "";
        }

        private Type GetColumnType(string tableName, string columnName, IDbConnection connection, out int len)
        {
            string str = null;
            len = 0;
            string query = Builder.Settings.GetColumnTypeQuery(connection.Database, tableName, columnName);
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
                            str = reader.GetString(0);
                            if (reader.FieldCount > 1)
                            {
                                object tmp = reader.GetValue(1);
                                if (tmp != null && !(tmp is DBNull))
                                {
                                    len = Convert.ToInt32(tmp);
                                }
                                if (len < 1 && reader.FieldCount > 2)
                                {
                                    tmp = reader.GetValue(2);
                                    if (tmp != null && !(tmp is DBNull))
                                    {
                                        len = Convert.ToInt32(tmp);
                                    }
                                }
                                break;
                            }
                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("SQL", query);
                throw;
            }
            Type type = Builder.GetDataType(str, len);
            if (type == null)
            {
                throw new ArgumentException("Invalid data type: " + str);
            }
            return type;
        }

        public string[] GetTables()
        {
            string query;
            IDbConnection connection = GetConnection();
            try
            {
                switch (Builder.Settings.Type)
                {
                    case DatabaseType.MySQL:
                        query = string.Format("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'",
                            connection.Database);
                        break;
                    case DatabaseType.MSSQL:
                        query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
                        break;
                    case DatabaseType.Oracle:
                        query = "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";
                        break;
                    case DatabaseType.SqLite:
                        query = "SELECT NAME FROM sqlite_master WHERE type='table'";
                        break;
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
                case DatabaseType.Access:
                    DataTable dt;
                    if (connection as System.Data.OleDb.OleDbConnection != null)
                    {
                        dt = (connection as System.Data.OleDb.OleDbConnection).GetSchema("Tables", new string[] { null, null, null });
                    }
                    else
                    {
                        dt = (connection as System.Data.Odbc.OdbcConnection).GetSchema("Tables", new string[] { null, null, null });
                    }
                    List<string> list = new List<string>();
                    foreach (DataRow it in dt.Rows)
                    {
                        if (it[3].ToString() == "TABLE")
                        {
                            list.Add(it[2].ToString());
                        }
                    }
                    return list.ToArray();
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0
                    default:
                        throw new ArgumentOutOfRangeException("TableExist failed. Unknown database connection.");
                }
                return ((List<string>)SelectInternal<string>(connection, query)).ToArray();
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Check is table empty.
        /// </summary>
        /// <returns>True, if thable is empty.</returns>
        public bool IsEmpty<T>()
        {
            string tableName = Builder.GetTableName(typeof(T), false);
            IDbConnection connection = GetConnection();
            try
            {
                string query = "SELECT 1 WHERE EXISTS(SELECT 1 FROM " + tableName + ")";
                object ret = ExecuteScalarInternal(connection, null, query, null);
                return ret == null || Convert.ToInt32(ret) == 0;
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Check if table exists.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        public bool TableExist(string tableName)
        {
            IDbConnection connection = GetConnection();
            try
            {
                return TableExist(connection, null, tableName);
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }
        /// <summary>
        /// Check if table exists.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        private bool TableExist(IDbConnection connection, IDbTransaction transaction, string tableName)
        {
            string query;
            switch (Builder.Settings.Type)
            {
                case DatabaseType.MySQL:
                    query = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}'",
                        tableName, connection.Database);
                    break;
                case DatabaseType.MSSQL:
                    query = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'",
                        tableName);
                    break;
                case DatabaseType.Oracle:
                    query = string.Format("SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = '{0}'", tableName.ToUpper());
                    break;
                case DatabaseType.SqLite:
                    query = string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = '{0}'", tableName);
                    break;
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
                case DatabaseType.Access:
                    DataTable dt;
                    if (connection as System.Data.OleDb.OleDbConnection != null)
                    {
                        dt = (connection as System.Data.OleDb.OleDbConnection).GetSchema("Tables", new string[] { null, null, tableName });
                    }
                    else
                    {
                        dt = (connection as System.Data.Odbc.OdbcConnection).GetSchema("Tables", new string[] { null, null, tableName });
                    }
                    return dt.Rows.Count != 0;
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0
                default:
                    throw new ArgumentOutOfRangeException("TableExist failed. Unknown database connection.");
            }
            return (int)ExecuteScalarInternal(connection, transaction, query, typeof(int)) != 0;
        }

        private string GetDataBaseType(Type type, object target)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            //100 character is allocated for type.
            if (type == typeof(Type))
            {
                return this.Builder.Settings.StringColumnDefinition(100);
            }
            else if (type.IsEnum)
            {
                if (this.Builder.Settings.UseEnumStringValue)
                {
                    StringLengthAttribute[] attr = (StringLengthAttribute[])type.GetCustomAttributes(typeof(StringLengthAttribute), true);
                    if (attr.Length == 0)
                    {
                        return this.Builder.Settings.StringColumnDefinition(100);
                    }
                    return this.Builder.Settings.StringColumnDefinition(attr[0].MaximumLength);
                }
                else
                {
                    return GetDataBaseType(Enum.GetUnderlyingType(type), null);
                }
            }
            else if (type == typeof(string) || type == typeof(char[]) || type == typeof(object))
            {
                if (target is PropertyInfo)
                {
                    StringLengthAttribute[] attr = (StringLengthAttribute[])(target as PropertyInfo).GetCustomAttributes(typeof(StringLengthAttribute), true);
                    if (attr.Length == 0)
                    {
                        return this.Builder.Settings.StringColumnDefinition(0);
                    }
                    return this.Builder.Settings.StringColumnDefinition(attr[0].MaximumLength);
                }
                return this.Builder.Settings.StringColumnDefinition(0);
            }
            if (type.IsArray && type != typeof(byte[]) && type != typeof(char[]))
            {
                return GetDataBaseType(GXInternal.GetPropertyType(type), null);
            }
            else if (!Builder.DbTypeMap.ContainsKey(type))
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    type = GXInternal.GetPropertyType(type);
                    foreach (var i in type.GetInterfaces())
                    {
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnique<>))
                        {
                            type = i.GenericTypeArguments[0];
                            break;
                        }
                    }
                }
                else if (GXInternal.IsGenericDataType(type))
                {
                    throw new Exception("Invalid data type: " + type.Name);
                }
                else
                {
                    throw new Exception("Invalid data type: " + type.Name + ". Make sure that you have added ForeignKey attribute to the property.");
                }
            }
            return Builder.DbTypeMap[type];
        }

        public async Task DeleteAsync(GXDeleteArgs arg)
        {
            await DeleteAsync(arg, CancellationToken.None);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task DeleteAsync(GXDeleteArgs arg, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                Delete(arg);
            });
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task DeleteAsync(IDbTransaction transaction, GXDeleteArgs arg, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                Delete(transaction, arg);
            });
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        public void Delete(GXDeleteArgs arg)
        {
            Delete(null, arg);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        public void Delete(IDbTransaction transaction, GXDeleteArgs arg)
        {
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = GetConnection();
                if (AutoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
            }
            try
            {
                arg.Settings = Builder.Settings;
                ExecuteNonQuery(connection, transaction, arg.ToString());
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <param name="id">Item's ID.</param>
        public T SelectById<T>(string id)
        {
            return SelectById<T>(id, null);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T>(string id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            throw new Exception("There are multiple items with same ID when id should be unique.");
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public async Task<T> SelectByIdAsync<T>(string id, CancellationToken cancellationToken)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public async Task<T> SelectByIdAsync<T>(string id, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            List<T> list;
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            IDbConnection connection = TryGetConnection(1000);
            if (connection != null)
            {
                try
                {
                    list = SelectInternal<T>(connection, null, args, cancellationToken);
                    if (list.Count == 0)
                    {
                        return default(T);
                    }
                    if (list.Count == 1)
                    {
                        return list[0];
                    }
                    throw new Exception("There are multiple items with same ID when id should be unique.");
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
            return await Task.Run(() =>
            {
                list = Select<T>(null, args, cancellationToken);
                if (list.Count == 0)
                {
                    return default(T);
                }
                if (list.Count == 1)
                {
                    return list[0];
                }
                throw new Exception("There are multiple items with same ID when id should be unique.");
            });
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <param name="id">Item's ID.</param>
        public T SelectById<T>(Guid id)
        {
            return SelectById<T>(id, null);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T>(Guid id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            throw new Exception("There are multiple items with same ID when id should be unique.");
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public async Task<T> SelectByIdAsync<T>(Guid id, CancellationToken cancellationToken)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public async Task<T> SelectByIdAsync<T>(Guid id, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            List<T> list;
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            IDbConnection connection = TryGetConnection(1000);
            if (connection != null)
            {
                try
                {
                    list = SelectInternal<T>(connection, null, args, cancellationToken);
                    if (list.Count == 0)
                    {
                        return default(T);
                    }
                    if (list.Count == 1)
                    {
                        return list[0];
                    }
                    throw new Exception("There are multiple items with same ID when id should be unique.");
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
            return await Task.Run(() =>
            {
                list = Select<T>(null, args, cancellationToken);
                if (list.Count == 0)
                {
                    return default(T);
                }
                if (list.Count == 1)
                {
                    return list[0];
                }
                throw new Exception("There are multiple items with same ID when id should be unique.");
            });
        }


        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <param name="id">Item's ID.</param>
        public T SelectById<T>(long id)
        {
            return SelectById<T>(id, null);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T>(long id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            throw new Exception("There are multiple items with same ID when id should be unique.");
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public async Task<T> SelectByIdAsync<T>(long id, CancellationToken cancellationToken)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public async Task<T> SelectByIdAsync<T>(long id, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            List<T> list;
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            IDbConnection connection = TryGetConnection(1000);
            if (connection != null)
            {
                try
                {
                    list = SelectInternal<T>(connection, null, args, cancellationToken);
                    if (list.Count == 0)
                    {
                        return default(T);
                    }
                    if (list.Count == 1)
                    {
                        return list[0];
                    }
                    throw new Exception("There are multiple items with same ID when id should be unique.");
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
            return await Task.Run(() =>
            {
                list = Select<T>(null, args, cancellationToken);
                if (list.Count == 0)
                {
                    return default(T);
                }
                if (list.Count == 1)
                {
                    return list[0];
                }
                throw new Exception("There are multiple items with same ID when id should be unique.");
            });
        }

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <param name="id">Item's ID.</param>
        public T SelectById<T>(UInt64 id)
        {
            return SelectById<T>(id, null);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T>(UInt64 id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            throw new Exception("There are multiple items with same ID when id should be unique.");
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        public async Task<T> SelectByIdAsync<T>(UInt64 id, CancellationToken cancellationToken)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public async Task<T> SelectByIdAsync<T>(UInt64 id, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            List<T> list;
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = this.Builder.Settings;
            IDbConnection connection = TryGetConnection(1000);
            if (connection != null)
            {
                try
                {
                    list = SelectInternal<T>(connection, null, args, cancellationToken);
                    if (list.Count == 0)
                    {
                        return default(T);
                    }
                    if (list.Count == 1)
                    {
                        return list[0];
                    }
                    throw new Exception("There are multiple items with same ID when id should be unique.");
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
            return await Task.Run(() =>
            {
                list = Select<T>(null, args, cancellationToken);
                if (list.Count == 0)
                {
                    return default(T);
                }
                if (list.Count == 1)
                {
                    return list[0];
                }
                throw new Exception("There are multiple items with same ID when id should be unique.");
            });
        }



        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T, ID_TYPE>(ID_TYPE id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T, ID_TYPE>(id, columns);
            args.Settings = this.Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            throw new Exception("There are multiple items with same ID when id should be unique.");
        }

        public List<T> SelectAll<T>()
        {
            return Select<T>((GXSelectArgs)null);
        }

        public async Task<List<T>> SelectAllAsync<T>()
        {
            return await SelectAsync<T>((GXSelectArgs)null, CancellationToken.None);
        }

        class GXColumnHelper
        {
            /// <summary>
            /// Column name.
            /// </summary>
            public string Name;
            /// <summary>
            /// Table name.
            /// </summary>
            public string Table;

            public Type TableType;

            /// <summary>
            /// Property setter.
            /// </summary>
            public GXSerializedItem Setter;

            /// <inheritdoc/>
            public override string ToString()
            {
                return Table + "." + Name;
            }
        }

        /// <summary>
        /// Initialize select. Save table indexes and column setters to make data handling faster.
        /// </summary>
        private static void InitializeSelect<T>(IDataReader reader,
            GXDBSettings settings,
            Dictionary<Type, GXSerializedItem> tables,
            Dictionary<Type, int> TableIndexes,
            Dictionary<int, GXColumnHelper> columns,
            Dictionary<Type, List<object>> mapTable,
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters)
        {
            GXSerializedItem si;
            string name;
            int tmp;
            Type tp;
            Dictionary<string, GXSerializedItem> properties = null;
            Type tableType = null;
            string lastTable = string.Empty;
            DataTable schema = null;
            int pos = 0, tableIndex = -1;
            if (tables.Count == 1)
            {
                tableType = typeof(T);
            }
            else if (!settings.SelectUsingAs)
            {
                schema = reader.GetSchemaTable();
                if (schema.Columns[10].ColumnName == "BaseTableName")
                {
                    tableIndex = 10;
                }
                else
                {
                    foreach (DataColumn index in schema.Columns)
                    {
                        if (index.ColumnName == "BaseTableName")
                        {
                            tableIndex = pos;
                            break;
                        }
                        ++pos;
                    }
                    if (tableIndex == -1)
                    {
                        throw new ArgumentOutOfRangeException("Table name not found.");
                    }
                }
            }

            for (pos = 0; pos != reader.FieldCount; ++pos)
            {
                GXColumnHelper c = new GXColumnHelper();
                //Get column and table name.
                name = reader.GetName(pos);
                //If table name is returned in schema.
                if (schema != null)
                {
                    tmp = name.LastIndexOf('.');
                    if (tmp != -1)
                    {
                        c.Name = name.Substring(tmp + 1);
                        c.Table = name.Substring(0, tmp);
                    }
                    else
                    {
                        c.Name = name;
                        c.Table = schema.Rows[pos].ItemArray[tableIndex].ToString();
                    }
                    if (string.IsNullOrEmpty(c.Table))
                    {
                        throw new Exception("Database must use SelectUsingAs attribute.");
                    }
                }
                else
                {
                    tmp = name.LastIndexOf('.');
                    if (tmp == -1)
                    {
                        c.Table = GXDbHelpers.GetTableName(tableType, false, '\0', null);
                        c.Name = name;
                        if (name[0] == settings.ColumnQuotation)
                        {
                            c.Name = name.Substring(1, name.Length - 2);
                        }
                    }
                    else
                    {
                        c.Table = name.Substring(0, tmp);
                        c.Name = name.Substring(tmp + 1);
                    }
                }
                //If table has change.
                if (lastTable.CompareTo(c.Table) != 0)
                {
                    si = null;
                    foreach (var it in tables)
                    {
                        if (string.Compare(GXDbHelpers.GetTableName(it.Key, false, '\0', null, false), c.Table, true) == 0 ||
                            string.Compare(GXDbHelpers.OriginalTableName(it.Key), c.Table, true) == 0)
                        {
                            si = it.Value;
                            break;
                        }
                    }
                    //If there is only one table.
                    if (si == null)
                    {
                        tableType = typeof(T);
                    }
                    else if (si.Relation != null)
                    {
                        tableType = si.Relation.PrimaryTable;
                    }
                    else
                    {
                        tableType = (si.Target as PropertyInfo).ReflectedType;
                    }
                    properties = GXSqlBuilder.GetProperties(tableType);
                    lastTable = c.Table;
                    if (tables.Count != 1)
                    {
                        //Find Relation table setter.
                        foreach (var it in properties)
                        {
                            if (it.Value.Relation != null && it.Value.Relation.RelationType != RelationType.OneToOne &&
                                it.Value.Relation.RelationType != RelationType.Relation &&
                                GXInternal.GetPropertyType(it.Value.Type) == it.Value.Relation.ForeignTable)
                            {
                                Dictionary<Type, GXSerializedItem> list;
                                if (it.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    tp = ((ForeignKeyAttribute[])(it.Value.Target as PropertyInfo).GetCustomAttributes(typeof(ForeignKeyAttribute), true))[0].MapTable;
                                    if (!mapTable.ContainsKey(tp))
                                    {
                                        if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                                        {
                                            continue;
                                        }
                                        List<object> list2 = new List<object>();
                                        mapTable.Add(tp, list2);
                                        list2.Add(it.Value.Relation.PrimaryTable);
                                        list2.Add(it.Value.Relation.ForeignTable);
                                        GXSerializedItem t = new GXSerializedItem();
                                        list = new Dictionary<Type, GXSerializedItem>();
                                        relationDataSetters.Add(tp, list);
                                        list.Add(it.Value.Relation.ForeignTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.ForeignTable));
                                        list.Add(it.Value.Relation.PrimaryTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.PrimaryTable));
                                    }
                                }
                                tp = GXInternal.GetPropertyType(it.Value.Type);
                                if (!tables.ContainsKey(it.Value.Relation.ForeignTable))
                                {
                                    continue;
                                }
                                if (relationDataSetters.ContainsKey(tp))
                                {
                                    if (relationDataSetters[tp].ContainsKey(it.Value.Type))
                                    {
                                        list = relationDataSetters[tp];
                                        if (list.ContainsKey(tableType))
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        list = new Dictionary<Type, GXSerializedItem>();
                                    }
                                }
                                else
                                {
                                    list = new Dictionary<Type, GXSerializedItem>();
                                    relationDataSetters.Add(tp, list);
                                }
                                list.Add(tableType, it.Value);
                            }
                        }
                    }
                }
                if (properties.Count != 0 && properties.ContainsKey(c.Name))
                {
                    columns.Add(pos, c);
                    c.Setter = properties[c.Name];
                    //Add table index position.
                    if (TableIndexes != null && (c.Setter.Attributes & Attributes.PrimaryKey) != 0)
                    {
                        if (!TableIndexes.ContainsKey(tableType))
                        {
                            TableIndexes.Add(tableType, pos);
                        }
                    }
                }
                c.TableType = tableType;
            }
        }

        private object SelectInternal<T>(IDbConnection connection, string query)
        {
            object value = null, item, id = null;
            List<object[]> objectList = null;
            List<T> baseList = null;
            List<T> list = null;
            Dictionary<string, GXSerializedItem> properties = null;
            object[] values = null;
            Dictionary<Type, GXSerializedItem> tables = null;
            Type type = typeof(T);
            //Dictionary of read tables by name.
            string maintable = Builder.GetTableName(type, false);
            Dictionary<int, GXColumnHelper> columns = null;
            Dictionary<Type, int> TableIndexes = null;
            //This is done because every object is created only once in relation data.
            Dictionary<Type, SortedDictionary<object, object>> objects = null;
            string targetTable;
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters = null;
            //If n:n relation is used make lists where relation tables are added by relation type.
            Dictionary<Type, List<object>> mapTables = null;
            //Columns that are updated when row is read. This is needed when relation data is try tu update and it's not read yet.
            List<KeyValuePair<int, object>> UpdatedColumns = new List<KeyValuePair<int, object>>();
            if (typeof(T) == typeof(object[]))
            {
                objectList = new List<object[]>();
            }
            else if (GXInternal.IsGenericDataType(typeof(T)))
            {
                baseList = new List<T>();
            }
            else
            {
                tables = new Dictionary<Type, GXSerializedItem>();
                GetTables(typeof(T), tables);
                //If there are no relations to other tables.
                if (!tables.ContainsKey(type))
                {
                    tables.Add(type, null);
                }
                list = new List<T>();
                columns = new Dictionary<int, GXColumnHelper>();
                //If we are using 1:n or n:n references.
                if (tables.Count != 1)
                {
                    relationDataSetters = new Dictionary<Type, Dictionary<Type, GXSerializedItem>>();
                    TableIndexes = new Dictionary<Type, int>();
                    objects = new Dictionary<Type, SortedDictionary<object, object>>();
                    mapTables = new Dictionary<Type, List<object>>();
                }
            }
            //Read column headers.
            if (columns != null)
            {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
                if (connection is OdbcConnection)
                {
                    using (IDbCommand com = ((OdbcConnection)connection).CreateCommand())
                    {
                        com.CommandType = CommandType.Text;
                        com.CommandText = query;
                        using (IDataReader reader = com.ExecuteReader(CommandBehavior.KeyInfo))
                        {
                            InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            reader.Close();
                        }
                    }
                }
                else if (connection is OleDbConnection)
                {
                    using (IDbCommand com = ((OleDbConnection)connection).CreateCommand())
                    {
                        com.CommandType = CommandType.Text;
                        com.CommandText = query;
                        using (IDataReader reader = com.ExecuteReader(CommandBehavior.KeyInfo))
                        {
                            InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            reader.Close();
                        }
                    }
                }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0

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
                            UpdatedColumns.Clear();
                            if (values == null)
                            {
                                values = new object[reader.FieldCount];
                            }
                            reader.GetValues(values);
                            if (columns != null && columns.Count == 0)
                            {
                                InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            }

                            targetTable = null;
                            if (list != null)
                            {
                                //If we want to read only basic data types example count(*)
                                if (GXInternal.IsGenericDataType(type))
                                {
                                    list.Add((T)GXInternal.ChangeType(reader.GetValue(0), type, Builder.Settings.UniversalTime));
                                    return list;
                                }
                                properties = GXSqlBuilder.GetProperties<T>();
                            }
                            if (objectList != null)
                            {
                                objectList.Add(values);
                            }
                            else if (baseList != null)
                            {
                                baseList.Add((T)Convert.ChangeType(values[0], type));
                            }
                            else
                            {
                                item = null;
                                //If we are reading values from multiple tables each component is created only once.
                                bool isCreated = false;
                                //For Oracle reader.FieldCount is too high. For this reason columns.Count is used.
                                for (int pos = 0; pos != Math.Min(reader.FieldCount, columns.Count); ++pos)
                                {
                                    value = null;
                                    //If we are asking some data from the DB that is not exist on class.
                                    //This is removed from the interface etc...
                                    if (!columns.ContainsKey(pos))
                                    {
                                        continue;
                                    }
                                    GXColumnHelper col = columns[pos];
                                    //If we are reading multiple objects and object has changed.
                                    if (string.Compare(col.Table, targetTable, true) != 0)
                                    {
                                        isCreated = false;
                                        if (TableIndexes != null && TableIndexes.ContainsKey(col.TableType))
                                        {
                                            id = values[TableIndexes[col.TableType]];
                                            if (id == null || id is DBNull)
                                            {
                                                isCreated = true;
                                            }
                                            else
                                            {
                                                if (objects.ContainsKey(col.TableType))
                                                {
                                                    // Check is item already created.
                                                    if (objects[col.TableType].ContainsKey(GXInternal.ChangeType(id, col.Setter.Type, Builder.Settings.UniversalTime)))
                                                    {
                                                        isCreated = true;
                                                    }
                                                }
                                                else
                                                {
                                                    objects.Add(col.TableType, new SortedDictionary<object, object>());
                                                }
                                            }
                                        }
                                        else //If Map table.
                                        {
                                            id = null;
                                        }
                                        if (!isCreated)
                                        {
                                            if (!GXInternal.IsGenericDataType(col.TableType) && item == null || item.GetType() != col.TableType)
                                            {
                                                item = Activator.CreateInstance(col.TableType);
                                                if (item != null && item.GetType() == typeof(T))
                                                {
                                                    list.Add((T)item);
                                                }
                                                //If we are adding map table.
                                                if (mapTables != null && item != null && id == null && mapTables.ContainsKey(item.GetType()))
                                                {
                                                    mapTables[item.GetType()].Add(item);
                                                }
                                            }
                                            if (objects != null && id != null)
                                            {
                                                //Id is not save directly because class might change it's type example from uint to int.
                                                if (GXInternal.IsGenericDataType(col.Setter.Type))
                                                {
                                                    objects[col.TableType].Add(GXInternal.ChangeType(id, col.Setter.Type, Builder.Settings.UniversalTime), item);
                                                }
                                                else //If we are saving table.
                                                {
                                                    objects[col.TableType].Add(id, item);
                                                }
                                            }
                                        }
                                        targetTable = col.Table;
                                    }
                                    if (!isCreated)
                                    {
                                        //If 1:1 relation.
                                        if (objects != null && !GXInternal.IsGenericDataType(col.Setter.Type) &&
                                            !GXInternal.IsGenericDataType(GXInternal.GetPropertyType(col.Setter.Type)) &&
                                            col.Setter.Type.IsClass && col.Setter.Type != typeof(byte[]))
                                        {
                                            Type pt = GXInternal.GetPropertyType(col.Setter.Type);
                                            if (GXInternal.IsGenericDataType(pt))
                                            {
                                                if (!string.IsNullOrEmpty(values[pos].ToString()))
                                                {
                                                    string[] tmp = values[pos].ToString().Split(new char[] { ';' });
                                                    Array items = Array.CreateInstance(pt, tmp.Length);
                                                    int pos2 = -1;
                                                    foreach (string it in tmp)
                                                    {
                                                        items.SetValue(GXInternal.ChangeType(it, pt, Builder.Settings.UniversalTime), ++pos2);
                                                    }
                                                    value = items;
                                                }
                                                else
                                                {
                                                    value = Array.CreateInstance(pt, 0);
                                                }
                                            }
                                            else
                                            {
                                                //Columns relations are updated when all data from the row is read.
                                                UpdatedColumns.Add(new KeyValuePair<int, object>(pos, item));
                                            }
                                        }
                                        else if (col.Setter != null)
                                        {
                                            value = GXInternal.ChangeType(values[pos], col.Setter.Type, Builder.Settings.UniversalTime);
                                        }
                                        else
                                        {
                                            value = values[pos];
                                        }
                                        if (value != null)
                                        {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                            //Access minimum date time is 98, 11, 26.
                                            if (this.Builder.Settings.Type == DatabaseType.Access && value is DateTime &&
                                                ((DateTime)value).Date <= new DateTime(100, 1, 1))
                                            {
                                                value = DateTime.MinValue;
                                            }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                            if (col.Setter.Set != null)
                                            {
                                                col.Setter.Set(item, value);
                                            }
                                            else
                                            {
                                                PropertyInfo pi = col.Setter.Target as PropertyInfo;
                                                if (pi != null)
                                                {
                                                    pi.SetValue(item, value, null);
                                                }
                                                else
                                                {
                                                    FieldInfo fi = col.Setter.Target as FieldInfo;
                                                    fi.SetValue(item, value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //Update columns that was not read yet.
                            foreach (var it in UpdatedColumns)
                            {
                                GXColumnHelper col = columns[it.Key];
                                object relationId = GXInternal.ChangeType(values[it.Key], col.Setter.Relation.ForeignId.Type, Builder.Settings.UniversalTime);
                                if (objects.ContainsKey(col.Setter.Type) && objects[col.Setter.Type].ContainsKey(relationId))
                                {
                                    object relationData = objects[col.Setter.Type][relationId];
                                    col.Setter.Set(it.Value, relationData);
                                }
                            }
                            UpdatedColumns.Clear();
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("SQL", query);
                throw;
            }
            if (list != null)
            {
                //Update relation data.
                if (relationDataSetters != null)
                {
                    Type mapTable = null;
                    foreach (var it in objects)
                    {
                        if (relationDataSetters.ContainsKey(it.Key))
                        {
                            var parents = relationDataSetters[it.Key];
                            foreach (var p in parents)
                            {
                                if (!objects.ContainsKey(p.Key))
                                {
                                    continue;
                                }
                                if (p.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    mapTable = p.Value.Relation.RelationMapTable.Relation.PrimaryTable;
                                }
                                SortedDictionary<object, object> parentList = objects[p.Key];
                                Dictionary<object, List<object>> parentValues = new Dictionary<object, List<object>>();
                                foreach (var p2 in parentList)
                                {
                                    parentValues.Add(p2.Key, new List<object>());
                                }
                                object pId;
                                if (p.Value.Relation.RelationType == RelationType.ManyToMany)
                                {
                                    foreach (object v in mapTables[mapTable])
                                    {
                                        pId = relationDataSetters[mapTable][p.Key].Get(v);
                                        object cId = relationDataSetters[mapTable][p.Value.Relation.ForeignTable].Get(v);
                                        //Loop values and map them to parent id.
                                        foreach (var c in it.Value)
                                        {
                                            object id2 = p.Value.Relation.ForeignId.Get(c.Value);
                                            if (id2.Equals(cId))
                                            {
                                                //Value is null if item is empty in that row.
                                                if (parentValues.ContainsKey(pId))
                                                {
                                                    parentValues[pId].Add(c.Value);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //Loop values and map them to parent id.
                                    foreach (var c in it.Value)
                                    {
                                        if (p.Value.Relation.RelationType != RelationType.Relation)
                                        {
                                            //If FK is primary data type like int.
                                            if (GXInternal.IsGenericDataType(p.Value.Relation.PrimaryId.Type))
                                            {
                                                pId = p.Value.Relation.PrimaryId.Get(c.Value);
                                            }
                                            else //If FK is class.
                                            {
                                                //Get target class.
                                                pId = p.Value.Relation.PrimaryId.Get(c.Value);
                                                //With SQLite there might be some empty rows after delete.
                                                if (pId != null)
                                                {
                                                    //Get ID from target class.
                                                    pId = p.Value.Relation.PrimaryId.Relation.ForeignId.Get(pId);
                                                }
                                            }
                                            //Value is null if item is empty in that row.
                                            if (pId != null && parentValues.ContainsKey(pId))
                                            {
                                                parentValues[pId].Add(c.Value);
                                            }
                                        }
                                    }
                                }
                                //Add collections of child values to the parent.
                                foreach (var p3 in parentValues)
                                {
                                    object p2 = parentList[p3.Key];
                                    GXInternal.SetValue(p2, p.Value.Target, p3.Value);
                                }
                            }
                        }
                    }
                }
                return list;
            }
            if (baseList != null)
            {
                return baseList;
            }
            return objectList;
        }

        public List<T> Select<T>(GXSelectArgs arg)
        {
            return Select<T>(null, arg, CancellationToken.None);
        }

        public List<T> Select<T>(GXSelectArgs arg, CancellationToken cancellationToken)
        {
            return Select<T>(null, arg, cancellationToken);
        }

        public List<T> Select<T>(IDbTransaction transaction, GXSelectArgs arg, CancellationToken cancellationToken)
        {
            return SelectInternal<T>(transaction?.Connection, transaction, arg, cancellationToken);
        }

        private List<T> SelectInternal<T>(
            IDbConnection connection,
            IDbTransaction? transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken)
        {
            if (arg == null)
            {
                arg = GXSelectArgs.SelectAll<T>();
            }
            arg.Verify();
            arg.Parent.Settings = Builder.Settings;
            arg.ExecutionTime = 0;
            DateTime tm = DateTime.Now;
            bool release = connection == null && transaction == null;
            if (release)
            {
                connection = GetConnection();
            }
            if (transaction != null)
            {
                connection = transaction.Connection;
            }
            List<T> value;
            try
            {
                value = (List<T>)SelectInternal2<T>(connection, transaction, arg, cancellationToken);
            }
            finally
            {
                if (release)
                {
                    ReleaseConnection(connection);
                }
            }
            arg.ExecutionTime = (int)(DateTime.Now - tm).TotalMilliseconds;
            if (sql != null)
            {
                sql(this, arg.query, arg.ExecutionTime);
            }
            return value;
        }

        public async Task<List<T>> SelectAsync<T>(IDbTransaction transaction, GXSelectArgs arg)
        {
            return await SelectAsync<T>(transaction, arg, CancellationToken.None);
        }

        public async Task<List<T>> SelectAsync<T>(IDbTransaction transaction, GXSelectArgs arg, CancellationToken cancellationToken)
        {
            bool useTransaction = transaction != null;
            IDbConnection connection;
            if (useTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = TryGetConnection(1000);
            }
            if (connection != null)
            {
                try
                {
                    return SelectInternal<T>(connection, transaction, arg, cancellationToken);
                }
                finally
                {
                    if (!useTransaction)
                    {
                        ReleaseConnection(connection);
                    }
                }
            }
            return await Task.Run(() =>
            {
                return SelectInternal<T>(transaction?.Connection, transaction, arg, cancellationToken);
            });
        }

        public async Task<List<T>> SelectAsync<T>(GXSelectArgs arg)
        {
            return await SelectAsync<T>(arg, CancellationToken.None);
        }

        public async Task<List<T>> SelectAsync<T>(GXSelectArgs arg, CancellationToken cancellationToken)
        {
            IDbConnection connection = TryGetConnection(1000);
            if (connection != null)
            {
                try
                {
                    return SelectInternal<T>(connection, null, arg, cancellationToken);
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
            return await Task.Run(() =>
            {
                return SelectInternal<T>(null, null, arg, cancellationToken);
            });
        }

        private object SelectInternal2<T>(
            IDbConnection connection,
            IDbTransaction? transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.Now;

            object value = null, item, id = null;
            List<object[]> objectList = null;
            List<T> baseList = null;
            List<T> list = null;
            Dictionary<string, GXSerializedItem> properties = null;
            object[] values = null;
            Dictionary<Type, GXSerializedItem> tables = null;
            Type type = typeof(T);
            //Dictionary of read tables by name.
            string maintable = Builder.GetTableName(type, false);
            Dictionary<int, GXColumnHelper> columns = null;
            //Collection of table indexes. 
            Dictionary<Type, int> TableIndexes = null;
            string targetTable;
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters = null;
            //If n:n relation is used make lists where relation tables are added by relation type.
            Dictionary<Type, List<object>> mapTables = null;

            //Columns that are updated when row is read. This is needed when relation data is try to update and it's not read yet.
            List<KeyValuePair<int, object>> rowReferences = null;

            //References are updated when data is read from multiple tables and if there are references for other tables.
            //Parent object is an example for this.
            List<KeyValuePair<object, object>> updatedReferences = null;
            //All created objects. Objects are set to the dictionary by type. This makes it faster to find correct object.
            //In the second dictionary is object ID and object.
            Dictionary<Type, Dictionary<object, object>> allObjects = null;
            //List of 1:N objects.
            Dictionary<Type, Dictionary<object, List<object>>> oneToManyObjects = null;
            //List of N:N objects.
            Dictionary<Type, Dictionary<object, List<object>>> manyToManyObjects = null;

            //Generate SQL again.
            arg.Parent.Updated = true;
            string query = arg.ToString(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(T) == typeof(object[]))
            {
                objectList = new List<object[]>();
            }
            else if (GXInternal.IsGenericDataType(typeof(T)))
            {
                baseList = new List<T>();
            }
            else
            {
                tables = new Dictionary<Type, GXSerializedItem>();
                Dictionary<Type, GXSerializedItem> tmp = new Dictionary<Type, GXSerializedItem>();
                //If there are no relations to other tables.
                if (arg.Joins.List.Count == 0)
                {
                    tmp.Add(type, null);
                }
                else
                {
                    List<GXJoin> joinList = new List<GXJoin>();
                    GXOrderByCollection.UpdateJoins(arg.Parent.Settings, arg.Joins, joinList);
                    foreach (var it in joinList)
                    {
                        if (!tmp.ContainsKey(it.Table1Type))
                        {
                            tmp.Add(it.Table1Type, GXSqlBuilder.FindUnique(it.Table1Type));
                        }
                        if (!tmp.ContainsKey(it.Table2Type))
                        {
                            tmp.Add(it.Table2Type, GXSqlBuilder.FindUnique(it.Table2Type));
                        }
                    }
                }
                ///Loop throw all tables and add only selected tables.
                foreach (var it in tmp)
                {
                    if (arg.Columns.ColumnList.ContainsKey(it.Key))
                    {
                        tables.Add(it.Key, it.Value);
                    }
                }
                list = new List<T>();
                columns = new Dictionary<int, GXColumnHelper>();
                //If we are using 1:n or n:n references.
                if (tables.Count != 1)
                {
                    relationDataSetters = new Dictionary<Type, Dictionary<Type, GXSerializedItem>>();
                    TableIndexes = new Dictionary<Type, int>();
                    mapTables = new Dictionary<Type, List<object>>();
                    rowReferences = new List<KeyValuePair<int, object>>();
                    updatedReferences = new List<KeyValuePair<object, object>>();
                    allObjects = new Dictionary<Type, Dictionary<object, object>>();
                    oneToManyObjects = new Dictionary<Type, Dictionary<object, List<object>>>();
                    manyToManyObjects = new Dictionary<Type, Dictionary<object, List<object>>>();
                }
            }
            //Read column headers.
            if (columns != null)
            {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
                if (connection is OdbcConnection)
                {
                    using (IDbCommand com = ((OdbcConnection)connection).CreateCommand())
                    {
                        com.CommandType = CommandType.Text;
                        com.CommandText = query;
                        using (IDataReader reader = com.ExecuteReader(CommandBehavior.KeyInfo))
                        {
                            InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            reader.Close();
                        }
                    }
                }
                else if (connection is OleDbConnection)
                {
                    using (IDbCommand com = ((OleDbConnection)connection).CreateCommand())
                    {
                        com.CommandType = CommandType.Text;
                        com.CommandText = query;
                        using (IDataReader reader = com.ExecuteReader(CommandBehavior.KeyInfo))
                        {
                            InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            reader.Close();
                        }
                    }
                }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0
            }
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<Type, List<string>> excluded = null;
            if (arg.Columns.Excluded.Any())
            {
                //Add excluded columns.
                excluded = new Dictionary<Type, List<string>>();
                foreach (KeyValuePair<Type, LambdaExpression> e in arg.Columns.Excluded)
                {
                    if (!excluded.ContainsKey(e.Key))
                    {
                        excluded[e.Key] = new List<string>();
                    }
                    string post = null;
                    excluded[e.Key].AddRange(GXDbHelpers.GetMembers(null, e.Value, '\0', false, ref post));
                }
            }
            using (IDbCommand com = connection.CreateCommand())
            {
                com.Transaction = transaction;
                com.CommandType = CommandType.Text;
                com.CommandText = query;
                try
                {
                    var start = DateTime.Now;
                    using (IDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (rowReferences != null)
                            {
                                rowReferences.Clear();
                            }
                            if (updatedReferences != null)
                            {
                                updatedReferences.Clear();
                            }
                            if (values == null)
                            {
                                values = new object[reader.FieldCount];
                            }
                            reader.GetValues(values);
                            if (columns != null && columns.Count == 0)
                            {
                                InitializeSelect<T>(reader, this.Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            }
                            targetTable = null;
                            if (list != null)
                            {
                                //If we want to read only basic data types example count(*)
                                if (GXInternal.IsGenericDataType(type))
                                {
                                    list.Add((T)GXInternal.ChangeType(reader.GetValue(0), type, Builder.Settings.UniversalTime));
                                    return list;
                                }
                                properties = GXSqlBuilder.GetProperties<T>();
                            }
                            if (objectList != null)
                            {
                                objectList.Add(values);
                            }
                            else if (baseList != null)
                            {
                                //If value is nullable.
                                if (values[0] is DBNull &&
                                        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    baseList.Add(default(T));
                                }
                                else
                                {
                                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        baseList.Add((T)values[0]);
                                    }
                                    else
                                    {
                                        baseList.Add((T)Convert.ChangeType(values[0], type));
                                    }
                                }
                            }
                            else
                            {
                                item = null;
                                //If we are reading values from multiple tables each component is created only once.
                                bool isCreated = false;
                                //For Oracle reader.FieldCount is too high. For this reason columns.Count is used.
                                for (int pos = 0; pos != Math.Min(reader.FieldCount, columns.Count); ++pos)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    value = null;
                                    //If we are asking some data from the DB that is not exist on class.
                                    //This is removed from the interface etc...
                                    if (!columns.ContainsKey(pos))
                                    {
                                        continue;
                                    }
                                    GXColumnHelper col = columns[pos];
                                    //If we are reading multiple objects and object has changed.
                                    if (string.Compare(col.Table, targetTable, true) != 0)
                                    {
                                        isCreated = false;
                                        if (TableIndexes != null && TableIndexes.ContainsKey(col.TableType))
                                        {
                                            id = values[TableIndexes[col.TableType]];
                                            //Id is not save directly because class might change it's type example from uint to int.
                                            id = GXInternal.ChangeType(id, col.Setter.Type, Builder.Settings.UniversalTime);
                                            if (id == null || id is DBNull)
                                            {
                                                UpdateReferences(item, relationDataSetters, mapTables, updatedReferences, manyToManyObjects);
                                                isCreated = true;
                                                item = null;
                                            }
                                            else
                                            {
                                                //Check is item created already and return item if it exists.
                                                if (allObjects.ContainsKey(col.TableType))
                                                {
                                                    if (allObjects[col.TableType].ContainsKey(id))
                                                    {
                                                        item = allObjects[col.TableType][id];
                                                        isCreated = true;
                                                        UpdateReferences(item, relationDataSetters, mapTables, updatedReferences, manyToManyObjects);
                                                    }
                                                }
                                            }
                                        }
                                        else //If only one table.
                                        {
                                            id = null;
                                        }
                                        if (!isCreated)
                                        {
                                            if (!GXInternal.IsGenericDataType(col.TableType) && item == null || item.GetType() != col.TableType)
                                            {
                                                item = Activator.CreateInstance(col.TableType);
                                                if (allObjects != null)
                                                {
                                                    if (!allObjects.ContainsKey(col.TableType))
                                                    {
                                                        allObjects.Add(col.TableType, new Dictionary<object, object>());
                                                    }
                                                    //If only one table is read.
                                                    if (id != null)
                                                    {
                                                        allObjects[col.TableType][id] = item;
                                                        if (col.Setter.Set != null)
                                                        {
                                                            col.Setter.Set(item, id);
                                                        }
                                                    }
                                                }
                                                if (item != null && item.GetType() == typeof(T))
                                                {
                                                    list.Add((T)item);
                                                }
                                                UpdateReferences(item, relationDataSetters, mapTables, updatedReferences, manyToManyObjects);
                                            }
                                        }
                                        targetTable = col.Table;
                                    }
                                    if (!isCreated)
                                    {
                                        //If 1:1 relation.
                                        if (rowReferences != null && !GXInternal.IsGenericDataType(col.Setter.Type) &&
                                            !GXInternal.IsGenericDataType(GXInternal.GetPropertyType(col.Setter.Type)) &&
                                            col.Setter.Type.IsClass && col.Setter.Type != typeof(byte[]))
                                        {
                                            Type pt = GXInternal.GetPropertyType(col.Setter.Type);
                                            if (GXInternal.IsGenericDataType(pt))
                                            {
                                                if (!string.IsNullOrEmpty(values[pos].ToString()))
                                                {
                                                    string[] tmp = values[pos].ToString().Split(new char[] { ';' });
                                                    Array items = Array.CreateInstance(pt, tmp.Length);
                                                    int pos2 = -1;
                                                    foreach (string it in tmp)
                                                    {
                                                        items.SetValue(GXInternal.ChangeType(it, pt, Builder.Settings.UniversalTime), ++pos2);
                                                    }
                                                    value = items;
                                                }
                                                else
                                                {
                                                    value = Array.CreateInstance(pt, 0);
                                                }
                                            }
                                            else
                                            {
                                                //Columns relations are updated when all data from the row is read.
                                                rowReferences.Add(new KeyValuePair<int, object>(pos, item));
                                            }
                                        }
                                        else if (col.Setter != null)
                                        {
                                            //Get value if not class.
                                            if (col.Setter.Type.IsArray || GXInternal.IsGenericDataType(col.Setter.Type))
                                            {
                                                value = GXInternal.ChangeType(values[pos], col.Setter.Type, Builder.Settings.UniversalTime);
                                                if (value == null && col.Setter.Type == typeof(string) && UseEmptyString)
                                                {
                                                    value = "";
                                                }
                                            }
                                            else //Parameter type is class. Set to null.
                                            {
                                                value = null;
                                            }
                                        }
                                        else
                                        {
                                            value = values[pos];
                                        }
                                        if (value != null)
                                        {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                            //Access minimum date time is 98, 11, 26.
                                            if (this.Builder.Settings.Type == DatabaseType.Access && value is DateTime &&
                                                ((DateTime)value).Date <= new DateTime(100, 1, 1))
                                            {
                                                value = DateTime.MinValue;
                                            }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                            if (col.Setter.Set != null)
                                            {
                                                col.Setter.Set(item, value);
                                            }
                                            else
                                            {
                                                PropertyInfo pi = col.Setter.Target as PropertyInfo;
                                                if (pi != null)
                                                {
                                                    pi.SetValue(item, value, null);
                                                }
                                                else
                                                {
                                                    FieldInfo fi = col.Setter.Target as FieldInfo;
                                                    fi.SetValue(item, value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //Update columns that was not read yet.
                            //Update One to one (1:1) and One to Many (1:N) releations.
                            if (rowReferences != null)
                            {
                                foreach (var it in rowReferences)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    GXColumnHelper col = columns[it.Key];
                                    object relationId = GXInternal.ChangeType(values[it.Key], col.Setter.Relation.ForeignId.Type, Builder.Settings.UniversalTime);
                                    if (allObjects.ContainsKey(col.Setter.Type))
                                    {
                                        foreach (var it2 in allObjects[col.Setter.Type])
                                        {
                                            if (it2.Key.Equals(relationId))
                                            {
                                                bool exclude = false;
                                                //If column is excluded.
                                                if (excluded != null && excluded.ContainsKey(it.Value.GetType())
                                                    && excluded[it.Value.GetType()].Contains(columns[it.Key].Name))
                                                {
                                                    exclude = true;
                                                }
                                                if (!exclude)
                                                {
                                                    col.Setter.Set(it.Value, it2.Value);
                                                }
                                                if (relationDataSetters.ContainsKey(it.Value.GetType()))
                                                {
                                                    if (relationDataSetters[it.Value.GetType()].ContainsKey(it2.Value.GetType()))
                                                    {
                                                        if (!oneToManyObjects.ContainsKey(it.Value.GetType()))
                                                        {
                                                            oneToManyObjects.Add(it.Value.GetType(), new Dictionary<object, List<object>>());
                                                        }
                                                        if (!oneToManyObjects[it.Value.GetType()].ContainsKey(it2.Value))
                                                        {
                                                            oneToManyObjects[it.Value.GetType()].Add(it2.Value, new List<object>());
                                                        }
                                                        oneToManyObjects[it.Value.GetType()][it2.Value].Add(it.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                rowReferences.Clear();
                            }
                        }
                        reader.Close();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0 && !NET6_0
                catch (SqlException ex)
#else
                catch (Exception ex)
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1 && !NETCOREAPP5_0 && !NET5_0
                {
                    throw new Exception(ex.Message + "\r\n" + com.CommandText, ex);
                }
            }
            //Update relation data.
            if (relationDataSetters != null)
            {
                //Update ManyToMany (N:N)
                foreach (var it in manyToManyObjects)
                {
                    if (relationDataSetters.ContainsKey(it.Key))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var p = relationDataSetters[it.Key];
                        foreach (var it2 in p)
                        {
                            //Check is column excluded.
                            if (excluded != null && excluded.ContainsKey(it2.Key) && it2.Value.Target is PropertyInfo pi)
                            {
                                if (excluded[it2.Key].Contains(pi.Name))
                                {
                                    continue;
                                }
                            }
                            foreach (var it3 in it.Value)
                            {
                                GXInternal.SetValue(it3.Key, it2.Value.Target, it3.Value);
                            }
                        }
                    }
                }
                //Update OneToMany (1:N) relations.
                foreach (var it in oneToManyObjects)
                {
                    if (relationDataSetters.ContainsKey(it.Key))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var p = relationDataSetters[it.Key];
                        foreach (var it2 in p)
                        {
                            //Check is column excluded.
                            if (excluded != null && excluded.ContainsKey(it2.Key) && it2.Value.Target is PropertyInfo pi)
                            {
                                if (excluded[it2.Key].Contains(pi.Name))
                                {
                                    continue;
                                }
                            }
                            foreach (var it3 in it.Value)
                            {
                                GXInternal.SetValue(it3.Key, it2.Value.Target, it3.Value);
                            }
                        }
                    }
                }
            }
            if (baseList != null)
            {
                return baseList;
            }
            if (objectList != null)
            {
                return objectList;
            }
            return list;
        }

        /// <summary>
        /// Update Multiple reference values (N:N).
        /// </summary>
        /// <param name="item"></param>
        /// <param name="relationDataSetters"></param>
        /// <param name="mapTables"></param>
        /// <param name="UpdatedReferences"></param>
        /// <param name="manyToManyObjects"></param>
        private static void UpdateReferences(object item,
            Dictionary<Type, Dictionary<Type, GXSerializedItem>> relationDataSetters,
            Dictionary<Type, List<object>> mapTables,
            List<KeyValuePair<object, object>> UpdatedReferences,
            Dictionary<Type, Dictionary<object, List<object>>> manyToManyObjects)
        {
            if (item != null && mapTables != null)
            {
                foreach (var map in mapTables)
                {
                    if (map.Value.Contains(item.GetType()))
                    {
                        if (relationDataSetters.ContainsKey(map.Key))
                        {
                            foreach (var si in relationDataSetters[map.Key])
                            {
                                bool found = false;
                                if (si.Key != item.GetType())
                                {
                                    foreach (var it5 in UpdatedReferences)
                                    {
                                        if (item.GetType().Equals(it5.Key))
                                        {
                                            found = true;
                                            if (!manyToManyObjects.ContainsKey(it5.Value.GetType()))
                                            {
                                                manyToManyObjects.Add(it5.Value.GetType(), new Dictionary<object, List<object>>());
                                            }
                                            if (!manyToManyObjects[it5.Value.GetType()].ContainsKey(item))
                                            {
                                                manyToManyObjects[it5.Value.GetType()].Add(item, new List<object>());
                                            }
                                            //Check that item is not added yet.
                                            if (!manyToManyObjects[it5.Value.GetType()][item].Contains(it5.Value))
                                            {
                                                manyToManyObjects[it5.Value.GetType()][item].Add(it5.Value);
                                            }

                                            if (!manyToManyObjects.ContainsKey(item.GetType()))
                                            {
                                                manyToManyObjects.Add(item.GetType(), new Dictionary<object, List<object>>());
                                            }
                                            if (!manyToManyObjects[item.GetType()].ContainsKey(it5.Value))
                                            {
                                                manyToManyObjects[item.GetType()].Add(it5.Value, new List<object>());
                                            }
                                            //Check that item is not added yet.
                                            if (!manyToManyObjects[item.GetType()][it5.Value].Contains(item))
                                            {
                                                manyToManyObjects[item.GetType()][it5.Value].Add(item);
                                            }
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        UpdatedReferences.Add(new KeyValuePair<object, object>(si.Key, item));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public T SingleOrDefault<T>(GXSelectArgs arg)
        {
            List<T> list = Select<T>(arg);
            if (list.Count == 0)
            {
                return default(T);
            }
            return list[0];
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(GXSelectArgs arg)
        {
            return await SingleOrDefaultAsync<T>(arg, CancellationToken.None);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(GXSelectArgs arg, CancellationToken cancellationToken)
        {
            return await SingleOrDefaultAsync<T>(null, arg, CancellationToken.None);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(IDbTransaction transaction, GXSelectArgs arg)
        {
            return await SingleOrDefaultAsync<T>(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(IDbTransaction transaction, GXSelectArgs arg, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<T> list = await SelectAsync<T>(transaction, arg, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (list.Count == 0)
            {
                return default(T);
            }
            return list[0];
        }


        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <param name="arg"></param>
        public void Insert(GXInsertArgs arg)
        {
            Insert(null, arg);
        }

        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Insert arguments.</param>
        public void Insert(IDbTransaction transaction, GXInsertArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentException("Insert failed. There is nothing to insert.");
            }
            arg.Settings = Builder.Settings;
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded,
                    true, false, Builder.Settings.ColumnQuotation, false, null, null, arg.insertedObjects);
            }
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = GetConnection();
            }
            try
            {
                UpdateOrInsert(connection, transaction, list, true);
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <param name="arg">Insert argument.</param>
        public async Task InsertAsync(GXInsertArgs arg)
        {
            await InsertAsync(null, arg, CancellationToken.None);
        }

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Insert argument.</param>
        public async Task InsertAsync(IDbTransaction transaction, GXInsertArgs arg)
        {
            await InsertAsync(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <param name="arg">Insert argument.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InsertAsync(GXInsertArgs arg, CancellationToken cancellationToken)
        {
            await InsertAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Insert argument.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InsertAsync(IDbTransaction transaction, GXInsertArgs arg, CancellationToken cancellationToken)
        {
            if (arg == null)
            {
                throw new ArgumentException("Insert failed. There is nothing to insert.");
            }
            arg.Settings = Builder.Settings;
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded, true, false,
                    Builder.Settings.ColumnQuotation, false, null, null, arg.insertedObjects);
            }
            IDbConnection connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = GetConnection();
            }
            try
            {
                await UpdateOrInsert(connection, transaction, list, true);
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Update object.
        /// </summary>
        /// <param name="arg">Update arguments.</param>
        public void Update(GXUpdateArgs arg)
        {
            Update(null, arg);
        }

        /// <summary>
        /// Update object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Update arguments.</param>
        public void Update(IDbTransaction transaction, GXUpdateArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentException("Update failed. There is nothing to update.");
            }
            arg.Settings = Builder.Settings;
            //Get values to insert first.
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> handledObjects = new List<object>();
            if (arg.Where == null || arg.Where.List.Count == 0)
            {
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded, true, false,
                        Builder.Settings.ColumnQuotation, false, arg.Where, handledObjects, null);
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
                connection = GetConnection();
            }
            try
            {
                UpdateOrInsert(connection, transaction, list, true);
                list.Clear();
                //Get updated values.
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded,
                        false, false, Builder.Settings.ColumnQuotation, true, arg.Where, handledObjects, null);
                }
                UpdateOrInsert(connection, transaction, list, false);
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="arg"></param>
        public Task UpdateAsync(GXUpdateArgs arg)
        {
            return UpdateAsync(null, arg, CancellationToken.None);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="arg">Update arguments.</param>
        public Task UpdateAsync(IDbTransaction transaction, GXUpdateArgs arg)
        {
            return UpdateAsync(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="arg">Update arguments.</param>
        public Task UpdateAsync(GXUpdateArgs arg, CancellationToken cancellationToken)
        {
            return UpdateAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="arg">Update arguments.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateAsync(IDbTransaction transaction, GXUpdateArgs arg, CancellationToken cancellationToken)
        {
            if (arg == null)
            {
                throw new ArgumentException("Update failed. There is nothing to update.");
            }
            arg.Settings = Builder.Settings;
            //Get values to insert first.
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> handledObjects = new List<object>();
            if (arg.Where == null || arg.Where.List.Count == 0)
            {
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded,
                        true, false, Builder.Settings.ColumnQuotation, false, arg.Where, handledObjects, null);
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
                connection = GetConnection();
            }
            try
            {
                await UpdateOrInsert(connection, transaction, list, true);
                list.Clear();
                //Get updated values.
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(arg.Settings, it.Key, null, it.Value, list, arg.Excluded,
                        false, false, Builder.Settings.ColumnQuotation, true, arg.Where, handledObjects, null);
                }
                await UpdateOrInsertAsync(connection, transaction, list, false);
            }
            finally
            {
                if (!tranactionOnProgress)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        private Task UpdateOrInsertAsync(IDbConnection connection, 
            IDbTransaction transaction, 
            List<KeyValuePair<Type, GXUpdateItem>> list, 
            bool insert)
        {
            return Task.Run(() => UpdateOrInsert(connection, transaction, list, insert));
        }

        /// <summary>
        /// Update or insert new value to the DB.
        /// </summary>
        /// <param name="list">List of tables to update.</param>
        /// <param name="insert">Insert or update.</param>
        private Task UpdateOrInsert(IDbConnection connection, 
            IDbTransaction transaction, 
            List<KeyValuePair<Type, GXUpdateItem>> list, 
            bool insert)
        {
            if (list.Count == 0)
            {
                return Task.CompletedTask;
            }
            int pos;
            string columnName;
            ulong id;
            int total = 0;
            Type type;
            bool transactionUninitialized = transaction == null;
            List<string> queries = new List<string>();
            try
            {
                if (AutoTransaction && transactionUninitialized)
                {
                    transaction = connection.BeginTransaction();
                }
                foreach (var q in list)
                {
                    queries.Clear();
                    if (insert)
                    {
                        total += GXDbHelpers.GetInsertQuery(q, Builder.Settings, queries);
                        q.Value.Inserted = true;
                    }
                    else
                    {
                        GXDbHelpers.GetUpdateQuery(q, Builder.Settings, queries);
                    }
                    type = q.Key;
                    pos = -1;
                    foreach (var it in q.Value.Rows[0])
                    {
                        if (it.Key.GetType() == type)
                        {
                            ++pos;
                            break;
                        }
                        ++pos;
                    }
                    if (Builder.Settings.MaximumRowUpdate != 1 && total > Builder.Settings.MaximumRowUpdate)
                    {
                        if (transaction != null && transactionUninitialized)
                        {
                            transaction.Commit();
                            transaction = connection.BeginTransaction();
                        }
                        total = 0;
                    }
                    GXSerializedItem si = GXSqlBuilder.FindAutoIncrement(type);
                    int index = 0;
                    foreach (string query in queries)
                    {
                        ExecuteNonQuery(connection, transaction, query);
                        //Update auto increment value if it's used and transaction is not updated.
                        if (total != 0 && si != null && pos != -1)
                        {
                            columnName = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
                            id = (ulong)GetLastInsertId(connection, transaction, typeof(ulong), columnName, type);
                            //If each value is added separately.
                            if (this.Builder.Settings.MaximumRowUpdate == 1)
                            {
                                if (Convert.ChangeType(si.Get(q.Value.Rows[index][pos].Key), si.Type).Equals(Convert.ChangeType(0, si.Type)))
                                {
                                    si.Set(q.Value.Rows[index][pos].Key, Convert.ChangeType(id, si.Type));
                                }
                                ++index;
                            }
                            else
                            {
                                if (!Builder.Settings.AutoIncrementFirst)
                                {
                                    id -= (ulong)(q.Value.Rows.Count - 1);
                                }
                                foreach (var it in q.Value.Rows)
                                {
                                    if (Convert.ChangeType(si.Get(it[pos].Key), si.Type).Equals(Convert.ChangeType(0, si.Type)))
                                    {
                                        si.Set(it[pos].Key, Convert.ChangeType(id, si.Type));
                                    }
                                    ++id;
                                }
                            }
                        }
                    }

                    /////////////////////////////////////////////////////////////////////////////////////////////////
                    //Update ID's after transaction is made and all the rows are updated.
                    //Update auto increment value if it's used.
                    if (insert && total == 0 && si != null && pos != -1)
                    {
                        foreach (string query in queries)
                        {
                            columnName = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
                            id = (ulong)GetLastInsertId(connection, transaction, typeof(ulong), columnName, type);
                            //If each value is added separately.
                            if (this.Builder.Settings.MaximumRowUpdate == 1)
                            {
                                if (Convert.ChangeType(si.Get(q.Value.Rows[index][pos].Key), si.Type).Equals(Convert.ChangeType(0, si.Type)))
                                {
                                    si.Set(q.Value.Rows[index][pos].Key, Convert.ChangeType(id, si.Type));
                                }
                                ++index;
                            }
                            else
                            {
                                if (!Builder.Settings.AutoIncrementFirst)
                                {
                                    id -= (ulong)(q.Value.Rows.Count - 1);
                                }
                                foreach (var it in q.Value.Rows)
                                {
                                    if (Convert.ChangeType(si.Get(it[pos].Key), si.Type).Equals(Convert.ChangeType(0, si.Type)))
                                    {
                                        si.Set(it[pos].Key, Convert.ChangeType(id, si.Type));
                                    }
                                    ++id;
                                }
                            }
                        }
                    }
                }
                if (transaction != null && transaction.Connection != null && transactionUninitialized)
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                if (transaction != null && transactionUninitialized)
                {
                    transaction.Rollback();
                }
                throw;
            }
            finally
            {
                if (transaction != null && transactionUninitialized)
                {
                    transaction.Dispose();
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get table name.
        /// </summary>
        /// <returns>Table name.</returns>
        public string GetTableName<T>()
        {
            return this.Builder.GetTableName(typeof(T), false);
        }

        /// <summary>
        /// Close connections.
        /// </summary>
        public void CloseConnections()
        {
            lock (sync)
            {
                if (Connections != null)
                {
                    foreach (DbConnection it in Connections)
                    {
                        try
                        {
                            it.Close();
                        }
                        catch (Exception)
                        {
                            //Ignore exceptions.
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (Connections != null)
                {
                    foreach (DbConnection it in Connections)
                    {
                        try
                        {
                            it.Close();
                            it.Dispose();
                        }
                        catch (Exception)
                        {
                            //Ignore exceptions.
                        }
                    }
                    Connections = null;
                }
            }
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

        private static void Add(StringBuilder sb, GXImportSettings settings, string text, int spaces)
        {
            for (int pos = 0; pos != spaces; ++pos)
            {
                sb.Append("    ");
            }
            sb.Append(text);
        }

        private static void AddLine(StringBuilder sb, GXImportSettings settings, string text, int spaces)
        {
            Add(sb, settings, text, spaces);
            sb.AppendLine("");
        }
        private static string GetName(GXImportSettings settings, string value)
        {
            return settings.TablePrefix + Char.ToUpper(value[0]) + value.Substring(1);
        }

        /// <summary>
        /// Import Database schema and convert them to C# classes.
        /// </summary>
        /// <param name="settings">Import settings.</param>
        public void GenerateTables(GXImportSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.Path))
            {
                throw new ArgumentException("Invalid settings.");
            }
            DirectoryInfo di = new DirectoryInfo(settings.Path);
            if (!di.Exists)
            {
                di.Create();
            }
            IDbConnection connection = GetConnection();
            try
            {
                foreach (string table in GetTables())
                {
                    if (ContainsTable(settings.Tables, table))
                    {
                        StringBuilder header = new StringBuilder();
                        StringBuilder data = new StringBuilder();
                        System.Diagnostics.Debug.WriteLine("Import " + table);
                        AddLine(header, settings, "using System;", 0);
                        AddLine(header, settings, "using System.Runtime.Serialization;", 0);
                        AddLine(header, settings, "using Gurux.Service.Orm;", 0);
                        header.AppendLine("");
                        if (!string.IsNullOrEmpty(settings.Namespace))
                        {
                            Add(header, settings, "namespace ", 0);
                            header.AppendLine(settings.Namespace);
                            AddLine(header, settings, "{", 0);
                        }
                        AddLine(header, settings, "[DataContract]", 1);
                        if (settings.Serializable)
                        {
                            AddLine(header, settings, "[Serializable]", 1);
                        }
                        Add(header, settings, "class ", 1);
                        //Convert first letter to Uppercase.
                        header.Append(GetName(settings, table));
                        AddLine(data, settings, "{", 1);
                        int len;
                        foreach (string col in GetColumns(table))
                        {
                            bool isunique = false;
                            //AutoIncrement
                            bool ai = IsAutoIncrement(table, col, connection);
                            //Data type
                            string def = GetColumnDefaultValueQuery(table, col, connection);
                            Type type = GetColumnType(table, col, connection, out len);
                            bool nullable = GetColumnNullableQuery(table, col, connection);

                            if (GetPrimaryKeyQuery(table, col, connection))
                            {
                                isunique = true;
                                header.Append(" : IUnique<" + type.Name + ">");
                                if (col != "Id")
                                {
                                    AddLine(data, settings, "[DataMember(Name = \"" + col + "\")]", 2);
                                }
                                else
                                {
                                    AddLine(data, settings, "[DataMember()]", 2);
                                }
                            }
                            else
                            {
                                if (nullable)
                                {
                                    AddLine(data, settings, "[DataMember()]", 2);
                                }
                                else
                                {
                                    AddLine(data, settings, "[DataMember(IsRequired = true)]", 2);
                                }
                                string[] refs = GetReferenceTablesQuery(table, col, connection);
                                if (refs.Length != 0)
                                {
                                    ForeignKeyDelete onDelete;
                                    ForeignKeyUpdate onUpdate;
                                    string t = GetColumnConstraintsQuery(table, col, connection, out onDelete, out onUpdate);
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
                                        foreach (string it in refs)
                                        {
                                            AddLine(data, settings, "[ForeignKey(typeof(" +
                                                GetName(settings, it) + "))]", 2);
                                        }
                                    }
                                    else if (onDelete != ForeignKeyDelete.None && onUpdate != ForeignKeyUpdate.None)
                                    {
                                        foreach (string it in refs)
                                        {
                                            AddLine(data, settings, "[ForeignKey(typeof(" +
                                            GetName(settings, it) + "), OnDelete = ForeignKeyDelete." + onDelete +
                                            ", OnUpdate = ForeignKeyUpdate." + onUpdate + ")]", 2);
                                        }
                                    }
                                    else if (onDelete != ForeignKeyDelete.None)
                                    {
                                        foreach (string it in refs)
                                        {
                                            AddLine(data, settings, "[ForeignKey(typeof(" +
                                            GetName(settings, it) + "), OnDelete = ForeignKeyDelete." + onDelete + ")]", 2);
                                        }
                                    }
                                    else if (onUpdate != ForeignKeyUpdate.None)
                                    {
                                        foreach (string it in refs)
                                        {
                                            AddLine(data, settings, "[ForeignKey(typeof(" +
                                            GetName(settings, it) + "), OnUpdate = ForeignKeyUpdate." + onUpdate + ")]", 2);
                                        }
                                    }
                                }
                            }

                            if (ai)
                            {
                                AddLine(data, settings, "[AutoIncrement]", 2);
                            }

                            if (def != "" && def != "NULL")
                            {
                                if (type == typeof(bool))
                                {
                                    if (def.IndexOfAny(new char[] { 't', 'T', '1' }) != -1)
                                    {
                                        AddLine(data, settings, "[DefaultValue(true)]", 2);
                                    }
                                    else
                                    {
                                        AddLine(data, settings, "[DefaultValue(false)]", 2);
                                    }
                                }
                                else
                                {
                                    AddLine(data, settings, "[DefaultValue(" + def + ")]", 2);
                                }
                            }
                            else if (len != 0 && len != -1 && len != 65535 && type == typeof(string))
                            {
                                AddLine(data, settings, "[StringLength(" + len.ToString() + ")]", 2);
                            }
                            Add(data, settings, "public ", 2);
                            if (type == typeof(bool))
                            {
                                data.Append("bool");
                            }
                            else if (type == typeof(string))
                            {
                                data.Append("string");
                            }
                            else
                            {
                                data.Append(type.Name);
                            }
                            data.Append(' ');
                            if (isunique)
                            {
                                data.AppendLine("Id");
                            }
                            else
                            {
                                data.AppendLine(settings.ColumnPrefix + col);
                            }
                            AddLine(data, settings, "{", 2);
                            AddLine(data, settings, "get;", 3);
                            AddLine(data, settings, "set;", 3);
                            AddLine(data, settings, "}", 2);
                            data.AppendLine("");
                        }
                        //Remove last new line.
                        data.Length -= 2;
                        AddLine(data, settings, "}", 1);
                        if (!string.IsNullOrEmpty(settings.Namespace))
                        {
                            AddLine(data, settings, "}", 0);
                        }

                        using (TextWriter tw = File.CreateText(Path.Combine(settings.Path, table + ".cs")))
                        {
                            tw.Write(header);
                            tw.WriteLine("");
                            tw.Write(data);
                        }
                    }
                }
            }
            finally
            {
                connection.Close();
                ReleaseConnection(connection);
            }
        }

        /// <summary>
        /// Delete ALL items from the table.
        /// </summary>
        public void Truncate<T>()
        {
            //SQLite don't support truncate.
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Delete(GXDeleteArgs.DeleteAll<T>());
            }
            else
            {
                IDbConnection connection = GetConnection();
                try
                {
                    string query = "TRUNCATE TABLE " + Builder.GetTableName(typeof(T), true);
                    ExecuteNonQuery(connection, null, query);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    ReleaseConnection(connection);
                }
            }
        }
    }
}