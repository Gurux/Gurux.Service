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

using Gurux.Common.Db;

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
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
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
            index = 1;
            return string.Format("PRAGMA table_info('{0}')", name);
        }

        /// <inheritdoc />
        override public char ColumnQuotation
        {
            get
            {
                return '\"';
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
                //return "VARCHAR(1000000)";
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
                return "CHAR(36)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "VARCHAR(30)";
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
        override public string DateTimeOffsetColumnDefinition
        {
            get
            {
                return "DATETIMEOFFSET";
            }
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
        override public string ByteArrayColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }

        /// <inheritdoc />
        override public string ObjectColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }

        /// <inheritdoc />
        public override string DataQuotaReplacement
        {
            get
            {
                return null;
            }
        }
    }
}
