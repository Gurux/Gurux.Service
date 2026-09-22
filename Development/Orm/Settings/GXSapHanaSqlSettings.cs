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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// SAP HANA SQL database settings.
    /// </summary>
    class GXSapHanaSqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSapHanaSqlSettings() : base(DatabaseType.SapHana)
        {

        }

        /// <inheritdoc />
        internal override DatabasePermission[] AvailablePermissions()
        {
            return [
            DatabasePermission.Create,
            DatabasePermission.Alter,
            DatabasePermission.Drop,
            DatabasePermission.Insert,
            DatabasePermission.Update,
            DatabasePermission.Delete,
            DatabasePermission.Select,
            DatabasePermission.Execute,
            DatabasePermission.Admin];
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT CONSTRAINT_NAME AS constraint_name, TABLE_NAME AS table_name
FROM SYS.REFERENTIAL_CONSTRAINTS
WHERE REFERENCED_TABLE_NAME = '{tableName}'
  AND SCHEMA_NAME = CURRENT_SCHEMA";
        }

        private static string GetTableFilter(string tableName, string? tableAlias = null)
        {
            tableName = tableName.Replace("'", "''").ToUpperInvariant();
            string tableColumn = string.IsNullOrEmpty(tableAlias)
                ? "TABLE_NAME"
                : tableAlias + ".TABLE_NAME";
            string schemaColumn = string.IsNullOrEmpty(tableAlias)
                ? "SCHEMA_NAME"
                : tableAlias + ".SCHEMA_NAME";
            return $@"UPPER({tableColumn}) = '{tableName}'
  AND
  (
      {schemaColumn} = CURRENT_SCHEMA
      OR
      (
          NOT EXISTS (
              SELECT 1
              FROM TABLES
              WHERE SCHEMA_NAME = CURRENT_SCHEMA
                AND UPPER(TABLE_NAME) = '{tableName}'
          )
          AND {schemaColumn} NOT LIKE '_SYS%'
          AND {schemaColumn} NOT IN ('PUBLIC', 'SYS', 'SYSTEM')
      )
  )";
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName)
        {
            return string.Format(@"SELECT rc.CONSTRAINT_NAME, rc.REFERENCED_SCHEMA_NAME, rc.REFERENCED_TABLE_NAME, rc.COLUMN_NAME, rc.REFERENCED_COLUMN_NAME, rc.POSITION, rc.DELETE_RULE, rc.UPDATE_RULE
FROM SYS.REFERENTIAL_CONSTRAINTS rc
WHERE {1}
ORDER BY rc.CONSTRAINT_NAME, rc.POSITION", schema, GetTableFilter(tableName, "rc"));
        }

        /// <inheritdoc />
        public override string GetDescriptionQuery(
            string schema,
            string tableName,
            string columnName)
        {
            schema = schema.Replace("'", "''");
            if (tableName.StartsWith("'"))
            {
                tableName = tableName.Replace("'", "''");
            }
            else
            {
                tableName = tableName.ToUpperInvariant();
            }
            if (string.IsNullOrEmpty(columnName))
            {
                return
                    $"SELECT COMMENTS " +
                    $"FROM SYS.TABLES " +
                    $"WHERE {GetTableFilter(tableName)}";
            }
            columnName = columnName.Replace("'", "''");
            return
                $"SELECT COMMENTS " +
                $"FROM SYS.TABLE_COLUMNS " +
                $"WHERE {GetTableFilter(tableName)} " +
                $"AND UPPER(COLUMN_NAME) = '{columnName.ToUpperInvariant()}'";
        }

        /// <inheritdoc />
        public override string GetOrdinalQuery(string schema, string tableName, string columnName)
        {
            schema = "CURRENT_SCHEMA";
            if (tableName.StartsWith("'"))
            {
                tableName = tableName.Replace("'", "''");
            }
            else
            {
                tableName = tableName.ToUpperInvariant();
            }
            columnName = columnName.Replace("'", "''");
            return
                $"SELECT POSITION " +
                $"FROM SYS.TABLE_COLUMNS " +
                $"WHERE {GetTableFilter(tableName)} " +
                $"AND UPPER(COLUMN_NAME) = '{columnName.ToUpperInvariant()}'";
        }

        /// <inheritdoc />
        public override string GetCommentQuery(string schema, string tableName, string columnName, string comment)
        {
            tableName = tableName.ToUpperInvariant();
            comment = comment.Replace("'", "''");
            if (string.IsNullOrEmpty(columnName))
            {
                return $"COMMENT ON TABLE {tableName} IS '{comment}'";
            }
            return $"COMMENT ON COLUMN {tableName}.{columnName} IS '{comment}'";
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            return string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), "TRUE", true) == 0 ||
                string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), "YES", true) == 0;
        }

        private static string GetPermissions(DatabasePermission value)
        {
            if (value.HasFlag(DatabasePermission.Admin))
                return "SELECT, INSERT, UPDATE, DELETE, CREATE ANY, DROP, ALTER, EXECUTE";

            var list = new List<string>();

            if (value.HasFlag(DatabasePermission.Select)) list.Add("SELECT");
            if (value.HasFlag(DatabasePermission.Insert)) list.Add("INSERT");
            if (value.HasFlag(DatabasePermission.Update)) list.Add("UPDATE");
            if (value.HasFlag(DatabasePermission.Delete)) list.Add("DELETE");
            if (value.HasFlag(DatabasePermission.Execute)) list.Add("EXECUTE");
            if (value.HasFlag(DatabasePermission.Create)) list.Add("CREATE ANY");
            if (value.HasFlag(DatabasePermission.Alter)) list.Add("ALTER");
            if (value.HasFlag(DatabasePermission.Drop)) list.Add("DROP");
            if (!list.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return list.Count == 0 ? "SELECT" : string.Join(", ", list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT_USER FROM DUMMY";
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT USER_NAME FROM USERS WHERE USER_NAME <> 'SYSTEM'
AND USER_NAME <> 'SYS'
AND USER_NAME NOT LIKE '_SYS\_%' ESCAPE '\'ORDER BY USER_NAME";
            }
            return $@"SELECT DISTINCT GRANTEE
FROM SYS.GRANTED_PRIVILEGES
WHERE OBJECT_TYPE = 'SCHEMA'
AND SCHEMA_NAME = '{databaseName}'
AND GRANTEE <> 'SYSTEM'
AND GRANTEE <> 'SYS'
AND GRANTEE NOT LIKE '_SYS\_%' ESCAPE '\'
ORDER BY GRANTEE;";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery(out int index)
        {
            index = 0;
            //SAP Hana uses schemas as databases.
            //The following query returns all schemas that are not system schemas.
            return @"SELECT SCHEMA_NAME
FROM SYS.SCHEMAS
WHERE SCHEMA_NAME NOT LIKE '_SYS%'
  AND SCHEMA_NAME NOT IN
  (
      'PUBLIC',
      'SYS',
      'SYSTEM'
  )
ORDER BY SCHEMA_NAME";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string? databaseName, string userName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return $@"SELECT privilege, object_type, schema_name, object_name
                    FROM effective_privileges WHERE user_name = '{userName}'";
            }
            return $@"SELECT privilege, object_type, schema_name, object_name
FROM effective_privileges WHERE user_name = '{userName}' AND schema_name = '{databaseName}'";
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            if (value.Contains("ALL PRIVILEGES"))
            {
                return DatabasePermission.Admin;
            }
            DatabasePermission permissions = DatabasePermission.None;
            if (value.Contains("SELECT"))
            {
                permissions |= DatabasePermission.Select;
            }
            if (value.Contains("INSERT"))
            {
                permissions |= DatabasePermission.Insert;
            }
            if (value.Contains("UPDATE"))
            {
                permissions |= DatabasePermission.Update;
            }
            if (value.Contains("DELETE"))
            {
                permissions |= DatabasePermission.Delete;
            }
            if (value.Contains("CREATE"))
            {
                permissions |= DatabasePermission.Create;
            }
            if (value.Contains("DROP"))
            {
                permissions |= DatabasePermission.Drop;
            }
            if (value.Contains("REFERENCES"))
            {
                permissions |= DatabasePermission.References;
            }
            if (value.Contains("EXECUTE"))
            {
                permissions |= DatabasePermission.Execute;
            }
            if (value.Contains("ALTER"))
            {
                permissions |= DatabasePermission.Alter;
            }
            return permissions;
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string? databaseName, string userName)
        {
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = "\"" + databaseName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
                return $"REVOKE SELECT, INSERT, UPDATE, DELETE ON SCHEMA {databaseName} FROM {userName}";
            }
            return $"DROP USER {userName} CASCADE";
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            foreach (DatabaseUser user in users)
            {
                string userName = user.UserName.ToUpper();
                queries.Add($@"CREATE USER {userName} PASSWORD ""{user.Password}""");
            }
        }

        /// <inheritdoc />
        public override void AddUsersToDatabaseQuery(List<string> queries,
            string databaseName,
            DatabasePermission permissions, IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string userName = user.ToUpper();
                queries.Add($@"GRANT {GetPermissions(permissions)} ON SCHEMA ""{databaseName}"" TO ""{userName}"";");
            }
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            string schema = "\"" + databaseName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
            foreach (string user in users)
            {
                string userName = "\"" + user.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
                queries.Add($@"REVOKE SELECT, INSERT, UPDATE, DELETE, EXECUTE, CREATE ANY, ALTER, DROP ON SCHEMA {schema} FROM {userName};");
            }
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT IS_NULLABLE FROM TABLE_COLUMNS WHERE {1} AND UPPER(COLUMN_NAME) = '{2}'", schema, GetTableFilter(tableName), columnName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }
        /// <inheritdoc />
        public override bool IsPrimaryKey(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
        }

        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override bool IsUnique(object value)
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            schema = schema.Replace("'", "''");
            tableName = tableName.Replace("'", "''");
            columnName = columnName.Replace("'", "''");
            return
                $"SELECT CASE " +
                $"WHEN GENERATION_TYPE IN ('ALWAYS AS IDENTITY', 'BY DEFAULT AS IDENTITY') THEN 1 " +
                $"ELSE 0 END " +
                $"FROM SYS.TABLE_COLUMNS " +
                $"WHERE {GetTableFilter(tableName)} " +
                $"AND UPPER(COLUMN_NAME) = '{columnName.ToUpperInvariant()}'";
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM CONSTRAINTS WHERE IS_PRIMARY_KEY = 'TRUE' AND {1} AND UPPER(COLUMN_NAME) = '{2}'", schema, GetTableFilter(tableName), columnName.ToUpperInvariant());
        }

        /// <inheritdoc/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT DEFAULT_VALUE FROM TABLE_COLUMNS WHERE {1} AND UPPER(COLUMN_NAME) = '{2}'", schema, GetTableFilter(tableName), columnName.ToUpperInvariant());
        }
        /// <inheritdoc />
        public override string GetColumnDefaultValue(object value, Type columnType)
        {
            if (value is DefaultValueKind v)
            {
                return v switch
                {
                    DefaultValueKind.Now when columnType == typeof(DateOnly) =>
                        "CURRENT_DATE",

                    DefaultValueKind.Now when columnType == typeof(TimeOnly) =>
                        "CURRENT_TIME",

                    DefaultValueKind.Now when columnType == typeof(DateTimeOffset) =>
                        "CURRENT_TIMESTAMP",

                    DefaultValueKind.Now =>
                        "CURRENT_TIMESTAMP",

                    DefaultValueKind.UtcNow when columnType == typeof(DateOnly) =>
                        "CURRENT_UTCDATE",

                    DefaultValueKind.UtcNow when columnType == typeof(TimeOnly) => "CURRENT_UTCTIME",
                    DefaultValueKind.UtcNow => "CURRENT_UTCTIMESTAMP",
                    DefaultValueKind.NewGuid => "SYSUUID",
                    _ => throw new ArgumentOutOfRangeException(nameof(value))
                };
            }
            if (columnType == typeof(bool))
            {
                return ConvertToString(value, ConvertOption.None);
            }
            return ConvertToString(value, ConvertOption.Quete);
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT DATA_TYPE_NAME, LENGTH, SCALE FROM TABLE_COLUMNS WHERE {1} AND UPPER(COLUMN_NAME) = '{2}'", schema, GetTableFilter(tableName), columnName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format(@"SELECT COLUMN_NAME
FROM TABLE_COLUMNS
WHERE {0}
ORDER BY POSITION", GetTableFilter(name));
        }

        /// <inheritdoc />
        public override string GetRenameTableQuery(string oldTableName, string newTableName)
        {
            return $"RENAME TABLE {oldTableName} TO {newTableName}";
        }

        /// <inheritdoc />
        public override string GetRenameTableColumnQuery(string tableName, string oldColumnName, string newColumnName)
        {
            return $"RENAME COLUMN {tableName}.{oldColumnName} TO {newColumnName}";
        }

        /// <inheritdoc />
        override public char ColumnNameQuoteCharacter
        {
            get
            {
                return '"';
            }
        }

        /// <inheritdoc/>
        public override int MaximumIndexNameLength
        {
            get
            {
                return 127;
            }
        }

        /// <inheritdoc/>
        public override char TableNameQuoteCharacter
        {
            get
            {
                return '"';
            }
        }

        /// <inheritdoc />
        override public int MaximumRowUpdate
        {
            get
            {
                return 500;
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
        override public string? AutoIncrementDefinition
        {
            get
            {
                return "GENERATED BY DEFAULT AS IDENTITY";
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "NCLOB";
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
                return "BOOLEAN";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                return "VARBINARY(16)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "TIMESTAMP";
            }
            return "TIMESTAMP";
        }

        /// <inheritdoc />
        override public string DateOnlyColumnDefinition
        {
            get
            {
                return "DATE";
            }
        }

        /// <inheritdoc />
        override public string TimeOnlyColumnDefinition
        {
            get
            {
                return "TIME";
            }
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
                return "TIMESTAMP";
            }
            return "TIMESTAMP";
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
                return "DECIMAL(20,0)";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "REAL";
            }
        }

        /// <inheritdoc/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "DOUBLE";
            }
        }

        /// <inheritdoc/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "DECIMAL(29,10)";
            }
        }

        /// <inheritdoc/>
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "BLOB";
            }
            return "VARBINARY(" + maxLength + ")";
        }

        /// <inheritdoc/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "NCLOB";
            }
        }

        /// <summary>
        /// ISO 8601 format with milliseconds. 
        /// </summary>
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return "TO_TIMESTAMP(" + GetQuetedValue("0001-01-01 00:00:00") + ")";
                }
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return "TO_TIMESTAMP(" + GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture)) + ")";
                }
                return "TO_TIMESTAMP(" + GetQuetedValue(dt.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture)) + ")";
            }
            if (value is DateTimeOffset dto)
            {
                DateTime utc = dto.UtcDateTime;
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return "TO_TIMESTAMP(" + GetQuetedValue(utc.ToString(DateTimeFormat, CultureInfo.InvariantCulture)) + ")";
                }
                return "TO_TIMESTAMP(" + GetQuetedValue(utc.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture)) + ")";
            }
            if (value is Guid guid)
            {
                return "X'" + Convert.ToHexString(guid.ToByteArray()) + "'";
            }
            if (value is byte[] ba)
            {
                return "X'" + Convert.ToHexString(ba) + "'";
            }
            if (value is bool b)
            {
                return b ? "TRUE" : "FALSE";
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
            if (type == typeof(DateTimeOffset) && value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return DateTimeOffset.MinValue;
                }
                if (dt == DateTime.MaxValue)
                {
                    return DateTimeOffset.MaxValue;
                }
                return new DateTimeOffset(dt, TimeSpan.Zero).ToLocalTime();
            }
            if (type == typeof(Guid))
            {
                if (value is byte[] b)
                {
                    return new Guid(b);
                }
                if (value is string s)
                {
                    return new Guid(s);
                }
            }
            if (type == typeof(DateTime) && value is string str)
            {
                if (str == "CURRENT_UTCTIMESTAMP")
                {
                    return DefaultValueKind.UtcNow;
                }
            }
            return base.ChangeType(value, type);
        }
        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return "SELECT CURRENT_IDENTITY_VALUE() FROM DUMMY";
        }

        /// <inheritdoc />
        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM TABLES WHERE SCHEMA_NAME = CURRENT_SCHEMA AND TABLE_NAME = '{0}'", tableName);
        }

        /// <inheritdoc />
        public override string GetTablesQuery(string schema)
        {
            return "SELECT TABLE_NAME FROM TABLES WHERE SCHEMA_NAME = CURRENT_SCHEMA";
        }

        public override string UniqueQuery(string schema, string tableName, string columnName)
        {
            return $@"SELECT COUNT(1)
FROM SYS.CONSTRAINTS
WHERE {GetTableFilter(tableName)}
  AND UPPER(COLUMN_NAME) = '{columnName.ToUpperInvariant()}'
  AND IS_UNIQUE_KEY = 'TRUE' AND IS_PRIMARY_KEY = 'FALSE'";
        }


        /// <inheritdoc />
        public override bool IsIdentity(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string IsIdentityQuery(string schema, string tableName, string columnName)
        {
            return $@"SELECT CASE 
    WHEN GENERATION_TYPE IN ('ALWAYS AS IDENTITY', 'BY DEFAULT AS IDENTITY') 
    THEN 1 ELSE 0 END
FROM SYS.TABLE_COLUMNS WHERE SCHEMA_NAME = CURRENT_SCHEMA AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}';";
        }

        public override string TableIndexesQuery(string schema, string tableName)
        {
            return $@"SELECT INDEX_NAME, CASE WHEN ""CONSTRAINT"" IN ('UNIQUE', 'NOT_NULL_UNIQUE') THEN 1 ELSE 0 END, COLUMN_NAME, POSITION,
CASE WHEN ASCENDING_ORDER = 'FALSE' THEN 'DESC' ELSE 'ASC' END
FROM SYS.INDEX_COLUMNS
WHERE SCHEMA_NAME = CURRENT_SCHEMA
  AND TABLE_NAME = UPPER('{tableName}')
  AND (""CONSTRAINT"" IS NULL OR ""CONSTRAINT"" NOT IN ('PRIMARY_KEY', 'PRIMARY KEY'))
ORDER BY INDEX_NAME, POSITION";
        }

        public override void UpdateTableIndexes(GXTableSchema schema, IEnumerable<IEnumerable<object>> value)
        {
            UpdateTableIndexesFromRows(schema, value);
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
