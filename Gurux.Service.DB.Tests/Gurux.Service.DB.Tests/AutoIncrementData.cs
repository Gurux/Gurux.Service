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
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

namespace Gurux.Service.DB.Tests
{
    public interface IAutoIncrementData<T> : IUnique<T>
    {
        string? Name { get; set; }
    }

    public class AutoIncrementSByteData : IAutoIncrementData<sbyte>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public sbyte Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementInt16Data : IAutoIncrementData<short>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public short Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementInt32Data : IAutoIncrementData<int>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementInt64Data : IAutoIncrementData<long>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public long Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementByteData : IAutoIncrementData<byte>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public byte Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementUInt16Data : IAutoIncrementData<ushort>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public ushort Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementUInt32Data : IAutoIncrementData<uint>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public uint Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }

    public class AutoIncrementUInt64Data : IAutoIncrementData<ulong>
    {
        /// <summary>
        /// Data Id.
        /// </summary>
        [DataMember]
        [AutoIncrement]
        public ulong Id
        {
            get;
            set;
        }
        public string? Name { get; set; }
    }
}
