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
using System.ComponentModel;
using System.Globalization;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Database specific settings.
    /// </summary>
    public abstract class GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Database type.</param>
        protected GXDBSettings(DatabaseType type)
        {
            //Update type so SQL queries are shown for correct database.
            GXDbConnection.DefaultDatabaseType = type;
            Type = type;
        }

        /// <summary>
        /// Database type.
        /// </summary>
        public DatabaseType Type
        {
            get;
            private set;
        }

        internal abstract DatabasePermission[] AvailablePermissions();

        /// <inheritdoc/>
        public override string ToString()
        {
            return Type.ToString();
        }

        /// <summary>
        /// Table prefix.
        /// </summary>
        public string TablePrefix
        {
            get;
            internal set;
        }

        /// <summary>
        /// Is Unix date format used.
        /// </summary>
        [DefaultValue(false)]
        public bool UseEpochTimeFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Enum values are saved by integer value as default. If string values are used set this to true.
        /// </summary>
        public bool UseEnumStringValue
        {
            get;
            set;
        }

        /// <summary>
        /// SQL server version. This is used to determine if some features are supported or not.
        /// </summary>
        public string ServerVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Maximum length of an index name.
        /// </summary>
        public abstract int MaximumIndexNameLength
        {
            get;
        }

        /// <summary>
        /// Select table columns using form "AS ColumnName.TableName". 
        /// This is needed when data is retreaved from multiple tables and column names are the same. 
        /// If table name is available on the schema, then column name is used without table name.
        /// </summary>
        /// <remarks>
        /// If table name is available on the schema, then column name is used without table name. 
        /// This is true, for example with Oracle and PostgreSQL databases where table name is not available on the schema.
        /// </remarks>
        public virtual bool SelectUsingAs
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Are column names upper case.
        /// </summary>
        public virtual bool UpperCase
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Used limiter type.
        /// </summary>
        internal virtual LimitType LimitType
        {
            get
            {
                return LimitType.Limit;
            }
        }

        /// <summary>
        /// Are column quotation marks used with Where column names.
        /// </summary>
        public virtual bool UseQuotationWhereColumns
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Is column quotation marks added to select columns.
        /// </summary>
        public virtual bool UseQuotationWithSelectColumns
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Retrieves the SQL query used to obtain foreign key information for a specified table.
        /// </summary>
        /// <param name="tableName">The name of the table for which to retrieve foreign key details.</param>
        /// <returns>A SQL query string that selects foreign key information.</returns>
        public abstract string GetForeignKeysQuery(string tableName);

        /// <summary>
        /// Get column constraints.
        /// </summary>
        /// <param name="values">Received values.</param>
        /// <param name="onDelete">Foreign key delete action.</param>
        /// <param name="onUpdate">Foreign key update action</param>
        /// <returns>reference class</returns>
        public abstract string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate);

        /// <summary>
        /// Get column constraints query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnConstraintsQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Get description query for table and column.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns></returns>
        public abstract string GetDescriptionQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Get column order query for the column.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns></returns>
        public abstract string GetOrdinalQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Get column comment query for the column.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <param name="comment">Added comment.</param>
        /// <returns>Generated SQL query.</returns>
        public abstract string GetCommentQuery(string schema, string tableName, string columnName, string comment);


        /// <summary>
        /// Is column nullable.
        /// </summary>
        /// <param name="value">Received string.</param>
        /// <returns>Key type.</returns>
        public abstract bool IsNullable(object value);

        /// <summary>
        /// Get get tables query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <returns>Tables query.</returns>
        public abstract string GetTables(string schema);

        /// <summary>
        /// Get table is empty query.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <returns>Table is empty query.</returns>
        public virtual string IsEmpty(string tableName)
        {
            return "SELECT 1 WHERE EXISTS(SELECT 1 FROM " + tableName + ")";
        }

        /// <summary>
        /// Retrieves the current user query.
        /// </summary>
        /// <returns>The SQL query string to retrieve the current user.</returns>
        public abstract string GetCurrentUserQuery();

        /// <summary>
        /// Retrieves the SQL query used to obtain a list of available users.
        /// </summary>
        /// <returns>A string containing the SQL query for fetching user names.</returns>
        public abstract string GetUsersQuery(string databaseName);

        /// <summary>
        /// Retrieves the SQL query used to remove a list of users.
        /// </summary>
        /// <returns>A string containing the SQL query for removing user names.</returns>
        public abstract string RemoveUserQuery(string databaseName, string userName);

        /// <summary>
        /// Retrieves the SQL query used to obtain a list of available databases.
        /// </summary>
        /// <returns>A string containing the SQL query for fetching database names.</returns>
        public abstract string GetDatabasesQuery();

        /// <summary>
        /// Generates a SQL query string to retrieve database user permissions.
        /// </summary>
        /// <returns>A string representing the SQL query for database permissions.</returns>
        public abstract string GetDatabaseUserPermissionQuery(string databaseName, string userName);

        /// <summary>
        /// Converts the specified string to its corresponding DatabasePermission object.
        /// </summary>
        /// <param name="value">The string representation of a database permission.</param>
        /// <returns>A DatabasePermission object that matches the specified string.</returns>
        public abstract DatabasePermission ToDatabasePermission(string value);

        /// <summary>
        /// Get add user queries.
        /// </summary>
        /// <param name="queries">Generated SQL queries.</param>
        /// <param name="users">Users to add.</param>
        public abstract void AddUsersQuery(List<string> queries,
            params DatabaseUser[] users);

        /// <summary>
        /// Get add user to database queries.
        /// </summary>
        /// <param name="queries">Generated SQL queries.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="permissions">Database permissions.</param>
        /// <param name="users">Users to add.</param>
        public abstract void AddUsersToDatabaseQuery(List<string> queries,
            string databaseName,
            DatabasePermission permissions,
            params IEnumerable<string> users);

        /// <summary>
        /// Get remove user from database queries.
        /// </summary>
        /// <param name="queries">Generated SQL queries.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="users">Users to add.</param>
        public abstract void RemoveUsersFromDatabaseQuery(List<string> queries,
            string databaseName,
            params IEnumerable<string> users);

        /// <summary>
        /// Get table exist query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>Table exist query.</returns>
        public abstract string TableExist(string schema, string tableName);

        /// <summary>
        /// Is column nullable.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnNullableQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Is column indexed query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnIndexQuery(string schema, string tableName, string columnName);


        /// <summary>
        /// Get reference tables query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetReferenceTablesQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Get column query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="name">Table name.</param>
        /// <param name="index">Index where column name found.</param>
        /// <returns>Columns query.</returns>
        public abstract string GetColumnsQuery(string schema, string name, out int index);

        /// <summary>
        /// Get key type.
        /// </summary>
        /// <param name="value">Received string.</param>
        /// <returns>Key type.</returns>
        public abstract bool IsPrimaryKey(object value);

        /// <summary>
        /// Is primary key.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column key type query.</returns>
        public abstract string GetPrimaryKeyQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Is column autoincrement.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool IsAutoIncrement(object value);

        /// <summary>
        /// Retrieves the identifier of the most recently inserted record based on the specified value.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>true if the last insert identifier was successfully retrieved; otherwise, false.</returns>
        public abstract string GetLastInsertId(string tableName, string columnName);

        /// <summary>
        /// Check is column AutoIncrement.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetAutoIncrementQuery(string schema, string tableName, string columnName);


        /// <summary>
        /// Get default value for column query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnDefaultValueQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Convert default value to string. This is used when default value is set to the column.
        /// </summary>
        /// <param name="value">Default value.</param>
        /// <returns>String representation of the default value.</returns>
        public abstract string GetColumnDefaultValue(object value, Type columnType);        

        /// <summary>
        /// Get column query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnTypeQuery(string schema, string tableName, string columnName);

        /// <summary>
        /// Get rename table query.
        /// </summary>
        /// <param name="oldTableName">Old table name.</param>
        /// <param name="newTableName">New table name.</param>
        public abstract string GetRenameTableQuery(string oldTableName, string newTableName);

        /// <summary>
        /// Get rename table column query.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="oldColumnName">Old column name.</param>
        /// <param name="newColumnName">New column name.</param>
        public abstract string GetRenameTableColumnQuery(string tableName, string oldColumnName, string newColumnName);

        /// <summary>
        /// Used data quotation replacement.
        /// </summary>
        public abstract string DataQuotaReplacement
        {
            get;
        }

        /// <summary>
        /// Column name quote character.
        /// </summary>
        public abstract char ColumnNameQuoteCharacter
        {
            get;
        }

        /// <summary>
        /// Table name quote character.
        /// </summary>
        public virtual char TableNameQuoteCharacter
        {
            get
            {
                return '\0';
            }
        }

        /// <summary>
        /// Value quote character is used e.g. with string values.
        /// </summary>
        public virtual char ValueQuoteCharacter
        {
            get
            {
                return '\'';
            }
        }


        /// <summary>
        /// Returns maximum row count that is allowed with one insert or update query.
        /// </summary>
        abstract public int MaximumRowUpdate
        {
            get;
        }

        /// <summary>
        /// Maximum length of a table name.
        /// </summary>
        abstract public int TableNameMaximumLength
        {
            get;
        }

        /// <summary>
        /// Maximum length of a column name.
        /// </summary>
        abstract public int ColumnNameMaximumLength
        {
            get;
        }

        /// <summary>
        /// If multiple rows are added is retrned autoincreament value first row or last.
        /// </summary>
        abstract public bool AutoIncrementFirst
        {
            get;
        }

        /// <summary>
        /// Auto increment column definition.
        /// </summary>
        abstract public string AutoIncrementDefinition
        {
            get;
        }

        /// <summary>
        /// String column definition.
        /// </summary>
        /// <param name="maxLength">Maximum length of the string.</param>
        /// <returns>String column definition.</returns>
        abstract public string StringColumnDefinition(int maxLength);

        /// <summary>
        /// Char column definition.
        /// </summary>
        abstract public string CharColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Boolean column definition.
        /// </summary>
        abstract public string BoolColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Guid column definition.
        /// </summary>
        abstract public string GuidColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Date time column definition.
        /// </summary>
        /// <param name="unit">Time storage unit.</param>
        /// <returns>Date time column definition.</returns>
        abstract public string DateTimeColumnDefinition(TimeStorageUnit unit = TimeStorageUnit.Seconds);

        /// <summary>
        /// Time span is saved in seconds because some DBs can save max few days.
        /// </summary>
        abstract public string TimeSpanColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Time only column definition.
        /// </summary>
        abstract public string TimeOnlyColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Date only column definition.
        /// </summary>
        abstract public string DateOnlyColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Date time offset column definition.
        /// </summary>
        /// <param name="unit">Time storage unit.</param>
        /// <returns>Date time offset column definition.</returns>
        abstract public string DateTimeOffsetColumnDefinition(TimeStorageUnit unit = TimeStorageUnit.Seconds);

        /// <summary>
        /// Byte column definition.
        /// </summary>
        abstract public string ByteColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Signed byte column definition.
        /// </summary>
        abstract public string SByteColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Short column definition.
        /// </summary>
        abstract public string ShortColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Unsigned short column definition.
        /// </summary>
        abstract public string UShortColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Integer column definition.
        /// </summary>
        abstract public string IntColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Unsigned integer column definition.
        /// </summary>
        abstract public string UIntColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Long column definition.
        /// </summary>
        abstract public string LongColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Unsigned long column definition.
        /// </summary>
        abstract public string ULongColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Float column definition.
        /// </summary>
        abstract public string FloatColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Double column definition.
        /// </summary>
        abstract public string DoubleColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Decimal column definition.
        /// </summary>
        abstract public string DesimalColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Byte array column definition.
        /// </summary>  
        abstract public string ByteArrayColumnDefinition(int maxLength);

        /// <summary>
        /// 
        /// </summary>
        abstract public string ObjectColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Change value type. 
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="type">Target type.</param>
        /// <returns>Converted value.</returns>
        internal virtual object ChangeType(object value, Type type)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string str)
            {
                if (type == typeof(Guid))
                {
                    return Guid.Parse(str);
                }
                if (type == typeof(string))
                {
                    //Escape ' and \ characters.
                    str = str.Replace(@"\\", @"\");
                    if (!string.IsNullOrEmpty(DataQuotaReplacement))
                    {
                        str = str.Replace(DataQuotaReplacement, "'");
                    }
                    return str;
                }
                if (type == typeof(DateOnly))
                {
                    return DateOnly.Parse(str, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeOnly))
                {
                    return TimeOnly.Parse(str, CultureInfo.InvariantCulture);
                }
            }
            if (value is Guid guid && type == typeof(string))
            {
                return guid.ToString();
            }
            if (value is string str2 && type == typeof(decimal))
            {
                return Decimal.Parse(str2, CultureInfo.InvariantCulture);
            }
            return Convert.ChangeType(value, type);
        }

        /// <summary>
        /// Convert value to string. 
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="options">Conversion options.</param>
        /// <returns>Converted string value.</returns>
        internal virtual string ConvertToString(object value, ConvertOption options = ConvertOption.Quete)
        {
            if (value is Guid id)
            {
                return GetQuetedValue(id.ToString());
            }
            if (value is string str)
            {
                //Escape ' and \ characters.
                str = str.Replace(@"\", @"\\");
                if (!string.IsNullOrEmpty(DataQuotaReplacement))
                {
                    str = str.Replace("'", DataQuotaReplacement);
                }
                if ((options & ConvertOption.Quete) != 0)
                {
                    str = GetQuetedValue(str);
                }
                return str;
            }
            if (value == null)
            {
                return "NULL";
            }
            if (value is bool b)
            {
                return b ? "1" : "0";
            }
            if (value is Enum e)
            {
                return Convert.ToUInt32(e).ToString();
            }
            if (value is DateTime dt)
            {
                string format = "yyyy-MM-dd HH:mm:ss.fff";
                return GetQuetedValue(dt.ToString(format, CultureInfo.InvariantCulture));
            }
            if (value is DateTimeOffset dto)
            {
                string format = "yyyy-MM-dd HH:mm:ss.fffzzz";
                return GetQuetedValue(dto.ToString(format, CultureInfo.InvariantCulture));
            }
            if (value is DateOnly date)
            {
                return GetQuetedValue(date.ToString("yyyy-MM-dd",
                    CultureInfo.InvariantCulture));
            }
            if (value is TimeOnly time)
            {
                return GetQuetedValue(time.ToString("HH:mm:ss.fffffff",
                    CultureInfo.InvariantCulture));
            }
            if (value is float f)
            {
                return f.ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is double d)
            {
                return d.ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is System.Decimal dec)
            {
                return dec.ToString(CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is byte[] ba)
            {
                return Convert.ToHexString(ba);
            }
            if (value is GXSelectArgs args)
            {
                return args.ToString(false);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// All DB's do not support Auto Increment by default.
        /// </summary>
        public virtual string[] CreateAutoIncrement(string tableName, string columnName)
        {
            return null;
        }

        /// <summary>
        /// All DB's do not support ON DELETE by default.
        /// </summary>
        public virtual string OnDelete(string primaryTable, string primaryColumn, string foreignTable, string foreignColumn, ForeignKeyDelete deleteType)
        {
            return null;
        }

        /// <summary>
        /// All DB's do not support ON UPDATE by default.
        /// </summary>
        public virtual string OnUpdate(string primaryTable, string primaryColumn, string foreignTable, string foreignColumn, ForeignKeyUpdate updateType)
        {
            return null;
        }

        /// <summary>
        /// Add value quote characters around the value.
        /// </summary>
        /// <param name="value">Value to quote.</param>
        /// <returns>Quoted value.</returns>
        protected string GetQuetedValue(string value)
        {
            return ValueQuoteCharacter + value + ValueQuoteCharacter;
        }

        /// <summary>
        /// All DB's do not support Auto Increment by default.
        /// </summary>
        public virtual string[] DropAutoIncrement(string tableName, string columnName)
        {
            return null;
        }

        /// <summary>
        /// Check if identifier is reserved word. If it is, then it needs to be quoted.
        /// </summary>
        /// <param name="identifier">The identifier to check.</param>
        /// <returns>True if the identifier is a reserved word, otherwise false.</returns>
        public virtual bool IsReservedWord(string identifier)
        {
            switch (Type)
            {
                case DatabaseType.MySQL:
                    return GXMySqlReservedWords.IsReservedWord(identifier);
                case DatabaseType.MSSQL:
                    return GXMSSqlReservedWords.IsReservedWord(identifier);
                case DatabaseType.SqLite:
                    return GXSqliteReservedWords.IsReservedWord(identifier);
                case DatabaseType.Oracle:
                    return GXOracleReservedWords.IsReservedWord(identifier);
                case DatabaseType.PostgreSQL:
                    return GXPostgreSqlReservedWords.IsReservedWord(identifier);
                case DatabaseType.MariaDB:
                    return GXMariaDBReservedWords.IsReservedWord(identifier);
                case DatabaseType.DB2:
                    return GXDB2ReservedWords.IsReservedWord(identifier);
                case DatabaseType.SapHana:
                    return GXSapHanaReservedWords.IsReservedWord(identifier);
                default:
                    throw new NotSupportedException("Database type not supported: " + Type);
            }
        }
    }
}
