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
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Gurux.Service_Simple_Unit_Test
{
    [DataContract]
    class ReservedClass : IUnique<int>
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        [DataMember(Name = "ID"), Index]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// MySql , PostgreSQL, DB2, Oracle, MariaDB, SqLite and SapHana reserved word.
        /// </summary>
        [DataMember(Name = "ALL")]
        public string All
        {
            get;
            set;
        } = default!;
    }
}
