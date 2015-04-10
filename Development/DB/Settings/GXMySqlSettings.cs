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
    /// MySQL database settings.
    /// </summary>
    class GXMySqlSettings : GXDBSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMySqlSettings()
            : base(DatabaseType.MySQL)
        {

        }

        /// <inheritdoc cref="GXDBSettings.ColumnQuotation"/>
        override public char ColumnQuotation
        {
            get
            {
                return '`';
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
                return "AUTO_INCREMENT";
            }
        }

        /// <inheritdoc cref="GXDBSettings.StringColumnDefinition"/>
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
                return "BIT(1)";
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
                return "DATETIME";
            }
        }

        /// <inheritdoc cref="GXDBSettings.TimeSpanColumnDefinition"/>
        override public string TimeSpanColumnDefinition
        {
            get
            {
                return DoubleColumnDefinition;
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
                return "TINYINT UNSIGNED";
            }
        }

        /// <inheritdoc cref="GXDBSettings.SByteColumnDefinition"/>
        override public string SByteColumnDefinition
        {
            get
            {
                return "TINYINT";
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
                return "SMALLINT UNSIGNED";
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
                return "INT UNSIGNED";
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
                return "BIGINT UNSIGNED";
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
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.DesimalColumnDefinition"/>
        override public string DesimalColumnDefinition
        {
            get
            {
                return "DOUBLE";
            }
        }

        /// <inheritdoc cref="GXDBSettings.ByteArrayColumnDefinition"/>
        override public string ByteArrayColumnDefinition
        {
            get
            {
                //binary, varbinary, blob, longblob
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
