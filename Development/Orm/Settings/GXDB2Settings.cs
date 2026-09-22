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
    /// IBM DB2 database settings.
    /// </summary>
    class GXDB2Settings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDB2Settings() : base(DatabaseType.DB2)
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
            return $@"SELECT CONSTNAME AS constraint_name, TABNAME AS table_name FROM SYSCAT.REFERENCES WHERE REFTABNAME = '{tableName.ToUpperInvariant()}'";
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName)
        {
            return string.Format(@"SELECT r.CONSTNAME, r.REFTABSCHEMA, r.REFTABNAME, child.COLNAME, parent.COLNAME, child.COLSEQ, r.DELETERULE, r.UPDATERULE
FROM SYSCAT.REFERENCES r
INNER JOIN SYSCAT.KEYCOLUSE child ON r.TABSCHEMA = child.TABSCHEMA AND r.TABNAME = child.TABNAME AND r.CONSTNAME = child.CONSTNAME
INNER JOIN SYSCAT.KEYCOLUSE parent ON r.REFTABSCHEMA = parent.TABSCHEMA AND r.REFTABNAME = parent.TABNAME AND r.REFKEYNAME = parent.CONSTNAME AND child.COLSEQ = parent.COLSEQ
WHERE r.TABSCHEMA = CURRENT SCHEMA AND r.TABNAME = '{1}'
ORDER BY r.CONSTNAME, child.COLSEQ", schema, tableName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetDescriptionQuery(string schema, string tableName, string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return string.Format("SELECT REMARKS FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}'", schema, tableName.ToUpperInvariant());
            }
            return string.Format("SELECT REMARKS FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND UPPER(COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetOrdinalQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COLNO + 1 FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND UPPER(COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
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
            return string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), "Y", true) == 0;
        }

        private static string GetPermissions(DatabasePermission p)
        {
            if (p.HasFlag(DatabasePermission.Admin))
                return "DBADM";

            var list = new List<string>();

            if (p.HasFlag(DatabasePermission.Create)) list.Add("CREATETAB");
            if (p.HasFlag(DatabasePermission.Create)) list.Add("CONNECT");
            if (p.HasFlag(DatabasePermission.Select)) list.Add("DATAACCESS");

            return list.Count == 0 ? "CONNECT" : string.Join(", ", list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT CURRENT USER FROM SYSIBM.SYSDUMMY1";
        }

        /// <inheritdoc />
        public override string GetUsersQuery(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT DISTINCT GRANTEE FROM SYSCAT.DBAUTH ORDER BY GRANTEE";
            }
            return @"SELECT DISTINCT GRANTEE
FROM SYSCAT.DBAUTH
WHERE CONNECTAUTH IN ('Y', 'G')
ORDER BY GRANTEE";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery(out int index)
        {
            index = 0;
            return @"SELECT RTRIM(SCHEMANAME) FROM SYSCAT.SCHEMATA WHERE SCHEMANAME NOT IN (
'SYSIBM', 'SYSIBMADM', 'SYSIBMINTERNAL', 'SYSIBMTS',
'SYSCAT', 'SYSFUN', 'SYSPROC', 'SYSPUBLIC',
'SYSSTAT', 'SYSTOOLS','NULLID','SQLJ'
) ORDER BY SCHEMANAME";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string? databaseName, string userName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return $@"SELECT* FROM SYSCAT.DBAUTH WHERE GRANTEE = UPPER('{userName}')";
            }
            return $@"SELECT DISTINCT PERMISSION
