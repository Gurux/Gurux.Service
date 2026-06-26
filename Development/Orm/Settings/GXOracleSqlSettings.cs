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
using Gurux.Service.Orm.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Oracle SQL database settings.
    /// </summary>
    class GXOracleSqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXOracleSqlSettings()
            : base(DatabaseType.Oracle)
        {

        }

        /// <inheritdoc />
        internal override DatabasePermission[] AvailablePermissions()
        {
            return [];
        }

        /// <inheritdoc />
        public override string GetForeignKeysQuery(string tableName)
        {
            return $@"SELECT child.constraint_name,
    child.table_name FROM user_constraints parent
JOIN user_constraints child ON child.r_constraint_name = parent.constraint_name
WHERE child.constraint_type = 'R' AND parent.table_name = UPPER('{tableName}')";
        }

        /// <inheritdoc />
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool IsNullable(object value)
        {
            throw new System.NotImplementedException();
        }

        private static string GetPermissions(DatabasePermission value)
        {
            if (value.HasFlag(DatabasePermission.Admin))
                return "DBA";

            var list = new List<string>();

            if (value.HasFlag(DatabasePermission.Create)) list.Add("CREATE TABLE");
            if (value.HasFlag(DatabasePermission.Create)) list.Add("CREATE VIEW");
            if (value.HasFlag(DatabasePermission.Create)) list.Add("CREATE PROCEDURE");
            if (value.HasFlag(DatabasePermission.Execute)) list.Add("CREATE PROCEDURE");

            if (!list.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(value), "No valid permissions specified.");
            }
            return list.Count == 0 ? "CREATE SESSION" : string.Join(", ", list);
        }

        /// <inheritdoc />
        public override string GetCurrentUserQuery()
        {
            return "SELECT USER FROM DUAL";
        }


        /// <inheritdoc />
        public override string GetUsersQuery(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return @"SELECT USERNAME
FROM ALL_USERS
WHERE USERNAME NOT IN (
'SYS',
'SYSTEM',
'ANONYMOUS',
'APPQOSSYS',
'AUDSYS',
'CTXSYS',
'DBSFWUSER',
'DBSNMP',
'DIP',
'GGSHARE',
'GSMADMIN_INTERNAL',
'GSMCATUSER',
'GSMROOTUSER',
'GSMUSER',
'LBACSYS',
'MDSYS',
'OJVMSYS',
'OLAPSYS',
'ORDDATA',
'ORDPLUGINS',
'ORDSYS',
'OUTLN',
'REMOTE_SCHEDULER_AGENT',
'SI_INFORMTN_SCHEMA',
'SYS$UMF',
'SYSBACKUP',
'SYSDG',
'SYSKM',
'SYSRAC',
'SYSBACKUP',
'SYS',
'SYSTEM',
'WMSYS',
'XDB',
'XS$NULL',
'DGPDB_INT',
'DVF',
'DVSYS',
'GGSYS',
'MDDATA',
'ORACLE_OCM',
'PDBADMIN'
)
ORDER BY USERNAME";
            }
            return $@"SELECT GRANTEE
FROM DBA_TAB_PRIVS
WHERE PRIVILEGE = 'CREATE SESSION' AND OWNER = UPPER('{databaseName}')
ORDER BY GRANTEE";
        }

        /// <inheritdoc />
        public override string GetDatabasesQuery()
        {
            //Oracle uses schemas instead of databases.
            //The following query returns all users, which can be considered as databases in Oracle.
            return "SELECT USERNAME FROM ALL_USERS ORDER BY USERNAME";
        }

        /// <inheritdoc />
        public override string GetDatabaseUserPermissionQuery(string databaseName, string userName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return $@"SELECT PRIVILEGE
FROM USER_SYS_PRIVS
UNION ALL
SELECT PRIVILEGE
FROM USER_TAB_PRIVS
UNION ALL
SELECT GRANTED_ROLE
FROM USER_ROLE_PRIVS
ORDER BY 1";
            }
            return $@"SELECT privilege FROM dba_sys_privs
