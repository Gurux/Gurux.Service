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

        /// <inheritdoc cref="GXDBSettings.GetColumnConstraints"/>
        public override string GetColumnConstraints(object[] values, out ForeignKeyDelete onDelete, out ForeignKeyUpdate onUpdate)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnConstraintsQuery"/>
        public override string GetColumnConstraintsQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.IsNullable"/>
        public override bool IsNullable(object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnNullableQuery"/>
        public override string GetColumnNullableQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnIndexQuery"/>
        public override string GetColumnIndexQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.IsPrimaryKey"/>
        public override bool IsPrimaryKey(object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetPrimaryKeyQuery"/>
        public override string GetPrimaryKeyQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.IsAutoIncrement"/>
        public override bool IsAutoIncrement(object value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetAutoIncrementQuery"/>
        public override string GetAutoIncrementQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetReferenceTablesQuery"/>
        public override string GetReferenceTablesQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnTypeQuery"/>
        public override string GetColumnDefaultValueQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnTypeQuery"/>
        public override string GetColumnTypeQuery(string schema, string tableName, string columnName)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="GXDBSettings.GetColumnsQuery"/>
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

        /// <inheritdoc cref="GXDBSettings.ColumnQuotation"/>
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

        /// <inheritdoc cref="GXDBSettings.MaximumRowUpdate"/>
        override public int MaximumRowUpdate
        {
            get
            {
                return 1;
            }
        }

        /// <inheritdoc cref="GXDBSettings.TableNameMaximumLength"/>
        override public int TableNameMaximumLength
        {
            get
            {
                return 64;
            }
        }

        /// <inheritdoc cref="GXDBSettings.ColumnNameMaximumLength"/>
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 64;
            }
        }


        /// <inheritdoc cref="GXDBSettings.AutoIncrementFirst"/>
        override public bool AutoIncrementFirst
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc cref="GXDBSettings.AutoIncrementDefinition"/>
        override public string AutoIncrementDefinition
        {
            get
            {
                return "AutoIncrement";
            }
        }

        /// <inheritdoc cref="GXDBSettings.StringColumnDefinition"/>
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
                //Field 'Bit' can't be used here because it don't support null.
                return "BYTE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.GuidColumnDefinition"/>
        override public string GuidColumnDefinition
        {
            get
            {
                return "TEXT(36)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DateTimeColumnDefinition"/>
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "DATETIME";
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
                return "DATETIME";
            }
        }


        /// <inheritdoc cref="GXDBSettings.ByteColumnDefinition"/>
        override public string ByteColumnDefinition
        {
            get
            {
                return "BYTE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.SByteColumnDefinition"/>
        override public string SByteColumnDefinition
        {
            get
            {
                return "SHORT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ShortColumnDefinition"/>
        override public string ShortColumnDefinition
        {
            get
            {
                return "SHORT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UShortColumnDefinition"/>
        override public string UShortColumnDefinition
        {
            get
            {
                return "LONG";
            }
        }

        /// <inheritdoc cref="GXDBSettings.IntColumnDefinition"/>
        override public string IntColumnDefinition
        {
            get
            {
                return "LONG";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UIntColumnDefinition"/>
        override public string UIntColumnDefinition
        {
            get
            {
                return "NUMERIC";
            }
        }

        /// <inheritdoc cref="GXDBSettings.LongColumnDefinition"/>
        override public string LongColumnDefinition
        {
            get
            {
                //Minimum value do not fit to NUMERIC.
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ULongColumnDefinition"/>
        override public string ULongColumnDefinition
        {
            get
            {
                //Maximum value do not fit to NUMERIC.
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.FloatColumnDefinition"/>
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
    }
}
#endif //!NETCOREAPP2_0 && !NETCOREAPP2_1
