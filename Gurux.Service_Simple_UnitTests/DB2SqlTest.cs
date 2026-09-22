using Gurux.Service.Orm.Common.Enums;

namespace Gurux.Service_Simple_Unit_Test
{
    /// <summary>
    /// IBM DB2 test class.
    /// </summary>
    [TestClass]
    public class DB2SqlTest : BaseTest
    {
        public DB2SqlTest() : base(DatabaseType.DB2) { }

        /// <inheritdoc/>
        [TestMethod]
        [DataRow("SELECT ID, GUID, TIME, TEXT, SIMPLETEXT, TEXT3, TEXT4, BOOLEANTEST, INTTEST, DOUBLETEST, FLOATTEST, SPAN, OBJECT, STATUS FROM TESTCLASS")]
        public override void SelectTest(string expected)
        {
            base.SelectTest(expected);
        }

        /// <summary>
        /// Select 1test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT 1 FROM TESTCLASS")]
        public override void Select1Test(string expected)
        {
            base.Select1Test(expected);
        }

        /// <summary>
        /// Select all by id test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, TEXT FROM TESTIDCLASS WHERE ID = 1")]
        public override void GetByIdTest(string expected)
        {
            base.GetByIdTest(expected);
        }

        /// <summary>
        /// Select only part of columns.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, TEXT FROM TESTIDCLASS WHERE ID = 1")]
        public override void GetPartTest(string expected)
        {
            base.GetPartTest(expected);
        }

        /// <summary>
        /// Select id by id test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTIDCLASS WHERE ID = 1")]
        public override void GetByIdColumnsTest(string expected)
        {
            base.GetByIdColumnsTest(expected);
        }