WHERE grantee = UPPER('{userName}')
UNION
SELECT privilege
FROM dba_tab_privs
WHERE grantee = UPPER('{userName}')";
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
                "ALL PRIVILEGES" or "DBA" => DatabasePermission.Admin,
                "CONNECT" or "CREATE SESSION" => DatabasePermission.Connect,
                "SELECT" => DatabasePermission.Select,
                "INSERT" => DatabasePermission.Insert,
                "UPDATE" => DatabasePermission.Update,
                "DELETE" => DatabasePermission.Delete,
                "CREATE" or "RESOURCE" or "CREATE TABLE" or "CREATE USER" => DatabasePermission.Create,
                "DROP" or "DROP ANY" or "DROP USER" => DatabasePermission.Drop,
                "REFERENCES" => DatabasePermission.References,
                _ => DatabasePermission.None
            };
        }

        /// <inheritdoc />
        public override string RemoveUserQuery(string databaseName, string userName)
        {
            if (!(userName.StartsWith("\"") && userName.EndsWith("\"")))
            {
                userName = "\"" + userName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
            }
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = "\"" + databaseName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
                return $@"
            BEGIN
              FOR r IN (
                SELECT owner, table_name, privilege
                FROM dba_tab_privs
                WHERE grantee = UPPER('{userName}')
                  AND owner = UPPER('{databaseName}')
              ) LOOP
                EXECUTE IMMEDIATE
                  'REVOKE ' || r.privilege || ' ON ' ||
                  r.owner || '.' || r.table_name || ' FROM {userName}';
              END LOOP;
            END;
            ";
            }
            return $"DROP USER {userName} CASCADE";
        }

        /// <inheritdoc />
        public override void AddUsersQuery(List<string> queries, params DatabaseUser[] users)
        {
            foreach (DatabaseUser user in users)
            {
                string userName = user.UserName;
                if (!(userName.StartsWith("\"") && userName.EndsWith("\"")))
                {
                    //userName = "\"" + userName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
                    userName = userName.Replace("\"", "\"\"").ToUpperInvariant();
                }
                queries.Add($@"CREATE USER {userName} IDENTIFIED BY ""{user.Password}"" DEFAULT TABLESPACE USERS");
            }
        }

        /// <inheritdoc />
        public override void AddUsersToDatabaseQuery(List<string> queries, 
            string database, 
            DatabasePermission permissions, 
            params IEnumerable<string> users)
        {
            foreach (string user in users)
            {
                string userName = user;
                if (!(userName.StartsWith("\"") && userName.EndsWith("\"")))
                {
                    //userName = "\"" + userName.Replace("\"", "\"\"").ToUpperInvariant() + "\"";
                    userName = userName.Replace("\"", "\"\"").ToUpperInvariant();
                }
                queries.Add($@"GRANT CREATE SESSION TO {userName}");
                queries.Add($@"GRANT {GetPermissions(permissions)} TO {userName}");
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
                if (!(userName.StartsWith("\"") && userName.EndsWith("\"")))
                {
                    userName = userName.Replace("\"", "\"\"").ToUpperInvariant();
                }
                string userNameLiteral = userName.Replace("'", "''");
                queries.Add($@"
BEGIN
  FOR r IN (
    SELECT privilege
    FROM dba_sys_privs
    WHERE grantee = UPPER('{userNameLiteral}')
      AND privilege IN ('CREATE SESSION', 'CREATE TABLE', 'CREATE VIEW', 'CREATE PROCEDURE', 'CREATE USER')
  ) LOOP
    EXECUTE IMMEDIATE 'REVOKE ' || r.privilege || ' FROM {userName}';
  END LOOP;
  FOR r IN (
    SELECT granted_role
    FROM dba_role_privs
    WHERE grantee = UPPER('{userNameLiteral}')
      AND granted_role IN ('DBA', 'RESOURCE', 'CONNECT')
  ) LOOP
    EXECUTE IMMEDIATE 'REVOKE ' || r.granted_role || ' FROM {userName}';
  END LOOP;
END;");
            }
        }

        /// <inheritdoc />
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool IsPrimaryKey(object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool IsAutoIncrement(object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = '{0}'", name.ToUpper());
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
                return 128;
            }
        }

        /// <inheritdoc/>
        public override char TableNameQuoteCharacter
        {
            get
            {
                return '\"';
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
        public override bool UpperCase
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
                //Oracle 11g and older.
                //return LimitType.Oracle;
                return LimitType.Fetch;
            }
        }

        /// <inheritdoc/>
        ///<remarks>
        ///Oracle needs separator to where column names.
        ///</remarks>
        public override bool UseQuotationWhereColumns
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
                return 30;
            }
        }

        /// <inheritdoc />
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 30;
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

        private int GetVersion()
        {
            return int.Parse(this.ServerVersion.Substring(0, 2));
        }

        /// <inheritdoc />
        override public string AutoIncrementDefinition
        {
            get
            {
                //IDENTITY don't work with multiple insert at the same query.
                //Within a single SQL statement containing a reference to NEXTVAL, Oracle increments the sequence once:
                //https://docs.oracle.com/cd/E11882_01/server.112/e41084/pseudocolumns002.htm#SQLRF50946
                /*
                if (GetVersion() > 11)
                {
                    return " GENERATED ALWAYS AS IDENTITY";
                }
                */
                return null;
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "NVARCHAR2(2000)";
            }
            return "NVARCHAR2(" + maxLength.ToString() + ")";

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
                //Boolean is available in Oracle 23c and later versions.
                //Before that NUMBER(1) is used.
                return "NUMBER(1)";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                return "RAW(16)";
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
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return "NUMBER";
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
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string SByteColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string ShortColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string UShortColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string IntColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string UIntColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string LongColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string ULongColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "FLOAT(24)";
            }
        }

        /// <inheritdoc/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "BINARY_DOUBLE";
            }
        }

        /// <inheritdoc/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "FLOAT";
            }
        }

        /// <inheritdoc/>
        override public string ByteArrayColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "BLOB";
            }
            return "RAW(" + maxLength + ")";
        }

        /// <inheritdoc/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }

        /// <summary>
        /// With Oracle DB sequency maximum length is 30 chars.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static internal string GetSequenceName(string tableName, string columnName)
        {
            string name = tableName + "_" + columnName;
            if (name.Length > 30)
            {
                return name.GetHashCode().ToString().ToUpper();
            }
            return name.ToUpper();
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
                return new Guid(bytes);
            }
            if (type == typeof(DateTime) && value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                {

                }
            }
            return base.ChangeType(value, type);
        }

        /// <inheritdoc/>
        internal override string ConvertToString(object value, ConvertOption options)
        {
            if (value is DateTime dt)
            {
                string format;
                if ((options & ConvertOption.Seconds) != 0)
                {
                    format = "yyyy-MM-dd HH:mm:ss";
                    return "TO_TIMESTAMP(" + GetQuetedValue(dt.ToString(format, CultureInfo.InvariantCulture)) + ", 'YYYY-MM-DD HH24:MI:SS')";
                }
                format = "yyyy-MM-dd HH:mm:ss.fff";
                return "TO_TIMESTAMP(" + GetQuetedValue(dt.ToString(format, CultureInfo.InvariantCulture)) + ", 'YYYY-MM-DD HH24:MI:SS.FF3')";
            }
            if (value is DateTimeOffset dto)
            {
                string format;
                if ((options & ConvertOption.Seconds) != 0)
                {
                    format = "yyyy-MM-dd HH:mm:sszzz";
                    return "TO_TIMESTAMP_TZ(" + GetQuetedValue(dto.ToString(format, CultureInfo.InvariantCulture)) + ", 'YYYY-MM-DD HH24:MI:SS TZH:TZM')";
                }
                format = "yyyy-MM-dd HH:mm:ss.fffzzz";
                return "TO_TIMESTAMP_TZ(" + GetQuetedValue(dto.ToString(format, CultureInfo.InvariantCulture)) + ", 'YYYY-MM-DD HH24:MI:SS.FF3 TZH:TZM')";
            }
            if (value is byte[] ba)
            {
                return "HEXTORAW('" + Convert.ToHexString(ba) + "')";
            }
            if (value is Guid g)
            {
                return "HEXTORAW('" + Convert.ToHexString(g.ToByteArray()) + "')";
            }
            if (value is float f)
            {
                return f.ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is double d)
            {
                if (d == double.MaxValue)
                {
                    return "TO_BINARY_DOUBLE('" + double.MaxValue.ToString() + "')";
                }
                if (d == double.MinValue)
                {
                    return "TO_BINARY_DOUBLE('" + double.MinValue.ToString() + "')";
                }
                return d.ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is System.Decimal dec)
            {
                return dec.ToString(CultureInfo.InvariantCulture.NumberFormat);
            }
            return base.ConvertToString(value, options);
        }

        static private string GetTriggerName(string tableName, string columnName)
        {
            string name = tableName + "_" + columnName;
            if (name.Length > 30)
            {
                return name.GetHashCode().ToString().ToUpper();
            }
            return name.ToUpper();
        }

        /// <inheritdoc/>
        public override string[] CreateAutoIncrement(string tableName, string columnName)
        {
            //IDENTITY don't work with multiple insert at the same query.
            //Within a single SQL statement containing a reference to NEXTVAL, Oracle increments the sequence once:
            //https://docs.oracle.com/cd/E11882_01/server.112/e41084/pseudocolumns002.htm#SQLRF50946

            string trigger = GetTriggerName(tableName, columnName);
            tableName = GXDbHelpers.ConvertToString(this, TargetType.Table, null, tableName, null);
            columnName = GXDbHelpers.ConvertToString(this, TargetType.Column, null, columnName, null);
            //Create sequence.
            return new string[]{"DECLARE\n C NUMBER;\nBEGIN\nSELECT COUNT(*) INTO C FROM USER_SEQUENCES WHERE SEQUENCE_NAME = '" + trigger + "';\n" +
                    "IF (C = 0) THEN\n EXECUTE IMMEDIATE 'CREATE SEQUENCE " + trigger + "';\nEND IF;END;",
                //Create or replace trigger.
                "CREATE OR REPLACE TRIGGER " + trigger + " BEFORE INSERT ON " + tableName +" FOR EACH ROW\n" +
                "BEGIN\n SELECT " + trigger + ".NEXTVAL\n INTO\n :new." + columnName + "\n \nFROM dual;\nEND;"
            };
        }

        /// <inheritdoc/>
        public override string OnUpdate(string primaryTable, string primaryColumn, string foreignTable, string foreignColumn, ForeignKeyUpdate updateType)
        {
            return null;
        }

        /// <inheritdoc/>
        public override string[] DropAutoIncrement(string tableName, string columnName)
        {
            //IDENTITY don't work with multiple insert at the same query.
            //Within a single SQL statement containing a reference to NEXTVAL, Oracle increments the sequence once:
            //https://docs.oracle.com/cd/E11882_01/server.112/e41084/pseudocolumns002.htm#SQLRF50946
            return new string[] { "DROP SEQUENCE " + GetSequenceName(tableName, columnName) };
        }

        /// <inheritdoc />
        public override string GetLastInsertId(string tableName, string columnName)
        {
            return "SELECT " + GetSequenceName(tableName, columnName) + ".CURRVAL FROM dual";
        }

        public override string TableExist(string schema, string tableName)
        {
            return string.Format("SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = '{0}'", tableName);
        }

        /// <inheritdoc />
        public override string GetTables(string schema)
        {
            return "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";
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
