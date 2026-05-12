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

using Gurux.Service.DB;
using Gurux.Service.Orm;
using System.Diagnostics;

namespace Gurux.Service_Test
{

    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CacheTest
    {
        GXQueryCache _cache = new GXQueryCache();

        /// <summary>
        /// Select test.
        /// </summary>
        [TestMethod]
        public void SelectTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(s => new { s.Id, s.Status }, _cache);
            Assert.AreEqual("SELECT `ID`, `Status` FROM TestClass", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Status` FROM TestClass", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Status` FROM TestClass", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);

            arg = GXSelectArgs.Select<TestClass>(s => s.Id, _cache);
            Assert.AreEqual("SELECT `ID` FROM TestClass", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            arg = GXSelectArgs.Select<TestClass>(s => s.Status, _cache);
            Assert.AreEqual("SELECT `Status` FROM TestClass", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);

        }

        /// <summary>
        /// Joins test.
        /// </summary>
        [TestMethod]
        public void JoinsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id), _cache);
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            Assert.AreEqual("SELECT COUNT(Supplier.`SupplierID`) FROM Supplier INNER JOIN Product ON Supplier.`SupplierID`=Product.`TargetID`", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT COUNT(Supplier.`SupplierID`) FROM Supplier INNER JOIN Product ON Supplier.`SupplierID`=Product.`TargetID`", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT COUNT(Supplier.`SupplierID`) FROM Supplier INNER JOIN Product ON Supplier.`SupplierID`=Product.`TargetID`", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);

            arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id), _cache);
            arg.Joins.AddInnerJoin<Product, Supplier>(j => j.SupplierID, j => j.Id);
            Assert.AreEqual("SELECT COUNT(Supplier.`SupplierID`) FROM Product INNER JOIN Supplier ON Product.`TargetID`=Supplier.`SupplierID`", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);

        }

        /// <summary>
        /// Where test.
        /// </summary>
        [TestMethod]
        public void WhereTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1, _cache);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=1", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=1", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=1", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            arg = GXSelectArgs.SelectById<TestIDClass>(2, _cache);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=2", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=2", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=2", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
        }


        /// <summary>
        /// Delete cache test.
        /// </summary>
        [TestMethod]
        public void DeleteTest()
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            GXDeleteArgs arg = GXDeleteArgs.Delete(t, _cache);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id='" + t.Id.ToString().ToUpper() + "'", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id='" + t.Id.ToString().ToUpper() + "'", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            t.Id = Guid.NewGuid();
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = t.Id;
            arg = GXDeleteArgs.DeleteRange([t, t2], _cache);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE `Id` IN('" + t.Id.ToString().ToUpper() + "', '" + t.Id.ToString().ToUpper() + "')", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE `Id` IN('" + t.Id.ToString().ToUpper() + "', '" + t.Id.ToString().ToUpper() + "')", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        public void HavingTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(g => g.Text);
            arg.Having.And<TestClass>(q => GXSql.Count(q.Id) > 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text` HAVING COUNT(TestClass.`ID`) > 1", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text` HAVING COUNT(TestClass.`ID`) > 1", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(q => new { q.Text, q.Status });
            arg.Having.And<TestClass>(q => GXSql.Count(1) > 2);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text`, `Status` HAVING COUNT(1) > 2", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text`, `Status` HAVING COUNT(1) > 2", arg.ToString());
            Debug.WriteLine("ExecutionTime: " + arg.ExecutionTime);
        }
    }
}