FROM
(
    SELECT CASE WHEN DBADMAUTH IN ('Y', 'G') THEN 'DBADM' END AS PERMISSION
    FROM SYSCAT.DBAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN CONNECTAUTH IN ('Y', 'G') THEN 'CONNECT' END
    FROM SYSCAT.DBAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN CREATETABAUTH IN ('Y', 'G') THEN 'CREATE' END
    FROM SYSCAT.DBAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN SELECTAUTH IN ('Y', 'G') THEN 'SELECT' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN INSERTAUTH IN ('Y', 'G') THEN 'INSERT' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN UPDATEAUTH IN ('Y', 'G') THEN 'UPDATE' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN DELETEAUTH IN ('Y', 'G') THEN 'DELETE' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN ALTERAUTH IN ('Y', 'G') THEN 'ALTER' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN INDEXAUTH IN ('Y', 'G') THEN 'INDEX' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')

    UNION ALL

    SELECT CASE WHEN REFAUTH IN ('Y', 'G') THEN 'REFERENCES' END
    FROM SYSCAT.TABAUTH
    WHERE GRANTEE = UPPER('DB2INST1')
) X
WHERE PERMISSION IS NOT NULL
ORDER BY PERMISSION;";
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
                // Database authorities
                "DBADM" =>
                    DatabasePermission.Admin,
                "SYSIBM" =>
                    DatabasePermission.Admin,
                "CONNECT" =>
                    DatabasePermission.None,
                "CREATE" or
                "CREATETAB" =>
                    DatabasePermission.Create,
                // Table privileges
                "SELECT" =>
                    DatabasePermission.Select,
                "INSERT" =>
                    DatabasePermission.Insert,
                "UPDATE" =>
                    DatabasePermission.Update,
                "DELETE" =>
                    DatabasePermission.Delete,
                "ALTER" =>
                    DatabasePermission.Alter,
                "INDEX" =>
                    DatabasePermission.Index,
                "REFERENCES" =>
                    DatabasePermission.References,
                "EXECUTE" =>
                    DatabasePermission.Execute,
                _ =>
                    DatabasePermission.None
            };
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string? databaseName, string userName)
        {
            throw new NotSupportedException("DB2 does not allow user management.");
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            throw new NotSupportedException("DB2 does not allow user management.");
        }

        /// <inheritdoc />
        public override void AddUsersToDatabaseQuery(List<string> queries,
            string databaseName,
            DatabasePermission permissions, params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string u = user.Replace("'", "''");
                queries.Add($@"GRANT {GetPermissions(permissions)} ON DATABASE TO USER ""{u}"";");
            }
        }

        /// <inheritdoc />
        public override void RemoveUsersFromDatabaseQuery(List<string> queries,
        string databaseName,
            params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string u = user.Replace("\"", "\"\"");
                queries.Add($@"REVOKE DBADM, SECADM, DATAACCESS, ACCESSCTRL, CREATETAB, CONNECT, BINDADD, CREATE_EXTERNAL_ROUTINE, CREATE_NOT_FENCED_ROUTINE, IMPLICIT_SCHEMA, LOAD ON DATABASE FROM USER ""{u}"";");
            }
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT NULLS FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND UPPER(COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
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
            if (value is bool b)
            {
                return b;
            }
            return string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), "Y", true) == 0;
        }

        /// <inheritdoc />
        public override bool IsUnique(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            return string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), "Y", true) == 0;
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT IDENTITY FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND UPPER(COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM SYSCAT.TABCONST tc INNER JOIN SYSCAT.KEYCOLUSE k ON tc.TABSCHEMA = k.TABSCHEMA AND tc.TABNAME = k.TABNAME AND tc.CONSTNAME = k.CONSTNAME WHERE tc.TYPE = 'P' AND tc.TABSCHEMA = CURRENT SCHEMA AND tc.TABNAME = '{1}' AND UPPER(k.COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
        }

        /// <inheritdoc/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT \"DEFAULT\" FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND UPPER(COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValue(object value, Type columnType)
        {
            if (value is DefaultValueKind v)
            {
                return v switch
                {
                    DefaultValueKind.Now when columnType == typeof(DateOnly) =>
                        "CURRENT DATE",

                    DefaultValueKind.Now when columnType == typeof(TimeOnly) =>
                        "CURRENT TIME",

                    DefaultValueKind.Now when columnType == typeof(DateTimeOffset) =>
                        "CURRENT TIMESTAMP",

                    DefaultValueKind.Now =>
                        "CURRENT TIMESTAMP",

                    DefaultValueKind.UtcNow when columnType == typeof(DateOnly) =>
                        "DATE CURRENT TIMESTAMP",

                    DefaultValueKind.UtcNow when columnType == typeof(TimeOnly) =>
                        "TIME CURRENT TIMESTAMP",

                    DefaultValueKind.UtcNow =>
                        "CURRENT TIMESTAMP",
                    //DB2 does not support default value for guid when data type is
                    //BINARY(16).
                    DefaultValueKind.NewGuid => "",
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
            tableName = tableName.Replace("\"", "");
            columnName = columnName.Replace("\"", "");
            return string.Format("SELECT TYPENAME, LENGTH, SCALE FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{1}' AND COLNAME = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT COLNAME FROM SYSCAT.COLUMNS WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{0}' ORDER BY COLNO", name);
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
                return 128;
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
                return "CLOB";
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
                return "BINARY(16)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition(TimeStorageUnit unit)
        {
            if (unit == TimeStorageUnit.Milliseconds)
            {
                return "TIMESTAMP(3)";
            }
            return "TIMESTAMP(0)";
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
                return "TIMESTAMP(3)";
            }
            return "TIMESTAMP(0)";
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
                return "CLOB";
            }
        }

        /// <summary>
        /// ISO 8601 format with milliseconds. 
        /// </summary>
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            if (value is Guid guid)
            {
                return "HEXTORAW('" + Convert.ToHexString(guid.ToByteArray()) + "')";
            }
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {
                    return "TIMESTAMP('0001-01-01 00:00:00')";
                }
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return "TIMESTAMP(" + GetQuetedValue(dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture)) + ")";
                }
                return "TIMESTAMP(" + GetQuetedValue(dt.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture)) + ")";
            }
            if (value is DateTimeOffset dto)
            {
                DateTime utc = dto.UtcDateTime;
                if (options.HasFlag(ConvertOption.Seconds))
                {
                    return "TIMESTAMP(" + GetQuetedValue(utc.ToString(DateTimeFormat, CultureInfo.InvariantCulture)) + ")";
                }
                return "TIMESTAMP(" + GetQuetedValue(utc.ToString(DateTimeFormat + ".fff", CultureInfo.InvariantCulture)) + ")";
            }
            if (value is byte[] ba)
            {
                return "BX'" + Convert.ToHexString(ba) + "'";
            }
            if (value is bool b)
            {
                return b ? "1" : "0";
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
            if (type == typeof(Guid) && value is byte[] ba)
            {
                return new Guid(ba);
            }
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
            return base.ChangeType(value, type);
        }
        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return $"SELECT MAX({columnName}) FROM {tableName}";
        }

        /// <inheritdoc />
        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA AND TABNAME = '{0}'", tableName);
        }

        /// <inheritdoc />
        public override string GetTablesQuery(string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return "SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = CURRENT SCHEMA AND TYPE = 'T'";
            }
            return $"SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{schema}' AND TYPE = 'T'";
        }

        public override string UniqueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM SYSCAT.INDEXES i INNER JOIN SYSCAT.INDEXCOLUSE ic ON i.INDSCHEMA = ic.INDSCHEMA AND i.INDNAME = ic.INDNAME WHERE i.UNIQUERULE IN ('U', 'D') AND i.TABSCHEMA = CURRENT SCHEMA AND i.TABNAME = '{1}' AND UPPER(ic.COLNAME) = '{2}'", schema, tableName.ToUpperInvariant(), columnName.ToUpperInvariant());
        }


        /// <inheritdoc />
        public override bool IsIdentity(object value)
        {
            if (value is string s)
            {
                return string.Compare(s, "Y", true) == 0;
            }
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc />
        public override string IsIdentityQuery(string schema, string tableName, string columnName)
        {
            return $@"SELECT IDENTITY FROM SYSCAT.COLUMNS WHERE TABSCHEMA = UPPER('{schema}') AND TABNAME = UPPER('{tableName}') AND COLNAME = UPPER('{columnName}')";
        }

        public override string TableIndexesQuery(string schema, string tableName)
        {
            return $@"SELECT i.INDNAME, CASE WHEN i.UNIQUERULE IN ('U', 'P') THEN 1 ELSE 0 END, ic.COLNAME, ic.COLSEQ, ic.COLORDER
FROM SYSCAT.INDEXES i
INNER JOIN SYSCAT.INDEXCOLUSE ic ON i.INDSCHEMA = ic.INDSCHEMA AND i.INDNAME = ic.INDNAME
WHERE i.TABSCHEMA = CURRENT SCHEMA
  AND i.TABNAME = UPPER('{tableName}')
  AND i.UNIQUERULE <> 'P'
ORDER BY i.INDNAME, ic.COLSEQ";
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
