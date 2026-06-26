using Gurux.Service.Orm.Enums;

namespace Gurux.Service_Simple_Unit_Test
{
    /// <summary>
    /// Postgre SQL test class.
    /// </summary>
    [TestClass]
    public class PostgreSqlTest : BaseTest
    {
        public PostgreSqlTest() : base(DatabaseType.PostgreSQL) { }

        /// <inheritdoc/>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Guid\", \"Time\", \"Text\", \"SimpleText\", \"Text3\", \"Text4\", \"BooleanTest\", \"IntTest\", \"DoubleTest\", \"FloatTest\", \"Span\", \"Object\", \"Status\" FROM \"TestClass\"")]
        public override void SelectTest(string expected)
        {
            base.SelectTest(expected);
        }

        /// <summary>
        /// Select 1test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT 1 FROM \"TestClass\"")]
        public override void Select1Test(string expected)
        {
            base.Select1Test(expected);
        }

        /// <summary>
        /// Select all by id test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Text\" FROM \"TestIDClass\" WHERE \"ID\" = 1")]
        public override void GetByIdTest(string expected)
        {
            base.GetByIdTest(expected);
        }

        /// <summary>
        /// Select only part of columns.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Text\" FROM \"TestIDClass\" WHERE \"ID\" = 1")]
        public override void GetPartTest(string expected)
        {
            base.GetPartTest(expected);
        }

        /// <summary>
        /// Select id by id test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestIDClass\" WHERE \"ID\" = 1")]
        public override void GetByIdColumnsTest(string expected)
        {
            base.GetByIdColumnsTest(expected);
        }

