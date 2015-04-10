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

        /// <inheritdoc cref="GXDBSettings.ColumnQuotation"/>
        override public char ColumnQuotation
        {
            get
            {
                return '\"';
            }
        }

        /// <inheritdoc cref="GXDBSettings.UseQuotationWithSelectColumns"/>
        public override bool UseQuotationWithSelectColumns
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
                return 1000;
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

        /// <inheritdoc cref="GXDBSettings.AutoIncrementDefinition"/>
        override public string AutoIncrementDefinition
        {
            get
            {
                return "AUTOINCREMENT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.StringColumnDefinition"/>
        override public string StringColumnDefinition(int maxLength)
        {
            if (maxLength == 0)
            {
                return "VARCHAR(1000000)";
            }
            return "VARCHAR(" + maxLength.ToString() + ")";
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
                return "INTEGER";
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
                return "VARCHAR(30)";
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
                return "DATETIMEOFFSET";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteColumnDefinition"/>
        override public string ByteColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.SByteColumnDefinition"/>
        override public string SByteColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ShortColumnDefinition"/>
        override public string ShortColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.UShortColumnDefinition"/>
        override public string UShortColumnDefinition
        {
            get
            {
                return "INTEGER";
            }
        }

        /// <inheritdoc cref="GXDBSettings.IntColumnDefinition"/>
        override public string IntColumnDefinition
        {
            get
            {
                return "INTEGER";
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
                return "BIGINT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.FloatColumnDefinition"/>
        override public string FloatColumnDefinition
        {
            get
            {
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DoubleColumnDefinition"/>
        override public string DoubleColumnDefinition
        {
            get
            {
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DesimalColumnDefinition"/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "DESIMAL";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteArrayColumnDefinition"/>
        override public string ByteArrayColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ObjectColumnDefinition"/>
        override public string ObjectColumnDefinition
        {
            get
            {
                return "BLOB";
            }
        }
    }
}
