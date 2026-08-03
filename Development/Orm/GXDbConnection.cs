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
using Gurux.Service.DB;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Model;
using Gurux.Service.Orm.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm
{
    /// <summary>
    /// Event handler for executed SQL.
    /// </summary>
    /// <param name="instance">Sender.</param>
    /// <param name="sql">Executed SQL.</param>
    /// <param name="executionTime">Execution time.</param>
    public delegate void SqlExecutedEventHandler(object instance, string sql, int executionTime);

    /// <summary>
    /// Event handler for column or table rename.
    /// </summary>
    /// <param name="target">Table or column</param>
    /// <param name="oldName">Old name.</param>
    /// <param name="newName">New name.</param>
    public delegate void RenameEventHandler(object target, string oldName, string newName);

    /// <summary>
    /// This class is used to communicate with database.
    /// </summary>
    public class GXDbConnection : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Default database type.
        /// </summary>
        /// <remarks>
        /// When this is changed generated SQL are shown using this database format.
        /// MySql settings are default settings because of MariaDB (https://mariadb.org/).
        /// </remarks>
        public static DatabaseType DefaultDatabaseType = DatabaseType.MySQL;
        readonly GXQueryCache queryCache;
        internal GXSqlBuilder Builder;

        /// <summary>
        /// Query cache instance.
        /// </summary>
        public GXQueryCache QueryCache
        {
            get
            {
                return queryCache;
            }
        }

        /// <summary>
        /// SQL query cache lifetime.
        /// </summary>
        /// <remarks>
        /// Default value is 10 minutes.
        /// </remarks>
        public TimeSpan QueryCacheTime
        {
            get
            {
                return queryCache.CacheTime;
            }
            set
            {
                queryCache.CacheTime = value;
            }
        }

        /// <summary>
        /// Clear all cached SQL query strings.
        /// </summary>
        public void ClearQueryCache()
        {
            queryCache.Clear();
        }

        private SqlExecutedEventHandler sql;
        private RenameEventHandler rename;

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

        /// <summary>
        /// Database connection.
        /// </summary>
        public DbConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// Command timeout.
        /// </summary>
        public int CommandTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Change database.
        /// </summary>
        /// <param name="databaseName">Name of the database to switch to.</param>
        public void ChangeDatabase(string databaseName)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                if (Connection.ConnectionString == "Data Source=:memory:")
                {
                    return;
                }
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
                ExecuteNonQuery("SET SCHEMA " + databaseName);
                return;
            }
            if (Builder.Settings.Type == DatabaseType.Oracle ||
                Builder.Settings.Type == DatabaseType.SapHana)
            {
                ExecuteNonQuery("ALTER SESSION SET CURRENT_SCHEMA = " + databaseName);
                return;
            }
            Connection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// Change database asynchronously.
        /// </summary>
        /// <param name="databaseName">Name of the database to switch to.</param>
        public async Task ChangeDatabaseAsync(string databaseName)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                if (Connection.ConnectionString == "Data Source=:memory:")
                {
                    return;
                }
                var type = Connection.GetType();
                Connection = (DbConnection)Activator.CreateInstance(
                    type,
                    Connection.ConnectionString)!;
                await Connection.OpenAsync();
            }
            else
            {
                await Connection.ChangeDatabaseAsync(databaseName);
            }
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
        /// Event handler for executed SQL.
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
        /// Event handler for column ora table update.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public event RenameEventHandler OnRename
        {
            add
            {
                rename += value;
            }
            remove
            {
                rename -= value;
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

        internal readonly object sync = new();

        /// <summary>
        /// Used connection string.
        /// </summary>
        public ConnectionState State
        {
            get
            {
                return Connection.State;
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
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        public GXDbConnection(DbConnection connection, string tablePrefix)
            : this(connection, tablePrefix, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        /// <param name="queryCache">Query cache instance.</param>
        public GXDbConnection(DbConnection connection, string tablePrefix, GXQueryCache queryCache)
        {
            AutoTransaction = true;
            Connection = connection ?? throw new ArgumentException(null, nameof(connection));
            if (connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
            Builder = new GXSqlBuilder(connection, tablePrefix);
            this.queryCache = queryCache ?? new GXQueryCache(TimeSpan.FromMinutes(10), Builder.Settings.Type);
        }

        /// <summary>
        /// Execute scalar.
        /// </summary>
        /// <param name="query">The scalar query.</param>
        /// <returns>Returns the result of the scalar query.</returns>
        public T ExecuteScalar<T>(string query)
        {
            return (T)GXSchemaManager.ExecuteScalarInternal(Connection, null, query, typeof(T));
        }

        /// <summary>
        /// Execute given SQL query that does not return any result.
        /// </summary>
        /// <param name="query">Query to execute.</param>
        public void ExecuteNonQuery(string query)
        {
            ExecuteNonQuery(null, query);
        }

        /// <summary>
        /// Execute given SQL query that does not return any result.
        /// </summary>
        /// <param name="transaction">Used transaction.</param>
        /// <param name="query">Query to execute.</param>
        public void ExecuteNonQuery(IDbTransaction transaction, string query)
        {
            IDbConnection connection;
            if (transaction == null)
            {
                connection = Connection;
            }
            else
            {
                connection = transaction.Connection;
            }
            GXSchemaManager.ExecuteNonQuery(connection, transaction, sql, query);
        }

        /// <summary>
        /// Returns last inserted ID.
        /// </summary>
        /// <returns>Last inserted row ID.</returns>
        private object GetLastInsertId(IDbConnection connection, IDbTransaction transaction, Type valueType, string columnName, Type tableType)
        {
            string table = null;
            if (tableType != null)
            {
                if (Builder.Settings.Type == DatabaseType.Oracle ||
                Builder.Settings.Type == DatabaseType.SapHana)
                {
                    table = Builder.GetTableName(tableType, false);
                }
                else
                {
                    table = Builder.GetTableName(tableType, true);
                }
            }
            string sql = Builder.Settings.GetLastInsertId(table, columnName);
            return GXSchemaManager.ExecuteScalarInternal(connection, transaction, sql, valueType);
        }

        /// <summary>
        /// Get the current connected user.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Name of the current user.</returns>
        public string GetCurrentUser(IDbTransaction transaction = null)
        {
            string query = Builder.Settings.GetCurrentUserQuery();
            return GXSchemaManager.ExecuteQuery(Connection, transaction, query)[0];
        }

        /// <summary>
        /// Get list of users.
        /// </summary>
        /// <param name="databaseName">Database name.</param>   
        /// <returns>Array of user names.</returns>
        public string[] GetUsers(string databaseName = null)
        {
            return GetUsers(null, databaseName);
        }

        /// <summary>
        /// Get list of users.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>   
        /// <returns>Array of user names.</returns>
        public string[] GetUsers(IDbTransaction transaction, string databaseName = null)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string query = Builder.Settings.GetUsersQuery(databaseName);
            if (Builder.Settings.Type == DatabaseType.DB2)
            {
                //DB2 can get users only from the current database, so we need to change the database first.
                var old = Connection.Database;
                if (!string.IsNullOrEmpty(databaseName) && old != databaseName)
                {
                    ChangeDatabase(databaseName);
                }
                try
                {
                    return ((List<string>)Builder.SelectInternal<string>(Connection, transaction, query)).ToArray();
                }
                finally
                {
                    if (!string.IsNullOrEmpty(databaseName) && old != databaseName)
                    {
                        ChangeDatabase(old);
                    }
                }
            }
            return ((List<string>)Builder.SelectInternal<string>(Connection, transaction, query)).ToArray();
        }

        /// <summary>
        /// Remove users.
        /// </summary>
        /// <param name="users">Array of users to remove.</param>   
        public void RemoveUsers(IEnumerable<string> users)
        {
            RemoveUsers(null, null, users);
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        /// <param name="users">Array of users to remove.</param>   
        public void RemoveUsers(string databaseName, IEnumerable<string> users)
        {
            RemoveUsers(null, databaseName, users);
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="users">Array of users to remove.</param>   
        /// <param name="databaseName">Database name.</param>   
        public void RemoveUsers(IDbTransaction transaction, string databaseName, IEnumerable<string> users)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                //SQL lite does not have users and permissions.
                return;
            }
            if (!users.Any())
            {
                throw new ArgumentException("At least one user name must be provided.");
            }
            List<string> queries = new List<string>();
            foreach (var it in users)
            {
                if (string.IsNullOrEmpty(it))
                {
                    throw new ArgumentException("User name cannot be empty.");
                }
                string query = Builder.Settings.RemoveUserQuery(databaseName, it);
                queries.Add(query);
            }

            foreach (var query in queries)
            {
                GXSchemaManager.ExecuteNonQuery(Connection, transaction, sql, query);
            }
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
        public bool DatabaseExists(IDbTransaction transaction, string databaseName)
        {
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string[] databases = GetDatabases(transaction);
            return databases.Contains(databaseName);
        }

        /// <summary>
        /// Returns the permissions of the given user for the given database.
        /// </summary>
        /// <param name="userName">User name to get permissions for.</param>
        /// <param name="databaseName">Database name.</param>
        public DatabasePermission GetUserPermission(string userName = null, string databaseName = null)
        {
            return GetUserPermission(null, userName, databaseName);
        }

        /// <summary>
        /// Returns the permissions of the given user for the given database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="userName">User name to get permissions for.</param>
        public DatabasePermission GetUserPermission(IDbTransaction transaction, string userName = null, string databaseName = null)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                //SQL lite does not have users and permissions, so we return Admin permission.
                return DatabasePermission.Admin;
            }
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string query;
            if (string.IsNullOrEmpty(userName))
            {
                query = Builder.Settings.GetCurrentUserQuery();
                userName = GXSchemaManager.ExecuteQuery(Connection, transaction, query)[0];
            }
            DatabasePermission permission = DatabasePermission.None;
            string old = Connection.Database;
            if (!string.IsNullOrEmpty(databaseName) && old != databaseName)
            {
                ChangeDatabase(databaseName);
            }
            try
            {
                query = Builder.Settings.GetDatabaseUserPermissionQuery(databaseName, userName);
                string[] values = GXSchemaManager.ExecuteQuery(Connection, transaction, query);
                foreach (var it in values)
                {
                    var value = Builder.Settings.ToDatabasePermission(it);
                    if (value == DatabasePermission.Admin)
                    {
                        permission = DatabasePermission.Admin;
                        break;
                    }
                    permission |= value;
                }

                if (Builder.Settings.Type == DatabaseType.MariaDB &&
                    permission == (DatabasePermission.Create | DatabasePermission.Alter |
                    DatabasePermission.Drop | DatabasePermission.Insert |
                    DatabasePermission.Update | DatabasePermission.Delete |
                    DatabasePermission.Select | DatabasePermission.Index |
                    DatabasePermission.References | DatabasePermission.Execute | DatabasePermission.CreateView |
                    DatabasePermission.CreateProcedure | DatabasePermission.CreateFunction))
                {
                    permission = DatabasePermission.Admin;
                }
                else if (Builder.Settings.Type == DatabaseType.SapHana &&
                    permission == (DatabasePermission.Create | DatabasePermission.Alter |
                    DatabasePermission.Drop | DatabasePermission.Insert |
                    DatabasePermission.Update | DatabasePermission.Delete |
                    DatabasePermission.Select | DatabasePermission.Execute))
                {
                    permission = DatabasePermission.Admin;
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(old) && old != databaseName)
                {
                    Connection.ChangeDatabase(old);
                }
            }
            return permission;
        }


        /// <summary>
        /// Add users to the database.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        /// <param name="permissions">Database permissions.</param>
        /// <param name="users">Array of user names to add to the database.</param>
        public void AddUsersToDatabase(string databaseName,
            DatabasePermission permissions,
            params IEnumerable<string> users)
        {
            AddUsersToDatabase(null, databaseName, permissions, users);
        }

        /// <summary>
        /// Add users to the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="permissions">Database permissions.</param>
        /// <param name="users">Array of user names to add.</param>
        public void AddUsersToDatabase(IDbTransaction transaction,
            string databaseName,
            DatabasePermission permissions,
            params IEnumerable<string> users)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite ||
                Builder.Settings.Type == DatabaseType.DB2 ||
                Builder.Settings.Type == DatabaseType.Oracle)
            {
                //SQL lite does not have users and permissions.
                return;
            }
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string old = Connection.Database;
            List<string> queries = [];
            Builder.Settings.AddUsersToDatabaseQuery(queries, databaseName, permissions, users);
            if (old != databaseName)
            {
                ChangeDatabase(databaseName);
            }
            try
            {
                foreach (var it in queries)
                {
                    ExecuteNonQuery(transaction, it);
                }
            }
            finally
            {
                if (old != databaseName)
                {
                    Connection.ChangeDatabase(old);
                }
            }
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="databaseName">Database name.</param>
        /// <param name="users">Array of user names to remove from the database.</param>
        public void RemoveUsersFromDatabase(string databaseName,
            params IEnumerable<string> users)
        {
            RemoveUsersFromDatabase(null, databaseName, users);
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="users">Array of user names to remove.</param>
        public void RemoveUsersFromDatabase(IDbTransaction transaction,
            string databaseName,
            params IEnumerable<string> users)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite ||
                Builder.Settings.Type == DatabaseType.DB2 ||
                Builder.Settings.Type == DatabaseType.Oracle)
            {
                //SQL lite does not have users and permissions.
                return;
            }
            databaseName = GXDbHelpers.GetDatabaseName(Builder.Settings.Type, databaseName);
            string old = Connection.Database;
            List<string> queries = [];
            Builder.Settings.RemoveUsersFromDatabaseQuery(queries, databaseName, users);
            if (old != databaseName)
            {
                ChangeDatabase(databaseName);
            }
            try
            {
                foreach (var it in queries)
                {
                    ExecuteNonQuery(transaction, it);
                }
            }
            finally
            {
                if (old != databaseName)
                {
                    Connection.ChangeDatabase(old);
                }
            }
        }

        /// <summary>
        /// Add users.
        /// </summary>
        /// <param name="users">Array of user names to add.</param>
        public void AddUsers(params DatabaseUser[] users)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite ||
                Builder.Settings.Type == DatabaseType.DB2)
            {
                //SQL lite does not have users and permissions.
                return;
            }
            List<string> queries = [];
            Builder.Settings.AddUsersQuery(queries, users);
            foreach (var it in queries)
            {
                ExecuteNonQuery(null, it);
            }
        }

        /// <summary>
        /// Check is table empty.
        /// </summary>
        /// <returns>True, if thable is empty.</returns>
        public bool IsEmpty<T>(IDbTransaction transaction = null)
        {
            string tableName = Builder.GetTableName(typeof(T), false);
            string query = Builder.Settings.IsEmpty(tableName);
            object ret = GXSchemaManager.ExecuteScalarInternal(Connection, transaction, query, null);
            return ret == null || Convert.ToInt32(ret) == 0;
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        public async Task DeleteAsync(GXDeleteArgs arg)
        {
            await DeleteAsync(arg, CancellationToken.None);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAsync(GXDeleteArgs arg, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() =>
            {
                Delete(arg);
            }, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAsync(IDbTransaction transaction, GXDeleteArgs arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() =>
            {
                Delete(transaction, arg);
            }, cancellationToken);
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
        /// <param name="transaction">Transaction.</param>  
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
                connection = Connection;
                if (AutoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
            }
            try
            {
                arg.UseQueryCache(QueryCache);
                arg.Settings = Builder.Settings;
                GXSchemaManager.ExecuteNonQuery(connection, transaction, sql, arg.ToString(false));
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
            args.Settings = Builder.Settings;
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(string id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(string id, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = Builder.Settings;
            List<T> list = SelectInternal<T>(Connection, null, args, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return SelectById<T>(id, columns);
            }, cancellationToken);
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
            args.Settings = Builder.Settings;
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(Guid id, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return SelectById<T>(id, columns);
            }, cancellationToken);
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
            args.Settings = Builder.Settings;
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<T> SelectByIdAsync<T>(long id, CancellationToken cancellationToken = default)
        {
            return SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(long id, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return SelectById<T>(id, columns);
            }, cancellationToken);
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
            args.Settings = Builder.Settings;
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(UInt64 id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(UInt64 id, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return SelectById<T>(id, columns);
            }, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">Type of the database.</typeparam>
        /// <typeparam name="ID_TYPE">ID type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T, ID_TYPE>(ID_TYPE id, Expression<Func<T, object>> columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T, ID_TYPE>(id, columns);
            args.Settings = Builder.Settings;
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
        /// Select all items from the database. This method is not recommended to use if there are many items in the database, 
        /// because it can cause performance issues.
        /// </summary>
        /// <typeparam name="T">Type of the database object.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <returns>List of all items.</returns>
        public List<T> SelectAll<T>(IDbTransaction transaction = default)
        {
            return Select<T>(transaction, (GXSelectArgs)null);
        }

        /// <summary>
        /// Select all items from the database. This method is not recommended to use if there are many items in the database, 
        /// because it can cause performance issues.
        /// </summary>
        /// <typeparam name="T">Type of the database object.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all items.</returns>
        public async Task<List<T>> SelectAllAsync<T>(IDbTransaction transaction = default,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await SelectAsync<T>(transaction, (GXSelectArgs)null, cancellationToken);
        }

        /// <summary>
        /// Select data from the database.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>List of selected entities.</returns>
        public List<T> Select<T>(GXSelectArgs arg)
        {
            return Select<T>(null, arg, CancellationToken.None);
        }

        /// <summary>
        /// Select data from the database.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of selected entities.</returns>
        public List<T> Select<T>(GXSelectArgs arg, CancellationToken cancellationToken = default)
        {
            return Select<T>(null, arg, cancellationToken);
        }

        /// <summary>
        ///  Select data from the database.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="transaction">Database transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of selected entities.</returns>
        public List<T> Select<T>(IDbTransaction transaction, GXSelectArgs arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SelectInternal<T>(transaction?.Connection, transaction, arg, cancellationToken);
        }

        private List<T> SelectInternal<T>(
            IDbConnection connection,
            IDbTransaction transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken)
        {
            if (arg == null)
            {
                arg = GXSelectArgs.SelectAll<T>(QueryCache);
            }
            arg.UseQueryCache(QueryCache);
            arg.Verify();
            arg.Parent.Settings = Builder.Settings;
            arg.GenerationTime = 0;
            DateTime tm = DateTime.Now;
            bool release = connection == null && transaction == null;
            if (release)
            {
                connection = Connection;
            }
            if (transaction != null)
            {
                connection = transaction.Connection;
            }
            List<T> value = (List<T>)SelectInternal2<T>(connection, transaction,
                    arg, cancellationToken);
            arg.GenerationTime = (int)(DateTime.Now - tm).TotalMilliseconds;
            if (sql != null)
            {
                sql(this, arg.query, arg.GenerationTime);
            }
            return value;
        }

        /// <summary>
        /// Select data from the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="transaction">Database transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>List of selected entities.</returns>
        public async Task<List<T>> SelectAsync<T>(IDbTransaction transaction, GXSelectArgs arg)
        {
            return await SelectAsync<T>(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Select data from the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="transaction">Database transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of selected entities.</returns>
        public async Task<List<T>> SelectAsync<T>(
            IDbTransaction transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool useTransaction = transaction != null;
            IDbConnection connection;
            if (useTransaction)
            {
                connection = transaction.Connection;
            }
            else
            {
                connection = Connection;
            }
            return await Task.Run(() =>
            {
                return SelectInternal<T>(connection, transaction, arg, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Select data from the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>List of selected entities.</returns>
        public async Task<List<T>> SelectAsync<T>(GXSelectArgs arg)
        {
            return await SelectAsync<T>(arg, CancellationToken.None);
        }

        /// <summary>
        /// Select data from the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of the database entity.</typeparam>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of selected entities.</returns>
        public async Task<List<T>> SelectAsync<T>(GXSelectArgs arg, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
        {
            return SelectInternal<T>(null, null, arg, cancellationToken);
        }, cancellationToken);
        }

        private object SelectInternal2<T>(
            IDbConnection connection,
            IDbTransaction transaction,
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
                //Loop throw all tables and add only selected tables.
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
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<Type, List<string>> excluded = null;
            if (arg.Columns.Excluded.Any())
            {
                GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Column)
                {
                };
                //Add excluded columns.
                excluded = new Dictionary<Type, List<string>>();
                foreach (KeyValuePair<Type, LambdaExpression> e in arg.Columns.Excluded)
                {
                    if (!excluded.ContainsKey(e.Key))
                    {
                        excluded[e.Key] = new List<string>();
                    }
                    args.Expression = e.Value;
                    var tmp = GXDbHelpers.GetMemberList(args);
                    excluded[e.Key].AddRange(tmp);
                }
            }
            using (IDbCommand com = connection.CreateCommand())
            {
                if (CommandTimeout > 0)
                {
                    com.CommandTimeout = CommandTimeout;
                }
                com.Transaction = transaction;
                com.CommandType = CommandType.Text;
                com.CommandText = query;
                try
                {
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
                                Builder.InitializeSelect<T>(reader, Builder.Settings, tables, TableIndexes, columns, mapTables, relationDataSetters);
                            }
                            targetTable = null;
                            if (list != null)
                            {
                                //If we want to read only basic data types example count(*)
                                if (GXInternal.IsGenericDataType(type))
                                {
                                    list.Add((T)Builder.Settings.ChangeType(reader.GetValue(0), type));
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
                                    else if (type == typeof(Guid) && values[0] is byte[] ba)
                                    {
                                        baseList.Add((T)(object)new Guid(ba));
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
                                int colCount = Math.Min(reader.FieldCount, columns.Count);
                                for (int pos = 0; pos != colCount; ++pos)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    value = null;
                                    //If we are asking some data from the DB that is not exist on class.
                                    //This is removed from the interface etc...
                                    if (!columns.TryGetValue(pos, out GXColumnHelper col))
                                    {
                                        continue;
                                    }
                                    //If we are reading multiple objects and object has changed.
                                    if (!string.Equals(col.Table, targetTable, StringComparison.OrdinalIgnoreCase))
                                    {
                                        isCreated = false;
                                        if (TableIndexes != null && TableIndexes.TryGetValue(col.TableType, out int tableIdx))
                                        {
                                            id = values[tableIdx];
                                            //Id is not save directly because class might change it's type example from uint to int.
                                            id = Builder.Settings.ChangeType(id, col.Setter.Type);
                                            if (id == null || id is DBNull)
                                            {
                                                UpdateReferences(item, relationDataSetters, mapTables, updatedReferences, manyToManyObjects);
                                                isCreated = true;
                                                item = null;
                                            }
                                            else
                                            {
                                                //Check is item created already and return item if it exists.
                                                if (allObjects.TryGetValue(col.TableType, out var typeObjs) &&
                                                    typeObjs.TryGetValue(id, out var existingItem))
                                                {
                                                    item = existingItem;
                                                    isCreated = true;
                                                    UpdateReferences(item, relationDataSetters, mapTables, updatedReferences, manyToManyObjects);
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
                                                item = GXInternal.CreateClass(col.TableType);
                                                if (allObjects != null)
                                                {
                                                    if (!allObjects.TryGetValue(col.TableType, out var typeMap))
                                                    {
                                                        typeMap = new Dictionary<object, object>();
                                                        allObjects.Add(col.TableType, typeMap);
                                                    }
                                                    //If only one table is read.
                                                    if (id != null)
                                                    {
                                                        typeMap[id] = item;
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
                                                string posStr = values[pos]?.ToString();
                                                if (!string.IsNullOrEmpty(posStr))
                                                {
                                                    string[] tmp = posStr.Split(new char[] { ';' });
                                                    Array items = Array.CreateInstance(pt, tmp.Length);
                                                    int pos2 = -1;
                                                    foreach (string it in tmp)
                                                    {
                                                        items.SetValue(Builder.Settings.ChangeType(it, pt), ++pos2);
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
                                                var tmp = col.Setter.Type;
                                                bool isNullable = tmp.IsGenericType && tmp.GetGenericTypeDefinition() == typeof(Nullable<>);
                                                if (isNullable)
                                                {
                                                    tmp = Nullable.GetUnderlyingType(tmp);
                                                }
                                                value = values[pos];
                                                if (value is DBNull)
                                                {
                                                    value = null;
                                                }
                                                value = Builder.Settings.ChangeType(value, tmp);
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
                                    object relationId = Builder.Settings.ChangeType(values[it.Key], col.Setter.Relation.ForeignId.Type);
                                    Type itValueType = it.Value.GetType();
                                    if (allObjects.TryGetValue(col.Setter.Type, out var relObjects))
                                    {
                                        foreach (var it2 in relObjects)
                                        {
                                            if (it2.Key.Equals(relationId))
                                            {
                                                bool exclude = false;
                                                //If column is excluded.
                                                if (excluded != null && excluded.TryGetValue(itValueType, out var excludedCols)
                                                    && excludedCols.Contains(columns[it.Key].Name))
                                                {
                                                    exclude = true;
                                                }
                                                if (!exclude)
                                                {
                                                    col.Setter.Set(it.Value, it2.Value);
                                                }
                                                if (relationDataSetters.TryGetValue(itValueType, out var relSetters) &&
                                                    relSetters.ContainsKey(it2.Value.GetType()))
                                                {
                                                    if (!oneToManyObjects.TryGetValue(itValueType, out var oneToMany))
                                                    {
                                                        oneToMany = new Dictionary<object, List<object>>();
                                                        oneToManyObjects.Add(itValueType, oneToMany);
                                                    }
                                                    if (!oneToMany.TryGetValue(it2.Value, out var oneToManyList))
                                                    {
                                                        oneToManyList = new List<object>();
                                                        oneToMany.Add(it2.Value, oneToManyList);
                                                    }
                                                    oneToManyList.Add(it.Value);
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
                catch (Exception ex)
                {
                    throw GXDatabaseException.Create(ex, com.CommandText);
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
                                it2.Value.Set(it3.Key, GXInternal.ConvertListIfNeeded(it3.Value, it2.Value.Type));
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
                                it2.Value.Set(it3.Key, GXInternal.ConvertListIfNeeded(it3.Value, it2.Value.Type));
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
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public T SingleOrDefault<T>(IDbTransaction transaction, GXSelectArgs arg)
        {
            List<T> list = Select<T>(transaction, arg);
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
        public async Task<T> SingleOrDefaultAsync<T>(
            GXSelectArgs arg,
            CancellationToken cancellationToken = default)
        {
            return await SingleOrDefaultAsync<T>(null,
                arg, cancellationToken);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(IDbTransaction transaction, GXSelectArgs arg)
        {
            return await SingleOrDefaultAsync<T>(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Database object.</returns>
        public async Task<T> SingleOrDefaultAsync<T>(
            IDbTransaction transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(arg);
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
                throw new ArgumentException("Insert arguments cannot be null.");
            }
            if (arg.ToString(false) == "")
            {
                //Nothing to insert.
                return;
            }
            arg.UseQueryCache(QueryCache);
            arg.Settings = Builder.Settings;
            GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Table)
            {
            };
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded,
                true, false, Builder.Settings.ColumnNameQuoteCharacter, false, null, null, arg.insertedObjects);
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
            UpdateOrInsert(connection, transaction, null, list);
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
        public async Task InsertAsync(GXInsertArgs arg,
            CancellationToken cancellationToken = default)
        {
            await InsertAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Insert argument.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InsertAsync(IDbTransaction transaction, GXInsertArgs arg,
            CancellationToken cancellationToken = default)
        {
            if (arg == null)
            {
                throw new ArgumentException("Insert failed. There is nothing to insert.");
            }
            arg.UseQueryCache(QueryCache);
            arg.Settings = Builder.Settings;
            GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Table)
            {
            };
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded, true, false,
                    Builder.Settings.ColumnNameQuoteCharacter, false, null, null, arg.insertedObjects);
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
            await UpdateOrInsert(connection, transaction, null, list);
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
            arg.UseQueryCache(QueryCache);
            arg.Settings = Builder.Settings;
            GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Table)
            {
            };
            //Get values to insert first.
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> handledObjects = new List<object>();
            if (arg.Where == null || arg.Where.List.Count == 0)
            {
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded, true, false,
                        Builder.Settings.ColumnNameQuoteCharacter, false, arg.Where, handledObjects, null);
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
            UpdateOrInsert(connection, transaction, null, list);
            list.Clear();
            //Get updated values.
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded,
                    false, false, Builder.Settings.ColumnNameQuoteCharacter, true, arg.Where, handledObjects, null);
            }
            UpdateOrInsert(connection, transaction, arg, list);
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task UpdateAsync(GXUpdateArgs arg, CancellationToken cancellationToken = default)
        {
            return UpdateAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="arg">Update arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task UpdateAsync(IDbTransaction transaction, GXUpdateArgs arg,
            CancellationToken cancellationToken = default)
        {
            if (arg == null)
            {
                throw new ArgumentException("Update failed. There is nothing to update.");
            }
            arg.UseQueryCache(QueryCache);
            arg.Settings = Builder.Settings;
            GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Table)
            {
            };

            //Get values to insert first.
            List<KeyValuePair<Type, GXUpdateItem>> list = new List<KeyValuePair<Type, GXUpdateItem>>();
            List<object> handledObjects = new List<object>();
            if (arg.Where == null || arg.Where.List.Count == 0)
            {
                foreach (var it in arg.Values)
                {
                    GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded,
                        true, false, Builder.Settings.ColumnNameQuoteCharacter, false, arg.Where, handledObjects, null);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
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
            await UpdateOrInsert(connection, transaction, null, list);
            list.Clear();
            //Get updated values.
            foreach (var it in arg.Values)
            {
                GXDbHelpers.GetValues(args, it.Key, null, it.Value, list, arg.Excluded,
                    false, false, Builder.Settings.ColumnNameQuoteCharacter, true, arg.Where, handledObjects, null);
            }
            await UpdateOrInsertAsync(connection, transaction, arg, list, cancellationToken);
        }

        private Task UpdateOrInsertAsync(IDbConnection connection,
            IDbTransaction transaction,
            object caller,
            List<KeyValuePair<Type, GXUpdateItem>> list,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.Run(() => UpdateOrInsert(connection, transaction, caller, list), cancellationToken);
        }

        /// <summary>
        /// Update or insert new value to the DB.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="caller">Insert or update.</param>
        /// <param name="list">List of tables to update.</param>
        private Task UpdateOrInsert(IDbConnection connection,
            IDbTransaction transaction,
            object caller,
            List<KeyValuePair<Type, GXUpdateItem>> list)
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
            List<int> queryRowCounts = new List<int>();
            bool update = caller is GXUpdateArgs;
            try
            {
                if (AutoTransaction && transactionUninitialized)
                {
                    transaction = connection.BeginTransaction();
                }
                GXGetMembersArgs args = new GXGetMembersArgs(Builder.Settings, TargetType.Table)
                {
                    SingleTable = true
                };
                foreach (var q in list)
                {
                    queries.Clear();
                    queryRowCounts.Clear();
                    if (update)
                    {
                        GXDbHelpers.GetUpdateQuery((GXUpdateArgs)caller, q, Builder.Settings, queries);
                    }
                    else
                    {
                        total += GXDbHelpers.GetInsertQuery(args, q, queries, queryRowCounts);
                        q.Value.Inserted = true;
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
                    int queryIndex = 0;
                    foreach (string query in queries)
                    {
                        GXSchemaManager.ExecuteNonQuery(connection, transaction, sql, query);
                        //Update auto increment value immediately after the insert statement.
                        if (!update && si != null && pos != -1)
                        {
                            columnName = GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Column | TargetType.Plain, null, si.Target as PropertyInfo, null);
                            id = (ulong)GetLastInsertId(connection, transaction, typeof(ulong), columnName, type);
                            int rowCount = queryRowCounts.Count > queryIndex ? queryRowCounts[queryIndex] : q.Value.Rows.Count;
                            if (!Builder.Settings.AutoIncrementFirst)
                            {
                                id -= (ulong)(rowCount - 1);
                            }
                            for (int pos2 = 0; pos2 < rowCount && index < q.Value.Rows.Count; ++pos2)
                            {
                                var it = q.Value.Rows[index];
                                if (Convert.ChangeType(si.Get(it[pos].Key), si.Type).Equals(Convert.ChangeType(0, si.Type)))
                                {
                                    si.Set(it[pos].Key, Convert.ChangeType(id, si.Type));
                                }
                                ++id;
                                ++index;
                            }
                        }
                        ++queryIndex;
                    }
                }
                if (transaction != null && transactionUninitialized)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
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
            return Builder.GetTableName(typeof(T), false);
        }

        /// <summary>
        /// Close connection.
        /// </summary>
        public void CloseConnection()
        {
            Connection.Close();
        }

        /// <summary>
        /// Dispose connection.
        /// </summary>
        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }

        /// <summary>
        /// Delete ALL items from the table.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <param name="transaction">Transaction to use.</param>
        public void Truncate<T>(IDbTransaction transaction = default)
        {
            //SQLite don't support truncate.
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Delete(GXDeleteArgs.DeleteAll<T>());
            }
            else
            {
                string query = "TRUNCATE TABLE " + Builder.GetTableName(typeof(T), true);
                GXSchemaManager.ExecuteNonQuery(Connection, transaction, sql, query);
            }
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction to use.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TruncateAsync<T>(IDbTransaction transaction = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() =>
            {
                Truncate<T>(transaction);
            }, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            Connection.DisposeAsync();
            return ValueTask.CompletedTask;
        }
    }
}
