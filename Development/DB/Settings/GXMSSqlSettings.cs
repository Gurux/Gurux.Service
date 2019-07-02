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
namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// Microsoft SQL database settings.
    /// </summary>
    class GXMSSqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMSSqlSettings() : base(DatabaseType.MSSQL)
        {

        }

        /// <inheritdoc cref="GXDBSettings.GetColumnConstraints"/>
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            string str = (string)values[1];
            if (str == "NO_ACTION")
            {
                onDelete = ForeignKeyDelete.None;
            }
            else
            {
                onDelete = (ForeignKeyDelete)Enum.Parse(typeof(ForeignKeyDelete), str, true);
            }
            str = (string)values[2];
            if (str == "NO_ACTION")
            {
                onUpdate = ForeignKeyUpdate.None;
            }
            else
            {
                onUpdate = (ForeignKeyUpdate)Enum.Parse(typeof(ForeignKeyUpdate), str, true);
            }
            return (string)values[0];
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnConstraintsQuery"/>
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT OBJECT_NAME(f.parent_object_id) AS 'Table name', delete_referential_action_desc AS 'On Delete', update_referential_action_desc AS 'On Update' FROM sys.foreign_keys AS f, sys.foreign_key_columns AS fc, sys.tables t WHERE f.OBJECT_ID = fc.constraint_object_id AND t.OBJECT_ID = fc.referenced_object_id AND f.parent_object_id = OBJECT_ID('{1}') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.IsNullable"/>
        public override bool IsNullable(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnNullableQuery"/>
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT is_nullable FROM sys.columns WHERE object_id = object_id('{0}') AND name = '{1}'", tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnIndexQuery"/>
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }
        /// <inheritdoc cref="GXDBSettings.IsPrimaryKey"/>
        public override bool IsPrimaryKey(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc cref="GXDBSettings.IsAutoIncrement"/>
        public override bool IsAutoIncrement(object value)
        {
            return Convert.ToBoolean(value);
        }

        /// <inheritdoc cref="GXDBSettings.GetAutoIncrementQuery"/>
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT is_identity FROM sys.columns WHERE object_id = object_id('{0}.{1}') AND name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetReferenceTablesQuery"/>
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT OBJECT_NAME(f.referenced_object_id)FROM sys.foreign_keys AS f, sys.foreign_key_columns AS fc, sys.tables t WHERE f.OBJECT_ID = fc.constraint_object_id AND t.OBJECT_ID = fc.referenced_object_id AND f.parent_object_id = OBJECT_ID('{1}') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetPrimaryKeyQuery"/>
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT COUNT(1) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 AND TABLE_CATALOG = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnDefaultValueQuery"/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT object_definition(default_object_id) AS definition FROM sys.columns WHERE object_id = object_id('{0}.{1}') AND name = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnTypeQuery"/>
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            return string.Format("SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'", schema, tableName, columnName);
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnsQuery"/>
        public override string GetColumnsQuery(string schema, string name, out int index)
        {
            index = 0;
            return string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", name);
        }

        /// <inheritdoc cref="GXDBSettings.ColumnQuotation"/>
        override public char ColumnQuotation
        {
            get
            {
                return '[';
            }
        }

        /// <inheritdoc cref="GXDBSettings.SelectUsingAs"/>
        public override bool SelectUsingAs
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc cref="GXDBSettings.TableQuotation"/>
        public override char TableQuotation
        {
            get
            {
                return '[';
            }
        }

        /// <inheritdoc cref="GXDBSettings.MaximumRowUpdate"/>
        override public int MaximumRowUpdate
        {
            get
            {
                return 999;
            }
        }

        /// <inheritdoc cref="GXDBSettings.TableNameMaximumLength"/>
        override public int TableNameMaximumLength
        {
            get
            {
                return 128;
            }
        }

        /// <inheritdoc cref="GXDBSettings.ColumnNameMaximumLength"/>
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 128;
            }
        }

        /// <inheritdoc cref="GXDBSettings.AutoIncrementFirst"/>
        override public bool AutoIncrementFirst
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc cref="GXDBSettings.LimitType"/>
        internal override LimitType LimitType
        {
            get
            {
                return LimitType.Top;
            }
        }

        /// <inheritdoc cref="GXDBSettings.AutoIncrementDefinition"/>
        override public string AutoIncrementDefinition
        {
            get
            {
                return "IDENTITY(1,1)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.StringColumnDefinition"/>
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "VARCHAR(MAX)";
            }
            return "VARCHAR(" + maxLength.ToString() + ")";
        }

        /// <inheritdoc cref="GXDBSettings.CharColumnDefinition"/>
        override public string CharColumnDefinition
        {
            get
            {
                return "CHAR(1)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.BoolColumnDefinition"/>
        override public string BoolColumnDefinition
        {
            get
            {
                return "BIT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.GuidColumnDefinition"/>
        override public string GuidColumnDefinition
        {
            get
            {
                return "CHAR(36)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DateTimeColumnDefinition"/>
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "DATETIME2";
            }
        }

        /// <inheritdoc cref="GXDBSettings.TimeSpanColumnDefinition"/>
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return "VARCHAR(30)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DateTimeOffsetColumnDefinition"/>
        override public string DateTimeOffsetColumnDefinition
        {
            get
            {
                return "DATETIME2";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteColumnDefinition"/>
        override public string ByteColumnDefinition
        {
            get
            {
                return "TINYINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.SByteColumnDefinition"/>
        override public string SByteColumnDefinition
        {
            get
            {
                return "SMALLINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ShortColumnDefinition"/>
        override public string ShortColumnDefinition
        {
            get
            {
                return "SMALLINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UShortColumnDefinition"/>
        override public string UShortColumnDefinition
        {
            get
            {
                return "INT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.IntColumnDefinition"/>
        override public string IntColumnDefinition
        {
            get
            {
                return "INT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UIntColumnDefinition"/>
        override public string UIntColumnDefinition
        {
            get
            {
                return "BIGINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.LongColumnDefinition"/>
        override public string LongColumnDefinition
        {
            get
            {
                return "BIGINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ULongColumnDefinition"/>
        override public string ULongColumnDefinition
        {
            get
            {
                return "NUMERIC(20,0)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.FloatColumnDefinition"/>
        override public string FloatColumnDefinition
        {
            get
            {
                return "FLOAT(53)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DoubleColumnDefinition"/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "FLOAT(53)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DesimalColumnDefinition"/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "FLOAT(23)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteArrayColumnDefinition"/>
        override public string ByteArrayColumnDefinition
        {
            get
            {
                return "varchar(max)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ObjectColumnDefinition"/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "varchar(max)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ConvertToString"/>
        public override string ConvertToString(object value)
        {
            if (value is DateTime)
            {
                string format = "yyyyMMdd HH:mm:ss";
                return GetQuetedValue(((DateTime)value).ToString(format, CultureInfo.InvariantCulture));
            }
            if (value is DateTimeOffset)
            {
                string format = "yyyyMMdd HH:mm:ss";
                return GetQuetedValue(((DateTimeOffset)value).ToString(format, CultureInfo.InvariantCulture));
            }
            return base.ConvertToString(value);
        }
    }
}
