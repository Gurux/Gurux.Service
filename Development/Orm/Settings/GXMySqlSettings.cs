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

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// MySQL database settings.
    /// </summary>
    internal class GXMySqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMySqlSettings()
            : base(DatabaseType.MySQL)
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
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            onDelete = (ForeignKeyDelete)Enum.Parse(typeof(ForeignKeyDelete), (string)values[2], true);
            onUpdate = (ForeignKeyUpdate)Enum.Parse(typeof(ForeignKeyUpdate), (string)values[1], true);
            return (string)values[0];
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT tb1.REFERENCED_TABLE_NAME, tb2.UPDATE_RULE, tb2.DELETE_RULE FROM information_schema.KEY_COLUMN_USAGE AS tb1 INNER JOIN information_schema.REFERENTIAL_CONSTRAINTS AS tb2 ON tb1.CONSTRAINT_NAME = tb2.CONSTRAINT_NAME WHERE table_schema = '{0}' AND tb1.table_name = '{1}' AND COLUMN_NAME = '{2}' AND referenced_column_name IS NOT NULL", schema, tableName, columnName);
        }

        internal static string GetPermissions(DatabasePermission value)
        {
            if (value.HasFlag(DatabasePermission.Admin))
            {
                return "ALL PRIVILEGES";
            }
            var list = new List<string>();
            if (value.HasFlag(DatabasePermission.Connect)) list.Add("CONNECT");
            if (value.HasFlag(DatabasePermission.Create)) list.Add("CREATE");
            if (value.HasFlag(DatabasePermission.Alter)) list.Add("ALTER");
            if (value.HasFlag(DatabasePermission.Drop)) list.Add("DROP");
            if (value.HasFlag(DatabasePermission.Insert)) list.Add("INSERT");
            if (value.HasFlag(DatabasePermission.Update)) list.Add("UPDATE");
            if (value.HasFlag(DatabasePermission.Delete)) list.Add("DELETE");
            if (value.HasFlag(DatabasePermission.Select)) list.Add("SELECT");
            if (value.HasFlag(DatabasePermission.Index)) list.Add("INDEX");
            if (value.HasFlag(DatabasePermission.References)) list.Add("REFERENCES");
            if (value.HasFlag(DatabasePermission.Execute)) list.Add("EXECUTE");
            if (value.HasFlag(DatabasePermission.CreateView)) list.Add("CREATE VIEW");
            if (value.HasFlag(DatabasePermission.CreateProcedure)) list.Add("CREATE ROUTINE");
            if (value.HasFlag(DatabasePermission.CreateFunction)) list.Add("CREATE ROUTINE");

            if (!list.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return string.Join(", ", list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT_USER()";
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT DISTINCT USER FROM mysql.user ORDER BY USER";
            }
            return @"SELECT DISTINCT GRANTEE
FROM information_schema.SCHEMA_PRIVILEGES
WHERE TABLE_SCHEMA = '{databaseName}'
ORDER BY GRANTEE";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery()
        {
            return @"SELECT SCHEMA_NAME AS database_name FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys')
ORDER BY SCHEMA_NAME";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string database, string userName)
        {
            if (userName.EndsWith("@%"))
            {
                userName = userName.Substring(0, userName.Length - 2);
            }
            return $"SHOW GRANTS FOR '{userName}'@'%'";
        }

        internal static DatabasePermission GetDatabasePermission(string value)
        {
            if (value.Contains("ALL PRIVILEGES") || value.Contains("RESOURCE_GROUP_ADMIN"))
            {
                return DatabasePermission.Admin;
            }
            if (value.Contains("GRANT USAGE ON"))
            {
                return DatabasePermission.None;
            }
            if (value == "EVENT" ||
                value == "LOCK TABLES" ||
                value == "SHOW VIEW" ||
                value == "TRIGGER")
            {
                //MariaDB.
                return DatabasePermission.None;
            }
            DatabasePermission permissions = DatabasePermission.None;
            if (value.Contains("EXECUTE"))
            {
                permissions |= DatabasePermission.Execute;
            }
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
                if (value.Contains("CREATE VIEW"))
                {
                    permissions |= DatabasePermission.CreateView;
                }
                else if (value.Contains("CREATE ROUTINE"))
                {
                    permissions |= DatabasePermission.CreateProcedure | DatabasePermission.CreateFunction;
                }
                else
                {
                    permissions |= DatabasePermission.Create;
                }

            }
            if (value.Contains("DROP"))
            {
                permissions |= DatabasePermission.Drop;
            }
            if (value.Contains("ALTER"))
            {
                permissions |= DatabasePermission.Alter;
            }
            if (value.Contains("REFERENCES"))
            {
                permissions |= DatabasePermission.References;
            }
            if (value.Contains("INDEX"))
            {
                permissions |= DatabasePermission.Index;
            }
            if (permissions == DatabasePermission.None)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return permissions;
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            return GetDatabasePermission(value);
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string databaseName, string userName)
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
                queries.Add($"CREATE USER IF NOT EXISTS '{u}'@'%' IDENTIFIED BY '{p}';");
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
                queries.Add($"GRANT {GetPermissions(permissions)} ON `{databaseName}`.* TO '{u}'@'%';");
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
                if (userName.EndsWith("@%"))
                {
                    userName = userName.Substring(0, userName.Length - 2);
                }
                userName = userName.Replace("'", "''");
                queries.Add($@"REVOKE ALL PRIVILEGES ON `{databaseName}`.* FROM '{userName}'@'%'");
            }
            queries.Add("FLUSH PRIVILEGES");
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
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT EXTRA FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT REFERENCED_TABLE_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
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
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}' AND COLUMN_NAME = '{1}' AND TABLE_SCHEMA = '{2}'", tableName, columnName, schema);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SHOW COLUMNS FROM {1}.{0}", name, schema);
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
        override public string AutoIncrementDefinition
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
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return DoubleColumnDefinition;
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
                return "FLOAT(53)";
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
                return "DOUBLE";
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

        public static Guid MySqlUuidToGuid(byte[] bytes)
        {
            byte[] guid =
            [
                bytes[7],
                bytes[6],
                bytes[5],
                bytes[4],
                bytes[3], bytes[2],
                bytes[1], bytes[0],
                bytes[8], bytes[9], bytes[10], bytes[11],
                bytes[12], bytes[13], bytes[14], bytes[15]
            ];
            return new Guid(guid);
        }

        /// <inheritdoc/>
        internal override object ChangeType(object value, Type type)
        {
            if (type == typeof(Guid) && value is string str)
            {
                return Guid.Parse(str);
            }
            if (type == typeof(Guid) && value is byte[] bytes)
            {
                return MySqlUuidToGuid(bytes);
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
                return "UUID_TO_BIN('" + id.ToString().ToUpper() + "', 1)";
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
                //MYSQL doesn't support time zone so all values are saving using UTC time zone.
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
        public override string GetTables(string schema)
        {
            return string.Format("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}'", schema);
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
