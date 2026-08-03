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
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// SQ Lite database settings.
    /// </summary>
    public class GXSqLiteSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSqLiteSettings()
            : base(DatabaseType.SqLite)
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
            DatabasePermission.Reload,
            DatabasePermission.Execute,
            DatabasePermission.CreateView ,
            DatabasePermission.CreateProcedure,
            DatabasePermission.CreateFunction ,
            DatabasePermission.Connect,
            DatabasePermission.Admin];
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT name AS table_name, sql FROM sqlite_master WHERE type = 'table' AND sql LIKE '%REFERENCES {tableName}%'";
        }

        /// <inheritdoc />
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetDescriptionQuery(string schema, string tableName, string columnName)
        {
            return "SELECT ''";

        }

        /// <inheritdoc />
        public override string GetOrdinalQuery(string schema, string tableName, string columnName)
        {
            tableName = tableName.Replace("'", "''");
            columnName = columnName.Replace("'", "''");
            return $"SELECT cid + 1 FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";

        }

        /// <inheritdoc />
        public override string GetCommentQuery(string schema, string tableName, string columnName, string comment)
        {
            //SQLIte doesn't support comments.
            return "";
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string databaseName)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery()
        {
            return @"PRAGMA database_list";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string database, string user)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string databaseName, string userName)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override void AddUsersToDatabaseQuery(List<string> queries,
            string databaseName,
            DatabasePermission permissions, params IEnumerable<string> users)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            throw new NotSupportedException("SQLite does not support database users or permissions.");
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return $"SELECT CASE WHEN \"notnull\" = 0 THEN 1 ELSE 0 END FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        }

        /// <inheritdoc />
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool IsPrimaryKey(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}' AND pk > 0";
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            return $"SELECT DISTINCT \"table\" FROM pragma_foreign_key_list('{tableName}') " + $"WHERE \"from\" = '{columnName}'";        
        }


        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format(
        @"SELECT CASE
              WHEN sql LIKE '%""{0}"" INTEGER PRIMARY KEY AUTOINCREMENT%'
                OR sql LIKE '%{0} INTEGER PRIMARY KEY AUTOINCREMENT%'
                OR sql LIKE '%""{0}"" INTEGER PRIMARY KEY%'
                OR sql LIKE '%{0} INTEGER PRIMARY KEY%'
              THEN 1
              ELSE 0
          END
          FROM sqlite_master
          WHERE type = 'table'
            AND name = '{1}'",
        columnName,
        tableName);
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return $"SELECT dflt_value FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValue(object value, Type columnType)
        {
            if (value is DefaultValueKind v)
            {
                return v switch
                {
                    DefaultValueKind.Now when columnType == typeof(DateOnly) =>
                        "date('now', 'localtime')",

                    DefaultValueKind.Now when columnType == typeof(TimeOnly) =>
                        "time('now', 'localtime')",

                    DefaultValueKind.Now when columnType == typeof(DateTimeOffset) =>
                        "datetime('now', 'localtime')",

                    DefaultValueKind.Now =>
                        "datetime('now', 'localtime')",

                    DefaultValueKind.UtcNow when columnType == typeof(DateOnly) =>
                        "date('now')",

                    DefaultValueKind.UtcNow when columnType == typeof(TimeOnly) =>
                        "time('now')",

                    DefaultValueKind.UtcNow =>
                        "CURRENT_TIMESTAMP",

                    DefaultValueKind.NewGuid =>
                        "randomblob(16)",

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
            return $"SELECT type FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 1;
            return string.Format("PRAGMA table_info('{0}')", name);
        }

        /// <inheritdoc />
        public override string GetRenameTableQuery(string oldTableName, string newTableName)
        {
            return $"ALTER TABLE '{oldTableName}' RENAME TO '{newTableName}'";
        }

        /// <inheritdoc />
        public override string GetRenameTableColumnQuery(string tableName, string oldColumnName, string newColumnName)
        {
            return $"ALTER TABLE '{tableName}' RENAME COLUMN '{oldColumnName}' TO '{newColumnName}'";
        }

        /// <inheritdoc />
        override public char ColumnNameQuoteCharacter
        {
            get
            {
                return '\"';
            }
        }

        /// <inheritdoc/>
        public override int MaximumIndexNameLength
        {
            get
            {
                return int.MaxValue;
            }
        }

        /// <inheritdoc />
        public override bool UseQuotationWithSelectColumns
        {
            get
            {
                return false;
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

        /// <inheritdoc />
        override public string AutoIncrementDefinition
        {
            get
            {
                return "AUTOINCREMENT";
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "TEXT";
            }
            return "VARCHAR(" + maxLength.ToString() + ")";
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
                return "INTEGER";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                //BLOB gives best performance.
                return "BLOB";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            return "TEXT";
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
                return "TEXT";
            }
        }

        /// <inheritdoc />
        override public string DateTimeOffsetColumnDefinition(TimeStorageUnit unit)
        {
            return "TEXT";
        }

        /// <inheritdoc />
        override public string ByteColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc />
        override public string SByteColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc />
        override public string ShortColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc />
        override public string UShortColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc />
        override public string IntColumnDefinition
        {
            get
            {
                return "INTEGER";
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
                return "BIGINT";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "DOUBLE";
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

        /// <inheritdoc />
        override public string DesimalColumnDefinition
        {
            get
            {
                return "DESIMAL";
            }
        }

        /// <inheritdoc />
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            return "BLOB";
        }

        /// <inheritdoc />
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
            if (value is string str1)
            {
                string upper = str1.ToUpperInvariant();
                if (upper.Contains("DATETIME('NOW', 'LOCALTIME')"))
                {
                    return DefaultValueKind.Now;
                }
                if (upper.Contains("TIME('NOW', 'LOCALTIME')") ||
                    upper.Contains("DATE('NOW', 'LOCALTIME')"))
                {
                    return DefaultValueKind.Now;
                }
                if (upper.Contains("RANDOMBLOB(16)"))
                {
                    return DefaultValueKind.NewGuid;
                }
            }
            if (type == typeof(DateTimeOffset) && value is string str)
            {
                string format = "yyyy-MM-dd HH:mm:ss.fffzzz";
                return DateTimeOffset.ParseExact(str, format, System.Globalization.CultureInfo.InvariantCulture);
            }
            if (type == typeof(Guid) && value is byte[] bytes)
            {
                return new Guid(bytes);
            }
            return base.ChangeType(value, type);
        }


        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            if (value is byte[] ba)
            {
                return "X'" + Convert.ToHexString(ba) + "'";
            }
            return base.ConvertToString(value, options);
        }

        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return "SELECT last_insert_rowid() FROM " + tableName;
        }

        /// <inheritdoc />
        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = '{0}'", tableName);
        }

        /// <inheritdoc />
        public override string GetTables(string schema)
        {
            return "SELECT NAME FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
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
