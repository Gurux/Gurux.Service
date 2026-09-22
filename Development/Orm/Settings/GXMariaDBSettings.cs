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

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// MariaDB database settings.
    /// </summary>
    internal class GXMariaDBSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMariaDBSettings()
            : base(DatabaseType.MariaDB)
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
            DatabasePermission.Index,
            DatabasePermission.References,
            DatabasePermission.Execute,
            DatabasePermission.CreateView ,
            DatabasePermission.CreateProcedure,
            DatabasePermission.CreateFunction ,
            DatabasePermission.Admin];
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            return string.Compare((string)value, "YES", true) == 0;
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT CONSTRAINT_NAME AS constraint_name, TABLE_NAME AS table_name FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND REFERENCED_TABLE_NAME = '{tableName}' AND REFERENCED_TABLE_NAME IS NOT NULL";
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName)
        {
            return string.Format(@"SELECT k.CONSTRAINT_NAME, k.REFERENCED_TABLE_SCHEMA, k.REFERENCED_TABLE_NAME, k.COLUMN_NAME, k.REFERENCED_COLUMN_NAME, k.ORDINAL_POSITION, rc.DELETE_RULE, rc.UPDATE_RULE
FROM information_schema.KEY_COLUMN_USAGE k
INNER JOIN information_schema.REFERENTIAL_CONSTRAINTS rc ON k.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA AND k.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
WHERE k.TABLE_SCHEMA = '{0}' AND k.TABLE_NAME = '{1}' AND k.REFERENCED_COLUMN_NAME IS NOT NULL
ORDER BY k.CONSTRAINT_NAME, k.ORDINAL_POSITION", schema, tableName);
        }

        /// <inheritdoc />
        public override string GetDescriptionQuery(string schema, string tableName, string columnName)
        {
            schema = schema.Replace("'", "''");
            tableName = tableName.Replace("'", "''");
            if (string.IsNullOrEmpty(columnName))
            {
                return $"SELECT TABLE_COMMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{tableName}'";
            }
            columnName = columnName.Replace("'", "''");
            return $"SELECT COLUMN_COMMENT FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";

        }

        /// <inheritdoc />
        public override string GetOrdinalQuery(string schema, string tableName, string columnName)
        {
            schema = schema.Replace("'", "''");
            tableName = tableName.Replace("'", "''");
            columnName = columnName.Replace("'", "''");
            return $"SELECT ORDINAL_POSITION FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";

        }

        /// <inheritdoc />
        public override string GetCommentQuery(string schema, string tableName, string columnName, string comment)
        {
            string quotedTableName = tableName.Replace("`", "``");
            comment = comment.Replace("'", "''");
            if (string.IsNullOrEmpty(columnName))
            {
                return $"ALTER TABLE `{quotedTableName}` COMMENT = '{comment}'";
            }
            return "";
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT_USER()";
        }

        /// <inheritdoc />
        public override string GetUsersQuery(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT DISTINCT USER FROM mysql.user ORDER BY USER";
            }
            return $@"SELECT DISTINCT GRANTEE
FROM information_schema.SCHEMA_PRIVILEGES
WHERE TABLE_SCHEMA = '{databaseName}'
ORDER BY GRANTEE";
        }


        /// <inheritdoc />
        public override string GetDatabasesQuery(out int index)
        {
            index = 0;
            return @"SELECT SCHEMA_NAME AS database_name FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys')
ORDER BY SCHEMA_NAME";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string? databaseName, string userName)
        {
            if (userName.EndsWith("@%"))
            {
                userName = userName.Substring(0, userName.Length - 2);
            }
            if (string.IsNullOrEmpty(databaseName))
            {
                return $"SHOW GRANTS FOR '{userName}'@'%'";
            }
            return $@"SELECT PRIVILEGE_TYPE FROM INFORMATION_SCHEMA.SCHEMA_PRIVILEGES
WHERE TABLE_SCHEMA = '{databaseName}' AND GRANTEE LIKE '''{userName}''@%'
ORDER BY PRIVILEGE_TYPE";
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            return GXMySqlSettings.GetDatabasePermission(value);
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string? databaseName, string userName)
        {
            if (userName.EndsWith("@%"))
            {
                userName = userName.Substring(0, userName.Length - 2);
            }
            userName = $"'{userName}'@'%'";

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                return $@"REVOKE ALL PRIVILEGES ON `{databaseName}`.* FROM {userName}; 
FLUSH PRIVILEGES;
            ";
            }
            return $@" DROP USER IF EXISTS {userName}; FLUSH PRIVILEGES;";
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            foreach (DatabaseUser user in users)
            {
                string u = user.UserName.Replace("'", "''");
                string p = user.Password.Replace("'", "''");
                queries.Add($"CREATE USER '{u}'@'%' IDENTIFIED BY '{p}';");
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
                queries.Add($"GRANT {GXMySqlSettings.GetPermissions(permissions)} ON `{databaseName}`.* TO '{u}'@'%'");
            }
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string userName = user;
                if (userName.EndsWith("@'%'"))
                {
                    userName = userName.Substring(0, userName.Length - 4);
                }
                if (userName.StartsWith('\'') && userName.EndsWith('\''))
                {
                    userName = userName.Substring(1, userName.Length - 2);
                }
                userName = userName.Replace("'", "''");
                queries.Add($@"REVOKE ALL PRIVILEGES ON `{databaseName}`.* FROM '{userName}'@'%';");
            }
            queries.Add("FLUSH PRIVILEGES;");
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool IsPrimaryKey(object value)
        {
            string str = ((string)value).ToUpper();
            return str.Contains("PRI");
        }

        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            string str = ((string)value);
            return str.Contains("auto_increment");
        }

        /// <inheritdoc />
        public override bool IsUnique(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT EXTRA FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COLUMN_KEY FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }
        /// <inheritdoc />
        public override string GetColumnDefaultValue(object value, Type columnType)
        {
            if (value is DefaultValueKind v)
            {
                return v switch
                {
                    DefaultValueKind.Now when columnType == typeof(DateOnly) => "(CURRENT_DATE())",
                    DefaultValueKind.Now when columnType == typeof(TimeOnly) => "(CURRENT_TIME())",
                    DefaultValueKind.Now => "CURRENT_TIMESTAMP",
                    DefaultValueKind.UtcNow when columnType == typeof(DateOnly) => "(UTC_DATE())",
                    DefaultValueKind.UtcNow when columnType == typeof(TimeOnly) => "(UTC_TIME())",
                    DefaultValueKind.UtcNow => "(UTC_TIMESTAMP())",
                    DefaultValueKind.NewGuid => "(UNHEX(REPLACE(UUID(), '-', '')))",
                    _ => throw new ArgumentOutOfRangeException(nameof(value))
                };
            }
            if (columnType == typeof(bool))
            {
                return "b'" + ConvertToString(value, ConvertOption.None) + "'";
            }
            return ConvertToString(value, ConvertOption.Quete);
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT CASE WHEN COLUMN_TYPE LIKE '% unsigned' THEN CONCAT(DATA_TYPE, ' unsigned') " +
                              "WHEN DATA_TYPE IN ('decimal', 'numeric', 'datetime', 'timestamp', 'time') THEN COLUMN_TYPE " +
                              "ELSE DATA_TYPE END, " +
                              "CHARACTER_MAXIMUM_LENGTH " +
                              "FROM INFORMATION_SCHEMA.COLUMNS " +
                              "WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'",
                              tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SHOW COLUMNS FROM {1}.{0}", name, schema);
        }

        /// <inheritdoc />
        public override string GetRenameTableQuery(string oldTableName, string newTableName)
        {
            return $"RENAME TABLE {oldTableName} TO {newTableName}";
        }

        /// <inheritdoc />
        public override string GetRenameTableColumnQuery(string tableName, string oldColumnName, string newColumnName)
        {
            return $"ALTER TABLE {tableName} RENAME COLUMN {oldColumnName} TO {newColumnName}";
        }

        /// <inheritdoc />
        public override char ColumnNameQuoteCharacter
        {
            get
            {
                return '`';
            }
        }

        /// <inheritdoc/>
        public override int MaximumIndexNameLength
        {
            get
            {
                return 64;
            }
        }

        /// <inheritdoc />
        override public int MaximumRowUpdate
        {
            get
            {
                return 1000;
            }
        }

        /// <inheritdoc />
        override public int TableNameMaximumLength
        {
            get
            {
                return 64;
            }
        }

        /// <inheritdoc />
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 64;
            }
        }

        /// <inheritdoc />
        override public bool AutoIncrementFirst
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc />
        override public string? AutoIncrementDefinition
        {
            get
            {
                return "AUTO_INCREMENT";
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            //char, varchar, text, longtext
            if (maxLength == 0)
            {
                return "TEXT";
            }
            else
            {
                return "VARCHAR(" + maxLength.ToString() + ")";
            }
        }

        /// <inheritdoc />
        override public string CharColumnDefinition
        {
            get
            {
                return "CHAR";
            }
        }

        /// <inheritdoc />
        override public string BoolColumnDefinition
        {
            get
            {
                return "BIT(1)";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                return "BINARY(16)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "DATETIME(3)";
            }
            return "DATETIME(0)";
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
                return "DECIMAL(24,9)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeOffsetColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "DATETIME(3)";
            }
            return "DATETIME(0)";
        }

        /// <inheritdoc />
        override public string ByteColumnDefinition
        {
            get
            {
                return "TINYINT UNSIGNED";
            }
        }

        /// <inheritdoc />
        override public string SByteColumnDefinition
        {
            get
            {
                return "TINYINT";
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
                return "SMALLINT UNSIGNED";
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
                return "INT UNSIGNED";
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
                return "BIGINT UNSIGNED";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "FLOAT";
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
                return "DECIMAL(29,9)";
            }
        }

        /// <inheritdoc/>
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "BLOB";
            }
            //binary, varbinary, blob, longblob
            return "VARBINARY(" + maxLength + ")";
        }

        /// <inheritdoc/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }

        /// <inheritdoc/>
        internal override object ChangeType(object value, Type type)
        {
            if (value is string str)
            {
                if (type == typeof(Guid))
                {
                    //The default value is return as unhex.
                    if (str.StartsWith("unhex('") && str.EndsWith("')"))
                    {
                        str = str.Substring(7, str.Length - 9);
                    }
                    return Guid.Parse(str);
                }
                if (type == typeof(bool))
                {
                    if (str == "b'1'")
                    {
                        //The default value.
                        return true;
                    }
                    if (str == "b'0'")
                    {
                        //The default value.
                        return false;
                    }
                    //The default value is return as unhex.
                    return bool.Parse(str);
                }
                if (type == typeof(DateTime))
                {
                    value = DateTime.Parse(str, CultureInfo.InvariantCulture);
                }
            }
            if (type == typeof(Guid) && value is byte[] bytes)
            {
                return new Guid(bytes, bigEndian: false);
            }
            if (type == typeof(DateTime) && value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }
                if (dt == DateTime.MaxValue)
                {
                    return DateTime.MaxValue;
                }
                return dt;
            }
            if (type == typeof(DateTimeOffset))
            {
                if (value is DateTime dt2)
                {
                    if (dt2 == DateTime.MinValue)
                    {
                        return DateTimeOffset.MinValue;
                    }
                    if (dt2 == DateTime.MaxValue)
                    {
                        return DateTimeOffset.MaxValue;
                    }
                    return new DateTimeOffset(dt2, TimeSpan.Zero).ToLocalTime();
                }
                //MySQL doesn't support time zone so all values are saving using UTC time zone.
                DateTimeOffset utc = (DateTimeOffset)Convert.ChangeType(value, type);
                return utc.ToLocalTime();
            }
            return base.ChangeType(value, type);
        }

        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            if (value is Guid id)
            {
                return "X'" + Convert.ToHexString(id.ToByteArray()) + "'";
            }
            if (value is DateTimeOffset dto)
            {
                DateTime utc = dto.UtcDateTime;
                if (dto == DateTimeOffset.MinValue)
                {
                    utc = DateTime.MinValue;
                }
                if (dto == DateTimeOffset.MaxValue)
                {
                    utc = DateTime.MaxValue;
                }
                //MariaDB doesn't support time zone so all values are saving using UTC time zone.
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(utc.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(utc.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture));
            }
            if (value is DateTime dt)
            {
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(dt.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture));
            }
            if (value is byte[] ba)
            {
                return "X'" + Convert.ToHexString(ba) + "'";
            }
            return base.ConvertToString(value, options);
        }


        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return "SELECT LAST_INSERT_ID() FROM " + tableName;
        }

        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}' AND TABLE_SCHEMA = '{1}'",
                        tableName, schema);
        }

        /// <inheritdoc />
        public override string GetTablesQuery(string schema)
        {
            return string.Format("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'", schema);
        }

        public override string UniqueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_NAME = kcu.TABLE_NAME WHERE tc.CONSTRAINT_TYPE = 'UNIQUE' AND tc.TABLE_SCHEMA = '{0}' AND tc.TABLE_NAME = '{1}' AND kcu.COLUMN_NAME = '{2}'", schema, tableName, columnName);
        }


        /// <inheritdoc />
        public override bool IsIdentity(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string IsIdentityQuery(string schema, string tableName, string columnName)
        {
            return $@"SELECT CASE WHEN EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END
FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}';";
        }

        public override string TableIndexesQuery(string schema, string tableName)
        {
            return $@"SELECT INDEX_NAME, CASE WHEN NON_UNIQUE = 0 THEN 1 ELSE 0 END, COLUMN_NAME, SEQ_IN_INDEX, COLLATION
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = '{schema}'
  AND TABLE_NAME = '{tableName}'
  AND INDEX_NAME <> 'PRIMARY'
ORDER BY INDEX_NAME, SEQ_IN_INDEX";
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
