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

        private static string GetTableFilter(
            string tableName,
            string schemaColumn = "table_schema",
            string tableColumn = "table_name")
        {
            if (tableName.Length > 1 &&
                tableName.StartsWith("\"", StringComparison.Ordinal) &&
                tableName.EndsWith("\"", StringComparison.Ordinal))
            {
                tableName = tableName[1..^1].Replace("\"\"", "\"");
            }
            tableName = tableName.Replace("'", "''").ToUpperInvariant();
            return $@"UPPER({tableColumn}) = '{tableName}'
  AND
  (
      {schemaColumn} = CURRENT_SCHEMA()
      OR
      (
          NOT EXISTS (
              SELECT 1
              FROM information_schema.tables
              WHERE table_schema = CURRENT_SCHEMA()
                AND UPPER(table_name) = '{tableName}'
          )
          AND {schemaColumn} <> 'information_schema'
          AND {schemaColumn} NOT LIKE 'pg_%'
      )
  )";
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT tc.constraint_name, tc.table_name FROM information_schema.table_constraints tc JOIN information_schema.constraint_column_usage ccu ON tc.constraint_catalog = ccu.constraint_catalog AND tc.constraint_schema = ccu.constraint_schema AND tc.constraint_name = ccu.constraint_name WHERE tc.constraint_type = 'FOREIGN KEY' AND ccu.table_name = '{tableName}'";
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName)
        {
            return string.Format(@"SELECT tc.constraint_name, ccu.table_schema, ccu.table_name, kcu.column_name, ccu.column_name, kcu.ordinal_position, rc.delete_rule, rc.update_rule
FROM information_schema.table_constraints AS tc
INNER JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
INNER JOIN information_schema.referential_constraints AS rc ON tc.constraint_name = rc.constraint_name AND tc.table_schema = rc.constraint_schema
INNER JOIN information_schema.key_column_usage AS ccu ON ccu.constraint_name = rc.unique_constraint_name AND ccu.constraint_schema = rc.unique_constraint_schema AND ccu.ordinal_position = kcu.position_in_unique_constraint
WHERE tc.constraint_type = 'FOREIGN KEY' AND {1}
ORDER BY tc.constraint_name, kcu.ordinal_position", schema, GetTableFilter(tableName, "kcu.table_schema", "kcu.table_name"));
        }

        /// <inheritdoc />
        public override string GetDescriptionQuery(string schema, string tableName, string columnName)
        {
            tableName = tableName.Replace("'", "''");
            if (string.IsNullOrEmpty(columnName))
            {
                return $"SELECT obj_description(c.oid, 'pg_class') FROM pg_class c INNER JOIN pg_namespace n ON n.oid = c.relnamespace WHERE {GetTableFilter(tableName, "n.nspname", "c.relname")}";
            }
            columnName = columnName.Replace("'", "''");
            return $"SELECT col_description(c.oid, a.attnum) FROM pg_class c INNER JOIN pg_namespace n ON n.oid = c.relnamespace INNER JOIN pg_attribute a ON a.attrelid = c.oid WHERE {GetTableFilter(tableName, "n.nspname", "c.relname")} AND a.attname = '{columnName}'";

        }

        /// <inheritdoc />
        public override string GetOrdinalQuery(string schema, string tableName, string columnName)
        {
            tableName = tableName.Replace("'", "''");
            columnName = columnName.Replace("'", "''");
            return $"SELECT ordinal_position FROM information_schema.columns WHERE {GetTableFilter(tableName)} AND column_name = '{columnName}'";

        }

        /// <inheritdoc />
        public override string GetCommentQuery(string schema, string tableName, string columnName, string comment)
        {
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
        public override string GetUsersQuery(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT rolname FROM pg_roles WHERE rolcanlogin AND rolname NOT LIKE 'pg_%' ORDER BY rolname;";
            }
            return $@"SELECT rolname
FROM pg_roles
WHERE rolcanlogin
  AND rolname NOT LIKE 'pg_%'
  AND
  (
      has_database_privilege(
          rolname,
          'guruxamidb',
          'CONNECT'
      )
      OR has_schema_privilege(
          rolname,
          'public',
          'USAGE'
      )
  )
ORDER BY rolname";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery(out int index)
        {
            index = 0;
            return @"SELECT datname AS database_name FROM pg_database WHERE datistemplate = false ORDER BY datname";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string? databaseName, string userName)
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
        public override string RemoveUserQuery(string? databaseName, string userName)
        {
            userName = "\"" + userName.Replace("\"", "\"\"") + "\""; ;
            if (!string.IsNullOrEmpty(databaseName))
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
            return string.Format("SELECT is_nullable FROM information_schema.columns WHERE {1} AND column_name = '{2}'", schema, GetTableFilter(tableName), columnName);
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
        public override bool IsUnique(object value)
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
            return $@"SELECT CASE WHEN is_identity = 'YES' THEN 1 ELSE 0 END
FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{tableName}' AND column_name = '{columnName}';";
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM information_schema.table_constraints AS tc INNER JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema WHERE tc.constraint_type = 'PRIMARY KEY' AND {1} AND kcu.column_name = '{2}'", schema, GetTableFilter(tableName, "kcu.table_schema", "kcu.table_name"), columnName);
        }

        /// <inheritdoc/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format(@"SELECT CASE
    WHEN column_default LIKE '''%' AND position('''::' in column_default) > 0
        THEN substring(column_default from 2 for position('''::' in column_default) - 2)
    ELSE column_default
END FROM information_schema.columns WHERE {1} AND column_name = '{2}'", schema, GetTableFilter(tableName), columnName);
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValue(object value, Type columnType)
        {
            if (value is DefaultValueKind v)
            {
                return v switch
                {
                    DefaultValueKind.Now when columnType == typeof(DateOnly) => "CURRENT_DATE",
                    DefaultValueKind.Now when columnType == typeof(TimeOnly) => "CURRENT_TIME",
                    DefaultValueKind.Now => "CURRENT_TIMESTAMP",
                    DefaultValueKind.UtcNow when columnType == typeof(DateOnly) => "(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')::date",
                    DefaultValueKind.UtcNow when columnType == typeof(TimeOnly) => "(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')::time",
                    DefaultValueKind.UtcNow => "(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')",
                    DefaultValueKind.NewGuid => "gen_random_uuid()",
                    _ => throw new ArgumentOutOfRangeException(nameof(value))
                };
            }
            return ConvertToString(value, ConvertOption.Quete);
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT data_type, character_maximum_length, numeric_precision FROM information_schema.columns WHERE {1} AND column_name = '{2}'", schema, GetTableFilter(tableName), columnName);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT column_name FROM information_schema.columns WHERE {0} ORDER BY ordinal_position", GetTableFilter(name));
        }

        /// <inheritdoc />
        public override string GetRenameTableQuery(string oldTableName, string newTableName)
        {
            return $"ALTER TABLE \"{oldTableName}\" RENAME TO \"{newTableName}\"";
        }

        /// <inheritdoc />
        public override string GetRenameTableColumnQuery(string tableName, string oldColumnName, string newColumnName)
        {
            return $"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{oldColumnName}\" TO \"{newColumnName}\"";
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
                return "TIME WITHOUT TIME ZONE";
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

        private readonly DateTime MaxValue = new DateTime(9999, 12, 31, 23, 59, 59, 000, DateTimeKind.Utc);

        /// <inheritdoc/>
        internal override object ChangeType(object value, Type type)
        {
            if (type == typeof(DateTime) && value is DateTime dt2)
            {
                if (dt2 == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }
                if (dt2 == DateTime.MaxValue || dt2 == MaxValue)
                {
                    return DateTime.MaxValue;
                }
                return dt2;
            }
            if (type == typeof(DateTimeOffset) && value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return DateTimeOffset.MinValue;
                }
                if (dt == DateTime.MaxValue || dt == MaxValue)
                {
                    return DateTimeOffset.MaxValue;
                }
                return new DateTimeOffset(dt, TimeSpan.Zero).ToLocalTime();
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
                if (dt == DateTime.MaxValue || options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(dt.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture));
            }
            if (value is DateTimeOffset dto)
            {
                if (dto == DateTimeOffset.MaxValue || options.HasFlag(ConvertOption.Seconds))
                {
                    return GetQuetedValue(dto.ToString(DateTimeFormat + "zzz", CultureInfo.InvariantCulture));
                }
                return GetQuetedValue(dto.ToString(DateTimeFormat + ".fffzzz", CultureInfo.InvariantCulture));
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
        public override string GetTablesQuery(string schema)
        {
            return string.Format("SELECT table_name FROM information_schema.tables WHERE table_catalog = '{0}' AND table_schema = CURRENT_SCHEMA()", schema);
        }

        public override string UniqueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM pg_index i INNER JOIN pg_class t ON t.oid = i.indrelid INNER JOIN pg_namespace n ON n.oid = t.relnamespace INNER JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(i.indkey) WHERE i.indisunique = TRUE AND i.indisprimary = FALSE AND {1} AND a.attname = '{2}'", schema, GetTableFilter(tableName, "n.nspname", "t.relname"), columnName);
        }


        /// <inheritdoc />
        public override bool IsIdentity(object value)
        {
            if (value is string s)
            {
                return s == "YES";
            }
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string IsIdentityQuery(string schema, string tableName, string columnName)
        {
            return $@"SELECT is_identity FROM information_schema.columns WHERE table_schema = CURRENT_SCHEMA() AND table_name = '{tableName}' AND column_name = '{columnName}'";
        }

        public override string TableIndexesQuery(string schema, string tableName)
        {
            return $@"SELECT ci.relname, i.indisunique, a.attname, k.ordinality,
CASE WHEN (i.indoption[k.ordinality - 1] & 1) = 1 THEN 'DESC' ELSE 'ASC' END
FROM pg_index i
INNER JOIN pg_class t ON t.oid = i.indrelid
INNER JOIN pg_namespace n ON n.oid = t.relnamespace
INNER JOIN pg_class ci ON ci.oid = i.indexrelid
INNER JOIN LATERAL unnest(i.indkey) WITH ORDINALITY AS k(attnum, ordinality) ON TRUE
INNER JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = k.attnum
WHERE i.indisprimary = FALSE
  AND {GetTableFilter(tableName, "n.nspname", "t.relname")}
ORDER BY ci.relname, k.ordinality";
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
