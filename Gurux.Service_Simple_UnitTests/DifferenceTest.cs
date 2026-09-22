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

using Gurux.Service;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.Service_Simple_Unit_Test
{

    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DifferenceTest
    {
        /// <summary>
        /// Objects are equal.
        /// </summary>
        [TestMethod]
        public void EmptyTest()
        {
            TestClass newClass = new TestClass();
            TestClass oldClass = new TestClass();
            var diff = GXObjectComparer.Differences(oldClass, newClass);
            Assert.AreEqual(0, diff.Count);
        }

        /// <summary>
        /// Objects have different string values.
        /// </summary>
        [TestMethod]
        public void StringTest()
        {
            TestClass newClass = new TestClass();
            TestClass oldClass = new TestClass();
            oldClass.Text = "Test";
            var diff = GXObjectComparer.Differences(oldClass, newClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("Text", diff[0].Name);

            diff = GXObjectComparer.Differences(newClass, oldClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("Text", diff[0].Name);

        }

        /// <summary>
        /// Objects have different GUID values.
        /// </summary>
        [TestMethod]
        public void GuidTest()
        {
            TestClass newClass = new TestClass();
            TestClass oldClass = new TestClass();
            oldClass.Guid = Guid.CreateVersion7();
            var diff = GXObjectComparer.Differences(oldClass, newClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("Guid", diff[0].Name);

            diff = GXObjectComparer.Differences(newClass, oldClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("Guid", diff[0].Name);
        }

        /// <summary>
        /// Objects have different integer values.
        /// </summary>
        [TestMethod]
        public void IntTest()
        {
            TestClass newClass = new TestClass();
            TestClass oldClass = new TestClass();
            oldClass.IntTest = 123;
            var diff = GXObjectComparer.Differences(oldClass, newClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("IntTest", diff[0].Name);

            diff = GXObjectComparer.Differences(newClass, oldClass);
            Assert.AreEqual(1, diff.Count);
            Assert.AreEqual("IntTest", diff[0].Name);
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        [TestMethod]
        public virtual void UpdateChangedValuesOnly1Test()
        {
            TestClass oldValue = new TestClass();
            TestClass newValue = new TestClass();
            newValue.Id = 1;
            oldValue.Id = 1;
            GXUpdateArgs args = GXUpdateArgs.UpdateChangedOnly(oldValue, newValue);
            Assert.AreEqual("", args.ToString(false));
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TestClass SET Time = '{0}' WHERE ID = 1")]
        public virtual void UpdateChangedValuesOnly2Test(string expected)
        {
            TestClass oldValue = new TestClass();
            TestClass newValue = new TestClass();
            newValue.Id = 1;
            newValue.Time = DateTime.Now;
            oldValue.Id = 1;
            GXUpdateArgs args = GXUpdateArgs.UpdateChangedOnly(oldValue, newValue);
            Assert.AreEqual(BaseTest.ResolveExpected(DatabaseType.MySQL, false, expected, newValue.Time), args.ToString(false));
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TestClass SET Time = '0001-01-01 00:00:00.000' WHERE ID = 1")]
        public virtual void UpdateChangedValuesOnly3Test(string expected)
        {
            TestClass oldValue = new TestClass();
            TestClass newValue = new TestClass();
            oldValue.Time = DateTime.Now;
            oldValue.Id = 1;
            newValue.Id = 1;
            GXUpdateArgs args = GXUpdateArgs.UpdateChangedOnly(oldValue, newValue);
            Assert.AreEqual(BaseTest.ResolveExpected(DatabaseType.MySQL, false, expected, newValue.Time), args.ToString(false));
        }

        /// <summary>
        /// Only changed values are updated.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TestClass SET Time = '{0}', Text = 'Gurux' WHERE ID = 1")]
        public virtual void UpdateChangedValuesOnly4Test(string expected)
        {
            TestClass oldValue = new TestClass();
            TestClass newValue = new TestClass();
            oldValue.Id = 1;
            newValue.Id = 1;
            newValue.Time = DateTime.Now;
            newValue.Text = "Gurux";
            GXUpdateArgs args = GXUpdateArgs.UpdateChangedOnly(oldValue, newValue);
            Assert.AreEqual(BaseTest.ResolveExpected(DatabaseType.MySQL, false, expected, newValue.Time), args.ToString(false));
        }
    }
}
