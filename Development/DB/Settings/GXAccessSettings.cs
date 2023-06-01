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
#if !NETCOREAPP2_0 && !NETCOREAPP2_1

using Gurux.Common.Db;
using System;
using System.Globalization;
using System.Text;
namespace Gurux.Service.Orm.Settings
{
    /// <summary>
    /// MS Access database settings.
    /// </summary>
    class GXAccessSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAccessSettings() : base(DatabaseType.Access)
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
            return null;
        }

        /// <inheritdoc cref="GXDBSettings.TableQuotation"/>
        public override char TableQuotation
        {
            get
            {
                return '[';
            }
        }

        /// <inheritdoc />
        override public char ColumnQuotation
        {
            get
            {
                return '[';
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

        /// <inheritdoc cref="GXDBSettings.SelectUsingAs"/>
        public override bool SelectUsingAs
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
                return 1;
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
                return "AutoIncrement";
            }
        }

        /// <inheritdoc />
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "Text";
            }
            else
            {
                return "Text(" + maxLength.ToString() + ")";
            }
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
                //Field 'Bit' can't be used here because it don't support null.
                return "BYTE";
            }
        }

        /// <inheritdoc />
        override public string GuidColumnDefinition
        {
            get
            {
                return "TEXT(36)";
            }
        }

        /// <inheritdoc />
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "DATETIME";
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
                return "DATETIME";
            }
        }


        /// <inheritdoc />
        override public string ByteColumnDefinition
        {
            get
            {
                return "BYTE";
            }
        }

        /// <inheritdoc />
        override public string SByteColumnDefinition
        {
            get
            {
                return "SHORT";
            }
        }

        /// <inheritdoc />
        override public string ShortColumnDefinition
        {
            get
            {
                return "SHORT";
            }
        }

        /// <inheritdoc />
        override public string UShortColumnDefinition
        {
            get
            {
                return "LONG";
            }
        }

        /// <inheritdoc />
        override public string IntColumnDefinition
        {
            get
            {
                return "LONG";
            }
        }

        /// <inheritdoc />
        override public string UIntColumnDefinition
        {
            get
            {
                return "NUMERIC";
            }
        }

        /// <inheritdoc />
        override public string LongColumnDefinition
        {
            get
            {
                //Minimum value do not fit to NUMERIC.
                return "DOUBLE";
            }
        }

        /// <inheritdoc />
        override public string ULongColumnDefinition
        {
            get
            {
                //Maximum value do not fit to NUMERIC.
                return "DOUBLE";
            }
        }

        /// <inheritdoc />
        override public string FloatColumnDefinition
        {
            get
            {
                return "SINGLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DoubleColumnDefinition"/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "Double";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DesimalColumnDefinition"/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "Double";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteArrayColumnDefinition"/>
        override public string ByteArrayColumnDefinition
        {
            get
            {
                return "Text";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ObjectColumnDefinition"/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "LONGBINARY";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ConvertToString"/>
        public override string ConvertToString(object value, bool where)
        {
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                //01/01/100 is the earliest date Access can handle.
                if (dt == DateTime.MinValue)
                {
                    return "#01/01/100#";
                }
                StringBuilder sb = new StringBuilder();
                sb.Append('#');
                sb.Append(dt.Month.ToString());
                sb.Append('/');
                sb.Append(dt.Day.ToString());
                sb.Append('/');
                sb.Append(dt.Year.ToString());
                sb.Append(' ');
                sb.Append(dt.Hour.ToString());
                sb.Append(':');
                sb.Append(dt.Minute.ToString());
                sb.Append(':');
                sb.Append(dt.Second.ToString());
                sb.Append('#');
                return sb.ToString();
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
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
