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

        /// <inheritdoc cref="GXDBSettings.ColumnQuotation"/>
        override public char ColumnQuotation
        {
            get
            {
                return '\"';
            }
        }

        /// <inheritdoc cref="GXDBSettings.TableQuotation"/>
        public override char TableQuotation
        {
            get
            {
                return '\"';
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

        ///<inheritdoc cref="GXDBSettings.UpperCase"/>
        ///<remarks>
        ///Oracle columns are upper case.
        ///</remarks>
        public override bool UpperCase
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
                return LimitType.Oracle;
            }
        }

        ///<inheritdoc cref="GXDBSettings.UseQuotationWhereColumns"/>
        ///<remarks>
        ///Oracle needs separator to where column names.
        ///</remarks>
        public override bool UseQuotationWhereColumns
        {
            get
            {
                return true;
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
                return 30;
            }
        }

        /// <inheritdoc cref="GXDBSettings.ColumnNameMaximumLength"/>
        override public int ColumnNameMaximumLength
        {
            get
            {
                return 30;
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

        private int GetVersion()
        {
            return int.Parse(this.ServerVersion.Substring(0, 2));
        }

        /// <inheritdoc cref="GXDBSettings.AutoIncrementDefinition"/>
        override public string AutoIncrementDefinition
        {
            get
            {
                if (GetVersion() > 11)
                {
                    return "IDENTITY";
                }
                return null;
            }
        }

        /// <inheritdoc cref="GXDBSettings.StringColumnDefinition"/>
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "NVARCHAR2(2000)";
            }
            return "NVARCHAR2(" + maxLength.ToString() + ")";
            
        }

        /// <inheritdoc cref="GXDBSettings.CharColumnDefinition"/>
        override public string CharColumnDefinition
        {
            get
            {
                return "CHAR";
            }
        }

        /// <inheritdoc cref="GXDBSettings.BoolColumnDefinition"/>
        override public string BoolColumnDefinition
        {
            get
            {
                return "NUMBER(1)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.GuidColumnDefinition"/>
        override public string GuidColumnDefinition
        {
            get
            {
                return "NVARCHAR2(36)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DateTimeColumnDefinition"/>
        override public string DateTimeColumnDefinition
        {
            get
            {
                return "DATE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.TimeSpanColumnDefinition"/>
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DateTimeOffsetColumnDefinition"/>
        override public string DateTimeOffsetColumnDefinition
        {
            get
            {
                return "DATE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteColumnDefinition"/>
        override public string ByteColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.SByteColumnDefinition"/>
        override public string SByteColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ShortColumnDefinition"/>
        override public string ShortColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UShortColumnDefinition"/>
        override public string UShortColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.IntColumnDefinition"/>
        override public string IntColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UIntColumnDefinition"/>
        override public string UIntColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.LongColumnDefinition"/>
        override public string LongColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ULongColumnDefinition"/>
        override public string ULongColumnDefinition
        {
            get
            {
                return "NUMBER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.FloatColumnDefinition"/>
        override public string FloatColumnDefinition
        {
            get
            {
                return "FLOAT(24)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DoubleColumnDefinition"/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "BINARY_DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DesimalColumnDefinition"/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "FLOAT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteArrayColumnDefinition"/>
        override public string ByteArrayColumnDefinition
        {
            get
            {
                return "NVARCHAR2(2000)";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ObjectColumnDefinition"/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "NVARCHAR2(2000)";
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

        /// <inheritdoc cref="GXDBSettings.ConvertToString"/>
        public override string ConvertToString(object value)
        {
            if (value is DateTime)
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                return "TO_DATE(" + GetQuetedValue(((DateTime)value).ToString(format)) + ", 'YYYY-MM-DD HH24:MI:SS')";
            }
            if (value is DateTimeOffset)
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                return "TO_DATE(" + GetQuetedValue(((DateTimeOffset)value).ToString(format)) + ", 'YYYY-MM-DD HH24:MI:SS')";
            }
            if (value is float)
            {
                return ((float)value).ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is double)
            {
                if ((double)value == double.MaxValue)
                {
                    return "TO_BINARY_DOUBLE('" + double.MaxValue.ToString() + "')";
                }
                if ((double)value == double.MinValue)
                {
                    return "TO_BINARY_DOUBLE('" + double.MinValue.ToString() + "')";
                }
                return ((double)value).ToString("r", CultureInfo.InvariantCulture.NumberFormat);
            }
            if (value is System.Decimal)
            {
                return ((System.Decimal)value).ToString(CultureInfo.InvariantCulture.NumberFormat);
            }
            throw new Exception("Unknown data type.");
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

        /// <inheritdoc cref="GXDBSettings.CreateAutoIncrement"/>
        public override string[] CreateAutoIncrement(string tableName, string columnName)
        {
            if (GetVersion() < 12)
            {
                string trigger = GetTriggerName(tableName, columnName);
                tableName = GXDbHelpers.AddQuotes(tableName, this.TableQuotation);
                columnName = GXDbHelpers.AddQuotes(columnName, this.ColumnQuotation);                
                //Create sequence.
                return new string[]{"DECLARE\n C NUMBER;\nBEGIN\nSELECT COUNT(*) INTO C FROM USER_SEQUENCES WHERE SEQUENCE_NAME = '" + trigger + "';\n" + 
                    "IF (C = 0) THEN\n EXECUTE IMMEDIATE 'CREATE SEQUENCE " + trigger + "';\nEND IF;END;",
                //Create or replace trigger.
                "CREATE OR REPLACE TRIGGER " + trigger + " BEFORE INSERT ON " + tableName +" FOR EACH ROW\n" + 
                "BEGIN\n SELECT " + trigger + ".NEXTVAL\n INTO\n :new." + columnName + "\n \nFROM dual;\nEND;"
            };
            }
            return null;
        }

        /// <inheritdoc cref="GXDBSettings.OnUpdate"/>
        public override string OnUpdate(string primaryTable, string primaryColumn, string foreignTable, string foreignColumn, ForeignKeyUpdate updateType)
        {
            return null;
        }

        /// <inheritdoc cref="GXDBSettings.DropAutoIncrement"/>
        public override string[] DropAutoIncrement(string tableName, string columnName)
        {
            if (GetVersion() < 12)
            {
                return new string[] {"DROP SEQUENCE " + GetSequenceName(tableName, columnName)};
            }
            return null;
        }
    }
}
