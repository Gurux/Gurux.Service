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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gurux.Service.Rest;
using Gurux.Service.Orm;
using System.Collections.Generic;

namespace Gurux.Service_Test
{
    class TestItem
    {
        public string Name
        {
            get;
            set;
        }

        public string Address
        {
            get;
            set;
        }
    }

    enum State
    {
        OK = 100,
        Failed = 200
    }

    [DataContract]
    class IndexTestClass : IUnique<int>
    {
        [DataMember(Name="ID"), Service.Orm.Index]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract]
    class UniqueIndexTestClass : IUnique<int>
    {
        [DataMember, Service.Orm.Index(Unique=true)]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract]
    class AutoIncreamentTestClass : IUnique<int>
    {
        [DataMember(Name="ID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember()]
        public string Text
        {
            get;
            set;
        }
    }

    [DataContract(Name = "GuruxName")]
    class NameTestClass : IUnique<int>
    {
        [DataMember(), AutoIncrement]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract]
    class NullableTest : IUnique<int>
    {
        [DataMember()]
        public int Id
        {
            get;
            set;
        }

        [DataMember()]
        public sbyte? SignedByte
        {
            get;
            set;
        }

        [DataMember()]
        public Int16? Int16
        {
            get;
            set;
        }

        [DataMember()]
        public Int32? Int32
        {
            get;
            set;
        }

        [DataMember()]
        public Int64? Int64
        {
            get;
            set;
        }

        [DataMember()]
        public byte? Byte
        {
            get;
            set;
        }

        [DataMember()]
        public UInt16? UInt16
        {
            get;
            set;
        }

        [DataMember()]
        public UInt32? UInt32
        {
            get;
            set;
        }

        [DataMember()]
        public UInt64? UInt64
        {
            get;
            set;
        }

        [DataMember()]
        public Guid? Guid
        {
            get;
            set;
        }

        [DataMember()]
        public DateTime? DateTime
        {
            get;
            set;
        }

        [DataMember()]
        public TimeSpan? TimeSpan
        {
            get;
            set;
        }

        [DataMember()]
        public string Text
        {
            get;
            set;
        }

        [DataMember()]
        public float? Float
        {
            get;
            set;
        }

        [DataMember()]
        public double? Double
        {
            get;
            set;
        }

        [DataMember()]
        public char? Char
        {
            get;
            set;
        }

        [DataMember()]
        public bool? Bool
        {
            get;
            set;
        }

        [DataMember()]
        public DateTimeOffset? DateTimeOffset
        {
            get;
            set;
        }

        [DataMember()]
        public decimal? Decimal
        {
            get;
            set;
        }

        [DataMember()]
        public byte[] ByteArray
        {
            get;
            set;
        }

        [DataMember()]
        public char[] CharArray
        {
            get;
            set;
        }
    }

    [DataContract]
    class TestIDClass : IUnique<long>
    {
        [DataMember(Name = "ID"), AutoIncrement]

        public long Id
        {
            get;
            set;
        }

        [DataMember]
        public string Text
        {
            get;
            set;
        }
    }

    [DataContract]
    class CollectionTestClass : IUnique<long>
    {
        [DataMember(Name = "ID"), AutoIncrement]
        public long Id
        {
            get;
            set;
        }

        [DataMember]
        public string Text
        {
            get;
            set;
        }

        [DataMember(Name = "TestClassID"), ForeignKey]
        public TestClass[] Items
        {
            get;
            set;
        }

        [DataMember(Name = "Items2ID"), ForeignKey]
        public List<TestIDClass> Items2
        {
            get;
            set;
        }
    }

    [DataContract]
    class CircularRelation1 : IUnique<int>
    {
        [DataMember(Name = "ID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember, ForeignKey]
        public CircularRelation2 Target
        {
            get;
            set;
        }
    }

    [DataContract]
    class CircularRelation2 : IUnique<int>
    {
        [DataMember(Name = "ID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember, ForeignKey]
        public CircularRelation1 Target
        {
            get;
            set;
        }

    }

    [DataContract]
    class TestClass : IUnique<int>
    {
        static public TestClass CreateTestClass(int index)
        {
            TestClass item = new TestClass();
            item.Guid = Guid.NewGuid();
            item.Time = DateTime.Now;
            item.Text2 = "Gurux" + index.ToString();
            item.Text3 = "[]{]:\"\"";
            item.Text4 = "\"Gurux\"";
            item.FloatTest = 1.1F;
            item.DoubleTest = 2.2;
            item.Items = new TestItem[2];
            item.Items[0] = new TestItem();
            item.Items[0].Name = "Item1";
            item.Items[0].Address = "Gurux01";
            item.Items[1] = new TestItem();
            item.Items[1].Name = "Item2";
            item.Items[1].Address = "Gurux02";
            item.Span = new TimeSpan(1, 2, 3);
            item.Object = 123;
            return item;
        }

        public void Verify(TestClass expected)
        {
            Assert.AreEqual(expected.Guid, Guid);
            Assert.AreEqual(expected.Time.ToString(), Time.ToString());
            Assert.AreEqual(expected.Text, Text);
            Assert.AreEqual(expected.Text2, Text2);
            Assert.AreEqual(expected.Text3, Text3);
            Assert.AreEqual(expected.Text4, Text4);
            Assert.AreEqual(expected.FloatTest, FloatTest);
            Assert.AreEqual(expected.DoubleTest, DoubleTest);
            Assert.AreEqual(expected.Span, Span);
            if (Items != null)
            {
                Assert.AreEqual(expected.Items.Length, Items.Length);
                Assert.AreEqual(expected.Items[0].Address, Items[0].Address);
                Assert.AreEqual(expected.Items[0].Name, Items[0].Name);
                Assert.AreEqual(expected.Items[1].Address, Items[1].Address);
                Assert.AreEqual(expected.Items[1].Name, Items[1].Name);
            }
            else
            {
                Assert.AreEqual(expected.Items, Items);
            }
        }

        [DataMember(Name="ID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember()]
        public Guid Guid
        {
            get;
            set;
        }

        [DataMember()]
        public DateTime Time
        {
            get;
            set;
        }

        [DataMember()]
        public String Text
        {
            get;
            set;
        }

        [DataMember(Name = "SimpleText")]
        public String Text2
        {
            get;
            set;
        }

        [DataMember()]
        public String Text3
        {
            get;
            set;
        }

        [DataMember()]
        public String Text4
        {
            get;
            set;
        }

        [DataMember()]
        public bool BooleanTest
        {
            get;
            set;
        }

        [DataMember()]
        public int IntTest
        {
            get;
            set;
        }

        [DataMember()]
        public double DoubleTest
        {
            get;
            set;
        }

        [DataMember()]
        public float FloatTest
        {
            get;
            set;
        }

        [DataMember()]
        public TimeSpan Span
        {
            get;
            set;
        }

        [DataMember()]
        public object Object
        {
            get;
            set;
        }

        [DataMember(), Gurux.Common.Ignore]
        public TestItem[] Items
        {
            get;
            set;
        }

        [DataMember()]
        [DefaultValue(State.OK)]
        public State Status
        {
            get;
            set;
        }
    }

    [DataContract]
    public class Supplier : IUnique<int>
    {
        [DataMember(Name="SupplierID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Text
        {
            get;
            set;
        }

        [DataMember(), ForeignKey]
        public List<Product> Items
        {
            get;
            set;
        }

        [DataMember(), ForeignKey]
        public List<Product2> NewProducts
        {
            get;
            set;
        }
    }

    [DataContract]
    public class Product : IUnique<int>
    {
        [DataMember(Name = "ProductID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Text
        {
            get;
            set;
        }

        [DataMember(Name = "TargetID"), ForeignKey(typeof(Supplier))]
        public int SupplierID
        {
            get;
            set;
        }
    }

    [DataContract]
    public class Product2 : IUnique<int>
    {
        [DataMember(Name = "Product2ID"), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Text
        {
            get;
            set;
        }

        [DataMember(Name = "Target2ID"), ForeignKey]
        public Supplier Supplier
        {
            get;
            set;
        }
    }

    [DataContract]
    public class TestClass2 : IUnique<int>
    {
        [DataMember]
        public int Id
        {
            get;
            set;
        }

        [DataMember(Name="ParentID"), ForeignKey(typeof(TestClass))]
        public int Parent
        {
            get;
            set;
        }

        [DataMember()]
        public string Name
        {
            get;
            set;
        }
    }

    [DataContract]
    public class GXEchoRequest : IGXRequest<GXEchoResponse>
    {
        [DataMember]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract]
    public class GXEchoResponse
    {
        [DataMember]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract]
    public class GXEchoServer : GXRestService
    {
        public GXEchoResponse Post(GXEchoRequest request)
        {
            GXEchoResponse res = new GXEchoResponse();
            res.Id = request.Id;
            return res;
        }


        public GXEchoResponse Get(GXEchoRequest request)
        {
            GXEchoResponse res = new GXEchoResponse();
            res.Id = request.Id;
            return res;
        }

        public GXEchoResponse Put(GXEchoRequest request)
        {
            GXEchoResponse res = new GXEchoResponse();
            if (request.Id == -1)
            {
                throw new ArgumentOutOfRangeException("Put");
            }
            res.Id = request.Id;
            return res;
        }

        [Authenticate]
        public GXEchoResponse Delete(GXEchoRequest request)
        {
            GXEchoResponse res = new GXEchoResponse();
            res.Id = request.Id;
            return res;
        }
    }

    [DataContract]
    class Company : IUnique<long>
    {
        [AutoIncrement]
        [DataMember]
        public long Id
        {
            get;
            set;
        }
        [DataMember]
        public string Name
        {
            get;
            set;
        }
        [DataMember(Name = "CountryID")]
        [ForeignKey]
        public Country Country
        {
            get;
            set;
        }
    }


    [DataContract]
    class Country : IUnique<int>
    {
        [DataMember(Name="ID")]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }
        [DataMember(Name = "CountryName")]
        public string Name
        {
            get;
            set;
        }
        /*
        [DataMember, Relation]
        public Company Company
        {
            get;
            set;
        }
         */
    }


    [DataContract]
    class Parent : IUnique<int>
    {
        [AutoIncrement]
        [DataMember]
        public int Id
        {
            get;
            set;
        }
        [DataMember(Name= "ParentName")]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public Child[] Childrens
        {
            get;
            set;
        }
    }

    [DataContract]
    class Child : IUnique<long>
    {
        [DataMember]
        [AutoIncrement]
        public long Id
        {
            get;
            set;
        }

        [DataMember, ForeignKey(typeof(Parent), OnDelete=ForeignKeyDelete.Cascade)]
        public int ParentId
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }
    }

    [DataContract]
    class User2 : IUnique<int>
    {
        [AutoIncrement]
        [DataMember]
        public int Id
        {
            get;
            set;
        }
        [DataMember]
        public string Name
        {
            get;
            set;
        }
        [DataMember]
        [ForeignKey(typeof(UserGroup2), typeof(UserToUserGroup))]
        public UserGroup2[] Groups
        {
            get;
            set;
        }
    }

    [DataContract]
    class UserToUserGroup
    {
        [DataMember]
        [ForeignKey(typeof(User2), OnDelete = ForeignKeyDelete.Cascade)]
        public int UserId
        {
            get;
            set;
        }

        [DataMember]
        [ForeignKey(typeof(UserGroup2), OnDelete = ForeignKeyDelete.Cascade)]
        public int GroupId
        {
            get;
            set;
        }
    }

    [DataContract]
    class UserGroup2 : IUnique<int>
    {
        [DataMember]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        [ForeignKey(typeof(User2), typeof(UserToUserGroup))]
        public User2[] Users
        {
            get;
            set;
        }
    }

    [DataContract]
    class Parameter2 : IUnique<int>
    {
        [DataMember]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string Value
        {
            get;
            set;
        }

        [DataMember, ForeignKey(typeof(Device2))]
        public int DeviceID
        {
            get;
            set;
        }
    }

    [DataContract]
    class Device2 : IUnique<int>
    {
        [DataMember]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember(Name = "DeviceID"), ForeignKey]
        public Parameter2[] Parameters
        {
            get;
            set;
        }
    }

    [DataContract(Name = "Property3"), Alias("P")]
    class Property3 : IUnique<int>
    {
        [DataMember]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }
    }

    [DataContract, Alias("DP")]
    class DeviceProperty : Property3
    {
        [DataMember, ForeignKey(typeof(Device3))]
        public int Device
        {
            get;
            set;
        }

    }

    [DataContract, Alias("DGP")]
    class DeviceGroupProperty : Property3
    {
        [DataMember, ForeignKey]
        public DeviceGroup3 DeviceGroup
        {
            get;
            set;
        }
    }

    [DataContract, Alias("D")]
    class Device3 : IUnique<int>
    {
        [DataMember]
        //[AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }


        [DataMember, ForeignKey]
        public DeviceProperty[] Properties
        {
            get;
            set;
        }

        [DataMember, ForeignKey(typeof(DeviceGroup3))]
        public int DeviceGroup
        {
            get;
            set;
        }

    }

    [DataContract, Alias("DG")]
    class DeviceGroup3 : IUnique<int>
    {
        [DataMember]
        //[AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember, ForeignKey]
        public Device3[] Devices
        {
            get;
            set;
        }

        [DataMember, ForeignKey]
        public DeviceGroupProperty[] Properties
        {
            get;
            set;
        }

    }


}
