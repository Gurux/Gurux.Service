// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
