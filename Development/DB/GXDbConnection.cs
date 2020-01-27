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
using Gurux.Common.JSon;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Gurux.Common;
using System.Data.Common;
using Gurux.Service.Orm;
using System.Reflection;
using System.Collections;
using Gurux.Common.Internal;
using System.Runtime.Serialization;
using System.Data.OleDb;
using System.Data.Odbc;
using Gurux.Service.Orm.Settings;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Event hanler for executed SQL.
    /// </summary>
    /// <param name="instance">Sender.</param>
    /// <param name="sql">Executed SQL.</param>
    public delegate void SqlExecutedEventHandler(object instance, string sql);

    /// <summary>
    /// This class is used to communicate with database.
    /// </summary>
    public class GXDbConnection : IDisposable
    {
        private SqlExecutedEventHandler sql;
        private DbTransaction Transaction;
        /// <summary>
        /// Synchronous object.
        /// </summary>
        private readonly object Sync = new object();

        public GXSqlBuilder Builder
        {
            get;
            private set;
        }

        public DbConnection Connection
        {
            get;
            private set;
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

        public DbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            Transaction = Connection.BeginTransaction(isolationLevel);
            return Transaction;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection"></param>
        public GXDbConnection(DbConnection connection, string tablePrefix)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            AutoTransaction = true;
            Type tp = connection.GetType();
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
                name = tp.GetProperty("DataSource").GetValue(connection, null).ToString();
                if (name == "ACCESS")
                {
                    type = DatabaseType.Access;
                }
                else
                {
                    if (connection.ServerVersion.Contains("Oracle"))
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
                name = tp.GetProperty("DataSource").GetValue(connection, null).ToString();
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
            Connection = connection;
            Builder = new GXSqlBuilder(type, tablePrefix);
            Builder.Settings.ServerVersion = connection.ServerVersion;
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
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
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
            }
            return list.ToArray();
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="relations">Are relation tables created also.</param>
        /// <param name="overwrite">Old table is dropped first if exists.</param>
        public void CreateTable(Type type, bool relations, bool overwrite)
        {
            IDbTransaction transaction = Transaction;
            bool autoTransaction = transaction == null;
            Dictionary<Type, GXSerializedItem> tables = new Dictionary<Type, GXSerializedItem>();
            if (relations)
            {
                GetTables(type, tables);
            }
            if (!tables.ContainsKey(type))
            {
                tables.Add(type, null);
            }
            StringBuilder sb = new StringBuilder();
            lock (Connection)
            {
                try
                {
                    if (Connection.State != ConnectionState.Open)
                    {
                        Connection.Open();
                    }
                    //Find dropped tables.
                    Dictionary<Type, GXSerializedItem> dropTables = new Dictionary<Type, GXSerializedItem>();
                    foreach (var it in tables)
                    {
                        Type tmp = it.Key;
                        if (TableExist(Builder.GetTableName(tmp, false)))
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
                    if (AutoTransaction)
                    {
                        transaction = Connection.BeginTransaction();
                    }
                    foreach (var it in tables)
                    {
                        //If table do not have relations.
                        if (it.Value == null)
                        {
                            DropTable(transaction, type, dropTables);
                        }
                        else
                        {
                            DropTable(transaction, it.Key, dropTables);
                        }
                    }

                    CreateTable(transaction, type, tables);
                    if (autoTransaction && transaction != null)
                    {
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    if (autoTransaction && transaction != null)
                    {
                        transaction.Rollback();
                    }
                    throw ex;
                }
                finally
                {
                    if (autoTransaction && transaction != null)
                    {
                        transaction.Dispose();
                    }
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
            lock (Connection)
            {
                string[] cols = GetColumns(type);
                string tableName = Builder.GetTableName(type, false);
                IDbTransaction transaction = Transaction;
                try
                {
                    if (Connection.State != ConnectionState.Open)
                    {
                        Connection.Open();
                    }
                    if (AutoTransaction)
                    {
                        transaction = Connection.BeginTransaction();
                    }
                    //Add new columns.
                    foreach (var it in GXSqlBuilder.GetProperties(type))
                    {
                        if (!cols.Contains(it.Key))
                        {
                            if (it.Value.Relation != null && it.Value.Relation.ForeignTable != type)
                            {
                                if (it.Value.Relation.RelationType == RelationType.OneToOne ||
                                    it.Value.Relation.RelationType == RelationType.OneToMany)
                                {
                                    continue;
                                }
                            }
                            StringBuilder sb = new StringBuilder();
                            sb.Append("ALTER TABLE ");
                            sb.Append(GXDbHelpers.AddQuotes(tableName, Builder.Settings.TableQuotation));
                            sb.Append(" ADD ");
                            sb.Append(it.Key);
                            sb.Append(" ");
                            sb.Append(GetDataBaseType(it.Value.Type, it.Value.Target));
                            ExecuteNonQuery(transaction, sb.ToString());
                        }
                    }
                    //TODO: Check is column type changed.
                    /*
                    int len;
                    foreach (string col in cols)
                    {
                        Type colType = GetColumnType(tableName, col, Connection, out len);
                    }
                    */
                    if (AutoTransaction && transaction != null)
                    {
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    if (AutoTransaction && transaction != null)
                    {
                        transaction.Rollback();
                    }
                    throw ex;
                }
                finally
                {
                    if (AutoTransaction && transaction != null)
                    {
                        transaction.Dispose();
                    }
                }
            }
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
                        sb.Append(GXDbHelpers.AddQuotes(tableName, Builder.Settings.TableQuotation));
                        tableItem.Queries.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else//Create table.
                    {
                        sb.Append("CREATE TABLE ");
                        sb.Append(GXDbHelpers.AddQuotes(tableName, Builder.Settings.TableQuotation));
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
                            sb.Append(GXDbHelpers.AddQuotes(name, Builder.Settings.ColumnQuotation));
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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                                    if (Builder.Settings.Type == DatabaseType.Access && ((it.Value.Attributes & (Attributes.PrimaryKey | Attributes.ForeignKey)) != 0))
                                    {
                                        if (it.Value.Relation == null)
                                        {
                                            tp = it.Value.Type;
                                        }
                                        else if ((it.Value.Relation.ForeignId.Attributes & Attributes.AutoIncrement) == 0)
                                        {
                                            tp = it.Value.Relation.ForeignId.Type;
                                        }
                                        else
                                        {
                                            tp = typeof(int);
                                        }
                                    }
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
                                    str = GetDataBaseType(tp, it.Value.Target);
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
                            if (!(Builder.Settings.Type == DatabaseType.Oracle &&
                                (it.Value.DefaultValue != null || (it.Value.Attributes & (Attributes.AutoIncrement | Attributes.PrimaryKey)) != 0)))
                            {
                                if ((it.Value.Attributes & Attributes.PrimaryKey) == 0 &&
                                    (!required ||
                                    (it.Value.Type.IsGenericType && it.Value.Type.GetGenericTypeDefinition() == typeof(Nullable<>)) ||
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
                                sb.Append(" DEFAULT ");
                                if (it.Value.DefaultValue is Enum)
                                {
                                    if (this.Builder.Settings.UseEnumStringValue)
                                    {
                                        sb.Append("'" + Convert.ToString(it.Value.DefaultValue) + "'");
                                    }
                                    else
                                    {
                                        sb.Append(Convert.ToString((int)it.Value.DefaultValue));
                                    }
                                }
                                else if (it.Value.DefaultValue is string)
                                {
                                    sb.Append("'" + (string)it.Value.DefaultValue + "'");
                                }
                                else if (it.Value.DefaultValue is bool)
                                {
                                    sb.Append((bool)it.Value.DefaultValue ? 1 : 0);
                                }
                                else
                                {
                                    sb.Append(Convert.ToString(it.Value.DefaultValue));
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
                                fkStr.Append(GXDbHelpers.AddQuotes(name, Builder.Settings.ColumnQuotation));
                                fkStr.Append(") REFERENCES ");
                                fkStr.Append(GXDbHelpers.AddQuotes(table2, Builder.Settings.TableQuotation));
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
            foreach (var it in GXSqlBuilder.GetProperties(type))
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
                        sb.Append("INDEX ");
                        if (this.Builder.Settings.UpperCase)
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
                        //sb.Append(tableName);
                        sb.Append("(");
                        sb.Append(GXDbHelpers.AddQuotes(name, this.Builder.Settings.ColumnQuotation));
                        sb.Append(")");
                        tableItem.Queries.Add(sb.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Create or drop selected table and it's dependencies.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="table"></param>
        void TableCreation(bool create, IDbTransaction transaction, GXTableCreateQuery table, List<Type> created)
        {
            Type type;
            //Create and drop depended tables first.
            foreach (var t in table.Dependencies)
            {
                TableCreation(create, transaction, t, created);
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
                    ExecuteNonQuery(transaction, q);
                }
            }
        }

        void CreateTable(IDbTransaction transaction, Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(true, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(true, transaction, it.Value, created);
            }
        }

        void DropTable(IDbTransaction transaction, Type type, Dictionary<Type, GXSerializedItem> tables)
        {
            Dictionary<Type, GXTableCreateQuery> tablesCreationQueries = new Dictionary<Type, GXTableCreateQuery>();
            GetCreateTableQueries(false, type, null, tables, tablesCreationQueries, true);
            List<Type> created = new List<Type>();
            foreach (var it in tablesCreationQueries)
            {
                TableCreation(false, transaction, it.Value, created);
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
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
                string table = Builder.GetTableName(type, false);
                if (TableExist(table))
                {
                    IDbTransaction transaction = Transaction;
                    bool autoTransaction = transaction == null;
                    try
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
                            if (!TableExist(Builder.GetTableName(it, false)))
                            {
                                tables.Remove(it);
                                --pos;
                            }
                        }
                        if (AutoTransaction)
                        {
                            transaction = Connection.BeginTransaction();
                        }
                        DropTable(transaction, type, tables);

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
                        if (transaction != null && autoTransaction)
                        {
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (autoTransaction && transaction != null)
                        {
                            transaction.Rollback();
                        }
                        throw ex;
                    }
                    finally
                    {
                        if (autoTransaction && transaction != null)
                        {
                            transaction.Dispose();
                        }
                    }
                }
            }
        }

        private void DropTable(IDbTransaction transaction, string table)
        {
            ExecuteNonQuery(transaction, "DROP TABLE " + GXDbHelpers.AddQuotes(table, this.Builder.Settings.TableQuotation));
        }

        public T ExecuteScalar<T>(string query)
        {
            return (T)ExecuteScalarInternal(null, query, typeof(T));
        }

        private object ExecuteScalarInternal(IDbTransaction transaction, string query, Type type)
        {
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
                using (IDbCommand com = Connection.CreateCommand())
                {
                    com.Transaction = transaction;
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    return Convert.ChangeType(com.ExecuteScalar(), type);
                }
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
            lock (this)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
                if (sql != null)
                {
                    sql(this, query);
                }
                using (IDbCommand com = Connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.Transaction = transaction;
                    com.CommandText = query;
                    com.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns last inserted ID.
        /// </summary>
        /// <returns>Last inserted row ID.</returns>
        private object GetLastInsertId(IDbTransaction transaction, Type valueType, string columnName, Type tableType)
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
                    return ExecuteScalarInternal(transaction, "SELECT LAST_INSERT_ID()" + name, valueType);
                case DatabaseType.Oracle:
                    return ExecuteScalarInternal(transaction, "SELECT " + Gurux.Service.Orm.Settings.GXOracleSqlSettings.GetSequenceName(table, columnName) + ".CURRVAL FROM dual", valueType);
                case DatabaseType.MSSQL:
                    return ExecuteScalarInternal(transaction, "SELECT @@IDENTITY" + name, valueType);
                case DatabaseType.SqLite:
                    return ExecuteScalarInternal(transaction, "SELECT last_insert_rowid()" + name, valueType);
#if !NETCOREAPP2_0 && !NETCOREAPP2_1
                case DatabaseType.Access:
                    return ExecuteScalarInternal(transaction, "SELECT @@IDENTITY" + name, valueType);
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
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
                return GetColumnsInteral(tableName, Builder.Settings.Type, Connection);
            }
        }

        private string[] GetColumnsInteral(string tableName, DatabaseType type, DbConnection connection)
        {
            string query;
            int index = 0;
            List<string> list;
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
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
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
            {
                query = Builder.Settings.GetColumnsQuery(connection.Database, tableName, out index);
            }
            list = new List<string>();
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
            return list.ToArray();
        }


        private bool IsAutoIncrement(string tableName, string columnName, DbConnection connection)
        {
            string query = Builder.Settings.GetAutoIncrementQuery(connection.Database, tableName, columnName);
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
            return false;
        }
        private string GetColumnConstraintsQuery(string tableName, string columnName, DbConnection connection, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            string targetTable = "";
            onDelete = ForeignKeyDelete.None;
            onUpdate = ForeignKeyUpdate.None;
            string query = Builder.Settings.GetColumnConstraintsQuery(connection.Database, tableName, columnName);
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
            return targetTable;
        }

        /// <summary>
        /// Check is column primary key.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private bool GetPrimaryKeyQuery(string tableName, string columnName, DbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetPrimaryKeyQuery(connection.Database, tableName, columnName);
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
            return ret;
        }

        private bool GetColumnNullableQuery(string tableName, string columnName, DbConnection connection)
        {
            bool ret = false;
            string query = Builder.Settings.GetColumnNullableQuery(connection.Database, tableName, columnName);
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
            return ret;
        }

        private string[] GetReferenceTablesQuery(string tableName, string columnName, DbConnection connection)
        {
            List<string> list = new List<string>();
            string query = Builder.Settings.GetReferenceTablesQuery(connection.Database, tableName, columnName);
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
            return list.ToArray();
        }

        private string GetColumnDefaultValueQuery(string tableName, string columnName, DbConnection connection)
        {
            string query = Builder.Settings.GetColumnDefaultValueQuery(connection.Database, tableName, columnName);
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
            return "";
        }

        private Type GetColumnType(string tableName, string columnName, DbConnection connection, out int len)
        {
            string str = null;
            len = 0;
            string query = Builder.Settings.GetColumnTypeQuery(connection.Database, tableName, columnName);
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
            switch (Builder.Settings.Type)
            {
                case DatabaseType.MySQL:
                    query = string.Format("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'",
                        Connection.Database);
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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                case DatabaseType.Access:
                    DataTable dt;
                    if (Connection as System.Data.OleDb.OleDbConnection != null)
                    {
                        dt = (Connection as System.Data.OleDb.OleDbConnection).GetSchema("Tables", new string[] { null, null, null });
                    }
                    else
                    {
                        dt = (Connection as System.Data.Odbc.OdbcConnection).GetSchema("Tables", new string[] { null, null, null });
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
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                default:
                    throw new ArgumentOutOfRangeException("TableExist failed. Unknown database connection.");
            }
            return ((List<string>)SelectInternal<string>(query)).ToArray();
        }

        /// <summary>
        /// Table exists.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Returns true if table exists.</returns>
        public bool TableExist(string tableName)
        {
            string query;
            switch (Builder.Settings.Type)
            {
                case DatabaseType.MySQL:
                    query = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}'",
                        tableName, Connection.Database);
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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                case DatabaseType.Access:
                    DataTable dt;
                    if (Connection as System.Data.OleDb.OleDbConnection != null)
                    {
                        dt = (Connection as System.Data.OleDb.OleDbConnection).GetSchema("Tables", new string[] { null, null, tableName });
                    }
                    else
                    {
                        dt = (Connection as System.Data.Odbc.OdbcConnection).GetSchema("Tables", new string[] { null, null, tableName });
                    }
                    return dt.Rows.Count != 0;
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                default:
                    throw new ArgumentOutOfRangeException("TableExist failed. Unknown database connection.");
            }
            return ExecuteScalar<int>(query) != 0;
        }

        private string GetDataBaseType(Type type, object target)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            //100 character is allocated for type.
            else if (type == typeof(Type))
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
                else //In default enumeration values are saved as long values.
                {
                    return this.Builder.Settings.LongColumnDefinition;
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
                }
                if (GXInternal.IsGenericDataType(type))
                {
                    throw new Exception("Invalid data type: " + type.Name);
                }
                throw new Exception("Invalid data type: " + type.Name + ". Make sure that you have added ForeignKey attribute to the property.");

            }
            return Builder.DbTypeMap[type];
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="items">List of items to remove.</param>
        public void Delete(GXDeleteArgs arg)
        {
            IDbTransaction transaction = Transaction;
            bool autoTransaction = transaction == null;
            lock (Connection)
            {
                if (AutoTransaction)
                {
                    transaction = Connection.BeginTransaction();
                }
                try
                {
                    arg.Settings = this.Builder.Settings;
                    ExecuteNonQuery(transaction, arg.ToString());
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    if (autoTransaction && transaction != null)
                    {
                        transaction.Rollback();
                    }
                    throw ex;
                }
                finally
                {
                    if (autoTransaction && transaction != null)
                    {
                        transaction.Dispose();
                    }
                }
            }
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
            DateTime tm = DateTime.Now;
            List<T> list = Select<T>(args);
            args.ExecutionTime = (DateTime.Now - tm).Milliseconds;
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
            DateTime tm = DateTime.Now;
            List<T> list = Select<T>(args);
            args.ExecutionTime = (DateTime.Now - tm).Milliseconds;
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

            public override string ToString()
            {
                return Table + "." + Name;
            }
        }

        /// <summary>
        /// Initialize select. Save table indexes and column setters to make data handling faster.
        /// </summary>
        private static void InitializeSelect<T>(IDataReader reader, GXDBSettings settings, Dictionary<Type, GXSerializedItem> tables, Dictionary<Type, int> TableIndexes,
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
                                        mapTable.Add(tp, new List<object>());
                                        GXSerializedItem t = new GXSerializedItem();
                                        list = new Dictionary<Type, GXSerializedItem>();
                                        relationDataSetters.Add(tp, list);
                                        list.Add(it.Value.Relation.ForeignTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.ForeignTable));
                                        list.Add(it.Value.Relation.PrimaryTable, GXSqlBuilder.FindRelation(tp, it.Value.Relation.PrimaryTable));
                                    }
                                }
                                tp = GXInternal.GetPropertyType(it.Value.Type);
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

        private object SelectInternal<T>(string query)
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
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                    if (Connection is OdbcConnection)
                    {
                        using (IDbCommand com = ((OdbcConnection)Connection).CreateCommand())
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
                    else if (Connection is OleDbConnection)
                    {
                        using (IDbCommand com = ((OleDbConnection)Connection).CreateCommand())
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
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1

                }
                using (IDbCommand com = Connection.CreateCommand())
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
                                                item = GXJsonParser.CreateInstance(col.TableType);
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
        }

        public List<T> Select<T>(GXSelectArgs arg)
        {
            if (arg == null)
            {
                arg = GXSelectArgs.SelectAll<T>();
            }
            arg.Verify();
            arg.Parent.Settings = Builder.Settings;
            arg.ExecutionTime = 0;
            DateTime tm = DateTime.Now;
            lock (this)
            {
                List<T> value = (List<T>)SelectInternal2<T>(arg);
                arg.ExecutionTime = (DateTime.Now - tm).Milliseconds;
                return value;
            }
        }

        private object SelectInternal2<T>(GXSelectArgs arg)
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
            //Columns that are updated when row is read. This is needed when relation data is try to update and it's not read yet.
            List<KeyValuePair<int, object>> UpdatedColumns = new List<KeyValuePair<int, object>>();
            string query = arg.ToString();
            lock (Connection)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
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
                    GetTables(typeof(T), tmp);
                    //If there are no relations to other tables.
                    if (!tmp.ContainsKey(type))
                    {
                        tmp.Add(type, null);
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
                        objects = new Dictionary<Type, SortedDictionary<object, object>>();
                        mapTables = new Dictionary<Type, List<object>>();
                    }
                }
                //Read column headers.
                if (columns != null)
                {
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                    if (Connection is OdbcConnection)
                    {
                        using (IDbCommand com = ((OdbcConnection)Connection).CreateCommand())
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
                    else if (Connection is OleDbConnection)
                    {
                        using (IDbCommand com = ((OleDbConnection)Connection).CreateCommand())
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
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                }
                using (IDbCommand com = Connection.CreateCommand())
                {
                    com.CommandType = CommandType.Text;
                    com.CommandText = query;
                    try
                    {
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
                                                    item = GXJsonParser.CreateInstance(col.TableType);
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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                    catch (SqlException ex)
#else
                    catch (Exception ex)
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1 && !NETCOREAPP3_1
                    {
                        throw new Exception(ex.Message + "\r\n" + com.CommandText, ex);
                    }
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
        }

        public T SingleOrDefault<T>(GXSelectArgs arg)
        {
            arg.Verify();
            arg.Parent.Settings = Builder.Settings;
            arg.ExecutionTime = 0;
            DateTime tm = DateTime.Now;
            List<T> list = Select<T>(arg);
            arg.ExecutionTime = (DateTime.Now - tm).Milliseconds;
            if (list.Count == 0)
            {
                return default(T);
            }
            return list[0];
        }

        /// <summary>
        /// Return values as read values.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public List<object[]> SelectAsArray<T>(GXSelectArgs arg)
        {
            if (arg == null)
            {
                return Select<object[]>((GXSelectArgs)null);
            }
            arg.Verify();
            arg.Settings = Builder.Settings;
            DateTime tm = DateTime.Now;
            arg.ExecutionTime = 0;
            List<object[]> value = Select<object[]>(arg);
            arg.ExecutionTime = (DateTime.Now - tm).Milliseconds;
            return value;
        }

        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <param name="arg"></param>
        public void Insert(GXInsertArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("Insert failed. There is nothing to insert.");
            }
            arg.Settings = Builder.Settings;
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(it.Key, null, it.Value, list, true, false, Builder.Settings.ColumnQuotation, false);
            }
            UpdateOrInsert(list, true);
        }

        /// <summary>
        /// Update object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        public void Update(GXUpdateArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("Update failed. There is nothing to update.");
            }
            arg.Settings = Builder.Settings;
            //Get values to insert first.
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> insertedList = new List<object>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(it.Key, null, it.Value, list, true, false, Builder.Settings.ColumnQuotation, true);
            }
            UpdateOrInsert(list, true);
            //Get updated values.
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(it.Key, null, it.Value, list, false, false, Builder.Settings.ColumnQuotation, true);
            }
            UpdateOrInsert(list, false);
        }

        /// <summary>
        /// Update or insert new value to the DB.
        /// </summary>
        /// <param name="list">List of tables to update.</param>
        /// <param name="insert">Insert or update.</param>
        private void UpdateOrInsert(List<KeyValuePair<Type, GXUpdateItem>> list, bool insert)
        {
            lock (this)
            {
                int pos = 0;
                string columnName;
                ulong id;
                int total = 0;
                Type type;
                IDbTransaction transaction = Transaction;
                bool autoTransaction = transaction == null;
                List<string> queries = new List<string>();
                lock (Connection)
                {
                    try
                    {
                        if (AutoTransaction)
                        {
                            transaction = Connection.BeginTransaction();
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
                                if (transaction != null && autoTransaction)
                                {
                                    transaction.Commit();
                                    transaction = Connection.BeginTransaction();
                                }
                                total = 0;
                            }
                            GXSerializedItem si = GXSqlBuilder.FindAutoIncrement(type);
                            int index = 0;
                            foreach (string query in queries)
                            {
                                ExecuteNonQuery(transaction, query);
                                //Update auto increment value if it's used and transaction is not updated.
                                if (total != 0 && si != null && pos != -1)
                                {
                                    columnName = GXDbHelpers.GetColumnName(si.Target as PropertyInfo, '\0');
                                    id = (ulong)GetLastInsertId(transaction, typeof(ulong), columnName, type);
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
                                    id = (ulong)GetLastInsertId(transaction, typeof(ulong), columnName, type);
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
                        if (transaction != null && autoTransaction)
                        {
                            transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (transaction != null && autoTransaction)
                        {
                            transaction.Rollback();
                        }
                        throw ex;
                    }
                    finally
                    {
                        if (transaction != null && autoTransaction)
                        {
                            transaction.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get table name.
        /// </summary>
        /// <returns>Table name.</returns>
        public string GetTableName<T>()
        {
            return this.Builder.GetTableName(typeof(T), false);
        }

        public void Dispose()
        {
            if (Connection != null)
            {
                try
                {
                    Connection.Close();
                    Connection.Dispose();
                }
                finally
                {
                    Connection = null;
                }
            }
        }

        private void AddSpaces(TextWriter tw, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int pos = 0; pos != count; ++pos)
            {
                sb.Append(' ');
            }
            tw.Write(sb);
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
            lock (Connection)
            {
                try
                {
                    if (Connection.State != ConnectionState.Open)
                    {
                        Connection.Open();
                    }
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
                                bool ai = IsAutoIncrement(table, col, Connection);
                                //Data type
                                string def = GetColumnDefaultValueQuery(table, col, Connection);
                                Type type = GetColumnType(table, col, Connection, out len);
                                bool nullable = GetColumnNullableQuery(table, col, Connection);

                                if (GetPrimaryKeyQuery(table, col, Connection))
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
                                    string[] refs = GetReferenceTablesQuery(table, col, Connection);
                                    if (refs.Length != 0)
                                    {
                                        ForeignKeyDelete onDelete;
                                        ForeignKeyUpdate onUpdate;
                                        string t = GetColumnConstraintsQuery(table, col, Connection, out onDelete, out onUpdate);
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
                    Connection.Close();
                }
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
                IDbTransaction transaction = Transaction;
                bool autoTransaction = transaction == null;
                lock (Connection)
                {
                    if (AutoTransaction)
                    {
                        transaction = Connection.BeginTransaction();
                    }
                    try
                    {
                        string query = "TRUNCATE TABLE " + Builder.GetTableName(typeof(T), true);
                        ExecuteNonQuery(transaction, query);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (autoTransaction && transaction != null)
                        {
                            transaction.Rollback();
                        }
                        throw ex;
                    }
                    finally
                    {
                        if (autoTransaction && transaction != null)
                        {
                            transaction.Dispose();
                        }
                    }
                }
            }
        }
    }
}
