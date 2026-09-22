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

namespace Gurux.Service_Simple_Unit_Test
{

    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CacheTest
    {
        readonly GXQueryCache _cache = new GXQueryCache();

        /// <summary>
        /// Select test.
        /// </summary>
        [TestMethod]
        public void SelectTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(s => new { s.Id, s.Status }, _cache);
            Assert.AreEqual("SELECT ID, Status FROM TestClass", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Status FROM TestClass", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Status FROM TestClass", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            arg = GXSelectArgs.Select<TestClass>(s => s.Id, _cache);
            Assert.AreEqual("SELECT ID FROM TestClass", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            arg = GXSelectArgs.Select<TestClass>(s => s.Status, _cache);
            Assert.AreEqual("SELECT Status FROM TestClass", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

        }

        /// <summary>
        /// Joins test.
        /// </summary>
        [TestMethod]
        public void JoinsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id), _cache);
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            Assert.AreEqual("SELECT COUNT(Supplier.SupplierID) FROM Supplier INNER JOIN Product ON Supplier.SupplierID = Product.TargetID", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT COUNT(Supplier.SupplierID) FROM Supplier INNER JOIN Product ON Supplier.SupplierID = Product.TargetID", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT COUNT(Supplier.SupplierID) FROM Supplier INNER JOIN Product ON Supplier.SupplierID = Product.TargetID", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id), _cache);
            arg.Joins.AddInnerJoin<Product, Supplier>(j => j.SupplierID, j => j.Id);
            Assert.AreEqual("SELECT COUNT(Supplier.SupplierID) FROM Product INNER JOIN Supplier ON Product.TargetID = Supplier.SupplierID", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

        }

        /// <summary>
        /// Where test.
        /// </summary>
        [TestMethod]
        public void WhereTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1, _cache);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 1", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 1", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 1", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            arg = GXSelectArgs.SelectById<TestIDClass>(2, _cache);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 2", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 2", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT ID, Text FROM TestIDClass WHERE ID = 2", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
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
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            t.Id = Guid.NewGuid();
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = t.Id;
            arg = GXDeleteArgs.DeleteRange([t, t2], _cache);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id IN(X'" + Convert.ToHexString(t.Id.ToByteArray()) + "', X'" + Convert.ToHexString(t.Id.ToByteArray()) + "')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id IN(X'" + Convert.ToHexString(t.Id.ToByteArray()) + "', X'" + Convert.ToHexString(t.Id.ToByteArray()) + "')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
        }

        /// <summary>
        /// Update cache test.
        /// </summary>
        [TestMethod]
        public void UpdateTest()
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            t.Time = DateTime.MinValue;
            t.Text2 = "First";
            GXUpdateArgs arg = GXUpdateArgs.Update(t, u => new { u.Time, u.Text2 }, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000', SimpleText = 'First' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            t.Time = DateTime.MaxValue;
            t.Text2 = "Second";
            arg = GXUpdateArgs.Update(t, u => new { u.Time, u.Text2 }, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '9999-12-31 23:59:59.499', SimpleText = 'Second' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            t.Time = DateTime.MinValue;
            arg = GXUpdateArgs.Update(t, u => u.Time, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            //Cached value.
            arg = GXUpdateArgs.Update(t, u => u.Time, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            t.Id = Guid.NewGuid();
            t.Text2 = "First";
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = Guid.NewGuid();
            t2.Time = DateTime.MaxValue;
            t2.Text2 = "Second";
            arg = GXUpdateArgs.UpdateRange([t, t2], u => new { u.Time, u.Text2 }, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000', SimpleText = 'First' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "' UPDATE GuidTestClass SET Time = '9999-12-31 23:59:59.499', SimpleText = 'Second' WHERE Id = X'" + Convert.ToHexString(t2.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            arg = GXUpdateArgs.UpdateRange([t, t2], u => new { u.Time }, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "' UPDATE GuidTestClass SET Time = '9999-12-31 23:59:59.499' WHERE Id = X'" + Convert.ToHexString(t2.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            //Cached value.
            arg = GXUpdateArgs.UpdateRange([t, t2], u => new { u.Time }, _cache);
            Assert.AreEqual("UPDATE GuidTestClass SET Time = '0001-01-01 00:00:00.000' WHERE Id = X'" + Convert.ToHexString(t.Id.ToByteArray()) + "' UPDATE GuidTestClass SET Time = '9999-12-31 23:59:59.499' WHERE Id = X'" + Convert.ToHexString(t2.Id.ToByteArray()) + "'", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
        }


        /// <summary>
        /// Insert cache test.
        /// </summary>
        [TestMethod]
        public void InsertTest()
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.Empty;
            t.Time = DateTime.MinValue;
            t.Text2 = "First";
            GXInsertArgs arg = GXInsertArgs.Insert(t, u => new { u.Id, u.Time, u.Text2 }, _cache);
            string actual = arg.ToString(false);
            Assert.AreEqual("INSERT INTO GuidTestClass (Id, Time, SimpleText) VALUES(X'" + Convert.ToHexString(t.Id.ToByteArray()) + "', '0001-01-01 00:00:00.000', 'First')", actual);
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            t.Id = Guid.Empty;
            t.Time = DateTime.MaxValue;
            t.Text2 = "Second";
            arg = GXInsertArgs.Insert(t, u => new { u.Time, u.Text2 }, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time, SimpleText) VALUES('9999-12-31 23:59:59.499', 'Second')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            t.Id = Guid.Empty;
            t.Time = DateTime.MinValue;
            arg = GXInsertArgs.Insert(t, u => u.Time, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time) VALUES('0001-01-01 00:00:00.000')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            //Cached value.
            t.Id = Guid.Empty;
            arg = GXInsertArgs.Insert(t, u => u.Time, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time) VALUES('0001-01-01 00:00:00.000')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            t.Id = Guid.Empty;
            t.Text2 = "First";
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = Guid.Empty;
            t2.Time = DateTime.MaxValue;
            t2.Text2 = "Second";
            arg = GXInsertArgs.InsertRange([t, t2], u => new { u.Time, u.Text2 }, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time, SimpleText) VALUES('0001-01-01 00:00:00.000', 'First'), ('9999-12-31 23:59:59.499', 'Second')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);

            t.Id = Guid.Empty;
            t2.Id = Guid.Empty;
            arg = GXInsertArgs.Insert([t, t2], u => new { u.Time }, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time) VALUES('0001-01-01 00:00:00.000'), ('9999-12-31 23:59:59.499')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            //Cached value.
            t.Id = Guid.Empty;
            t2.Id = Guid.Empty;
            arg = GXInsertArgs.Insert([t, t2], u => new { u.Time }, _cache);
            Assert.AreEqual("INSERT INTO GuidTestClass (Time) VALUES('0001-01-01 00:00:00.000'), ('9999-12-31 23:59:59.499')", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
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
            Assert.AreEqual("SELECT Guid FROM TestClass GROUP BY Text HAVING COUNT(ID) > 1", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT Guid FROM TestClass GROUP BY Text HAVING COUNT(ID) > 1", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(q => new { q.Text, q.Status });
            arg.Having.And<TestClass>(q => GXSql.Count(1) > 2);
            Assert.AreEqual("SELECT Guid FROM TestClass GROUP BY Text, Status HAVING COUNT(1) > 2", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
            Assert.AreEqual("SELECT Guid FROM TestClass GROUP BY Text, Status HAVING COUNT(1) > 2", arg.ToString(false));
            Debug.WriteLine("GenerationTime: " + arg.GenerationTime);
        }
    }
}
