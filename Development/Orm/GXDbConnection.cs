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
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Internal;
using Gurux.Service.Orm.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Gurux.Service.Orm
{
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
        readonly GXQueryCache _queryCache;
        internal GXSqlBuilder Builder;

        /// <summary>Gets the table-description cache shared with schema managers on this connection.</summary>
        public GXSchemaCache SchemaCache => GXSchemaCache.ForConnection(Connection);

        /// <summary>
        /// Query cache instance.
        /// </summary>
        public GXQueryCache QueryCache
        {
            get
            {
                return _queryCache;
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
                return _queryCache.CacheTime;
            }
            set
            {
                _queryCache.CacheTime = value;
            }
        }

        /// <summary>
        /// Clear all cached SQL query strings.
        /// </summary>
        public void ClearQueryCache()
        {
            _queryCache.Clear();
        }

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
        public event EventHandler<GXSqlExecutedEventArgs>? OnSqlExecuted;

        /// <summary>
        /// Event handler for column or table update.
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
        public IDbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            IDbTransaction transaction = Connection.BeginTransaction(isolationLevel);
            return transaction;
        }

        /// <inheritdoc />
        public ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
          => Connection.BeginTransactionAsync(System.Data.IsolationLevel.Unspecified, cancellationToken);

        /// <inheritdoc />
        public ValueTask<DbTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
            => Connection.BeginTransactionAsync(isolationLevel, cancellationToken);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        public GXDbConnection(DbConnection connection, string? tablePrefix = null)
            : this(connection, tablePrefix, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="tablePrefix">Table prefix.</param>
        /// <param name="queryCache">Query cache instance.</param>
        public GXDbConnection(DbConnection connection,
            string? tablePrefix,
            GXQueryCache? queryCache)
        {
            AutoTransaction = true;
            Connection = connection ?? throw new ArgumentException(null, nameof(connection));
            if (connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
            Builder = new GXSqlBuilder(connection, tablePrefix);
            _queryCache = queryCache ?? new GXQueryCache(TimeSpan.FromMinutes(10), Builder.Settings.Type);
            if (Builder.Settings.Type == DatabaseType.DB2 &&
                !string.IsNullOrEmpty(connection.Database))
            {
                ChangeDatabase(connection.Database);
            }
        }

        /// <summary>
        /// Execute scalar.
        /// </summary>
        /// <param name="query">The scalar query.</param>
        /// <returns>Returns the result of the scalar query.</returns>
        public T? ExecuteScalar<T>(string query)
        {
            return (T?)GXSchemaManager.ExecuteScalarInternal(Connection, null, query, typeof(T));
        }

        /// <summary>
        /// Execute scalar.
        /// </summary>
        /// <param name="query">The scalar query.</param>
        /// <returns>Returns the result of the scalar query.</returns>
        /// <typeparam name="T">CLR type used to convert the scalar result.</typeparam>
        /// <param name="cancellationToken">Token used to cancel query execution.</param>
        public async ValueTask<T?> ExecuteScalarAsync<T>(string query, CancellationToken cancellationToken = default)
        {
            return (T?)await GXSchemaManager.ExecuteScalarInternalAsync(Connection, query, typeof(T), cancellationToken);
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
        public void ExecuteNonQuery(IDbTransaction? transaction, string query)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            GXSchemaManager.ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
        }

        /// <summary>
        /// Returns last inserted ID.
        /// </summary>
        /// <returns>Last inserted row ID.</returns>
        private object? GetLastInsertId(IDbConnection connection,
            IDbTransaction? transaction,
            Type valueType,
            string columnName,
            Type tableType)
        {
            string table = Builder.GetTableName(tableType, true);
            columnName = Builder.Settings.EscapeIdentifier(null, columnName);
            string sql = Builder.Settings.GetLastInsertId(table, columnName);
            return GXSchemaManager.ExecuteScalarInternal(connection, transaction, sql, valueType);
        }

        /// <summary>
        /// Get the current connected user.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <returns>Name of the current user.</returns>
        public string GetCurrentUser(IDbTransaction? transaction = null)
        {
            string query = Builder.Settings.GetCurrentUserQuery();
            return GXSchemaManager.ExecuteQuery(Connection, transaction, query)[0];
        }

        /// <summary>
        /// Get list of users.
        /// </summary>
        /// <param name="databaseName">Database name.</param>   
        /// <returns>Array of user names.</returns>
        public string[] GetUsers(string? databaseName = null)
        {
            return GetUsers(null, databaseName);
        }

        /// <summary>
        /// Get list of users.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>   
        /// <returns>Array of user names.</returns>
        public string[] GetUsers(IDbTransaction? transaction, string? databaseName = null)
        {
            if (!string.IsNullOrEmpty(databaseName))
            {
                databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
            }
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
                    return ((List<string>)Builder.SelectInternal<string>(this,
                        Connection, transaction, OnSqlExecuted, query, 0, CommandTimeout)).ToArray();
                }
                finally
                {
                    if (!string.IsNullOrEmpty(databaseName) && old != databaseName)
                    {
                        ChangeDatabase(old);
                    }
                }
            }
            var values = Builder.SelectInternal<string>(this, Connection,
                transaction, OnSqlExecuted, query, 0, CommandTimeout);
            return values.ToArray();
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="users">Array of users to remove.</param>   
        public void RemoveDatabaseUsers(params IEnumerable<string> users)
        {
            RemoveDatabaseUsers(null, users);
        }

        /// <summary>
        /// Remove users from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="users">Array of users to remove.</param>   
        public void RemoveDatabaseUsers(IDbTransaction? transaction,
            params IEnumerable<string> users)
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
                string query = Builder.Settings.RemoveUserQuery(null, it);
                queries.Add(query);
            }

            foreach (var query in queries)
            {
                GXSchemaManager.ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
            }
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
        /// Returns the permissions of the given user for the given database.
        /// </summary>
        /// <param name="userName">User name to get permissions for. If null, the current user is used.</param>
        /// <param name="databaseName">Database name. If null, the current database is used.</param>
        public DatabasePermission GetUserPermission(string? userName = null, string? databaseName = null)
        {
            return GetUserPermission(null, userName, databaseName);
        }

        /// <summary>
        /// Returns the permissions of the given user for the given database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="userName">User name to get permissions for.</param>
        public DatabasePermission GetUserPermission(IDbTransaction? transaction,
            string? userName = null, string? databaseName = null)
        {
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                //SQL lite does not have users and permissions, so we return Admin permission.
                return DatabasePermission.Admin;
            }
            if (!string.IsNullOrEmpty(databaseName))
            {
                databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
            }
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
        public void AddUsersToDatabase(IDbTransaction? transaction,
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
            databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
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
        public void RemoveUsersFromDatabase(IDbTransaction? transaction,
            string databaseName,
            params IEnumerable<string> users)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Database name cannot be empty.");
            }
            if (Builder.Settings.Type == DatabaseType.SqLite ||
                Builder.Settings.Type == DatabaseType.DB2 ||
                Builder.Settings.Type == DatabaseType.Oracle)
            {
                //SQL lite does not have users and permissions.
                return;
            }
            databaseName = Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, databaseName);
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
        /// <returns>True, if table is empty.</returns>
        public bool IsEmpty<T>(IDbTransaction? transaction = null)
        {
            string tableName = Builder.GetTableName(typeof(T), false);
            return IsEmpty(transaction, tableName);
        }

        /// <summary>
        /// Check is table empty.
        /// </summary>
        /// <returns>True, if table is empty.</returns>
        public bool IsEmpty(string tableName)
        {
            return IsEmpty(null, tableName);
        }

        /// <summary>
        /// Check is table empty.
        /// </summary>
        /// <returns>True, if table is empty.</returns>
        public bool IsEmpty(IDbTransaction? transaction, string tableName)
        {
            string query = Builder.Settings.IsEmpty(tableName);
            object? ret = GXSchemaManager.ExecuteScalarInternal(Connection, transaction, query, null);
            return ret == null || Convert.ToInt32(ret) == 0;
        }

        /// <summary>
        /// Get table row count.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Number of rows in the table.</returns>
        public T GetTableRowCount<T>(IDbTransaction? transaction, string tableName)
        {
            ArgumentNullException.ThrowIfNull(tableName);
            string query = Builder.Settings.IsEmpty(tableName);
            object? ret = GXSchemaManager.ExecuteScalarInternal(Connection, transaction, query, null);
            return ret == null ? (T)Convert.ChangeType(0, typeof(T)) : (T)Convert.ChangeType(ret, typeof(T));
        }

        /// <summary>
        /// Get table row count.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Number of rows in the table.</returns>
        public T GetTableRowCount<T>(string tableName)
        {
            return GetTableRowCount<T>(null, tableName);
        }

        /// <summary>
        /// Get table row count.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Number of rows in the table.</returns>
        public async Task<T> GetTableRowCountAsync<T>(IDbTransaction? transaction, string tableName)
        {
            return await Task.Run(() => GetTableRowCount<T>(transaction, tableName));
        }

        /// <summary>
        /// Get table row count.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Number of rows in the table.</returns>
        public Task<T> GetTableRowCountAsync<T>(string tableName)
        {
            return GetTableRowCountAsync<T>(null, tableName);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        public Task<int> DeleteAsync(GXDeleteArgs arg)
        {
            return DeleteAsync(arg, CancellationToken.None);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<int> DeleteAsync(GXDeleteArgs arg, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Delete arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<int> DeleteAsync(IDbTransaction? transaction,
            GXDeleteArgs arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return Delete(transaction, arg);
            }, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="arg">Delete arguments.</param>
        public int Delete(GXDeleteArgs arg)
        {
            return Delete(null, arg);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>  
        /// <param name="arg">Delete arguments.</param>
        public int Delete(IDbTransaction? transaction, GXDeleteArgs arg)
        {
            IDbConnection? connection;
            bool tranactionOnProgress = transaction != null;
            if (tranactionOnProgress)
            {
                connection = transaction?.Connection;
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
                int count = GXSchemaManager.ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, arg.ToString(false));
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction?.Commit();
                }
                return count;
            }
            catch (Exception)
            {
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction?.Rollback();
                }
                throw;
            }
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Name of the table to delete items from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<int> DeleteAsync(IDbTransaction? transaction, string tableName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() =>
            {
                return Delete(transaction, tableName);
            }, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="tableName">Name of the table to delete items from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<int> DeleteAsync(string tableName, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(null, tableName, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="tableName">Name of the table to delete items from.</param>
        public int Delete(string tableName)
        {
            return Delete(null, tableName);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="tableName">Name of the table to delete items from.</param>
        public int Delete(IDbTransaction? transaction, string tableName)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            bool tranactionOnProgress = transaction != null;
            if (AutoTransaction && !tranactionOnProgress)
            {
                transaction = connection.BeginTransaction();
            }
            try
            {
                string query = "DELETE FROM " + tableName;
                int count = GXSchemaManager.ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction?.Commit();
                }
                return count;
            }
            catch (Exception)
            {
                if (!tranactionOnProgress && AutoTransaction)
                {
                    transaction?.Rollback();
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        public T SelectById<T>(string id, Expression<Func<T, object>>? columns)
        {
            GXSelectArgs args = GXSelectArgs.SelectById<T>(id, columns);
            args.Settings = Builder.Settings;
            List<T> list = Select<T>(args);
            if (list.Count == 0)
            {
                throw new Exception($"Item with ID '{id}' does not exist.");
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(string id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(string id, Expression<Func<T, object>>? columns, CancellationToken cancellationToken = default)
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<T> SelectByIdAsync<T>(long id, CancellationToken cancellationToken = default)
        {
            return SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        /// <typeparam name="T">The mapped entity type.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<T> SelectByIdAsync<T>(UInt64 id, CancellationToken cancellationToken = default)
        {
            return await SelectByIdAsync<T>(id, null, cancellationToken);
        }

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">The mapped entity type.</typeparam>
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
        public List<T> SelectAll<T>(IDbTransaction? transaction = default)
        {
            return Select<T>(transaction, null);
        }

        /// <summary>
        /// Select all items from the database. This method is not recommended to use if there are many items in the database, 
        /// because it can cause performance issues.
        /// </summary>
        /// <typeparam name="T">Type of the database object.</typeparam>
        /// <param name="transaction">Transaction.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all items.</returns>
        public async Task<List<T>> SelectAllAsync<T>(IDbTransaction? transaction = default,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await SelectAsync<T>(transaction, (GXSelectArgs?)null, cancellationToken);
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
        public List<T> Select<T>(IDbTransaction? transaction, GXSelectArgs? arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SelectInternal<T>(transaction?.Connection ?? Connection,
                transaction, arg, cancellationToken);
        }

        private List<T> SelectInternal<T>(
            IDbConnection? connection,
            IDbTransaction? transaction,
            GXSelectArgs? arg,
            CancellationToken cancellationToken)
        {
            if (arg == null)
            {
                arg = GXSelectArgs.SelectAll<T>(QueryCache);
            }
            if (QueryCache != null)
            {
                arg.UseQueryCache(QueryCache);
            }
            arg.Verify();
            arg.GenerationTime = 0;
            DateTime tm = DateTime.Now;
            List<T> value = (List<T>)Builder.SelectInternal<T>(connection!, transaction,
                    arg, CommandTimeout, cancellationToken);
            arg.GenerationTime = (int)(DateTime.Now - tm).TotalMilliseconds;
            if (OnSqlExecuted != null)
            {
                OnSqlExecuted(this, new GXSqlExecutedEventArgs()
                {
                    Sql = arg.query!,
                    Elapsed = TimeSpan.FromMilliseconds(arg.GenerationTime)
                });
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
            IDbTransaction? transaction,
            GXSelectArgs arg,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IDbConnection connection = transaction?.Connection ?? Connection;
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
            return SelectInternal<T>(Connection, null, arg, cancellationToken);
        }, cancellationToken);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public T? SingleOrDefault<T>(IDbTransaction transaction, GXSelectArgs arg)
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
        public T? SingleOrDefault<T>(GXSelectArgs arg)
        {
            List<T> list = Select<T>(arg);
            if (list.Count == 0)
            {
                return default(T);
            }
            if (list.Count != 1)
            {
                throw new Exception("There are multiple items with same ID when id should be unique.");
            }
            return list[0];
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <returns>Database object.</returns>
        public async Task<T?> SingleOrDefaultAsync<T>(GXSelectArgs arg)
        {
            return await SingleOrDefaultAsync<T>(arg, CancellationToken.None);
        }

        /// <summary>
        /// Select object by ID and create empty object if it's not found from the database.
        /// </summary>
        /// <param name="arg">Selection arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Database object.</returns>
        public async Task<T?> SingleOrDefaultAsync<T>(
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
        public async Task<T?> SingleOrDefaultAsync<T>(IDbTransaction transaction, GXSelectArgs arg)
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
        public async Task<T?> SingleOrDefaultAsync<T>(
            IDbTransaction? transaction,
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
            if (typeof(T) == typeof(string) && list[0] is string str)
            {
                str = str.Replace(@"\\", @"\");
                return (T)(object)str;
            }
            return list[0];
        }


        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <param name="arg">The insert arguments describing the entities to insert.</param>
        public void Insert(GXInsertArgs arg)
        {
            Insert(null, arg);
        }

        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Insert arguments.</param>
        public void Insert(IDbTransaction? transaction,
            GXInsertArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentException("Insert arguments cannot be null.");
            }
            if (arg == null)
            {
                throw new ArgumentException(Properties.Resources.InsertFailedBecauseThereIsNoDataToInsert);
            }
            if (QueryCache != null)
            {
                arg.UseQueryCache(QueryCache);
            }
            arg.Settings = Builder.Settings;
            string query = arg.ToString(false);
            if (string.IsNullOrEmpty(query))
            {
                //If there is no data to insert, we just return.
                return;
            }
            bool autoTransaction = transaction == null && AutoTransaction;
            IDbConnection connection = transaction?.Connection ?? Connection;
            try
            {
                if (autoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
                GXSchemaManager.ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                if (arg.Id != null)
                {
                    //Get last ID.
                    GXSerializedItem? s = arg.Id?.ValuePair?.Value;
                    var id = GetLastInsertId(connection, transaction, s.Type, arg.Id?.ValuePair?.Key, arg.Id?.Type);
                    if (arg.Values.Count > 1)
                    {
                        if (Builder.Settings.Type != DatabaseType.MySQL &&
                            Builder.Settings.Type != DatabaseType.MariaDB)
                        {
                            //If SQL database returns the last inserted ID,
                            //so we need to subtract the number of inserted items - 1.
                            id = GXDbHelpers.Add(id, -(arg.Values.Count - 1));
                        }
                    }
                    foreach (var it in arg.Values)
                    {
                        s.Set?.Invoke(it.Key!, id);
                        id = GXDbHelpers.Add(id, 1);
                    }
                }
                if (autoTransaction)
                {
                    transaction?.Commit();
                }
            }
            catch (Exception)
            {
                if (autoTransaction)
                {
                    transaction?.Rollback();
                }
                throw;
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
        public async Task InsertAsync(IDbTransaction? transaction, GXInsertArgs arg)
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
        public async Task InsertAsync(IDbTransaction? transaction,
            GXInsertArgs arg,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Insert(transaction, arg), cancellationToken);
        }

        /// <summary>
        /// Update object.
        /// </summary>
        /// <param name="arg">Update arguments.</param>
        public int Update(GXUpdateArgs arg)
        {
            return Update(null, arg);
        }

        /// <summary>
        /// Update object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="arg">Update arguments.</param>
        public int Update(IDbTransaction? transaction, GXUpdateArgs arg)
        {
            if (arg == null)
            {
                throw new ArgumentException(Properties.Resources.NoDataToUpdate);
            }
            arg.UseQueryCache(QueryCache);
            arg.Settings = Builder.Settings;
            string query = arg.ToString(false);
            if (string.IsNullOrEmpty(query))
            {
                //If there is no data to update, we just return.
                return 0;
            }
            bool autoTransaction = transaction == null && AutoTransaction;
            IDbConnection connection = transaction?.Connection ?? Connection;
            try
            {
                if (autoTransaction)
                {
                    transaction = connection.BeginTransaction();
                }
                int count = GXSchemaManager.ExecuteNonQuery(this, connection, transaction, OnSqlExecuted, query);
                if (autoTransaction)
                {
                    transaction?.Commit();
                }
                return count;
            }
            catch (Exception)
            {
                if (autoTransaction)
                {
                    transaction?.Rollback();
                }
                throw;
            }
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="arg">The update arguments describing the values and row filter.</param>
        public Task<int> UpdateAsync(GXUpdateArgs arg)
        {
            return UpdateAsync(null, arg, CancellationToken.None);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="arg">Update arguments.</param>
        public Task<int> UpdateAsync(IDbTransaction transaction, GXUpdateArgs arg)
        {
            return UpdateAsync(transaction, arg, CancellationToken.None);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="arg">Update arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<int> UpdateAsync(GXUpdateArgs arg, CancellationToken cancellationToken = default)
        {
            return UpdateAsync(null, arg, cancellationToken);
        }

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="arg">Update arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<int> UpdateAsync(IDbTransaction? transaction, GXUpdateArgs arg,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Update(transaction, arg), cancellationToken);
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
        public void Truncate<T>(IDbTransaction? transaction = null)
        {
            //SQLite don't support truncate.
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Delete(GXDeleteArgs.DeleteAll<T>());
            }
            else
            {
                string query = "TRUNCATE TABLE " + Builder.GetTableName(typeof(T), true);
                GXSchemaManager.ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
            }
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction to use.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TruncateAsync<T>(IDbTransaction? transaction = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() =>
            {
                Truncate<T>(transaction);
            }, cancellationToken);
        }

        /// <summary>
        /// Delete ALL items from the table.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <param name="transaction">Transaction to use.</param>
        public void Truncate(IDbTransaction? transaction, string tableName)
        {
            //SQLite don't support truncate.
            if (Builder.Settings.Type == DatabaseType.SqLite)
            {
                Delete(transaction, tableName);
            }
            else
            {
                string query = "TRUNCATE TABLE " + GXDbHelpers.ConvertToString(Builder.Settings, TargetType.Table, null, tableName, null);
                GXSchemaManager.ExecuteNonQuery(this, Connection, transaction, OnSqlExecuted, query);
            }
        }

        /// <summary>
        /// Delete ALL items from the table.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        public void Truncate(string tableName)
        {
            Truncate(null, tableName);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="transaction">Transaction to use.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TruncateAsync(IDbTransaction? transaction, string tableName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() =>
            {
                Truncate(transaction, tableName);
            }, cancellationToken);
        }

        /// <summary>
        /// Delete items from the DB.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task TruncateAsync(string tableName, CancellationToken cancellationToken = default)
        {
            return TruncateAsync(null, tableName, cancellationToken);
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

        private string GetTableName(string table)
        {
            return Builder.Settings.EscapeIdentifier(Builder.Settings.TablePrefix, table);
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
            return ExecuteScalar<int>(query) != 0;
        }

        /// <summary>
        /// Initiates disposal of the underlying database connection.
        /// </summary>
        /// <returns>A completed value task; the underlying asynchronous disposal is initiated but is not awaited by this implementation.</returns>
        public ValueTask DisposeAsync()
        {
            Connection.DisposeAsync();
            return ValueTask.CompletedTask;
        }
    }
}