        /// <summary>
        /// Relation where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Id\" AS \"DG.Id\" FROM \"DeviceGroup3\" \"DG\" WHERE \"Id\" = 1")]
        public override void WhereByReferenceTest(string expected)
        {
            base.WhereByReferenceTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM \"TestClass\"")]
        public override void CountTest(string expected)
        {
            base.CountTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(\"ID\") FROM \"TestClass\"")]
        public override void CountTest2(string expected)
        {
            base.CountTest2(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(\"Supplier\".\"SupplierID\") FROM \"Supplier\" INNER JOIN \"Product\" ON \"Supplier\".\"SupplierID\" = \"Product\".\"TargetID\"")]
        public override void CountTest3(string expected)
        {
            base.CountTest3(expected);
        }

        /// <summary>
        /// Distinct count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(DISTINCT \"Supplier\".\"SupplierID\") FROM \"Supplier\" INNER JOIN \"Product\" ON \"Supplier\".\"SupplierID\" = \"Product\".\"TargetID\"")]
        public override void DistinctCountTest(string expected)
        {
            base.DistinctCountTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void CountWhereTest(string expected)
        {
            base.CountWhereTest(expected);
        }

        /// <summary>
        /// Select single column test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Text\" FROM \"TestClass\"")]
        public override void SelectSingleColumnTest(string expected)
        {
            base.SelectSingleColumnTest(expected);
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\", \"Text\" FROM \"TestClass\"")]
        public override void SelectColumnsTest(string expected)
        {
            base.SelectColumnsTest(expected);
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Company\".\"Name\", \"Country\".\"CountryName\" FROM \"Company\" INNER JOIN \"Country\" ON \"Company\".\"CountryID\" = \"Country\".\"ID\"")]
        public override void SelectSubItemsTest(string expected)
        {
            base.SelectSubItemsTest(expected);
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Product2\".\"Product2ID\", \"Supplier\".\"SupplierID\" FROM \"Product2\" INNER JOIN \"Supplier\" ON \"Product2\".\"Target2ID\" = \"Supplier\".\"SupplierID\"")]
        public override void SelectSubItemsTest2(string expected)
        {
            base.SelectSubItemsTest2(expected);
        }

        /// <summary>
        /// Limit test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" ORDER BY \"Guid\" OFFSET 1 ROWS FETCH NEXT 2 ROWS ONLY")]
        public override void LimitTest(string expected)
        {
            base.LimitTest(expected);
        }

        /// <summary>
        /// Select Distinct test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT DISTINCT \"Guid\" FROM \"TestClass\"")]
        public override void SelectDistinctTest(string expected)
        {
            base.SelectDistinctTest(expected);
        }

        /// <summary>
        /// Select two tables test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"TestClass2\".\"Name\", \"TestClass\".\"Guid\" FROM \"TestClass2\" RIGHT OUTER JOIN \"TestClass\" ON \"TestClass2\".\"ParentID\" = \"TestClass\".\"ID\"")]
        public override void SelectTablesTest(string expected)
        {
            base.SelectTablesTest(expected);
        }

        /// <summary>
        /// Select all columns from one table when multiple tables are used.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"TestClass2\".\"Id\", \"TestClass2\".\"ParentID\", \"TestClass2\".\"Name\" FROM \"TestClass2\" RIGHT OUTER JOIN \"TestClass\" ON \"TestClass2\".\"ParentID\" = \"TestClass\".\"ID\"")]
        public override void SelectOneTableFromManyTest(string expected)
        {
            base.SelectOneTableFromManyTest(expected);
        }


        /// <summary>
        /// Delete by primary key test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void DeleteByPrimaryKeyTest(string expected)
        {
            base.DeleteByPrimaryKeyTest(expected);
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"GuidTestClass\" WHERE \"Id\" = '{0}'::UUID")]
        public override void DeleteByGuidPrimaryKeyTest(string expected)
        {
            base.DeleteByGuidPrimaryKeyTest(expected);
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        [DataRow(new string[]
           {
            "550e8400-e29b-41d4-a716-446655440000",
            "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
            }, "DELETE FROM \"GuidTestClass\" WHERE \"Id\" IN('{0}'::uuid, '{1}'::uuid)")]
        public override void DeleteByGuidRangeTest(string[] guids, string expected)
        {
            base.DeleteByGuidRangeTest(guids, expected);
        }

        /// <summary>
        /// Delete using where.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"TestClass\" WHERE \"Text\" = 'Gurux'")]
        public override void DeleteByWhereTest(string expected)
        {
            base.DeleteByWhereTest(expected);
        }

        /// <summary>
        /// Delete using select.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"TestClass\" WHERE EXISTS (SELECT 1 FROM \"TestClass\" WHERE \"Text\" = 'Gurux')")]
        public override void DeleteBySelectTest(string expected)
        {
            base.DeleteBySelectTest(expected);
        }

        /// <summary>
        /// Delete using list.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"Child2\" WHERE \"Parent\" IN (1)")]
        public override void DeleteByListTest(string expected)
        {
            base.DeleteByListTest(expected);
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        [DataRow("ID,Guid,Time,Text,SimpleText,Text3,Text4,BooleanTest,IntTest,DoubleTest,FloatTest,Span,Object,Status")]
        public override void GetFieldsTest(string expected)
        {
            base.GetFieldsTest(expected);
        }

        /// <summary>
        /// Right join test
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"TestClass\".\"ID\", \"TestClass\".\"Guid\", \"TestClass\".\"Time\", \"TestClass\".\"Text\", \"TestClass\".\"SimpleText\", \"TestClass\".\"Text3\", \"TestClass\".\"Text4\", \"TestClass\".\"BooleanTest\", \"TestClass\".\"IntTest\", \"TestClass\".\"DoubleTest\", \"TestClass\".\"FloatTest\", \"TestClass\".\"Span\", \"TestClass\".\"Object\", \"TestClass\".\"Status\", \"TestClass2\".\"Id\", \"TestClass2\".\"ParentID\", \"TestClass2\".\"Name\" FROM \"TestClass2\" RIGHT OUTER JOIN \"TestClass\" ON \"TestClass2\".\"ParentID\" = \"TestClass\".\"ID\"")]
        public override void RightJoinTest(string expected)
        {
            base.RightJoinTest(expected);
        }

        /// <summary>
        /// Left join test
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"TestClass\".\"ID\", \"TestClass\".\"Guid\", \"TestClass\".\"Time\", \"TestClass\".\"Text\", \"TestClass\".\"SimpleText\", \"TestClass\".\"Text3\", \"TestClass\".\"Text4\", \"TestClass\".\"BooleanTest\", \"TestClass\".\"IntTest\", \"TestClass\".\"DoubleTest\", \"TestClass\".\"FloatTest\", \"TestClass\".\"Span\", \"TestClass\".\"Object\", \"TestClass\".\"Status\", \"TestClass2\".\"Id\", \"TestClass2\".\"ParentID\", \"TestClass2\".\"Name\" FROM \"TestClass2\" LEFT OUTER JOIN \"TestClass\" ON \"TestClass2\".\"ParentID\" = \"TestClass\".\"ID\"")]
        public override void LeftJoinTest(string expected)
        {
            base.LeftJoinTest(expected);
        }

        /// <summary>
        /// Full join test
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"TestClass\".\"ID\", \"TestClass\".\"Guid\", \"TestClass\".\"Time\", \"TestClass\".\"Text\", \"TestClass\".\"SimpleText\", \"TestClass\".\"Text3\", \"TestClass\".\"Text4\", \"TestClass\".\"BooleanTest\", \"TestClass\".\"IntTest\", \"TestClass\".\"DoubleTest\", \"TestClass\".\"FloatTest\", \"TestClass\".\"Span\", \"TestClass\".\"Object\", \"TestClass\".\"Status\", \"TestClass2\".\"Id\", \"TestClass2\".\"ParentID\", \"TestClass2\".\"Name\" FROM \"TestClass2\" FULL OUTER JOIN \"TestClass\" ON \"TestClass2\".\"ParentID\" = \"TestClass\".\"ID\"")]
        public override void FullJoinTest(string expected)
        {
            base.FullJoinTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void WhereSimpleTest(string expected)
        {
            base.WhereSimpleTest(expected);
        }

        /// <summary>
        /// Select Time where Datetime is bigger Min date time and Datetime is smaller than max date time and text is not empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Time\" FROM \"TestClass\" WHERE \"Time\" > '0001-01-01 00:00:00.000' AND \"Time\" < '9999-12-31 23:59:59.999'")]
        public override void WhereDateTimeTest(string expected)
        {
            base.WhereDateTimeTest(expected);
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Text\" FROM \"TestClass\" WHERE (\"Text\" <> '') AND (\"Text\" IS NOT NULL)")]
        public override void WhereStringEmptyTest(string expected)
        {
            base.WhereStringEmptyTest(expected);
        }

        /// <summary>
        /// Select Text where string is empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Text\" FROM \"TestClass\" WHERE (\"Text\" IS NULL OR \"Text\" = '')")]
        public override void WhereStringIsNullOrEmptyTest(string expected)
        {
            base.WhereStringIsNullOrEmptyTest(expected);
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Text\" FROM \"TestClass\" WHERE (\"Text\" IS NOT NULL AND \"Text\" <> '')")]
        public override void WhereStringNotIsNullOrEmptyTest(string expected)
        {
            base.WhereStringNotIsNullOrEmptyTest(expected);
        }

        /// <summary>
        /// Select Guid where Enum is string.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Status\" = 'OK'")]
        public override void WhereEnumTest(string expected)
        {
            base.WhereEnumTest(expected);
        }

        /// <summary>
        /// Select Guid where Enum is saved as int.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Status\" = 100")]
        public override void WhereEnumAsIntTest(string expected)
        {
            base.WhereEnumAsIntTest(expected);
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void WhereClassTest(string expected)
        {
            base.WhereClassTest(expected);
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void WhereClassTest2(string expected)
        {
            base.WhereClassTest2(expected);
        }

        /// <summary>
        /// Select Guid where class array is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN(1, 2)")]
        public override void WhereClassArrayTest(string expected)
        {
            base.WhereClassArrayTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void WhereSimple2Test(string expected)
        {
            base.WhereSimple2Test(expected);
        }

        /// <summary>
        /// Select all by string test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Text\" FROM \"TestIDClass\" WHERE \"Text\" = 'Gurux'")]
        public override void WhereExactString(string expected)
        {
            base.WhereExactString(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('Gurux%')")]
        public override void WhereStartsWithTest(string expected)
        {
            base.WhereStartsWithTest(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('Gurux%')")]
        public override void WhereStartsWith2Test(string expected)
        {
            base.WhereStartsWith2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('%Gurux')")]
        public override void WhereEndsWithTest(string expected)
        {
            base.WhereEndsWithTest(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('%Gurux%')")]
        public override void WhereContainsTest(string expected)
        {
            base.WhereContainsTest(expected);
        }

        /// <summary>
        /// Select Guid where Text contains upper Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('%GURUX%')")]
        public override void WhereContainsUpperTest(string expected)
        {
            base.WhereContainsUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('GURUX%')")]
        public override void WhereStartsWithUpperTest(string expected)
        {
            base.WhereStartsWithUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with upper Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('%GURUX')")]
        public override void WhereEndsWithUpperTest(string expected)
        {
            base.WhereEndsWithUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where list contains Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" IN ('Gurux')")]
        public override void WhereContains2Test(string expected)
        {
            base.WhereContains2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text contains with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" LIKE('%Gurux%')")]
        public override void WhereContains3Test(string expected)
        {
            base.WhereContains3Test(expected);
        }

        /// <summary>
        /// Select Guid where list contains -1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, -1)")]
        public override void WhereContains5Test(string expected)
        {
            base.WhereContains5Test(expected);
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IN ('00000000-0000-0000-0000-000000000000'::uuid)")]
        public override void WhereContainsListGuidTest(string expected)
        {
            base.WhereContainsListGuidTest(expected);
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        [DataRow(
        new string[]
           {
            "550e8400-e29b-41d4-a716-446655440000",
            "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
            }, "SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IN ({0})")]
        public override void WhereContainsIEnumerableGuidTest(string[] guids, string expected)
        {
            expected = string.Format(expected, string.Join(", ", guids.Select(it => $"'{it}'::uuid")));
            base.WhereContainsIEnumerableGuidTest(guids, expected);
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        [DataRow(
        new string[]
           {
            "550e8400-e29b-41d4-a716-446655440000",
            "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
            }, "SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IN ({0})")]
        public override void WhereContainsListGuidTest(string[] guids, string expected)
        {
            expected = string.Format(expected, string.Join(", ", guids.Select(it => $"'{it}'::uuid")));
            base.WhereContainsListGuidTest(guids, expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE UPPER(\"Text\") LIKE('GURUX')")]
        public override void WhereEqualsTest(string expected)
        {
            base.WhereEqualsTest(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE UPPER(\"Text\") LIKE('GURUX')")]
        public override void WhereEquals2Test(string expected)
        {
            base.WhereEquals2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" = 'Gurux'")]
        public override void WhereEquals3Test(string expected)
        {
            base.WhereEquals3Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" = 1 AND UPPER(\"Text\") LIKE('GURUX')")]
        public override void WhereEquals4Test(string expected)
        {
            base.WhereEquals4Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE UPPER(\"Text\") LIKE('GURUX') AND \"ID\" = 1")]
        public override void WhereEquals5Test(string expected)
        {
            base.WhereEquals5Test(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" = 1 OR \"ID\" = 2 OR \"ID\" = 3")]
        public override void WhereOrTest(string expected)
        {
            base.WhereOrTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = -1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" = -1919693511")]
        public override void WhereMinusTest(string expected)
        {
            base.WhereMinusTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE (\"ID\" = 1 OR \"ID\" = 2) OR (\"ID\" = 3)")]
        public override void WhereOr2Test(string expected)
        {
            base.WhereOr2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 or text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE (\"ID\" = 1) OR (\"Text\" LIKE('Gurux%'))")]
        public override void WhereOr3Test(string expected)
        {
            base.WhereOr3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" > 1 AND \"ID\" <> 2")]
        public override void WhereAndTest(string expected)
        {
            base.WhereAndTest(expected);
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE (\"ID\" > 1) AND (\"ID\" <> 2)")]
        public override void WhereAnd2Test(string expected)
        {
            base.WhereAnd2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SqlInTest(string expected)
        {
            base.SqlInTest(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SqlInTest1(string expected)
        {
            base.SqlInTest1(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SqlInTest1_1(string expected)
        {
            base.SqlInTest1_1(expected);
        }


        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SqlInTest2(string expected)
        {
            base.SqlInTest2(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IN ('00000000-0000-0000-0000-000000000000'::uuid)")]
        public override void SqlInTest3(string expected)
        {
            base.SqlInTest3(expected);
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" NOT IN (1, 2, 3)")]
        public override void SqlNotInTest(string expected)
        {
            base.SqlNotInTest(expected);
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" NOT IN (1, 2, 3)")]
        public override void SqlNotInTest2(string expected)
        {
            base.SqlNotInTest2(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" NOT IN ('00000000-0000-0000-0000-000000000000'::uuid)")]
        public override void SqlNotInTest3(string expected)
        {
            base.SqlNotInTest3(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" ORDER BY \"ID\", \"Guid\"")]
        public override void SqlOrderTest(string expected)
        {
            base.SqlOrderTest(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" ORDER BY \"ID\", \"Guid\"")]
        public override void SqlOrder2Test(string expected)
        {
            base.SqlOrder2Test(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" ORDER BY \"ID\"")]
        public override void SqlOrder3Test(string expected)
        {
            base.SqlOrder3Test(expected);
        }

        /// <summary>
        /// Order desc test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" ORDER BY \"ID\" DESC")]
        public override void SqlOrderDescTest(string expected)
        {
            base.SqlOrderDescTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Country\" (\"CountryName\") VALUES('Finland')")]
        public override void InsertAllTest(string expected)
        {
            base.InsertAllTest(expected);
        }

        [TestMethod]
        [DataRow("INSERT INTO \"Country\" (\"CountryName\") VALUES('Finland')")]
        public override void InsertNameOnlyTest(string expected)
        {
            base.InsertNameOnlyTest(expected);
        }

        /// <summary>
        /// Insert range test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Parameter2\" (\"Name\", \"Value\", \"DeviceID\") VALUES('Name1', 'Value1', 0), ('Name2', 'Value2', 0), ('Name3', 'Value3', 0)", "INSERT INTO \"Parameter2\" (\"Name\", \"Value\") VALUES('Name1', 'Value1'), ('Name2', 'Value2'), ('Name3', 'Value3')")]
        public override void InsertRangeTest(string expected, string expected2)
        {
            base.InsertRangeTest(expected, expected2);
        }

        /// <summary>
        /// Insert emptyrange test.
        /// </summary>
        [TestMethod]
        [DataRow("")]
        public override void InsertEmptyRangeTest(string expected)
        {
            base.InsertEmptyRangeTest(expected);
        }


        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"TestClass\" (\"Text\", \"Guid\") VALUES('Gurux', '00000000-0000-0000-0000-000000000000'::uuid)")]
        public override void InsertTest(string expected)
        {
            base.InsertTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Supplier\" (\"Text\") VALUES('Gurux') INSERT INTO \"Product2\" (\"Text\", \"Target2ID\") VALUES('Virtual-serial', 0)")]
        public override void InsertTest2(string expected)
        {
            base.InsertTest2(expected);
        }

        /// <summary>
        /// Create table test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"NullableTestClass\" (\"Id\", \"Active\", \"Text\") VALUES('{0}'::uuid, FALSE, 'Gurux')")]
        public override void CreateNullableTableTest(string expected)
        {
            base.CreateNullableTableTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"NullableTestClass\" (\"Id\", \"Active\", \"Text\") VALUES('{0}'::uuid, TRUE, 'Gurux')")]
        public override void InsertNullableTest(string expected)
        {
            base.InsertNullableTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Guid\" = '{0}'::uuid, \"Time\" = '{1}' WHERE \"ID\" = 2")]
        public override void UpdateTest(string expected)
        {
            base.UpdateTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"GuidTestClass\" SET \"Time\" = '{0}' WHERE \"Id\" = '{1}'::uuid")]
        public override void UpdateTest2(string expected)
        {
            base.UpdateTest2(expected);
        }

        /// <summary>
        /// Update using where.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Guid\" = '00000000-0000-0000-0000-000000000000'::uuid, \"Time\" = '2014-01-02 00:00:00.000' WHERE \"Text\" = 'Gurux'")]
        public override void UpdateWhereTest(string expected)
        {
            base.UpdateWhereTest(expected);
        }

        /// <summary>
        /// Update default null test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"NullableTestClass\" SET \"Active\" = TRUE, \"Text\" = NULL WHERE \"Id\" = '{0}'::uuid", "UPDATE \"NullableTestClass\" SET \"Active\" = TRUE, \"Text\" = NULL WHERE \"Id\" = '{0}'::uuid", "UPDATE \"NullableTestClass\" SET \"Active\" = FALSE, \"Text\" = NULL WHERE \"Id\" = '{0}'::uuid")]
        public override void UpdateDefaultNullTest(string expected, string expected2, string expected3)
        {
            base.UpdateDefaultNullTest(expected, expected2, expected3);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Time\" = 1388620800 WHERE \"ID\" = 1")]
        public override void EpochTimeFormatTest(string expected)
        {
            base.EpochTimeFormatTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("TestClass")]
        public override void TableNameTest(string expected)
        {
            base.TableNameTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("gx_TestClass")]
        public override void TableNamePrefixTest(string expected)
        {
            base.TableNamePrefixTest(expected);
        }

        /// <summary>
        /// Where string is null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Guid\", \"Time\", \"Text\", \"SimpleText\", \"Text3\", \"Text4\", \"BooleanTest\", \"IntTest\", \"DoubleTest\", \"FloatTest\", \"Span\", \"Object\", \"Status\" FROM \"TestClass\" WHERE \"Text\" IS NULL",
            "SELECT \"ID\", \"Guid\", \"Time\", \"Text\", \"SimpleText\", \"Text3\", \"Text4\", \"BooleanTest\", \"IntTest\", \"DoubleTest\", \"FloatTest\", \"Span\", \"Object\", \"Status\" FROM \"TestClass\" WHERE \"Text\" IS NULL")]
        public override void WhereStringIsNullTest(string expected, string expected2)
        {
            base.WhereStringIsNullTest(expected, expected2);
        }

        /// <summary>
        /// Where string is empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"Guid\", \"Time\", \"Text\", \"SimpleText\", \"Text3\", \"Text4\", \"BooleanTest\", \"IntTest\", \"DoubleTest\", \"FloatTest\", \"Span\", \"Object\", \"Status\" FROM \"TestClass\" WHERE (\"Text\" IS NULL OR \"Text\" = '')")]
        public override void WhereStringIsEmptyTest(string expected)
        {
            base.WhereStringIsEmptyTest(expected);
        }


        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SqlIn2Test(string expected)
        {
            base.SqlIn2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Id\", \"Name\", \"CountryID\" FROM \"Company\" WHERE \"CountryID\" IN (SELECT \"ID\" FROM \"Country\" WHERE \"CountryName\" = 'Finland')")]
        public override void SqlIn3Test(string expected)
        {
            base.SqlIn3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" NOT IN (1, 2, 3)")]
        public override void SqlNotIn2Test(string expected)
        {
            base.SqlNotIn2Test(expected);
        }

        /// <summary>
        /// Select all countries where company exists. 
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE EXISTS (SELECT 1 FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX') AND \"Company\".\"CountryID\" = \"Country\".\"ID\")")]
        public override void ExistsTest(string expected)
        {
            base.ExistsTest(expected);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE EXISTS (SELECT \"Id\" FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX'))")]
        public override void Exists2Test(string expected)
        {
            base.Exists2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE EXISTS (SELECT 1 FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX'))")]
        public override void Exists3Test(string expected)
        {
            base.Exists3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE NOT EXISTS (SELECT \"Id\" FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX') AND \"Country\".\"ID\" = \"Company\".\"CountryID\")")]
        public override void NotExistsTest(string expected)
        {
            base.NotExistsTest(expected);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE NOT EXISTS (SELECT \"Id\" FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX'))")]
        public override void NotExists2Test(string expected)
        {
            base.NotExists2Test(expected);
        }

        /// <summary>
        /// Create simple view where data is retreaved from one table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View \"Countries\" AS SELECT \"ID\", \"CountryName\" FROM \"Country\" WHERE NOT EXISTS (SELECT \"Id\" FROM \"Company\" WHERE UPPER(\"Name\") LIKE('GURUX') AND \"Country\".\"ID\" = \"Company\".\"CountryID\")")]
        public override void CreateSimpleViewTest(string expected)
        {
            base.CreateSimpleViewTest(expected);
        }

        /// <summary>
        /// Create simple view where data is retreaved from two table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View \"Countries\" AS SELECT \"Company\".\"Id\", \"Company\".\"Name\", \"Country\".\"CountryName\" FROM \"Company\" INNER JOIN \"Country\" ON \"Company\".\"CountryID\" = \"Country\".\"ID\"")]
        public override void CreateSimpleViewTest2(string expected)
        {
            base.CreateSimpleViewTest2(expected);
        }

        /// <summary>
        /// Create simple view where data is map from two table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View \"Countries\" AS SELECT \"Company\".\"Id\", \"Company\".\"Name\" AS \"Companies.CompanyName\", \"Country\".\"CountryName\" AS \"Companies.Name\" FROM \"Company\" INNER JOIN \"Country\" ON \"Company\".\"CountryID\" = \"Country\".\"ID\"")]
        public override void CreateSimpleViewTest3(string expected)
        {
            base.CreateSimpleViewTest3(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Guid\" = '{0}'::uuid, \"Time\" = '{1}' WHERE \"ID\" = 2")]
        public override void ExcludeUpdateTest(string expected)
        {
            base.ExcludeUpdateTest(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Guid\" = '{0}'::uuid, \"Time\" = '{1}' WHERE \"ID\" = 2")]
        public override void ExcludeUpdateTest2(string expected)
        {
            base.ExcludeUpdateTest2(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"TestClass\" SET \"Guid\" = '{0}'::uuid, \"Time\" = '{1}' WHERE \"ID\" = 2")]
        public override void ExcludeUpdateTest3(string expected)
        {
            base.ExcludeUpdateTest3(expected);
        }

        /// <summary>
        /// Exclude insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"TestClass\" (\"Guid\", \"Text\") VALUES('00000000-0000-0000-0000-000000000000'::uuid, 'Gurux')")]
        public override void ExcludeInsertTest(string expected)
        {
            base.ExcludeInsertTest(expected);
        }

        /// <summary>
        /// Append where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"ID\" IN (1, 2, 3)")]
        public override void SelectGuidWhereTest(string expected)
        {
            base.SelectGuidWhereTest(expected);
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE (\"SimpleText\" = 'More') AND (\"Text3\" = 'Gurux') AND (\"Status\" = 0)")]
        public override void FilterByTest(string expected)
        {
            base.FilterByTest(expected);
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Status\" = 0")]
        public override void FilterByTest2(string expected)
        {
            base.FilterByTest2(expected);
        }

        /// <summary>
        /// Filter by status.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Status\" = 200")]
        public override void FilterByStatus(string expected)
        {
            base.FilterByStatus(expected);
        }

        /// <summary>
        /// Filter by date-time.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" >= '{0}'")]
        public override void FilterByDateTime(string expected)
        {
            base.FilterByDateTime(expected);
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IS NULL")]
        public override void FindEmptyGuid(string expected)
        {
            base.FindEmptyGuid(expected);
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IS NOT NULL")]
        public override void FindNotEmptyGuid(string expected)
        {
            base.FindNotEmptyGuid(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" IS NULL")]
        public override void FindEmptyDateTime(string expected)
        {
            base.FindEmptyDateTime(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" IS NULL")]
        public override void FindEmptyDateTime2(string expected)
        {
            base.FindEmptyDateTime2(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" IS NOT NULL")]
        public override void FindNotEmptyDateTime2(string expected)
        {
            base.FindNotEmptyDateTime2(expected);
        }

        /// <summary>
        /// Find Empty guid values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IS NULL OR \"Guid\" = '00000000-0000-0000-0000-000000000000'::UUID")]
        public override void EmptyGuidTest(string expected)
        {
            base.EmptyGuidTest(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" IS NULL OR \"Time\" = '{0}'")]
        public override void EmptyDateTimeTest(string expected)
        {
            base.EmptyDateTimeTest(expected);
        }

        /// <summary>
        /// Guid in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Guid\" IN ('00000000-0000-0000-0000-000000000000'::uuid)")]
        public override void GuidInTest(string expected)
        {
            base.GuidInTest(expected);
        }


        /// <summary>
        /// DateTime in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Time\" IN ('{0}')")]
        public override void DateTimeInTest(string expected)
        {
            base.DateTimeInTest(expected);
        }

        /// <summary>
        /// string in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" WHERE \"Text\" IN ('Gurux')")]
        public override void StringInTest(string expected)
        {
            base.StringInTest(expected);
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\"")]
        public override void ExcludeSelectTest(string expected)
        {
            base.ExcludeSelectTest(expected);
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\"")]
        public override void ExcludeSelectTest2(string expected)
        {
            base.ExcludeSelectTest2(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM \"TestClass\") THEN 1 ELSE 0 END AS IsEmpty")]
        public override void IsEmptyTest(string expected)
        {
            base.IsEmptyTest(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM \"TestClass\" WHERE \"ID\" = 1) THEN 1 ELSE 0 END AS IsEmpty")]
        public override void IsEmpty1Test(string expected)
        {
            base.IsEmpty1Test(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM \"TestClass\" WHERE \"ID\" = 1) THEN 1 ELSE 0 END AS IsEmpty")]
        public override void IsEmpty2Test(string expected)
        {
            base.IsEmpty2Test(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM \"TestClass\" WHERE \"ID\" = 1")]
        public override void IsEmpty3Test(string expected)
        {
            base.IsEmpty3Test(expected);
        }

        /// <summary>
        /// Find rows where Id count is greater than 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" GROUP BY \"Guid\" HAVING COUNT(\"ID\") > 1")]
        public override void WhereCountTest(string expected)
        {
            base.WhereCountTest(expected);
        }

        /// <summary>
        /// Find rows where Id count is equal to 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" GROUP BY \"Guid\" HAVING COUNT(\"ID\") = 1")]
        public override void WhereCountTest2(string expected)
        {
            base.WhereCountTest2(expected);
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\" FROM \"TestClass\" GROUP BY \"Text\" HAVING COUNT(\"ID\") > 1")]
        public override void HavingTest(string expected)
        {
            base.HavingTest(expected);
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Guid\", \"Text\", \"Status\" FROM \"TestClass\" GROUP BY \"Text\", \"Status\" HAVING COUNT(1) > 1")]
        public override void HavingTest2(string expected)
        {
            base.HavingTest2(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Country\" (\"CountryName\") SELECT \"CountryName\" FROM \"Country\"")]
        public override void CopyTest(string expected)
        {
            base.CopyTest(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Country\" (\"CountryName\") SELECT \"CountryName\" FROM \"Country\"")]
        public override void CopyTest2(string expected)
        {
            base.CopyTest2(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company\" (\"Name\", \"CountryID\") SELECT \"Name\", \"CountryID\" FROM \"Company\"")]
        public override void CopyTest3(string expected)
        {
            base.CopyTest3(expected);
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company2\" (\"Name\", \"CountryID\") SELECT \"Name\", \"CountryID\" FROM \"Company\"")]
        public override void CopyTest4(string expected)
        {
            base.CopyTest4(expected);
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company\" (\"Name\", \"CountryID\") SELECT \"Name\", \"CountryID\" FROM \"Company2\"")]
        public override void CopyTest5(string expected)
        {
            base.CopyTest5(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company\" (\"Name\", \"CountryID\") SELECT \"Company\".\"Name\", \"Company\".\"CountryID\" FROM \"Company\" INNER JOIN \"Country\" ON \"Company\".\"CountryID\" = \"Country\".\"ID\"")]
        public override void CopyTest6(string expected)
        {
            base.CopyTest6(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company\" (\"Name\", \"CountryID\") SELECT 'Gurux', \"ID\" FROM \"Country\"")]
        public override void InsertSelectedValueTest(string expected)
        {
            base.InsertSelectedValueTest(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company2\" (\"Name\", \"ExtraField\", \"CountryID\") SELECT 'Gurux', 'Extra', \"ID\" FROM \"Country\"")]
        public override void InsertSelectedValue2Test(string expected)
        {
            base.InsertSelectedValue2Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company2\" (\"Name\", \"ExtraField\", \"CountryID\") SELECT 'Gurux', 'Extra', \"ID\" FROM \"Country\"")]
        public override void InsertSelectedValue3Test(string expected)
        {
            base.InsertSelectedValue3Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company2\" (\"Name\", \"ExtraField\") VALUES('Gurux', 'Extra')")]
        public override void InsertSelectedValue4Test(string expected)
        {
            base.InsertSelectedValue4Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company\" (\"Name\", \"CountryID\") SELECT 'Gurux', \"ID\" FROM \"Country\" WHERE \"CountryName\" IN ('Finland')")]
        public override void InsertSelectedValue5Test(string expected)
        {
            base.InsertSelectedValue5Test(expected);
        }

        /// <summary>
        /// The purpose of this test is check that old column is overrided 
        /// and CountryID is not twice.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"Company2\" (\"Name\", \"CountryID\", \"ExtraField\") SELECT 'Gurux', \"ID\", 'Extra' FROM \"Country\"")]
        public override void UpdateInsertParameterTest(string expected)
        {
            base.UpdateInsertParameterTest(expected);
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"Company\" SET \"Name\" = 'Gurux' WHERE EXISTS (SELECT \"ID\" FROM \"Country\" WHERE \"ID\" = 1)")]
        public override void UpdateSelectedValueTest(string expected)
        {
            base.UpdateSelectedValueTest(expected);
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"UserToUserGroup\" (\"UserId\", \"GroupId\") VALUES(2, 1)", "INSERT INTO \"UserToUserGroup\" (\"UserId\", \"GroupId\") VALUES(2, 1)")]
        public override void UpdateParameterCollectionTest(string expected, string expected2)
        {
            base.UpdateParameterCollectionTest(expected, expected2);
        }

        /// <summary>
        /// Data quota where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM \"TestClass\" WHERE \"Text\" = 'Gurux'''")]
        public override void DataQuotaWhereTest(string expected)
        {
            base.DataQuotaWhereTest(expected);
        }

        /// <summary>
        /// Data quota insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO \"User2\" (\"Name\") VALUES('Gurux''')")]
        public override void DataQuotaInsertTest(string expected)
        {
            base.DataQuotaInsertTest(expected);
        }

        /// <summary>
        /// Data quota update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE \"User2\" SET \"Name\" = 'Gurux''' WHERE \"Id\" = 2")]
        public override void DataQuotaUpdateTest(string expected)
        {
            base.DataQuotaUpdateTest(expected);
        }

        /// <summary>
        /// Data quota delete test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM \"TestClass\" WHERE \"Text\" = 'Gurux'''")]
        public override void DataQuotaDeleteTest(string expected)
        {
            base.DataQuotaDeleteTest(expected);
        }

        /// <summary>
        /// Sum test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT SUM(\"DoubleTest\") AS SUM1 FROM \"TestClass\"")]
        public override void SumTest(string expected)
        {
            base.SumTest(expected);
        }

        /// <summary>
        /// Sum columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT SUM(\"DoubleTest\" + \"FloatTest\") AS SUM1 FROM \"TestClass\"")]
        public override void SumColumnsTest(string expected)
        {
            base.SumColumnsTest(expected);
        }


        /// <summary>
        /// Min test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MIN(\"DoubleTest\") AS MIN1 FROM \"TestClass\"")]
        public override void MinTest(string expected)
        {
            base.MinTest(expected);
        }

        /// <summary>
        /// Min columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MIN(\"DoubleTest\" + \"FloatTest\") AS MIN1 FROM \"TestClass\"")]
        public override void MinColumnsTest(string expected)
        {
            base.MinColumnsTest(expected);
        }

        /// <summary>
        /// Max test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MAX(\"DoubleTest\") AS MAX1 FROM \"TestClass\"")]
        public override void MaxTest(string expected)
        {
            base.MaxTest(expected);
        }

        /// <summary>
        /// Max columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MAX(\"DoubleTest\" + \"FloatTest\") AS MAX1 FROM \"TestClass\"")]
        public override void MaxColumnsTest(string expected)
        {
            base.MaxColumnsTest(expected);
        }

        /// <summary>
        /// Average test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT AVG(\"DoubleTest\") AS AVG1 FROM \"TestClass\"")]
        public override void AverageTest(string expected)
        {
            base.AverageTest(expected);
        }

        /// <summary>
        /// Average columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT AVG(\"DoubleTest\" + \"FloatTest\") AS AVG1 FROM \"TestClass\"")]
        public override void AverageColumnsTest(string expected)
        {
            base.AverageColumnsTest(expected);
        }

        /// <summary>
        /// Bitwice test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"ID\" FROM \"TestClass\" WHERE \"IntTest\" & 1 <> 0")]
        public override void BitwiseTest(string expected)
        {
            base.BitwiseTest(expected);
        }

        /// <summary>
        /// Verify that a query using LEFT JOIN combined with a WHERE Company.Id IS NULL filter 
        /// returns only the rows from the left table that have no matching row in the joined (right) table.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT \"Country\".\"CountryName\" FROM \"Country\" INNER JOIN \"Company\" ON \"Country\".\"ID\" = \"Company\".\"CountryID\" WHERE \"Company\".\"Id\" IS NULL")]
        public override void NotExistOnJoinTableTest(string expected)
        {
            base.NotExistOnJoinTableTest(expected);
        }

    }
}



