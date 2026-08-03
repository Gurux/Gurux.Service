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
    public class DataTypesStringData
    {
        /// <summary>
        /// Id.
        /// </summary>
        [DataMember]
        public string Id
        {
            get;
            set;
        }

        [DataMember, MaxLength(32)]
        public string? Text1 { get; set; }

        [DataMember, MaxLength(32)]
        public string? ByteArray { get; set; }

        [DataMember]
        public string? ByteValue { get; set; }
        [DataMember]
        public string? SByteValue { get; set; }

        [DataMember]
        public string? ShortValue { get; set; }
        [DataMember]
        public string? IntValue { get; set; }
        [DataMember]
        public string? LongValue { get; set; }
        [DataMember]
        public string? FloatValue { get; set; }
        [DataMember]
        public string? DoubleValue { get; set; }
        [DataMember]
        public string? DecimalValue { get; set; }
        [DataMember]
        public string? BoolValue { get; set; }
        [DataMember]
        public string? DateTimeValue { get; set; }

        [DataMember]
        public string? UInt16Value { get; set; }
        [DataMember]
        public string? UInt32Value { get; set; }
        [DataMember]
        public string? UInt64Value { get; set; }
    }
}