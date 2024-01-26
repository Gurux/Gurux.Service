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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gurux.Service.Orm;
using System.Runtime.Serialization;
using System.Globalization;
using Gurux.Common.Db;
using System.Collections.Generic;

namespace Gurux.Service_Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DBTest
    {
        public DBTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        /// Select test.
        /// </summary>
        [TestMethod]
        public void SelectTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>();
            Assert.AreEqual("SELECT `ID`, `Guid`, `Time`, `Text`, `SimpleText`, `Text3`, `Text4`, `BooleanTest`, `IntTest`, `DoubleTest`, `FloatTest`, `Span`, `Object`, `Status` FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Select 1test.
        /// </summary>
        [TestMethod]
        public void Select1Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.One);
            Assert.AreEqual("SELECT 1 FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Select all by id test.
        /// </summary>
        [TestMethod]
        public void GetByIdTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE ID=1", arg.ToString());
        }

        /// <summary>
        /// Select only part of columns.
        /// </summary>
        [TestMethod]
        public void GetPartTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestIDClass>(c => new object[] { c.Id, c.Text }, q => q.Id == 1);
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE TestIDClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select id by id test.
        /// </summary>
        [TestMethod]
        public void GetByIdColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1, q => q.Id);
            Assert.AreEqual("SELECT `ID` FROM TestIDClass WHERE ID=1", arg.ToString());
        }

        /// <summary>
        /// Relation where test.
        /// </summary>
        [TestMethod]
        public void WhereByReferenceTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<DeviceGroup3>(q => q.Id);
            arg.Where.And<DeviceGroup3>(q => q.Id == 1);
            Assert.AreEqual("SELECT `Id` AS `DG.Id` FROM DeviceGroup3 DG WHERE DG.`Id` = 1", arg.ToString());
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public void CountTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(GXSql.One));
            Assert.AreEqual("SELECT COUNT(1) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public void CountTest2()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q.Id));
            Assert.AreEqual("SELECT COUNT(ID) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public void CountTest3()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id));
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            Assert.AreEqual("SELECT COUNT(Supplier.`SupplierID`) FROM Supplier INNER JOIN Product ON Supplier.`SupplierID`=Product.`TargetID`", arg.ToString());
        }

        /// <summary>
        /// Distinct count test.
        /// </summary>
        [TestMethod]
        public void DistinctCountTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.DistinctCount(q.Id));
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            Assert.AreEqual("SELECT COUNT(DISTINCT Supplier.`SupplierID`) FROM Supplier INNER JOIN Product ON Supplier.`SupplierID`=Product.`TargetID`", arg.ToString());
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public void CountWhereTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), q => q.Id == 1);
            Assert.AreEqual("SELECT COUNT(1) FROM TestClass WHERE TestClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select single column test.
        /// </summary>
        [TestMethod]
        public void SelectSingleColumnTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Text);
            Assert.AreEqual("SELECT `Text` FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        public void SelectColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid, x.Text });
            Assert.AreEqual("SELECT `Guid`, `Text` FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        public void SelectSubItemsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => q.Name);
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(x => x.Country, y => y.Id);
            Assert.AreEqual("SELECT Company.`Name`, Country.`CountryName` FROM Company INNER JOIN Country ON Company.`CountryID`=Country.`ID`", arg.ToString());
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        public void SelectSubItemsTest2()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Product2>(q => q.Id);
            arg.Columns.Add<Supplier>(q => q.Id);
            arg.Joins.AddInnerJoin<Product2, Supplier>(x => x.Supplier, y => y.Id);
            Assert.AreEqual("SELECT Product2.`Product2ID`, Supplier.`SupplierID` FROM Product2 INNER JOIN Supplier ON Product2.`Target2ID`=Supplier.`SupplierID`", arg.ToString());
        }

        /// <summary>
        /// Limit test.
        /// </summary>
        [TestMethod]
        public void LimitTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Index = 1;
            arg.Count = 2;
            Assert.AreEqual("SELECT `Guid` FROM TestClass LIMIT 1,2", arg.ToString());
        }

        /// <summary>
        /// Select Distinct test.
        /// </summary>
        [TestMethod]
        public void SelectDistinctTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Distinct = true;
            Assert.AreEqual("SELECT DISTINCT `Guid` FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Select two tables test.
        /// </summary>
        [TestMethod]
        public void SelectTablesTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass2>(q => q.Parent);
            arg.Columns.Clear();
            arg.Columns.Add<TestClass2>(q => q.Name);
            arg.Columns.Add<TestClass>(q => q.Guid);
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            Assert.AreEqual("SELECT TestClass2.`Name`, TestClass.`Guid` FROM TestClass2 RIGHT OUTER JOIN TestClass ON TestClass2.`ParentID`=TestClass.`ID`", arg.ToString());
        }

        /// <summary>
        /// Select all columns from one table when multiple tables are used.
        /// </summary>
        [TestMethod]
        public void SelectOneTableFromManyTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass2>(q => "*");
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            Assert.AreEqual("SELECT TestClass2.`Id`, TestClass2.`ParentID`, TestClass2.`Name` FROM TestClass2 RIGHT OUTER JOIN TestClass ON TestClass2.`ParentID`=TestClass.`ID`", arg.ToString());
        }


        /// <summary>
        /// Delete by primary key test.
        /// </summary>
        [TestMethod]
        public void DeleteByPrimaryKeyTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXDeleteArgs arg = GXDeleteArgs.DeleteById<TestClass>(1);
            Assert.AreEqual("DELETE FROM TestClass WHERE ID=1", arg.ToString());
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        public void DeleteByGuidPrimaryKeyTest()
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            GXDeleteArgs arg = GXDeleteArgs.Delete(t);
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE Id='" + t.Id.ToString().ToUpper() + "'", arg.ToString());
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        public void DeleteByGuidRangeTest()
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = t.Id;
            GXDeleteArgs arg = GXDeleteArgs.DeleteRange(new GuidTestClass[] { t, t2 });
            Assert.AreEqual("DELETE FROM GuidTestClass WHERE `Id` IN('" + t.Id.ToString().ToUpper() + "', '" + t.Id.ToString().ToUpper() + "')", arg.ToString());
        }

        /// <summary>
        /// Delete using where.
        /// </summary>
        [TestMethod]
        public void DeleteByWhereTest()
        {
            GXDeleteArgs del = GXDeleteArgs.Delete<TestClass>(q => q.Text == "Gurux");
            Assert.AreEqual("DELETE FROM TestClass WHERE TestClass.`Text` = 'Gurux'", del.ToString());
        }

        /// <summary>
        /// Delete using select.
        /// </summary>
        [TestMethod]
        public void DeleteBySelectTest()
        {
            GXSelectArgs sel = GXSelectArgs.Select<TestClass>(q => GXSql.One, q => q.Text == "Gurux");
            GXDeleteArgs del = GXDeleteArgs.Delete<TestClass>(a => GXSql.Exists(sel));
            Assert.AreEqual("DELETE FROM TestClass WHERE EXISTS (SELECT 1 FROM TestClass WHERE TestClass.`Text` = 'Gurux')", del.ToString());
        }
        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        public void GetFieldsTest()
        {
            Assert.AreEqual("ID,Guid,Time,Text,SimpleText,Text3,Text4,BooleanTest,IntTest,DoubleTest,FloatTest,Span,Object,Status", string.Join(",", GXSqlBuilder.GetFields<TestClass>()));
        }

        /// <summary>
        /// Right join test
        /// </summary>
        [TestMethod]
        public void RightJoinTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>();
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            Assert.AreEqual("SELECT TestClass.`ID`, TestClass.`Guid`, TestClass.`Time`, TestClass.`Text`, TestClass.`SimpleText`, TestClass.`Text3`, TestClass.`Text4`, TestClass.`BooleanTest`, TestClass.`IntTest`, TestClass.`DoubleTest`, TestClass.`FloatTest`, TestClass.`Span`, TestClass.`Object`, TestClass.`Status`, TestClass2.`Id`, TestClass2.`ParentID`, TestClass2.`Name` FROM TestClass2 RIGHT OUTER JOIN TestClass ON TestClass2.`ParentID`=TestClass.`ID`", arg.ToString());
        }

        /// <summary>
        /// Left join test
        /// </summary>
        [TestMethod]
        public void LeftJoinTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>();
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddLeftJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            Assert.AreEqual("SELECT TestClass.`ID`, TestClass.`Guid`, TestClass.`Time`, TestClass.`Text`, TestClass.`SimpleText`, TestClass.`Text3`, TestClass.`Text4`, TestClass.`BooleanTest`, TestClass.`IntTest`, TestClass.`DoubleTest`, TestClass.`FloatTest`, TestClass.`Span`, TestClass.`Object`, TestClass.`Status`, TestClass2.`Id`, TestClass2.`ParentID`, TestClass2.`Name` FROM TestClass2 LEFT OUTER JOIN TestClass ON TestClass2.`ParentID`=TestClass.`ID`", arg.ToString());
        }

        /// <summary>
        /// Full join test
        /// </summary>
        [TestMethod]
        public void FullJoinTest()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>();
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddFullJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            Assert.AreEqual("SELECT TestClass.`ID`, TestClass.`Guid`, TestClass.`Time`, TestClass.`Text`, TestClass.`SimpleText`, TestClass.`Text3`, TestClass.`Text4`, TestClass.`BooleanTest`, TestClass.`IntTest`, TestClass.`DoubleTest`, TestClass.`FloatTest`, TestClass.`Span`, TestClass.`Object`, TestClass.`Status`, TestClass2.`Id`, TestClass2.`ParentID`, TestClass2.`Name` FROM TestClass2 FULL OUTER JOIN TestClass ON TestClass2.`ParentID`=TestClass.`ID`", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        public void WhereSimpleTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Id == 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select Time where Datetime is bigger Min date time and Datetime is smaller than max date time and text is not empty.
        /// </summary>
        [TestMethod]
        public void WhereDateTimeTest()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Time);
            arg.Where.And<TestClass>(q => q.Time > DateTime.MinValue && q.Time < DateTime.MaxValue);
            Assert.AreEqual("SELECT `Time` FROM TestClass WHERE (TestClass.`Time` > '" + DateTime.MinValue.ToString(format) + "') AND (TestClass.`Time` < '" + DateTime.MaxValue.ToString(format) + "')", arg.ToString());
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        public void WhereStringEmptyTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text);
            arg.Where.And<TestClass>(q => q.Text != string.Empty);
            arg.Where.And<TestClass>(q => q.Text != null);
            Assert.AreEqual("SELECT `Text` FROM TestClass WHERE ((TestClass.`Text` <> '') AND (TestClass.`Text` IS NOT NULL))", arg.ToString());
        }

        /// <summary>
        /// Select Text where string is empty or null.
        /// </summary>
        [TestMethod]
        public void WhereStringIsNullOrEmptyTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text);
            arg.Where.And<TestClass>(q => string.IsNullOrEmpty(q.Text));
            Assert.AreEqual("SELECT `Text` FROM TestClass WHERE (TestClass.`Text` IS NULL OR TestClass.`Text` = '')", arg.ToString());
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        public void WhereStringNotIsNullOrEmptyTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text);
            arg.Where.And<TestClass>(q => !string.IsNullOrEmpty(q.Text));
            Assert.AreEqual("SELECT `Text` FROM TestClass WHERE (TestClass.`Text` IS NOT NULL AND TestClass.`Text` <> '')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Enum is string.
        /// </summary>
        [TestMethod]
        public void WhereEnumTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Settings.UseEnumStringValue = true;
            arg.Where.And<TestClass>(q => q.Status == State.OK);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Status` = 'OK'", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Enum is saved as int.
        /// </summary>
        [TestMethod]
        public void WhereEnumAsIntTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Settings.UseEnumStringValue = false;
            arg.Where.And<TestClass>(q => q.Status == State.OK);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Status` = 100", arg.ToString());
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        public void WhereClassTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Id);
            arg.Where.And<TestClass>(q => q.Id == t.Id);
            Assert.AreEqual("SELECT `ID` FROM TestClass WHERE TestClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        public void WhereClassTest2()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Id);
            arg.Where.And<TestClass>(q => t);
            Assert.AreEqual("SELECT `ID` FROM TestClass WHERE `ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select Guid where class array is given as parameter.
        /// </summary>
        [TestMethod]
        public void WhereClassArrayTest()
        {
            TestClass[] list = new TestClass[] { new TestClass(), new TestClass() };
            list[0].Id = 1;
            list[1].Id = 2;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => list);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE `ID` IN(1, 2)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        public void WhereSimple2Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Id == t.Id);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Select all by string test.
        /// </summary>
        [TestMethod]
        public void WhereExactString()
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestIDClass>(q => q.Text == "Gurux");
            Assert.AreEqual("SELECT `ID`, `Text` FROM TestIDClass WHERE TestIDClass.`Text` = 'Gurux'", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereStartsWithTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Text.StartsWith("Gurux"));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` LIKE('Gurux%')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereStartsWith2Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Text.StartsWith(t.Text));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` LIKE('Gurux%')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEndsWithTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Text.EndsWith("Gurux"));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` LIKE('%Gurux')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereContainsTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Text.Contains("Gurux"));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` LIKE('%Gurux%')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where list contains Gurux.
        /// </summary>
        [TestMethod]
        public void WhereContains2Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            List<string> list = new List<string>();
            list.Add("Gurux");
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => list.Contains(q.Text));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` IN ('Gurux')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text contains with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereContains3Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Text.Contains(t.Text));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` LIKE('%Gurux%')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEqualsTest()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Text.Equals("Gurux", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE UPPER(TestClass.`Text`) LIKE('GURUX')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEquals2Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Text.Equals(t.Text, StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE UPPER(TestClass.`Text`) LIKE('GURUX')", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEquals3Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Text == t.Text);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` = 'Gurux'", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEquals4Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Id == t.Id && q.Text.Equals(t.Text));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (TestClass.`ID` = 1) AND (UPPER(TestClass.`Text`) LIKE('GURUX'))", arg.ToString());
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereEquals5Test()
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Text.Equals(t.Text) && q.Id == t.Id);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (UPPER(TestClass.`Text`) LIKE('GURUX')) AND (TestClass.`ID` = 1)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        public void WhereOrTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Id == 1 || q.Id == 2 || q.Id == 3);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE ((TestClass.`ID` = 1) OR (TestClass.`ID` = 2)) OR (TestClass.`ID` = 3)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = -1.
        /// </summary>
        [TestMethod]
        public void WhereMinusTest()
        {
            int value = -1919693511;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, q => q.Id == value);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` = -1919693511", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        public void WhereOr2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Id == 1 || q.Id == 2);
            arg.Where.Or<TestClass>(q => q.Id == 3);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (((TestClass.`ID` = 1) OR (TestClass.`ID` = 2)) OR (TestClass.`ID` = 3))", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID = 1 or text starts with Gurux.
        /// </summary>
        [TestMethod]
        public void WhereOr3Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Id == 1);
            arg.Where.Or<TestClass>(x => x.Text.StartsWith("Gurux"));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE ((TestClass.`ID` = 1) OR (TestClass.`Text` LIKE('Gurux%')))", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        public void WhereAndTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid });
            arg.Where.And<TestClass>(q => q.Id > 1 && q.Id != 2);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (TestClass.`ID` > 1) AND (TestClass.`ID` <> 2)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        public void WhereAnd2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => q.Id > 1);
            arg.Where.And<TestClass>(q => q.Id != 2);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE ((TestClass.`ID` > 1) AND (TestClass.`ID` <> 2))", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public void SqlInTest()
        {

            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => new int[] { 1, 2, 3 }.Contains(q.Id));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public void SqlInTest2()
        {
            List<int> list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => list.Contains(q.Id));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` IN (1, 2, 3)", arg.ToString());
        }


        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        public void SqlNotInTest()
        {
            int[] list = new int[] { 1, 2, 3 };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => !list.Contains(q.Id));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` NOT IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        public void SqlNotInTest2()
        {
            List<int> list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => !list.Contains(q.Id));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` NOT IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public void SqlOrderTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(q => new { q.Id, q.Guid });
            Assert.AreEqual("SELECT `ID` FROM TestClass ORDER BY TestClass.`ID`, TestClass.`Guid`", arg.ToString());
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public void SqlOrder2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(q => q.Guid);
            Assert.AreEqual("SELECT `ID` FROM TestClass ORDER BY TestClass.`ID`, TestClass.`Guid`", arg.ToString());
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public void SqlOrder3Test()
        {
            string value = "Id";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(value);
            Assert.AreEqual("SELECT `ID` FROM TestClass ORDER BY TestClass.`ID`", arg.ToString());
        }

        /// <summary>
        /// Order desc test.
        /// </summary>
        [TestMethod]
        public void SqlOrderDescTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(q => q.Id);
            arg.Descending = true;
            Assert.AreEqual("SELECT `ID` FROM TestClass ORDER BY TestClass.`ID` DESC", arg.ToString());
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public void InsertAllTest()
        {
            Country c = new Country();
            c.Name = "Finland";
            GXInsertArgs args = GXInsertArgs.Insert(c);
            Assert.AreEqual("INSERT INTO Country (`CountryName`) VALUES('Finland')", args.ToString());
        }

        /// <summary>
        /// Insert range test.
        /// </summary>
        [TestMethod]
        public void InsertRangeTest()
        {
            List<Parameter2> list = new List<Parameter2>()
                {
                new Parameter2()
                {
                    Name = "Name1",
                    Value = "Value1"
                },
                new Parameter2()
                {
                    Name = "Name2",
                    Value = "Value2"
                },
                new Parameter2()
                {
                    Name = "Name3",
                    Value = "Value3"
                }
                    };
            GXInsertArgs args = GXInsertArgs.InsertRange(list);
            Assert.AreEqual("INSERT INTO Parameter2 (`Name`, `Value`, `DeviceID`) VALUES('Name1', 'Value1', 0), ('Name2', 'Value2', 0), ('Name3', 'Value3', 0)", args.ToString());
            args = GXInsertArgs.InsertRange(list, c => new { c.Name, c.Value });
            Assert.AreEqual("INSERT INTO Parameter2 (`Name`, `Value`) VALUES('Name1', 'Value1'), ('Name2', 'Value2'), ('Name3', 'Value3')", args.ToString());
        }


        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public void InsertTest()
        {
            TestClass t = new TestClass();
            t.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(t, x => new { x.Text, x.Guid });
            Assert.AreEqual("INSERT INTO TestClass (`Text`, `Guid`) VALUES('Gurux', '00000000-0000-0000-0000-000000000000')", args.ToString());
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public void InsertTest2()
        {
            Supplier supplier = new Supplier();
            supplier.Text = "Gurux";
            supplier.NewProducts.Add(new Product2() { Text = "Virtual-serial" });
            GXInsertArgs args = GXInsertArgs.Insert(supplier);
            args.ToString();
            Assert.AreEqual("INSERT INTO Supplier (`Text`) VALUES('Gurux') INSERT INTO Product2 (`Text`, `Target2ID`) VALUES('Virtual-serial', 0)", args.ToString());
        }

        /// <summary>
        /// Create table test.
        /// </summary>
        [TestMethod]
        public void CreateNullableTableTest()
        {
            NullableTestClass c = new NullableTestClass();
            c.Active = false;
            c.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(c);
            string str = args.ToString();
            Assert.AreEqual("INSERT INTO NullableTestClass (`Id`, `Active`, `Text`) VALUES('" + c.Id + "', 0, 'Gurux')", str);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public void InsertNullableTest()
        {
            NullableTestClass c = new NullableTestClass();
            c.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(c);
            string str = args.ToString();
            Assert.AreEqual("INSERT INTO NullableTestClass (`Id`, `Active`, `Text`) VALUES('" + c.Id + "', NULL, 'Gurux')", str);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void UpdateTest()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Guid, x.Time });
            Assert.AreEqual("UPDATE TestClass SET `Guid` = '00000000-0000-0000-0000-000000000000', `Time` = '" + dt.ToString(format) + "' WHERE `ID` = 2", args.ToString());
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void UpdateTest2()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, u => u.Time);
            Assert.AreEqual("UPDATE GuidTestClass SET `Time` = '2014-01-02 00.00.00.000' WHERE `Id` = '" + t.Id.ToString() + "'", args.ToString());
        }

        /// <summary>
        /// Update using where.
        /// </summary>
        [TestMethod]
        public void UpdateWhereTest()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Guid, x.Time });
            args.Where.And<TestClass>(q => q.Text == "Gurux");
            Assert.AreEqual("UPDATE TestClass SET `Guid` = '00000000-0000-0000-0000-000000000000', `Time` = '" + dt.ToString(format) + "' WHERE TestClass.`Text` = 'Gurux'", args.ToString());
        }


        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void EpochTimeFormatTest()
        {
            TestClass t = new TestClass();
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            t.Id = 1;
            GXUpdateArgs args = GXUpdateArgs.Update(t);
            args.Settings.UseEpochTimeFormat = true;
            args.Add<TestClass>(t, x => x.Time);
            Assert.AreEqual("UPDATE TestClass SET `Time` = 1388620800 WHERE `ID` = 1", args.ToString());
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void TableNameTest()
        {
            GXSqlBuilder parser = new GXSqlBuilder(DatabaseType.MySQL, null);
            Assert.AreEqual("TestClass", parser.GetTableName<TestClass>());
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void TableNamePrefixTest()
        {
            GXSqlBuilder parser = new GXSqlBuilder(DatabaseType.MySQL, "gx_");
            Assert.AreEqual("gx_TestClass", parser.GetTableName<TestClass>());
        }

        /// <summary>
        /// Where string is null.
        /// </summary>
        [TestMethod]
        public void WhereStringIsNullTest()
        {
            string expected = "WHERE TestClass.`Text` IS NULL";
            string text = null;
            GXSelectArgs args = GXSelectArgs.SelectAll<TestClass>(q => q.Text == text);
            string actual = args.Where.ToString();
            Assert.AreEqual(expected, actual);
            args = GXSelectArgs.SelectAll<TestClass>(q => q.Text.Equals(text));
            actual = args.Where.ToString();
            Assert.AreEqual(expected, actual);

            expected = "WHERE (TestClass.`Text` IS NULL OR TestClass.`Text` = '')";
            args = GXSelectArgs.SelectAll<TestClass>(q => string.IsNullOrEmpty(q.Text));
            actual = args.Where.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public void SqlIn2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public void SqlNotIn2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => !GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` NOT IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        public void ExistsTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>();
            arg.Where.And<Country>(q => GXSql.Exists<Company, Country>(a => a.Country, b => b.Id, arg2));
            string expected = "SELECT `ID`, `CountryName` FROM Country WHERE EXISTS (SELECT `Id` FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX') AND Country.`ID` = Company.`CountryID`)";
            string actual = arg.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        public void Exists2Test()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>();
            arg.Where.And<Country>(q => GXSql.Exists(arg2));
            string expected = "SELECT `ID`, `CountryName` FROM Country WHERE EXISTS (SELECT `Id` FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX'))";
            string actual = arg.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        public void Exists3Test()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => GXSql.One);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>();
            arg.Where.And<Country>(q => GXSql.Exists(arg2));
            string expected = "SELECT `ID`, `CountryName` FROM Country WHERE EXISTS (SELECT 1 FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX'))";
            string actual = arg.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        public void NotExistsTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>();
            arg.Where.And<Country>(q => !GXSql.Exists<Company, Country>(a => a.Country, b => b.Id, arg2));
            string expected = "SELECT `ID`, `CountryName` FROM Country WHERE NOT EXISTS (SELECT `Id` FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX') AND Country.`ID` = Company.`CountryID`)";
            string actual = arg.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        public void NotExists2Test()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>();
            arg.Where.And<Country>(q => !GXSql.Exists(arg2));
            string expected = "SELECT `ID`, `CountryName` FROM Country WHERE NOT EXISTS (SELECT `Id` FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX'))";
            string actual = arg.ToString();
            Assert.AreEqual(expected, actual);
        }

        [DataContract(Name = "Countries")]
        public class CountriesView
        {
            [DataMember(Name = "ID")]
            [AutoIncrement]
            public int Id
            {
                get;
                set;
            }

            public string CountryName
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Create simple view where data is retreaved from one table.
        /// </summary>
        [TestMethod]
        public void CreateSimpleViewTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.Select<Country>(q => new { q.Id, q.Name });
            arg.Where.And<Country>(q => !GXSql.Exists<Company, Country>(a => a.Country, b => b.Id, arg2));
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            string expected = "Create View Countries AS SELECT `ID`, `CountryName` FROM Country WHERE NOT EXISTS (SELECT `Id` FROM Company WHERE UPPER(Company.`Name`) LIKE('GURUX') AND Country.`ID` = Company.`CountryID`)";
            string actual = view.ToString();
            Assert.AreEqual(expected, actual);
        }

        [DataContract(Name = "Companies")]
        public class CompaniesView
        {
            [DataMember(Name = "ID")]
            [AutoIncrement]
            public int Id
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }

            public string CountryName
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Create simple view where data is retreaved from two table.
        /// </summary>
        [TestMethod]
        public void CreateSimpleViewTest2()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => new { q.Id, q.Name });
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(a => a.Country, b => b.Id);
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            string expected = "Create View Countries AS SELECT Company.`Id`, Company.`Name`, Country.`CountryName` FROM Company INNER JOIN Country ON Company.`CountryID`=Country.`ID`";
            string actual = view.ToString();
            Assert.AreEqual(expected, actual);
        }


        [DataContract(Name = "Companies")]
        public class CompaniesView2
        {
            [DataMember(Name = "ID")]
            [AutoIncrement]
            public int Id
            {
                get;
                set;
            }
            public string CompanyName
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Create simple view where data is map from two table.
        /// </summary>
        [TestMethod]
        public void CreateSimpleViewTest3()
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => new { q.Id, q.Name });
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(a => a.Country, b => b.Id);
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            view.Maps.AddMap<CompaniesView2, Company>(t => t.CompanyName, s => s.Name);
            view.Maps.AddMap<CompaniesView2, Country>(t => t.Name, s => s.Name);
            string expected = "Create View Countries AS SELECT Company.`Id`, Company.`Name` AS `CompanyName`, Country.`CountryName` AS `Name` FROM Company INNER JOIN Country ON Company.`CountryID`=Country.`ID`";
            string actual = view.ToString();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        public void ExcludeUpdateTest()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Id, x.Guid, x.Time });
            args.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            Assert.AreEqual("UPDATE TestClass SET `Guid` = '00000000-0000-0000-0000-000000000000', `Time` = '" + dt.ToString(format) + "' WHERE `ID` = 2", args.ToString());
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        public void ExcludeUpdateTest2()
        {
            string format = "yyyy-MM-dd HH:mm:ss:fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t);
            args.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            Assert.AreEqual("UPDATE TestClass SET `Guid` = '00000000-0000-0000-0000-000000000000', `Time` = '" + dt.ToString(format) + "' WHERE `ID` = 2", args.ToString());
        }

        /// <summary>
        /// Exclude insert test.
        /// </summary>
        [TestMethod]
        public void ExcludeInsertTest()
        {
            TestClass t = new TestClass();
            t.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(t);
            args.Exclude<TestClass>(x => new { x.Time, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            Assert.AreEqual("INSERT INTO TestClass (`Guid`, `Text`) VALUES('00000000-0000-0000-0000-000000000000', 'Gurux')", args.ToString());
        }

        /// <summary>
        /// Append where test.
        /// </summary>
        [TestMethod]
        public void AppendWhereTest()
        {
            GXSelectArgs append = GXSelectArgs.Select<TestClass>(x => x.Guid);
            append.Where.And<TestClass>(q => GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.Append(append.Where);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`ID` IN (1, 2, 3)", arg.ToString());
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        public void FilterByTest()
        {
            TestClass filter = new TestClass();
            filter.Text2 = "More";
            filter.Text3 = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.FilterBy(filter);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE ((TestClass.`SimpleText` = 'More') AND (TestClass.`Text3` = 'Gurux') AND (TestClass.`Status` = 0))", arg.ToString());
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        public void FilterByTest2()
        {
            TestClass filter = new TestClass();
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.FilterBy(filter);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Status` = 0", arg.ToString());
        }

        /// <summary>
        /// Filter by status.
        /// </summary>
        [TestMethod]
        public void FilterByStatus()
        {
            TestClass filter = new TestClass();
            filter.Status = State.Failed;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.FilterBy(filter);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Status` = 200", arg.ToString());
        }

        /// <summary>
        /// Filter by date-time.
        /// </summary>
        [TestMethod]
        public void FilterByDateTime()
        {
            TestClass filter = new TestClass();
            filter.Status = State.OK;
            filter.Time = new DateTime(2020, 4, 1);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.FilterBy(filter);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Time` >= '2020-04-01 00.00.00.000'", arg.ToString());
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        public void FindEmptyGuid()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Guid.Equals(null));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Guid` IS NULL", arg.ToString());
            arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Guid.Equals(null));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Guid` IS NULL", arg.ToString());
        }
        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        public void FindEmptyDateTime()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Time == null);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Time` IS NULL", arg.ToString());
            arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Time.Equals(null));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Time` IS NULL", arg.ToString());
        }

        /// <summary>
        /// Find Empty guid values.
        /// </summary>
        [TestMethod]
        public void EmptyGuidTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => q.Guid.Equals(null) || q.Guid.Equals(Guid.Empty));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (TestClass.`Guid` IS NULL) OR (TestClass.`Guid`='00000000-0000-0000-0000-000000000000')", arg.ToString());
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        public void EmptyDateTimeTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => q.Time.Equals(null) || q.Time.Equals(DateTime.MinValue));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE (TestClass.`Time` IS NULL) OR (TestClass.`Time`='0001-01-01 00.00.00.000')", arg.ToString());
        }

        /// <summary>
        /// Guid in test.
        /// </summary>
        [TestMethod]
        public void GuidInTest()
        {
            List<Guid> list = new List<Guid>();
            list.Add(Guid.Empty);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Guid));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Guid` IN ('00000000-0000-0000-0000-000000000000')", arg.ToString());
        }


        /// <summary>
        /// DateTime in test.
        /// </summary>
        [TestMethod]
        public void DateTimeInTest()
        {
            List<DateTime> list = new List<DateTime>();
            list.Add(DateTime.MinValue);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Time));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Time` IN ('1.1.0001 0.00.00')", arg.ToString());
        }

        /// <summary>
        /// string in test.
        /// </summary>
        [TestMethod]
        public void StringInTest()
        {
            List<string> list = new List<string>();
            list.Add("Gurux");
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Text));
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE TestClass.`Text` IN ('Gurux')", arg.ToString());
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        public void ExcludeSelectTest()
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<TestClass>();
            args.Columns.Exclude<TestClass>(x => new { x.Id, x.Time, x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            Assert.AreEqual("SELECT `Guid` FROM TestClass", args.ToString());
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        public void ExcludeSelectTest2()
        {
            GXSelectArgs args = GXSelectArgs.Select<TestClass>(q => new { q.Guid, q.Text });
            args.Columns.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            Assert.AreEqual("SELECT `Guid` FROM TestClass", args.ToString());
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public void IsEmptyTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.IsEmpty(q), a => a.Id == 1);
            Assert.AreEqual("SELECT COUNT(1) WHERE NOT EXISTS (SELECT 1 FROM TestClass WHERE TestClass.`ID` = 1)", arg.ToString());
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public void IsEmpty2Test()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), a => a.Id == 1);
            Assert.AreEqual("SELECT COUNT(1) FROM TestClass WHERE TestClass.`ID` = 1", arg.ToString());
        }

        /// <summary>
        /// Find rows where Id count is greater than 1.
        /// </summary>
        [TestMethod]
        public void WhereCountTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid);
            arg.Where.And<TestClass>(q => GXSql.Count(q.Id) > 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE COUNT(TestClass.`ID`) > 1", arg.ToString());
        }

        /// <summary>
        /// Find rows where Id count is equal to 1.
        /// </summary>
        [TestMethod]
        public void WhereCountTest2()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid);
            arg.Where.And<TestClass>(q => GXSql.Count(q.Id) == 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE COUNT(TestClass.`ID`) = 1", arg.ToString());
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        public void HavingTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid);
            arg.GroupBy.Add<TestClass>(g => g.Text);
            arg.Having.And<TestClass>(q => GXSql.Count(q.Id) > 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text` HAVING COUNT(TestClass.`ID`) > 1", arg.ToString());
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        public void HavingTest2()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid);
            arg.GroupBy.Add<TestClass>(q => new { q.Text, q.Status });
            arg.Having.And<TestClass>(q => GXSql.Count(1) > 1);
            Assert.AreEqual("SELECT `Guid` FROM TestClass GROUP BY `Text`, `Status` HAVING COUNT(1) > 1", arg.ToString());
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public void CopyTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Name);
            GXInsertArgs args = GXInsertArgs.Insert<Country>(arg2);
            Assert.AreEqual("INSERT INTO Country (`CountryName`) SELECT `CountryName` FROM Country", args.ToString());
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public void CopyTest2()
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Country>();
            GXInsertArgs args = GXInsertArgs.Insert<Country>(arg2);
            Assert.AreEqual("INSERT INTO Country (`CountryName`) SELECT `CountryName` FROM Country", args.ToString());
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public void CopyTest3()
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Company>();
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2);
            Assert.AreEqual("INSERT INTO Company (`Name`, `CountryID`) SELECT `Name`, `CountryID` FROM Company", args.ToString());
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        public void CopyTest4()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => new { q.Name, q.Country });
            GXInsertArgs args = GXInsertArgs.Insert<Company2>(arg2, q => new { q.Name, q.Country });
            Assert.AreEqual("INSERT INTO Company2 (`Name`, `CountryID`) SELECT `Name`, `CountryID` FROM Company", args.ToString());
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        public void CopyTest5()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company2>(q => new { q.Name, q.Country });
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2, q => new { q.Name, q.Country });
            Assert.AreEqual("INSERT INTO Company (`Name`, `CountryID`) SELECT `Name`, `CountryID` FROM Company2", args.ToString());
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public void CopyTest6()
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Company>();
            arg2.Joins.AddInnerJoin<Company, Country>(q => q.Country, x => x.Id);
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2);
            Assert.AreEqual("INSERT INTO Company (`Name`, `CountryID`) SELECT Company.`Name`, Company.`CountryID` FROM Company INNER JOIN Country ON Company.`CountryID`=Country.`ID`", args.ToString());
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public void InsertSelectedValueTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id);
            Company comp = new Company() { Name = "Gurux" };
            GXInsertArgs args = GXInsertArgs.Insert<Company>(comp, q => new { q.Name, q.Country });
            args.Add<Company>(arg2, q => q.Country);
            Assert.AreEqual("INSERT INTO Company (`Name`, `CountryID`) SELECT 'Gurux', `ID` FROM Country", args.ToString());
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public void InsertSelectedValue2Test()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField });
            args.Add<Company2>(arg2, q => q.Country);
            Assert.AreEqual("INSERT INTO Company2 (`Name`, `ExtraField`, `CountryID`) SELECT 'Gurux', 'Extra', `ID` FROM Country", args.ToString());
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public void InsertSelectedValue3Test()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField });
            args.Add<Company2>(arg2, q => q.Country);
            Assert.AreEqual("INSERT INTO Company2 (`Name`, `ExtraField`, `CountryID`) SELECT 'Gurux', 'Extra', `ID` FROM Country", args.ToString());
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public void InsertSelectedValue4Test()
        {
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField });
            Assert.AreEqual("INSERT INTO Company2 (`Name`, `ExtraField`) VALUES('Gurux', 'Extra')", args.ToString());
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public void InsertSelectedValue5Test()
        {
            List<string> list = new List<string>();
            list.Add("Finland");
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, q => list.Contains(q.Name));
            Company comp = new Company() { Name = "Gurux" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => q.Name);
            args.Add<Company>(arg2, q => q.Country);
            Assert.AreEqual("INSERT INTO Company (`Name`, `CountryID`) SELECT 'Gurux', `ID` FROM Country WHERE Country.`CountryName` IN ('Finland')", args.ToString());
        }

        /// <summary>
        /// The purpose of this test is check that old column is overrided 
        /// and CountryID is not twice.
        /// </summary>
        [TestMethod]
        public void UpdateInsertParameterTest()
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp);
            args.Add<Company2>(arg2, q => q.Country);
            Assert.AreEqual("INSERT INTO Company2 (`Name`, `CountryID`, `ExtraField`) SELECT 'Gurux', `ID` FROM Country, 'Extra'", args.ToString());
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        public void UpdateSelectedValueTest()
        {
            GXSelectArgs sel = GXSelectArgs.Select<Country>(q => q.Id, q => q.Id == 1);
            Company comp = new Company() { Name = "Gurux" };
            GXUpdateArgs update = GXUpdateArgs.Update<Company>(comp, q => q.Name);
            update.Where.And<Company>(a => GXSql.Exists(sel));
            Assert.AreEqual("UPDATE Company SET `Name` = 'Gurux' WHERE EXISTS (SELECT `ID` FROM Country WHERE Country.`ID` = 1)", update.ToString());
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        public void UpdateParameterCollectionTest()
        {
            User2 user = new User2() { Id = 2, Name = "User1" };
            UserGroup2 ug = new UserGroup2() { Id = 1, Name = "Gurux" };
            ug.Users = new User2[] { user };
            GXInsertArgs i = GXInsertArgs.Insert(ug);
            Assert.AreEqual("INSERT INTO UserToUserGroup (`UserId`, `GroupId`) VALUES(2, 1)", i.ToString());
            i = GXInsertArgs.Insert(ug, q => q.Users);
            Assert.AreEqual("INSERT INTO UserToUserGroup (`UserId`, `GroupId`) VALUES(2, 1)", i.ToString());
        }

        /// <summary>
        /// Data quota where test.
        /// </summary>
        [TestMethod]
        public void DataQuotaWhereTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), q => q.Text == "Gurux'");
            Assert.AreEqual("SELECT COUNT(1) FROM TestClass WHERE TestClass.`Text` = 'Gurux\''", arg.ToString());
        }

        /// <summary>
        /// Data quota insert test.
        /// </summary>
        [TestMethod]
        public void DataQuotaInsertTest()
        {
            User2 user = new User2() { Name = "Gurux'" };
            GXInsertArgs i = GXInsertArgs.Insert(user);
            Assert.AreEqual("INSERT INTO User2 (`Name`) VALUES('Gurux''')", i.ToString());
        }

        /// <summary>
        /// Data quota update test.
        /// </summary>
        [TestMethod]
        public void DataQuotaUpdateTest()
        {
            User2 user = new User2() { Id = 2, Name = "Gurux'" };
            GXUpdateArgs args = GXUpdateArgs.Update(user, x => x.Name);
            Assert.AreEqual("UPDATE User2 SET `Name` = 'Gurux''' WHERE `Id` = 2", args.ToString());
        }

        /// <summary>
        /// Data quota delete test.
        /// </summary>
        [TestMethod]
        public void DataQuotaDeleteTest()
        {
            GXDeleteArgs args = GXDeleteArgs.Delete<TestClass>(q => q.Text == "Gurux'");
            Assert.AreEqual("DELETE FROM TestClass WHERE TestClass.`Text` = 'Gurux''", args.ToString());
        }

        /// <summary>
        /// Sum test.
        /// </summary>
        [TestMethod]
        public void SumTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Sum(q.DoubleTest));
            Assert.AreEqual("SELECT SUM(`DoubleTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Sum columns test.
        /// </summary>
        [TestMethod]
        public void SumColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Sum(new { q.DoubleTest, q.FloatTest }));
            Assert.AreEqual("SELECT SUM(`DoubleTest` + `FloatTest`) FROM TestClass", arg.ToString());
        }


        /// <summary>
        /// Min test.
        /// </summary>
        [TestMethod]
        public void MinTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Min(q.DoubleTest));
            Assert.AreEqual("SELECT MIN(`DoubleTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Min columns test.
        /// </summary>
        [TestMethod]
        public void MinColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Min(new { q.DoubleTest, q.FloatTest }));
            Assert.AreEqual("SELECT MIN(`DoubleTest` + `FloatTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Max test.
        /// </summary>
        [TestMethod]
        public void MaxTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Max(q.DoubleTest));
            Assert.AreEqual("SELECT MAX(`DoubleTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Max columns test.
        /// </summary>
        [TestMethod]
        public void MaxColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Max(new { q.DoubleTest, q.FloatTest }));
            Assert.AreEqual("SELECT MAX(`DoubleTest` + `FloatTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Average test.
        /// </summary>
        [TestMethod]
        public void AverageTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Avg(q.DoubleTest));
            Assert.AreEqual("SELECT AVG(`DoubleTest`) FROM TestClass", arg.ToString());
        }

        /// <summary>
        /// Average columns test.
        /// </summary>
        [TestMethod]
        public void AverageColumnsTest()
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Avg(new { q.DoubleTest, q.FloatTest }));
            Assert.AreEqual("SELECT AVG(`DoubleTest` + `FloatTest`) FROM TestClass", arg.ToString());
        }       
    }
}
