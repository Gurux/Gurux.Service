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
using Gurux.Service.Orm.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Microsoft SQL database settings.
    /// </summary>
    class GXMSSqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMSSqlSettings() : base(DatabaseType.MSSQL)
        {

        }

        /// <inheritdoc />
        internal override DatabasePermission[] AvailablePermissions()
        {
            return [
            DatabasePermission.Create,
            DatabasePermission.Alter,
            DatabasePermission.Insert,
            DatabasePermission.Update,
            DatabasePermission.Delete,
            DatabasePermission.Select,
            DatabasePermission.References,
            DatabasePermission.CreateView ,
            DatabasePermission.CreateProcedure,
            DatabasePermission.CreateFunction ,
            DatabasePermission.Connect,
            DatabasePermission.Admin];
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT fk.name AS constraint_name, OBJECT_NAME(fk.parent_object_id) AS table_name FROM sys.foreign_keys fk WHERE OBJECT_NAME(fk.referenced_object_id) = '{tableName}'";
        }

        /// <inheritdoc />
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            string str = (string)values[1];
            if (str == "NO_ACTION")
            {
                onDelete = ForeignKeyDelete.None;
            }
            else
            {
                onDelete = (ForeignKeyDelete)Enum.Parse(typeof(ForeignKeyDelete), str, true);
            }
            str = (string)values[2];
            if (str == "NO_ACTION")
            {
                onUpdate = ForeignKeyUpdate.None;
            }
            else
            {
                onUpdate = (ForeignKeyUpdate)Enum.Parse(typeof(ForeignKeyUpdate), str, true);
            }
            return (string)values[0];
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT OBJECT_NAME(f.parent_object_id) AS 'Table name', delete_referential_action_desc AS 'On Delete', update_referential_action_desc AS 'On Update' FROM sys.foreign_keys AS f, sys.foreign_key_columns AS fc, sys.tables t WHERE f.OBJECT_ID = fc.constraint_object_id AND t.OBJECT_ID = fc.referenced_object_id AND f.parent_object_id = OBJECT_ID('{1}') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            return Convert.ToBoolean(value);
        }

        private static string GetPermissions(DatabasePermission value, string user)
        {
            if (value.HasFlag(DatabasePermission.Admin))
            {
                return $"ALTER ROLE db_owner ADD MEMBER [{user}];";
            }
            var list = new List<string>();
            if (value.HasFlag(DatabasePermission.Connect)) list.Add($"GRANT CONNECT TO [{user}];");
            if (value.HasFlag(DatabasePermission.Select)) list.Add($"GRANT SELECT TO [{user}];");
            if (value.HasFlag(DatabasePermission.Insert)) list.Add($"GRANT INSERT TO [{user}];");
            if (value.HasFlag(DatabasePermission.Update)) list.Add($"GRANT UPDATE TO [{user}];");
            if (value.HasFlag(DatabasePermission.Delete)) list.Add($"GRANT DELETE TO [{user}];");
            if (value.HasFlag(DatabasePermission.Execute)) list.Add($"GRANT EXECUTE TO [{user}];");
            if (value.HasFlag(DatabasePermission.Create)) list.Add($"GRANT CREATE TABLE TO [{user}];");
            if (value.HasFlag(DatabasePermission.Alter)) list.Add($"GRANT ALTER TO [{user}];");
            if (value.HasFlag(DatabasePermission.References)) list.Add($"GRANT REFERENCES TO [{user}];");
            if (value.HasFlag(DatabasePermission.CreateView)) list.Add($"GRANT CREATE VIEW TO [{user}];");
            if (value.HasFlag(DatabasePermission.CreateProcedure)) list.Add($"GRANT CREATE PROCEDURE TO [{user}];");
            if (value.HasFlag(DatabasePermission.CreateFunction)) list.Add($"GRANT CREATE FUNCTION TO [{user}];");

            if (!list.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return string.Join(Environment.NewLine, list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT_USER;";
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT name FROM sys.server_principals WHERE type IN ('S', 'U', 'G') ORDER BY name";
            }
            string query = @"SELECT name FROM sys.database_principals WHERE type IN ('S', 'U', 'G') AND principal_id > 4 ORDER BY name";
            return $"USE [{databaseName}]; " + query;
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery()
        {
            return @"SELECT name AS database_name FROM sys.databases WHERE database_id > 4 ORDER BY name";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string databaseName, string userName)
        {
            userName = userName.Replace("'", "''");
            if (string.IsNullOrEmpty(databaseName))
            {
                return $@"WITH UserPrincipal AS
(
    SELECT principal_id
    FROM sys.server_principals
    WHERE name = N'{userName}'
        OR sid = SUSER_SID(N'{userName}')
        OR (N'{userName}' = CURRENT_USER AND sid = SUSER_SID())
),
UserRoles AS
(
    SELECT rm.role_principal_id
    FROM sys.server_role_members rm
    INNER JOIN UserPrincipal up
        ON rm.member_principal_id = up.principal_id
)
SELECT DISTINCT role.name COLLATE DATABASE_DEFAULT AS permission_name
FROM UserRoles ur
INNER JOIN sys.server_principals role
    ON ur.role_principal_id = role.principal_id
UNION
SELECT DISTINCT permission_name COLLATE DATABASE_DEFAULT AS permission_name
FROM sys.server_permissions
WHERE state_desc IN ('GRANT', 'GRANT_WITH_GRANT_OPTION')
    AND grantee_principal_id IN
    (
        SELECT principal_id FROM UserPrincipal
        UNION
        SELECT role_principal_id FROM UserRoles
    )
ORDER BY permission_name";
            }
            return $@"WITH UserPrincipal AS
(
    SELECT principal_id
    FROM sys.database_principals
    WHERE name = N'{userName}'
),
UserRoles AS
(
    SELECT rm.role_principal_id
    FROM sys.database_role_members rm
    INNER JOIN UserPrincipal up
        ON rm.member_principal_id = up.principal_id
)
SELECT DISTINCT role.name COLLATE DATABASE_DEFAULT AS permission_name
FROM UserRoles ur
INNER JOIN sys.database_principals role
    ON ur.role_principal_id = role.principal_id
UNION
SELECT DISTINCT permission_name COLLATE DATABASE_DEFAULT AS permission_name
FROM sys.database_permissions
WHERE state_desc IN ('GRANT', 'GRANT_WITH_GRANT_OPTION')
    AND grantee_principal_id IN
    (
        SELECT principal_id FROM UserPrincipal
        UNION
        SELECT role_principal_id FROM UserRoles
    )
ORDER BY permission_name";
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DatabasePermission.None;
            }
            value = value
                .Trim()
                .Replace("_", " ")
                .Replace("-", " ")
                .ToUpperInvariant();
            return value switch
            {
                // Generic admin roles / privileges
                "ADMIN" or
                "DBA" or
                "DBADM" or
                "SYSADMIN" or
                "SERVERADMIN" or
                "SECURITYADMIN" or
                "SETUPADMIN" or
                "CONTROL SERVER" or
                "DB OWNER" or
                "DBOWNER" or
                "DB OWNER ROLE" or
                "ALL" or
                "ALL PRIVILEGES" or
                "CONTROL" =>
                    DatabasePermission.Admin,
                "DB DATAREADER" =>
                    DatabasePermission.Select,
                "DB DATAWRITER" =>
                    DatabasePermission.Insert |
                    DatabasePermission.Update |
                    DatabasePermission.Delete,

                "DB DDLADMIN" =>
                    DatabasePermission.Create |
                    DatabasePermission.Alter |
                    DatabasePermission.Drop |
                    DatabasePermission.Index |
                    DatabasePermission.References,

                "DB SECURITYADMIN" =>
                    DatabasePermission.Admin,

                "DB ACCESSADMIN" =>
                    DatabasePermission.Admin,
                "DBCREATOR" =>
                    DatabasePermission.Create |
                    DatabasePermission.Drop,
                "DB BACKUPOPERATOR" =>
                    DatabasePermission.None,
                "DB DENYDATAREADER" =>
                    DatabasePermission.None,
                "DB DENYDATAWRITER" =>
                    DatabasePermission.None,
                // Common SQL permissions
                "CREATE" or
                "CREATE TABLE" or
                "CREATE ANY" or
                "CREATETAB" =>
                    DatabasePermission.Create,
                "CREATE VIEW" =>
                    DatabasePermission.CreateView,
                "CREATE FUNCTION" =>
                    DatabasePermission.CreateFunction,
                "CREATE PROCEDURE" =>
                    DatabasePermission.CreateProcedure,
                "ALTER" or
                "ALTER ANY" =>
                    DatabasePermission.Alter,
                "DROP" =>
                    DatabasePermission.Drop,
                "INSERT" =>
                    DatabasePermission.Insert,
                "UPDATE" =>
                    DatabasePermission.Update,
                "DELETE" =>
                    DatabasePermission.Delete,
                "SELECT" or
                "READ" =>
                    DatabasePermission.Select,
                "INDEX" or
                "CREATE INDEX" =>
                    DatabasePermission.Index,
                "REFERENCES" =>
                    DatabasePermission.References,
                "EXECUTE" or
                "EXEC" => DatabasePermission.Execute,
                // Login/connect-only permissions
                "CONNECT" => DatabasePermission.Connect,
                "CREATE SESSION" or
                "USAGE" =>
                    DatabasePermission.None,
                _ => DatabasePermission.None
            };
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string databaseName, string userName)
        {
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                string db = "[" + databaseName + "]";
                return $@" USE {db};
            IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{userName}')
            BEGIN
                DROP USER [{userName}];
            END
            ";
            }
            return $@"IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{userName}')
        BEGIN
            DROP LOGIN [{userName}];
        END
        ";
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            foreach (DatabaseUser user in users)
            {
                string u = user.UserName.Replace("'", "''");
                string p = user.Password.Replace("'", "''");
                queries.Add($@"USE master;
IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = '{u}')
    CREATE LOGIN [{user.UserName}] WITH PASSWORD = '{p}';");
            }
        }

        /// <inheritdoc />
        public override void AddUsersToDatabaseQuery(List<string> queries,
            string databaseName,
            DatabasePermission permissions,
            params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string u = user.Replace("'", "''");
                queries.Add($@"USE [{databaseName}]; CREATE USER [{user}] FOR LOGIN [{user}];");
                queries.Add(GetPermissions(permissions, user));
            }
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string u = user.Replace("'", "''");
                queries.Add($@"USE [{databaseName}];
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{u}')
BEGIN
    DROP USER [{user}];
END");
            }
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT is_nullable FROM sys.columns WHERE object_id = object_id('{0}') AND name = '{1}'", tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }
        /// <inheritdoc />
        public override bool IsPrimaryKey(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT is_identity FROM sys.columns WHERE object_id = object_id('{0}.{1}') AND name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT OBJECT_NAME(f.referenced_object_id)FROM sys.foreign_keys AS f, sys.foreign_key_columns AS fc, sys.tables t WHERE f.OBJECT_ID = fc.constraint_object_id AND t.OBJECT_ID = fc.referenced_object_id AND f.parent_object_id = OBJECT_ID('{1}') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 AND TABLE_CATALOG = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT object_definition(default_object_id) AS definition FROM sys.columns WHERE object_id = object_id('{0}.{1}') AND name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", name);
        }

        /// <inheritdoc />
        override public char ColumnNameQuoteCharacter
        {
            get
            {
                return '[';
            }
        }

        /// <inheritdoc/>
        public override int MaximumIndexNameLength
        {
            get
            {
                return 128;
            }
        }

        /// <inheritdoc/>
        public override bool SelectUsingAs
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override char TableNameQuoteCharacter
        {
            get
            {
                return '[';
            }
        }

        /// <inheritdoc />
        override public int MaximumRowUpdate
        {
            get
            {
                return 999;
            }
        }

        /// <inheritdoc />
        override public int TableNameMaximumLength
        {
            get
            {
                return 128;
            }
        }

        /// <inheritdoc />
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 128;
            }
        }

        /// <inheritdoc />
        override public bool AutoIncrementFirst
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        internal override LimitType LimitType
        {
            get
            {
                return LimitType.Fetch;
            }
        }

        /// <inheritdoc />
        override public string AutoIncrementDefinition
        {
            get
            {
                return "IDENTITY(1,1)";
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "NVARCHAR(MAX)";
            }
            return "NVARCHAR(" + maxLength.ToString() + ")";
        }

        /// <inheritdoc />
        override public string CharColumnDefinition
        {
            get
            {
                return "CHAR(1)";
            }
        }

        /// <inheritdoc />
        override public string BoolColumnDefinition
        {
            get
            {
                return "BIT";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                return "UNIQUEIDENTIFIER";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "DATETIME2(3)";
            }
            return "DATETIME2(0)";
        }

        /// <inheritdoc />
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return "VARCHAR(30)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeOffsetColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "DATETIMEOFFSET(3)";
            }
            return "DATETIMEOFFSET(0)";
        }

        /// <inheritdoc />
        override public string ByteColumnDefinition
        {
            get
            {
                return "TINYINT";
            }
        }

        /// <inheritdoc />
        override public string SByteColumnDefinition
        {
            get
            {
                return "SMALLINT";
            }
        }

        /// <inheritdoc />
        override public string ShortColumnDefinition
        {
            get
            {
                return "SMALLINT";
            }
        }

        /// <inheritdoc />
        override public string UShortColumnDefinition
        {
            get
            {
                return "INT";
            }
        }

        /// <inheritdoc />
        override public string IntColumnDefinition
        {
            get
            {
                return "INT";
            }
        }

        /// <inheritdoc />
        override public string UIntColumnDefinition
        {
            get
            {
                return "BIGINT";
            }
        }

        /// <inheritdoc />
        override public string LongColumnDefinition
        {
            get
            {
                return "BIGINT";
            }
        }

        /// <inheritdoc />
        override public string ULongColumnDefinition
        {
            get
            {
                return "NUMERIC(20,0)";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "FLOAT(53)";
            }
        }

        /// <inheritdoc/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "FLOAT(53)";
            }
        }

        /// <inheritdoc/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "FLOAT(23)";
            }
        }

        /// <inheritdoc/>
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "VARBINARY(MAX)";
            }
            return "VARBINARY(" + maxLength + ")";
        }

        /// <inheritdoc/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "VARCHAR(max)";
            }
        }

        /// <summary>
        /// ISO 8601 format with milliseconds. 
        /// </summary>
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return GetQuetedValue("17530101");
                }
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(dt.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture));
            }
            if (value is DateTimeOffset dto)
            {
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dto.ToString(DateTimeFormat + "zzz", CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(dto.ToString(DateTimeFormat + ".fffzzz", CultureInfo.InvariantCulture));
            }
            if (value is byte[] ba)
            {
                return "0x" + Convert.ToHexString(ba);
            }
            return base.ConvertToString(value, options);
        }

        /// <summary>
        /// Change value type. 
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="type">Target type.</param>
        /// <returns>Converted value.</returns>
        internal override object ChangeType(object value, Type type)
        {
            return base.ChangeType(value, type);
        }
        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return "SELECT @@IDENTITY FROM " + tableName;
        }

        /// <inheritdoc />
        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", tableName);
        }

        /// <inheritdoc />
        public override string GetTables(string schema)
        {
            return $@"
SELECT t.name
FROM sys.tables t
JOIN sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = '{schema}'
  AND t.is_ms_shipped = 0
ORDER BY t.name";
            // return "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'";
        }

        /// <inheritdoc />
        public override string DataQuotaReplacement
        {
            get
            {
                return "''";
            }
        }
    }
}
