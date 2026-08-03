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
using Gurux.Service.Orm.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.Service.DB.Tests
{
    public class DataTypesDataDefaultValue
    {
        /// <summary>
        /// Id.
        /// </summary>
        [DataMember]
        [DefaultValue(DefaultValueKind.NewGuid)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        [MaxLength(32)]
        [DefaultValue("Text1")]
        public string Text1 { get; set; } = default!;

        [DataMember]
        [DefaultValue((byte)2)]
        public byte ByteValue { get; set; }
        [DataMember]
        [DefaultValue((sbyte)3)]
        public sbyte SByteValue { get; set; }

        [DataMember]
        [DefaultValue((short)4)]
        public short ShortValue { get; set; }
        [DataMember]
        [DefaultValue(5)]
        public int IntValue { get; set; }
        [DataMember]
        [DefaultValue((long)6)]
        public long LongValue { get; set; }
        [DataMember]
        [DefaultValue(7.0f)]
        public float FloatValue { get; set; }
        [DataMember]
        [DefaultValue(8.0)]
        public double DoubleValue { get; set; }
        [DataMember]
        [DefaultValue(typeof(decimal), "9.0")]
        public decimal DecimalValue { get; set; }

        [DataMember]
        [DefaultValue(true)]
        public bool BoolValue { get; set; }

        [DataMember]
        [DefaultValue(DefaultValueKind.UtcNow)]
        public DateTime DateTimeValue { get; set; }

        [DataMember]
        [DefaultValue(DefaultValueKind.Now)]
        public DateTimeOffset DateTimeOffset { get; set; }

        [DataMember]
        [DefaultValue(DefaultValueKind.Now)]
        public TimeOnly TimeOnlyValue { get; set; }

        [DataMember]
        [DefaultValue(DefaultValueKind.Now)]
        public DateOnly DateOnlyValue { get; set; }

        [DataMember]
        [DefaultValue((ushort)1)]
        public UInt16 UInt16Value { get; set; }
        [DataMember]
        [DefaultValue((UInt32)10)]
        public UInt32 UInt32Value { get; set; }
        [DataMember]
        [DefaultValue((UInt64)11)]
        public UInt64 UInt64Value { get; set; }
    }
}
