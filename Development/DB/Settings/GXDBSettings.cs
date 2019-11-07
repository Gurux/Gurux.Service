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

using System;
using System.Globalization;
using System.ComponentModel;
using System.Data.Common;

namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Database specific settings.
    /// </summary>
    public abstract class GXDBSettings
    {
        public GXDBSettings(DatabaseType type)
        {
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
        /// Is datetime saved in universal time.
        /// </summary>
        [DefaultValue(false)]
        public bool UniversalTime
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


        public string ServerVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// Select table columns using form "AS ColumnName.TableName". Oracle needs this.
        /// </summary>
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
        /// Is column nullable.
        /// </summary>
        /// <param name="value">Received string.</param>
        /// <returns>Key type.</returns>
        public abstract bool IsNullable(object value);

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
        /// Get column query.
        /// </summary>
        /// <param name="schema">Schema name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Column data type query.</returns>
        public abstract string GetColumnTypeQuery(string schema, string tableName, string columnName);


        /// <summary>
        /// Used column quotation mark.
        /// </summary>
        public abstract char ColumnQuotation
        {
            get;
        }

        /// <summary>
        /// Used table Quotation char.
        /// </summary>
        public virtual char TableQuotation
        {
            get
            {
                return '\0';
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

        abstract public string AutoIncrementDefinition
        {
            get;
        }

        abstract public string StringColumnDefinition(int maxLength);

        abstract public string CharColumnDefinition
        {
            get;
        }

        abstract public string BoolColumnDefinition
        {
            get;
        }

        abstract public string GuidColumnDefinition
        {
            get;
        }

        abstract public string DateTimeColumnDefinition
        {
            get;
        }

        /// <summary>
        /// Time span is saved in seconds because some DBs can save max few days.
        /// </summary>
        abstract public string TimeSpanColumnDefinition
        {
            get;
        }

        abstract public string DateTimeOffsetColumnDefinition
        {
            get;
        }


        abstract public string ByteColumnDefinition
        {
            get;
        }


        abstract public string SByteColumnDefinition
        {
            get;
        }


        abstract public string ShortColumnDefinition
        {
            get;
        }

        abstract public string UShortColumnDefinition
        {
            get;
        }


        abstract public string IntColumnDefinition
        {
            get;
        }


        abstract public string UIntColumnDefinition
        {
            get;
        }


        abstract public string LongColumnDefinition
        {
            get;
        }

        abstract public string ULongColumnDefinition
        {
            get;
        }

        abstract public string FloatColumnDefinition
        {
            get;
        }

        abstract public string DoubleColumnDefinition
        {
            get;
        }

        abstract public string DesimalColumnDefinition
        {
            get;
        }

        abstract public string ByteArrayColumnDefinition
        {
            get;
        }

        abstract public string ObjectColumnDefinition
        {
            get;
        }

        public virtual string ConvertToString(object value, bool where)
        {
            if (value is DateTime)
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                return GetQuetedValue(((DateTime)value).ToString(format));
            }
            if (value is DateTimeOffset)
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                return GetQuetedValue(((DateTimeOffset)value).ToString(format));
            }
            if (value is float)
            {
                return ((float)value).ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is double)
            {
                return ((double)value).ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is System.Decimal)
            {
                return ((System.Decimal)value).ToString(CultureInfo.InvariantCulture.NumberFormat);
            }
            throw new Exception("Unknown data type.");
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

        protected static string GetQuetedValue(string value)
        {
            return '\'' + value + '\'';
        }

        /// <summary>
        /// All DB's do not support Auto Increment by default.
        /// </summary>
        public virtual string[] DropAutoIncrement(string tableName, string columnName)
        {
            return null;
        }
    };
}
