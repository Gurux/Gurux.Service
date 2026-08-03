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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.Service.DB.Tests
{
    [DataContract(Name = "DataTypesData")]
    [System.ComponentModel.Description("Test class")]
    public class DataTypesData
    {
        /// <summary>
        /// Id.
        /// </summary>
        [DataMember]
        [System.ComponentModel.Description("Identifier.")]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        [MaxLength(32)]
        public string? Text1 { get; set; }

        [DataMember]
        [MaxLength(32)]
        public byte[]? ByteArray { get; set; }

        [DataMember]
        public byte? ByteValue { get; set; }
        [DataMember]
        public sbyte? SByteValue { get; set; }

        [DataMember]
        public short? ShortValue { get; set; }
        [DataMember]
        public int? IntValue { get; set; }
        [DataMember]
        public long? LongValue { get; set; }
        [DataMember]
        public float? FloatValue { get; set; }
        [DataMember]
        public double? DoubleValue { get; set; }
        [DataMember]
        public decimal? DecimalValue { get; set; }
        [DataMember]
        public bool? BoolValue { get; set; }
        [DataMember]
        public DateTime? DateTimeValue { get; set; }

        [DataMember]
        public UInt16? UInt16Value { get; set; }
        [DataMember]
        public UInt32? UInt32Value { get; set; }
        [DataMember]
        public UInt64? UInt64Value { get; set; }
    }
}