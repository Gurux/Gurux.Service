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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gurux.Service.Orm;

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
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q));
            Assert.AreEqual("SELECT COUNT(1) FROM TestClass", arg.ToString());
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
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Time);
            arg.Where.And<TestClass>(q => q.Time > DateTime.MinValue && q.Time < DateTime.MaxValue);
            Assert.AreEqual("SELECT `Time` FROM TestClass WHERE (TestClass.`Time` > '0001-01-01 00:00:00') AND (TestClass.`Time` < '9999-12-31 23:59:59')", arg.ToString());
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
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid);
            arg.Where.And<TestClass>(q => t);
            Assert.AreEqual("SELECT `Guid` FROM TestClass WHERE `ID` = 1", arg.ToString());
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
            string mikko = t.Text;
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
            string mikko = t.Text;
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
        public void InsertTest()
        {
            TestClass t = new TestClass();
            t.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(t, x => new { x.Text, x.Guid });
            Assert.AreEqual("INSERT INTO TestClass (`Text`, `Guid`) VALUES('Gurux', '00000000000000000000000000000000')", args.ToString());
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public void UpdateTest()
        {
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Id, x.Guid, x.Time });
            Assert.AreEqual("UPDATE TestClass SET `ID` = 2, `Guid` = '00000000000000000000000000000000', `Time` = '2014-01-02 00:00:00' WHERE `ID` = 2", args.ToString());
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
    }
}