        /// <summary>
        /// Relation where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID AS DG.ID FROM DEVICEGROUP3 DG WHERE ID = 1")]
        public override void WhereByReferenceTest(string expected)
        {
            base.WhereByReferenceTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM TESTCLASS")]
        public override void CountTest(string expected)
        {
            base.CountTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(ID) FROM TESTCLASS")]
        public override void CountTest2(string expected)
        {
            base.CountTest2(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(SUPPLIER.SUPPLIERID) FROM SUPPLIER INNER JOIN PRODUCT ON SUPPLIER.SUPPLIERID = PRODUCT.TARGETID")]
        public override void CountTest3(string expected)
        {
            base.CountTest3(expected);
        }

        /// <summary>
        /// Distinct count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(DISTINCT SUPPLIER.SUPPLIERID) FROM SUPPLIER INNER JOIN PRODUCT ON SUPPLIER.SUPPLIERID = PRODUCT.TARGETID")]
        public override void DistinctCountTest(string expected)
        {
            base.DistinctCountTest(expected);
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM TESTCLASS WHERE ID = 1")]
        public override void CountWhereTest(string expected)
        {
            base.CountWhereTest(expected);
        }

        /// <summary>
        /// Select single column test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TEXT FROM TESTCLASS")]
        public override void SelectSingleColumnTest(string expected)
        {
            base.SelectSingleColumnTest(expected);
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID, TEXT FROM TESTCLASS")]
        public override void SelectColumnsTest(string expected)
        {
            base.SelectColumnsTest(expected);
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COMPANY.NAME, COUNTRY.COUNTRYNAME FROM COMPANY INNER JOIN COUNTRY ON COMPANY.COUNTRYID = COUNTRY.ID")]
        public override void SelectSubItemsTest(string expected)
        {
            base.SelectSubItemsTest(expected);
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT PRODUCT2.PRODUCT2ID, SUPPLIER.SUPPLIERID FROM PRODUCT2 INNER JOIN SUPPLIER ON PRODUCT2.TARGET2ID = SUPPLIER.SUPPLIERID")]
        public override void SelectSubItemsTest2(string expected)
        {
            base.SelectSubItemsTest2(expected);
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT PRODUCT2.PRODUCT2ID, SUPPLIER.SUPPLIERID FROM PRODUCT2 INNER JOIN SUPPLIER ON PRODUCT2.TARGET2ID = SUPPLIER.SUPPLIERID")]
        public override void SelectSubItemsTest3(string expected)
        {
            base.SelectSubItemsTest3(expected);
        }

        /// <summary>
        /// Limit test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS ORDER BY GUID OFFSET 1 ROWS FETCH NEXT 2 ROWS ONLY")]
        public override void LimitTest(string expected)
        {
            base.LimitTest(expected);
        }

        /// <summary>
        /// Select Distinct test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT DISTINCT GUID FROM TESTCLASS")]
        public override void SelectDistinctTest(string expected)
        {
            base.SelectDistinctTest(expected);
        }

        /// <summary>
        /// Select two tables test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TESTCLASS2.NAME, TESTCLASS.GUID FROM TESTCLASS2 RIGHT OUTER JOIN TESTCLASS ON TESTCLASS2.PARENTID = TESTCLASS.ID")]
        public override void SelectTablesTest(string expected)
        {
            base.SelectTablesTest(expected);
        }

        /// <summary>
        /// Select all columns from one table when multiple tables are used.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TESTCLASS2.ID, TESTCLASS2.PARENTID, TESTCLASS2.NAME FROM TESTCLASS2 RIGHT OUTER JOIN TESTCLASS ON TESTCLASS2.PARENTID = TESTCLASS.ID")]
        public override void SelectOneTableFromManyTest(string expected)
        {
            base.SelectOneTableFromManyTest(expected);
        }


        /// <summary>
        /// Delete by primary key test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM TESTCLASS WHERE ID = 1")]
        public override void DeleteByPrimaryKeyTest(string expected)
        {
            base.DeleteByPrimaryKeyTest(expected);
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM GUIDTESTCLASS WHERE ID = {0}")]
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
            }, "DELETE FROM GUIDTESTCLASS WHERE ID IN({0}, {1})")]
        public override void DeleteByGuidRangeTest(string[] guids, string expected)
        {
            base.DeleteByGuidRangeTest(guids, expected);
        }

        /// <summary>
        /// Delete using where.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM TESTCLASS WHERE TEXT = 'Gurux'")]
        public override void DeleteByWhereTest(string expected)
        {
            base.DeleteByWhereTest(expected);
        }

        /// <summary>
        /// Delete using select.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM TESTCLASS WHERE EXISTS (SELECT 1 FROM TESTCLASS WHERE TEXT = 'Gurux')")]
        public override void DeleteBySelectTest(string expected)
        {
            base.DeleteBySelectTest(expected);
        }

        /// <summary>
        /// Delete using list.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM CHILD2 WHERE PARENT IN (1)")]
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
        [DataRow("SELECT TESTCLASS.ID, TESTCLASS.GUID, TESTCLASS.TIME, TESTCLASS.TEXT, TESTCLASS.SIMPLETEXT, TESTCLASS.TEXT3, TESTCLASS.TEXT4, TESTCLASS.BOOLEANTEST, TESTCLASS.INTTEST, TESTCLASS.DOUBLETEST, TESTCLASS.FLOATTEST, TESTCLASS.SPAN, TESTCLASS.OBJECT, TESTCLASS.STATUS, TESTCLASS2.ID, TESTCLASS2.PARENTID, TESTCLASS2.NAME FROM TESTCLASS2 RIGHT OUTER JOIN TESTCLASS ON TESTCLASS2.PARENTID = TESTCLASS.ID")]
        public override void RightJoinTest(string expected)
        {
            base.RightJoinTest(expected);
        }

        /// <summary>
        /// Left join test
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TESTCLASS.ID, TESTCLASS.GUID, TESTCLASS.TIME, TESTCLASS.TEXT, TESTCLASS.SIMPLETEXT, TESTCLASS.TEXT3, TESTCLASS.TEXT4, TESTCLASS.BOOLEANTEST, TESTCLASS.INTTEST, TESTCLASS.DOUBLETEST, TESTCLASS.FLOATTEST, TESTCLASS.SPAN, TESTCLASS.OBJECT, TESTCLASS.STATUS, TESTCLASS2.ID, TESTCLASS2.PARENTID, TESTCLASS2.NAME FROM TESTCLASS2 LEFT OUTER JOIN TESTCLASS ON TESTCLASS2.PARENTID = TESTCLASS.ID")]
        public override void LeftJoinTest(string expected)
        {
            base.LeftJoinTest(expected);
        }

        /// <summary>
        /// Full join test
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TESTCLASS.ID, TESTCLASS.GUID, TESTCLASS.TIME, TESTCLASS.TEXT, TESTCLASS.SIMPLETEXT, TESTCLASS.TEXT3, TESTCLASS.TEXT4, TESTCLASS.BOOLEANTEST, TESTCLASS.INTTEST, TESTCLASS.DOUBLETEST, TESTCLASS.FLOATTEST, TESTCLASS.SPAN, TESTCLASS.OBJECT, TESTCLASS.STATUS, TESTCLASS2.ID, TESTCLASS2.PARENTID, TESTCLASS2.NAME FROM TESTCLASS2 FULL OUTER JOIN TESTCLASS ON TESTCLASS2.PARENTID = TESTCLASS.ID")]
        public override void FullJoinTest(string expected)
        {
            base.FullJoinTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID = 1")]
        public override void WhereSimpleTest(string expected)
        {
            base.WhereSimpleTest(expected);
        }

        /// <summary>
        /// Select Time where Datetime is bigger Min date time and Datetime is smaller than max date time and text is not empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TIME FROM TESTCLASS WHERE TIME > TIMESTAMP('0001-01-01 00:00:00') AND TIME < TIMESTAMP('9999-12-31 23:59:59.999')")]
        public override void WhereDateTimeTest(string expected)
        {
            base.WhereDateTimeTest(expected);
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TEXT FROM TESTCLASS WHERE (TEXT <> '') AND (TEXT IS NOT NULL)")]
        public override void WhereStringEmptyTest(string expected)
        {
            base.WhereStringEmptyTest(expected);
        }

        /// <summary>
        /// Select Text where string is empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TEXT FROM TESTCLASS WHERE (TEXT IS NULL OR TEXT = '')")]
        public override void WhereStringIsNullOrEmptyTest(string expected)
        {
            base.WhereStringIsNullOrEmptyTest(expected);
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT TEXT FROM TESTCLASS WHERE (TEXT IS NOT NULL AND TEXT <> '')")]
        public override void WhereStringNotIsNullOrEmptyTest(string expected)
        {
            base.WhereStringNotIsNullOrEmptyTest(expected);
        }

        /// <summary>
        /// Select Guid where Enum is string.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE STATUS = 'OK'")]
        public override void WhereEnumTest(string expected)
        {
            base.WhereEnumTest(expected);
        }

        /// <summary>
        /// Select Guid where Enum is saved as int.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE STATUS = 100")]
        public override void WhereEnumAsIntTest(string expected)
        {
            base.WhereEnumAsIntTest(expected);
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS WHERE ID = 1")]
        public override void WhereClassTest(string expected)
        {
            base.WhereClassTest(expected);
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS WHERE ID = 1")]
        public override void WhereClassTest2(string expected)
        {
            base.WhereClassTest2(expected);
        }

        /// <summary>
        /// Select Guid where class array is given as parameter.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN(1, 2)")]
        public override void WhereClassArrayTest(string expected)
        {
            base.WhereClassArrayTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID = 1")]
        public override void WhereSimple2Test(string expected)
        {
            base.WhereSimple2Test(expected);
        }

        /// <summary>
        /// Select all by string test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, TEXT FROM TESTIDCLASS WHERE TEXT = 'Gurux'")]
        public override void WhereExactString(string expected)
        {
            base.WhereExactString(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('Gurux%')")]
        public override void WhereStartsWithTest(string expected)
        {
            base.WhereStartsWithTest(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('Gurux%')")]
        public override void WhereStartsWith2Test(string expected)
        {
            base.WhereStartsWith2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('%Gurux')")]
        public override void WhereEndsWithTest(string expected)
        {
            base.WhereEndsWithTest(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('%Gurux%')")]
        public override void WhereContainsTest(string expected)
        {
            base.WhereContainsTest(expected);
        }

        /// <summary>
        /// Select Guid where Text contains upper Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('%GURUX%')")]
        public override void WhereContainsUpperTest(string expected)
        {
            base.WhereContainsUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('GURUX%')")]
        public override void WhereStartsWithUpperTest(string expected)
        {
            base.WhereStartsWithUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where Text ends with upper Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('%GURUX')")]
        public override void WhereEndsWithUpperTest(string expected)
        {
            base.WhereEndsWithUpperTest(expected);
        }

        /// <summary>
        /// Select Guid where list contains Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT IN ('Gurux')")]
        public override void WhereContains2Test(string expected)
        {
            base.WhereContains2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text contains with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT LIKE('%Gurux%')")]
        public override void WhereContains3Test(string expected)
        {
            base.WhereContains3Test(expected);
        }

        /// <summary>
        /// Select Guid where list contains -1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, -1)")]
        public override void WhereContains5Test(string expected)
        {
            base.WhereContains5Test(expected);
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IN (HEXTORAW('00000000000000000000000000000000'))")]
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
            }, "SELECT GUID FROM TESTCLASS WHERE GUID IN ({0})")]
        public override void WhereContainsIEnumerableGuidTest(string[] guids, string expected)
        {
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
            }, "SELECT GUID FROM TESTCLASS WHERE GUID IN ({0})")]
        public override void WhereContainsListGuidTest(string[] guids, string expected)
        {
            base.WhereContainsListGuidTest(guids, expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE UPPER(TEXT) LIKE('GURUX')")]
        public override void WhereEqualsTest(string expected)
        {
            base.WhereEqualsTest(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE UPPER(TEXT) LIKE('GURUX')")]
        public override void WhereEquals2Test(string expected)
        {
            base.WhereEquals2Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT = 'Gurux'")]
        public override void WhereEquals3Test(string expected)
        {
            base.WhereEquals3Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID = 1 AND UPPER(TEXT) LIKE('GURUX')")]
        public override void WhereEquals4Test(string expected)
        {
            base.WhereEquals4Test(expected);
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE UPPER(TEXT) LIKE('GURUX') AND ID = 1")]
        public override void WhereEquals5Test(string expected)
        {
            base.WhereEquals5Test(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID = 1 OR ID = 2 OR ID = 3")]
        public override void WhereOrTest(string expected)
        {
            base.WhereOrTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = -1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID = -1919693511")]
        public override void WhereMinusTest(string expected)
        {
            base.WhereMinusTest(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE (ID = 1 OR ID = 2) OR (ID = 3)")]
        public override void WhereOr2Test(string expected)
        {
            base.WhereOr2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID = 1 or text starts with Gurux.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE (ID = 1) OR (TEXT LIKE('Gurux%'))")]
        public override void WhereOr3Test(string expected)
        {
            base.WhereOr3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID > 1 AND ID <> 2")]
        public override void WhereAndTest(string expected)
        {
            base.WhereAndTest(expected);
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE (ID > 1) AND (ID <> 2)")]
        public override void WhereAnd2Test(string expected)
        {
            base.WhereAnd2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SqlInTest(string expected)
        {
            base.SqlInTest(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SqlInTest1(string expected)
        {
            base.SqlInTest1(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SqlInTest1_1(string expected)
        {
            base.SqlInTest1_1(expected);
        }


        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SqlInTest2(string expected)
        {
            base.SqlInTest2(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IN (HEXTORAW('00000000000000000000000000000000'))")]
        public override void SqlInTest3(string expected)
        {
            base.SqlInTest3(expected);
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID NOT IN (1, 2, 3)")]
        public override void SqlNotInTest(string expected)
        {
            base.SqlNotInTest(expected);
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID NOT IN (1, 2, 3)")]
        public override void SqlNotInTest2(string expected)
        {
            base.SqlNotInTest2(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID NOT IN (HEXTORAW('00000000000000000000000000000000'))")]
        public override void SqlNotInTest3(string expected)
        {
            base.SqlNotInTest3(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS ORDER BY ID, GUID")]
        public override void SqlOrderTest(string expected)
        {
            base.SqlOrderTest(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS ORDER BY ID, GUID")]
        public override void SqlOrder2Test(string expected)
        {
            base.SqlOrder2Test(expected);
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS ORDER BY ID")]
        public override void SqlOrder3Test(string expected)
        {
            base.SqlOrder3Test(expected);
        }

        /// <summary>
        /// Order desc test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS ORDER BY ID DESC")]
        public override void SqlOrderDescTest(string expected)
        {
            base.SqlOrderDescTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COUNTRY (COUNTRYNAME) VALUES('Finland')")]
        public override void InsertAllTest(string expected)
        {
            base.InsertAllTest(expected);
        }

        [TestMethod]
        [DataRow("INSERT INTO COUNTRY (COUNTRYNAME) VALUES('Finland')")]
        public override void InsertNameOnlyTest(string expected)
        {
            base.InsertNameOnlyTest(expected);
        }

        /// <summary>
        /// Insert range test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO PARAMETER2 (NAME, \"Value\", DEVICEID) VALUES('Name1', 'Value1', 0), ('Name2', 'Value2', 0), ('Name3', 'Value3', 0)", "INSERT INTO PARAMETER2 (NAME, \"Value\") VALUES('Name1', 'Value1'), ('Name2', 'Value2'), ('Name3', 'Value3')")]
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
        [DataRow("INSERT INTO TESTCLASS (TEXT, GUID) VALUES('Gurux', HEXTORAW('00000000000000000000000000000000'))")]
        public override void InsertTest(string expected)
        {
            base.InsertTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO SUPPLIER (TEXT) VALUES('Gurux')")]
        public override void InsertOneToOneTest(string expected)
        {
            base.InsertOneToOneTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO PRODUCT2 (TEXT, TARGET2ID) VALUES('Product1', 1)")]
        public override void InsertOneToOneTest2(string expected)
        {
            base.InsertOneToOneTest2(expected);
        }

        /// <summary>
        /// Create table test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO NULLABLETESTCLASS (ID, ACTIVE, TEXT) VALUES({0}, 0, 'Gurux')")]
        public override void CreateNullableTableTest(string expected)
        {
            base.CreateNullableTableTest(expected);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO NULLABLETESTCLASS (ID, ACTIVE, TEXT) VALUES({0}, 1, 'Gurux')")]
        public override void InsertNullableTest(string expected)
        {
            base.InsertNullableTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET GUID = HEXTORAW('00000000000000000000000000000000'), TIME = TIMESTAMP('{1}') WHERE ID = 2")]
        public override void UpdateTest(string expected)
        {
            base.UpdateTest(expected);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE GUIDTESTCLASS SET TIME = TIMESTAMP('{0}') WHERE ID = {1}")]
        public override void UpdateTest2(string expected)
        {
            base.UpdateTest2(expected);
        }

        /// <summary>
        /// Update using where.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET GUID = HEXTORAW('00000000000000000000000000000000'), TIME = TIMESTAMP('2014-01-02 00:00:00.000') WHERE TEXT = 'Gurux'")]
        public override void UpdateWhereTest(string expected)
        {
            base.UpdateWhereTest(expected);
        }

        /// <summary>
        /// Update default null test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE NULLABLETESTCLASS SET ACTIVE = 1, TEXT = NULL WHERE ID = {0}", "UPDATE NULLABLETESTCLASS SET ACTIVE = 1, TEXT = NULL WHERE ID = {0}", "UPDATE NULLABLETESTCLASS SET ACTIVE = 0, TEXT = NULL WHERE ID = {0}")]
        public override void UpdateDefaultNullTest(string expected, string expected2, string expected3)
        {
            base.UpdateDefaultNullTest(expected, expected2, expected3);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET TIME = 1388620800 WHERE ID = 1")]
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
        [DataRow("SELECT ID, GUID, TIME, TEXT, SIMPLETEXT, TEXT3, TEXT4, BOOLEANTEST, INTTEST, DOUBLETEST, FLOATTEST, SPAN, OBJECT, STATUS FROM TESTCLASS WHERE TEXT IS NULL",
            "SELECT ID, GUID, TIME, TEXT, SIMPLETEXT, TEXT3, TEXT4, BOOLEANTEST, INTTEST, DOUBLETEST, FLOATTEST, SPAN, OBJECT, STATUS FROM TESTCLASS WHERE TEXT IS NULL")]
        public override void WhereStringIsNullTest(string expected, string expected2)
        {
            base.WhereStringIsNullTest(expected, expected2);
        }

        /// <summary>
        /// Where string is empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, GUID, TIME, TEXT, SIMPLETEXT, TEXT3, TEXT4, BOOLEANTEST, INTTEST, DOUBLETEST, FLOATTEST, SPAN, OBJECT, STATUS FROM TESTCLASS WHERE (TEXT IS NULL OR TEXT = '')")]
        public override void WhereStringIsEmptyTest(string expected)
        {
            base.WhereStringIsEmptyTest(expected);
        }


        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SqlIn2Test(string expected)
        {
            base.SqlIn2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, NAME, COUNTRYID FROM COMPANY WHERE COUNTRYID IN (SELECT ID FROM COUNTRY WHERE COUNTRYNAME = 'Finland')")]
        public override void SqlIn3Test(string expected)
        {
            base.SqlIn3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID NOT IN (1, 2, 3)")]
        public override void SqlNotIn2Test(string expected)
        {
            base.SqlNotIn2Test(expected);
        }

        /// <summary>
        /// Select all countries where company exists. 
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, COUNTRYNAME FROM COUNTRY WHERE EXISTS (SELECT 1 FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX') AND COMPANY.COUNTRYID = COUNTRY.ID)")]
        public override void ExistsTest(string expected)
        {
            base.ExistsTest(expected);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, COUNTRYNAME FROM COUNTRY WHERE EXISTS (SELECT ID FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX'))")]
        public override void Exists2Test(string expected)
        {
            base.Exists2Test(expected);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, COUNTRYNAME FROM COUNTRY WHERE EXISTS (SELECT 1 FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX'))")]
        public override void Exists3Test(string expected)
        {
            base.Exists3Test(expected);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, COUNTRYNAME FROM COUNTRY WHERE NOT EXISTS (SELECT ID FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX') AND COUNTRY.ID = COMPANY.COUNTRYID)")]
        public override void NotExistsTest(string expected)
        {
            base.NotExistsTest(expected);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID, COUNTRYNAME FROM COUNTRY WHERE NOT EXISTS (SELECT ID FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX'))")]
        public override void NotExists2Test(string expected)
        {
            base.NotExists2Test(expected);
        }

        /// <summary>
        /// Create simple view where data is retreaved from one table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View COUNTRIES AS SELECT ID, COUNTRYNAME FROM COUNTRY WHERE NOT EXISTS (SELECT ID FROM COMPANY WHERE UPPER(NAME) LIKE('GURUX') AND COUNTRY.ID = COMPANY.COUNTRYID)")]
        public override void CreateSimpleViewTest(string expected)
        {
            base.CreateSimpleViewTest(expected);
        }

        /// <summary>
        /// Create simple view where data is retreaved from two table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View COUNTRIES AS SELECT COMPANY.ID, COMPANY.NAME, COUNTRY.COUNTRYNAME FROM COMPANY INNER JOIN COUNTRY ON COMPANY.COUNTRYID = COUNTRY.ID")]
        public override void CreateSimpleViewTest2(string expected)
        {
            base.CreateSimpleViewTest2(expected);
        }

        /// <summary>
        /// Create simple view where data is map from two table.
        /// </summary>
        [TestMethod]
        [DataRow("Create View COUNTRIES AS SELECT COMPANY.ID, COMPANY.NAME AS COMPANIES.COMPANYNAME, COUNTRY.COUNTRYNAME AS COMPANIES.NAME FROM COMPANY INNER JOIN COUNTRY ON COMPANY.COUNTRYID = COUNTRY.ID")]
        public override void CreateSimpleViewTest3(string expected)
        {
            base.CreateSimpleViewTest3(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET GUID = {0}, TIME = TIMESTAMP('{1}') WHERE ID = 2")]
        public override void ExcludeUpdateTest(string expected)
        {
            base.ExcludeUpdateTest(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET GUID = {0}, TIME = TIMESTAMP('{1}') WHERE ID = 2")]
        public override void ExcludeUpdateTest2(string expected)
        {
            base.ExcludeUpdateTest2(expected);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE TESTCLASS SET GUID = {0}, TIME = TIMESTAMP('{1}') WHERE ID = 2")]
        public override void ExcludeUpdateTest3(string expected)
        {
            base.ExcludeUpdateTest3(expected);
        }

        /// <summary>
        /// Exclude insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO TESTCLASS (GUID, TEXT) VALUES(HEXTORAW('00000000000000000000000000000000'), 'Gurux')")]
        public override void ExcludeInsertTest(string expected)
        {
            base.ExcludeInsertTest(expected);
        }

        /// <summary>
        /// Append where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE ID IN (1, 2, 3)")]
        public override void SelectGuidWhereTest(string expected)
        {
            base.SelectGuidWhereTest(expected);
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE (SIMPLETEXT = 'More') AND (TEXT3 = 'Gurux') AND (STATUS = 0)")]
        public override void FilterByTest(string expected)
        {
            base.FilterByTest(expected);
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE STATUS = 0")]
        public override void FilterByTest2(string expected)
        {
            base.FilterByTest2(expected);
        }

        /// <summary>
        /// Filter by status.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE STATUS = 200")]
        public override void FilterByStatus(string expected)
        {
            base.FilterByStatus(expected);
        }

        /// <summary>
        /// Filter by date-time.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME >= TIMESTAMP('{0}')")]
        public override void FilterByDateTime(string expected)
        {
            base.FilterByDateTime(expected);
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IS NULL")]
        public override void FindEmptyGuid(string expected)
        {
            base.FindEmptyGuid(expected);
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IS NOT NULL")]
        public override void FindNotEmptyGuid(string expected)
        {
            base.FindNotEmptyGuid(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME IS NULL")]
        public override void FindEmptyDateTime(string expected)
        {
            base.FindEmptyDateTime(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME IS NULL")]
        public override void FindEmptyDateTime2(string expected)
        {
            base.FindEmptyDateTime2(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME IS NOT NULL")]
        public override void FindNotEmptyDateTime2(string expected)
        {
            base.FindNotEmptyDateTime2(expected);
        }

        /// <summary>
        /// Find Empty guid values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IS NULL OR GUID = HEXTORAW('00000000000000000000000000000000')")]
        public override void EmptyGuidTest(string expected)
        {
            base.EmptyGuidTest(expected);
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME IS NULL OR TIME = TIMESTAMP('{0}')")]
        public override void EmptyDateTimeTest(string expected)
        {
            base.EmptyDateTimeTest(expected);
        }

        /// <summary>
        /// Guid in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE GUID IN (HEXTORAW('00000000000000000000000000000000'))")]
        public override void GuidInTest(string expected)
        {
            base.GuidInTest(expected);
        }


        /// <summary>
        /// DateTime in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TIME IN (TIMESTAMP('{0}'))")]
        public override void DateTimeInTest(string expected)
        {
            base.DateTimeInTest(expected);
        }

        /// <summary>
        /// string in test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS WHERE TEXT IN ('Gurux')")]
        public override void StringInTest(string expected)
        {
            base.StringInTest(expected);
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS")]
        public override void ExcludeSelectTest(string expected)
        {
            base.ExcludeSelectTest(expected);
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS")]
        public override void ExcludeSelectTest2(string expected)
        {
            base.ExcludeSelectTest2(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM TESTCLASS) THEN 1 ELSE 0 END AS IsEmpty FROM SYSIBM.SYSDUMMY1")]
        public override void IsEmptyTest(string expected)
        {
            base.IsEmptyTest(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM TESTCLASS WHERE ID = 1) THEN 1 ELSE 0 END AS IsEmpty FROM SYSIBM.SYSDUMMY1")]
        public override void IsEmpty1Test(string expected)
        {
            base.IsEmpty1Test(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM TESTCLASS WHERE ID = 1) THEN 1 ELSE 0 END AS IsEmpty FROM SYSIBM.SYSDUMMY1")]
        public override void IsEmpty2Test(string expected)
        {
            base.IsEmpty2Test(expected);
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM TESTCLASS WHERE ID = 1")]
        public override void IsEmpty3Test(string expected)
        {
            base.IsEmpty3Test(expected);
        }

        /// <summary>
        /// Find rows where Id count is greater than 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS GROUP BY GUID HAVING COUNT(ID) > 1")]
        public override void WhereCountTest(string expected)
        {
            base.WhereCountTest(expected);
        }

        /// <summary>
        /// Find rows where Id count is equal to 1.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS GROUP BY GUID HAVING COUNT(ID) = 1")]
        public override void WhereCountTest2(string expected)
        {
            base.WhereCountTest2(expected);
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID FROM TESTCLASS GROUP BY TEXT HAVING COUNT(ID) > 1")]
        public override void HavingTest(string expected)
        {
            base.HavingTest(expected);
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT GUID, TEXT, STATUS FROM TESTCLASS GROUP BY TEXT, STATUS HAVING COUNT(1) > 1")]
        public override void HavingTest2(string expected)
        {
            base.HavingTest2(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COUNTRY (COUNTRYNAME) SELECT COUNTRYNAME FROM COUNTRY")]
        public override void CopyTest(string expected)
        {
            base.CopyTest(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COUNTRY (COUNTRYNAME) SELECT COUNTRYNAME FROM COUNTRY")]
        public override void CopyTest2(string expected)
        {
            base.CopyTest2(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY (NAME, COUNTRYID) SELECT NAME, COUNTRYID FROM COMPANY")]
        public override void CopyTest3(string expected)
        {
            base.CopyTest3(expected);
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY2 (NAME, COUNTRYID) SELECT NAME, COUNTRYID FROM COMPANY")]
        public override void CopyTest4(string expected)
        {
            base.CopyTest4(expected);
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY (NAME, COUNTRYID) SELECT NAME, COUNTRYID FROM COMPANY2")]
        public override void CopyTest5(string expected)
        {
            base.CopyTest5(expected);
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY (NAME, COUNTRYID) SELECT COMPANY.NAME, COMPANY.COUNTRYID FROM COMPANY INNER JOIN COUNTRY ON COMPANY.COUNTRYID = COUNTRY.ID")]
        public override void CopyTest6(string expected)
        {
            base.CopyTest6(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY (NAME, COUNTRYID) SELECT 'Gurux', ID FROM COUNTRY")]
        public override void InsertSelectedValueTest(string expected)
        {
            base.InsertSelectedValueTest(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY2 (NAME, EXTRAFIELD, COUNTRYID) SELECT 'Gurux', 'Extra', ID FROM COUNTRY")]
        public override void InsertSelectedValue2Test(string expected)
        {
            base.InsertSelectedValue2Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY2 (NAME, EXTRAFIELD, COUNTRYID) SELECT 'Gurux', 'Extra', ID FROM COUNTRY")]
        public override void InsertSelectedValue3Test(string expected)
        {
            base.InsertSelectedValue3Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY2 (NAME, EXTRAFIELD) VALUES('Gurux', 'Extra')")]
        public override void InsertSelectedValue4Test(string expected)
        {
            base.InsertSelectedValue4Test(expected);
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY (NAME, COUNTRYID) SELECT 'Gurux', ID FROM COUNTRY WHERE COUNTRYNAME IN ('Finland')")]
        public override void InsertSelectedValue5Test(string expected)
        {
            base.InsertSelectedValue5Test(expected);
        }

        /// <summary>
        /// The purpose of this test is check that old column is overrided 
        /// and CountryID is not twice.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO COMPANY2 (NAME, EXTRAFIELD, COUNTRYID) SELECT 'Gurux', 'Extra', ID FROM COUNTRY")]
        public override void UpdateInsertParameterTest(string expected)
        {
            base.UpdateInsertParameterTest(expected);
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE COMPANY SET NAME = 'Gurux' WHERE EXISTS (SELECT ID FROM COUNTRY WHERE ID = 1)")]
        public override void UpdateSelectedValueTest(string expected)
        {
            base.UpdateSelectedValueTest(expected);
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO USERTOUSERGROUP (USERID, GROUPID) VALUES(2, 1)")]
        public override void UpdateParameterCollectionTest(string expected)
        {
            base.UpdateParameterCollectionTest(expected);
        }

        /// <summary>
        /// Data quota where test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNT(1) FROM TESTCLASS WHERE TEXT = 'Gurux'''")]
        public override void DataQuotaWhereTest(string expected)
        {
            base.DataQuotaWhereTest(expected);
        }

        /// <summary>
        /// Data quota insert test.
        /// </summary>
        [TestMethod]
        [DataRow("INSERT INTO USER2 (NAME) VALUES('Gurux''')")]
        public override void DataQuotaInsertTest(string expected)
        {
            base.DataQuotaInsertTest(expected);
        }

        /// <summary>
        /// Data quota update test.
        /// </summary>
        [TestMethod]
        [DataRow("UPDATE USER2 SET NAME = 'Gurux''' WHERE ID = 2")]
        public override void DataQuotaUpdateTest(string expected)
        {
            base.DataQuotaUpdateTest(expected);
        }

        /// <summary>
        /// Data quota delete test.
        /// </summary>
        [TestMethod]
        [DataRow("DELETE FROM TESTCLASS WHERE TEXT = 'Gurux'''")]
        public override void DataQuotaDeleteTest(string expected)
        {
            base.DataQuotaDeleteTest(expected);
        }

        /// <summary>
        /// Sum test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT SUM(DOUBLETEST) AS SUM1 FROM TESTCLASS")]
        public override void SumTest(string expected)
        {
            base.SumTest(expected);
        }

        /// <summary>
        /// Sum columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT SUM(DOUBLETEST + FLOATTEST) AS SUM1 FROM TESTCLASS")]
        public override void SumColumnsTest(string expected)
        {
            base.SumColumnsTest(expected);
        }


        /// <summary>
        /// Min test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MIN(DOUBLETEST) AS MIN1 FROM TESTCLASS")]
        public override void MinTest(string expected)
        {
            base.MinTest(expected);
        }

        /// <summary>
        /// Min columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MIN(DOUBLETEST + FLOATTEST) AS MIN1 FROM TESTCLASS")]
        public override void MinColumnsTest(string expected)
        {
            base.MinColumnsTest(expected);
        }

        /// <summary>
        /// Max test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MAX(DOUBLETEST) AS MAX1 FROM TESTCLASS")]
        public override void MaxTest(string expected)
        {
            base.MaxTest(expected);
        }

        /// <summary>
        /// Max columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT MAX(DOUBLETEST + FLOATTEST) AS MAX1 FROM TESTCLASS")]
        public override void MaxColumnsTest(string expected)
        {
            base.MaxColumnsTest(expected);
        }

        /// <summary>
        /// Average test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT AVG(DOUBLETEST) AS AVG1 FROM TESTCLASS")]
        public override void AverageTest(string expected)
        {
            base.AverageTest(expected);
        }

        /// <summary>
        /// Average columns test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT AVG(DOUBLETEST + FLOATTEST) AS AVG1 FROM TESTCLASS")]
        public override void AverageColumnsTest(string expected)
        {
            base.AverageColumnsTest(expected);
        }

        /// <summary>
        /// Bitwice test.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT ID FROM TESTCLASS WHERE INTTEST & 1 <> 0")]
        public override void BitwiseTest(string expected)
        {
            base.BitwiseTest(expected);
        }

        /// <summary>
        /// Verify that a query using LEFT JOIN combined with a WHERE Company.Id IS NULL filter 
        /// returns only the rows from the left table that have no matching row in the joined (right) table.
        /// </summary>
        [TestMethod]
        [DataRow("SELECT COUNTRY.COUNTRYNAME FROM COUNTRY INNER JOIN COMPANY ON COUNTRY.ID = COMPANY.COUNTRYID WHERE COMPANY.ID IS NULL")]
        public override void NotExistOnJoinTableTest(string expected)
        {
            base.NotExistOnJoinTableTest(expected);
        }

        [TestMethod]
        [DataRow("SELECT ID, \"ALL\" FROM RESERVEDCLASS")]
        public override void ReservedWordSelectTest(string expected)
        {
            base.ReservedWordSelectTest(expected);
        }

        [TestMethod]
        [DataRow("INSERT INTO RESERVEDCLASS (ID, \"ALL\") VALUES(2, 'Gurux')")]
        public override void ReservedWordInsertTest(string expected)
        {
            base.ReservedWordInsertTest(expected);
        }

        [TestMethod]
        [DataRow("UPDATE RESERVEDCLASS SET \"ALL\" = 'Gurux' WHERE ID = 2")]
        public override void ReservedWordUpdateTest(string expected)
        {
            base.ReservedWordUpdateTest(expected);
        }
    }
}



