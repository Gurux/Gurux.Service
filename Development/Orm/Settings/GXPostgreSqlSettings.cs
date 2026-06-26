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
    /// PostgreSQL database settings.
    /// </summary>
    class GXPostgreSqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXPostgreSqlSettings() : base(DatabaseType.PostgreSQL)
        {

        }

        /// <inheritdoc />
        internal override DatabasePermission[] AvailablePermissions()
        {
            return [
            DatabasePermission.Insert,
            DatabasePermission.Update,
            DatabasePermission.Delete,
            DatabasePermission.Select,
            DatabasePermission.References,
            DatabasePermission.Admin];
        }

        /// <inheritdoc />
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            onDelete = ParseOnDelete((string)values[1]);
            onUpdate = ParseOnUpdate((string)values[2]);
            return (string)values[0];
        }

        private static ForeignKeyDelete ParseOnDelete(string value)
        {
            string str = value.Replace(" ", "_").ToUpperInvariant();
            if (str == "NO_ACTION" || str == "SET_NULL" || str == "SET_DEFAULT")
            {
                return ForeignKeyDelete.None;
            }
            return (ForeignKeyDelete)Enum.Parse(typeof(ForeignKeyDelete), str, true);
        }

        private static ForeignKeyUpdate ParseOnUpdate(string value)
        {
            string str = value.Replace(" ", "_").ToUpperInvariant();
            if (str == "NO_ACTION" || str == "SET_DEFAULT")
            {
                return ForeignKeyUpdate.None;
            }
            if (str == "SET_NULL")
            {
                return ForeignKeyUpdate.Null;
            }
            return (ForeignKeyUpdate)Enum.Parse(typeof(ForeignKeyUpdate), str, true);
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT tc.constraint_name, tc.table_name FROM information_schema.table_constraints tc JOIN information_schema.constraint_column_usage ccu ON tc.constraint_catalog = ccu.constraint_catalog AND tc.constraint_schema = ccu.constraint_schema AND tc.constraint_name = ccu.constraint_name WHERE tc.constraint_type = 'FOREIGN KEY' AND ccu.table_name = '{tableName}'";
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT ccu.table_name, rc.delete_rule, rc.update_rule FROM information_schema.table_constraints AS tc INNER JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema INNER JOIN information_schema.referential_constraints AS rc ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema INNER JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = rc.unique_constraint_name AND ccu.constraint_schema = rc.unique_constraint_schema WHERE tc.constraint_type = 'FOREIGN KEY' AND kcu.table_schema = CURRENT_SCHEMA() AND kcu.table_name = '{1}' AND kcu.column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            return string.Compare(Convert.ToString(value), "YES", true) == 0;
        }

        private static string GetPermissions(DatabasePermission value)
        {
            if (value.HasFlag(DatabasePermission.Admin))
            {
                return "ALL PRIVILEGES";
            }
            var list = new List<string>();
            if (value.HasFlag(DatabasePermission.Insert)) list.Add("INSERT");
            if (value.HasFlag(DatabasePermission.Update)) list.Add("UPDATE");
            if (value.HasFlag(DatabasePermission.Delete)) list.Add("DELETE");
            if (value.HasFlag(DatabasePermission.Select)) list.Add("SELECT");
            if (value.HasFlag(DatabasePermission.References)) list.Add("REFERENCES");

            if (!list.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return list.Count == 0 ? "SELECT" : string.Join(", ", list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT_USER";
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT rolname FROM pg_roles WHERE rolcanlogin AND rolname NOT LIKE 'pg_%' ORDER BY rolname;";
            }
            databaseName = databaseName.Replace("'", "''");
            return $@"SELECT rolname
FROM pg_roles
WHERE rolcanlogin
  AND rolname NOT LIKE 'pg_%'
  AND
  (
    EXISTS
    (
      SELECT 1
      FROM pg_database d
      CROSS JOIN LATERAL aclexplode(d.datacl) a
      WHERE d.datname = '{databaseName}'
        AND a.grantee = pg_roles.oid
    )
    OR EXISTS
    (
      SELECT 1
      FROM pg_namespace n
      CROSS JOIN LATERAL aclexplode(n.nspacl) a
      WHERE n.nspname = 'public'
        AND a.grantee = pg_roles.oid
    )
    OR EXISTS
    (
      SELECT 1
      FROM pg_class c
      INNER JOIN pg_namespace n
        ON c.relnamespace = n.oid
      CROSS JOIN LATERAL aclexplode(c.relacl) a
      WHERE n.nspname = 'public'
        AND a.grantee = pg_roles.oid
    )
  )
ORDER BY rolname";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery()
        {
            return @"SELECT datname AS database_name FROM pg_database WHERE datistemplate = false ORDER BY datname";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string databaseName, string userName)
        {
            return $@"SELECT DISTINCT privilege_type FROM information_schema.role_table_grants WHERE grantee = '{userName}'";
        }

        /// <inheritdoc />
        public override DatabasePermission ToDatabasePermission(string value)
        {
            value = value ?? string.Empty;
            value = value
               .Trim()
               .Replace("_", " ")
               .Replace("-", " ")
               .ToUpperInvariant();

            return value switch
            {
                "ALL PRIVILEGES" => DatabasePermission.Admin,
                "SELECT" => DatabasePermission.Select,
                "INSERT" => DatabasePermission.Insert,
                "UPDATE" => DatabasePermission.Update,
                "DELETE" => DatabasePermission.Delete,
                "CREATE" => DatabasePermission.Create,
                "DROP" => DatabasePermission.Drop,
                "REFERENCES" => DatabasePermission.References,
                _ => DatabasePermission.None
            };
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string databaseName, string userName)
        {
            userName = "\"" + userName.Replace("\"", "\"\"") + "\""; ;
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                string db = "\"" + databaseName + "\"";

                return $@"REVOKE CONNECT, TEMPORARY ON DATABASE {db} FROM {userName};";
            }
            return $@"DROP OWNED BY {userName};
DROP ROLE IF EXISTS {userName};";
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            foreach (DatabaseUser user in users)
            {
                string u = user.UserName.Replace("'", "''");
                string p = user.Password.Replace("'", "''");
                queries.Add($@"DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{u}') THEN
        CREATE USER ""{user.UserName}"" WITH PASSWORD '{p}';
    END IF;
END
$$;");
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
                queries.Add($@"GRANT CONNECT ON DATABASE ""{databaseName}"" TO ""{user}"";");
                queries.Add($@"GRANT {GetPermissions(permissions)} ON ALL TABLES IN SCHEMA public TO ""{user}"";");
                queries.Add($@"GRANT USAGE ON SCHEMA public TO ""{user}"";");
            }
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            string db = "\"" + databaseName.Replace("\"", "\"\"") + "\"";
            foreach (string user in users)
            {
                string userName = "\"" + user.Replace("\"", "\"\"") + "\"";
                queries.Add($@"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM {userName};");
                queries.Add($@"REVOKE USAGE ON SCHEMA public FROM {userName};");
                queries.Add($@"REVOKE CONNECT, TEMPORARY ON DATABASE {db} FROM {userName};");
            }
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT is_nullable FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{1}' AND column_name = '{2}'", schema, tableName, columnName);
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
            return Convert.ToInt32(value) != 0;
        }

        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            return Convert.ToInt32(value) != 0;
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT CASE WHEN column_default LIKE 'nextval(%' THEN 1 ELSE 0 END FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{1}' AND column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT ccu.table_name FROM information_schema.table_constraints AS tc INNER JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema INNER JOIN information_schema.referential_constraints AS rc ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema INNER JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = rc.unique_constraint_name AND ccu.constraint_schema = rc.unique_constraint_schema WHERE tc.constraint_type = 'FOREIGN KEY' AND kcu.table_schema = CURRENT_SCHEMA() AND kcu.table_name = '{1}' AND kcu.column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM information_schema.table_constraints AS tc INNER JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema WHERE tc.constraint_type = 'PRIMARY KEY' AND kcu.table_schema = CURRENT_SCHEMA() AND kcu.table_name = '{1}' AND kcu.column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT column_default FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{1}' AND column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT data_type, character_maximum_length, numeric_precision FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{1}' AND column_name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT column_name FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{0}'", name);
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
                return 63;
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

        /// <inheritdoc />
        public override bool UseQuotationWhereColumns
        {
            get
            {
                return true;
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
                return 1000;
            }
        }

        /// <inheritdoc />
        override public int TableNameMaximumLength
        {
            get
            {
                return 63;
            }
        }

        /// <inheritdoc />
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 63;
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
                return "GENERATED BY DEFAULT AS IDENTITY";
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
                return "UUID";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "TIMESTAMP(3) WITHOUT TIME ZONE";
            }
            return "TIMESTAMP(0) WITHOUT TIME ZONE";
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
                return "TIMESTAMP(3) WITH TIME ZONE";
            }
            return "TIMESTAMP(0) WITH TIME ZONE";
        }

        /// <inheritdoc />
        override public string ByteColumnDefinition
        {
            get
            {
                return "SMALLINT";
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
                return "NUMERIC(20,0)";
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
                return "DOUBLE PRECISION";
            }
        }

        /// <inheritdoc/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "NUMERIC(29,10)";
            }
        }

        /// <inheritdoc/>
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            return "BYTEA";
        }

        /// <inheritdoc/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "TEXT";
            }
        }

        /// <inheritdoc/>
        internal override object ChangeType(object value, Type type)
        {
            if (type == typeof(DateTimeOffset))
            {
                if (value is DateTime dt)
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
            }
            return base.ChangeType(value, type);
        }

        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            if (value is Guid id)
            {
                return GetQuetedValue(id.ToString()) + "::uuid";
            }
            if (value is DateTime dt)
            {
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(((DateTime)value).ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture));
            }
            if (value is DateTimeOffset)
            {
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(((DateTimeOffset)value).ToString(DateTimeFormat + "zzz", CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(((DateTimeOffset)value).ToString(DateTimeFormat + ".fffzzz", CultureInfo.InvariantCulture));
            }
            if (value is byte[] ba)
            {
                return "'\\x" + Convert.ToHexString(ba) + "'::bytea";
            }
            if (value is bool b)
            {
                return b ? "TRUE" : "FALSE";
            }
            return base.ConvertToString(value, options);
        }

        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return string.Format("SELECT CURRVAL(pg_get_serial_sequence('{0}', '{1}'))", tableName, columnName);
        }

        /// <inheritdoc />
        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_catalog = '{0}' AND table_schema = CURRENT_SCHEMA() AND table_name = '{1}'", schema, tableName);
        }

        /// <inheritdoc />
        public override string GetTables(string schema)
        {
            return string.Format("SELECT table_name FROM information_schema.tables WHERE table_catalog = '{0}' AND table_schema = CURRENT_SCHEMA()", schema);
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
