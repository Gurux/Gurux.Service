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
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Common.Enums;

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
        public override bool IsNullable(object value)
        {
            return string.Compare((string)value, "YES", true) == 0;
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
        public override char ColumnQuotation
        {
            get
            {
                return '`';
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
                return "CHAR(36)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "datetime(3)";
            }
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
        override public string DateTimeOffsetColumnDefinition
        {
            get
            {
                return "datetime(3)";
            }
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
        override public string ByteArrayColumnDefinition
        {
            get
            {
                //binary, varbinary, blob, longblob
                return "BLOB";
            }
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
        public override string ConvertToString(object value, bool where)
        {
            //MYSQL doesn't support time zone so all values are saving using current time zone.
            if (value is DateTimeOffset)
            {
                string format = "yyyy-MM-dd HH:mm:ss.fff";
                return GetQuetedValue(((DateTimeOffset)value).ToString(format, CultureInfo.InvariantCulture));
            }
            return base.ConvertToString(value, where);
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
