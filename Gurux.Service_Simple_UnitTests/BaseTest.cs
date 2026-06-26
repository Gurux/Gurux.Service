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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Enums;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace Gurux.Service_Simple_Unit_Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    abstract public class BaseTest
    {
        static readonly object SqlLogSync = new object();
        static readonly HashSet<DatabaseType> InitializedSqlLogs = new HashSet<DatabaseType>();
        GXQueryCache _cache;

        protected BaseTest(DatabaseType databaseType)
        {
            _cache = new GXQueryCache(TimeSpan.FromMinutes(5), databaseType);
            InitializeSqlLog();
        }
        private string GetDateTimeFormat(DateTime value)
        {
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return "MySQL";
                case DatabaseType.MSSQL:
                    return "yyyyMMddTHH:mm:ss.fff";
                case DatabaseType.PostgreSQL:
                    return "PostgreSQL";
                case DatabaseType.Oracle:
                    return "Oracle";
                case DatabaseType.DB2:
                    return "DB2";
                case DatabaseType.SapHana:
                    return "SapHana";
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }

        private string DateTimeToString(DateTime? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.MSSQL:
                    if (value == DateTime.MinValue)
                    {
                        return "17530101";
                    }
                    return value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.PostgreSQL:
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.Oracle:
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.SqLite:
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.DB2:
                    if (value == DateTime.MinValue)
                    {
                        return "0001-01-01 00:00:00";
                    }
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                case DatabaseType.SapHana:
                    if (value == DateTime.MinValue)
                    {
                        return "0001-01-01 00:00:00";
                    }
                    return value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }
        private string GuidToString(Guid guid)
        {
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return "UUID_TO_BIN('" + guid.ToString().ToUpper() + "', 1)";
                case DatabaseType.MSSQL:
                    return guid.ToString().ToLower();
                case DatabaseType.PostgreSQL:
                    return guid.ToString().ToLower();
                case DatabaseType.Oracle:
                    return Convert.ToHexString(guid.ToByteArray());
                case DatabaseType.SqLite:
                    return guid.ToString().ToLower();
                case DatabaseType.DB2:
                    return guid.ToString().ToLower();
                case DatabaseType.SapHana:
                    return guid.ToString().ToLower();
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }

        private string FormatDateTimeValue(DateTime value)
        {
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.MSSQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.PostgreSQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.Oracle:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.DB2:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.SapHana:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }

        private string FormatDateTimeOffsetValue(DateTimeOffset value)
        {
            string format = "yyyy-MM-dd HH:mm:ss.fff zzz";
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.MSSQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.PostgreSQL:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.Oracle:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.DB2:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                case DatabaseType.SapHana:
                    return value.ToString(format, CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }

        private string FormatGuidValue(Guid value)
        {
            switch (_cache.DatabaseType)
            {
                case DatabaseType.MySQL:
                    return value.ToString().ToUpper();
                case DatabaseType.MSSQL:
                    return value.ToString().ToUpper();
                case DatabaseType.PostgreSQL:
                    return value.ToString().ToUpper();
                case DatabaseType.Oracle:
                    return Convert.ToHexString(value.ToByteArray());
                case DatabaseType.SqLite:
                    return value.ToString().ToUpper();
                case DatabaseType.DB2:
                    return value.ToString().ToUpper();
                case DatabaseType.SapHana:
                    return value.ToString().ToUpper();
                default:
                    throw new NotSupportedException("Database type not supported: " + _cache.DatabaseType);
            }
        }

        private string FormatGuidList(IEnumerable<Guid> list)
        {
            if (_cache.DatabaseType == DatabaseType.MSSQL)
            {
                return string.Join(", ", list.Select(g => "'" + g.ToString().ToLower() + "'"));
            }
            if (_cache.DatabaseType == DatabaseType.PostgreSQL)
            {
                return string.Join(", ", list.Select(g => "'" + g.ToString().ToLower() + "'::uuid"));
            }
            if (_cache.DatabaseType == DatabaseType.Oracle)
            {
                return string.Join(", ", list.Select(g => "HEXTORAW('" + FormatGuidValue(g) + "')"));
            }
            if (_cache.DatabaseType == DatabaseType.DB2 || _cache.DatabaseType == DatabaseType.SapHana)
            {
                return string.Join(", ", list.Select(g => "'" + g.ToString().ToLower() + "'"));
            }
            return string.Join(", ", list.Select(g => "UUID_TO_BIN('" + g.ToString().ToUpper() + "', 1)"));
        }

        protected string ResolveExpected(string expected, params object[] args)
        {
            if (args.Length == 1)
            {
                object value = args[0];
                if (value is string str)
                {
                    //Do nothing...
                }
                else if (value is Guid guid)
                {
                    value = FormatGuidValue(guid);
                }
                else if (value is DateTime dt)
                {
                    value = FormatDateTimeValue(dt);
                }
                else if (value is DateTimeOffset dto)
                {
                    value = FormatDateTimeOffsetValue(dto);
                }
                else if (value is IEnumerable<Guid> list)
                {
                    value = FormatGuidList(list);
                }
                else
                {
                    throw new NotSupportedException("Value type not supported: " + value.GetType());
                }
                return string.Format(CultureInfo.InvariantCulture, expected, value);
            }
            return args.Length == 0 ? expected : string.Format(CultureInfo.InvariantCulture, expected, args);
        }

        void AssertSqlEqual(string expected, string actual)
        {
            LogSql(actual);
            Assert.AreEqual(expected, actual);
        }

        void InitializeSqlLog()
        {
            lock (SqlLogSync)
            {
                if (InitializedSqlLogs.Add(_cache.DatabaseType))
                {
                    File.WriteAllText(GetSqlLogPath(), string.Empty, Encoding.UTF8);
                }
            }
        }

        void LogSql(string sql)
        {
            File.AppendAllText(GetSqlLogPath(), sql + Environment.NewLine, Encoding.UTF8);
        }

        string GetSqlLogPath()
        {
            return Path.Combine(AppContext.BaseDirectory, _cache.DatabaseType + ".txt");
        }

        /// <summary>
        /// Select test.
        /// </summary>
        [TestMethod]
        public virtual void SelectTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>(_cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select 1test.
        /// </summary>
        [TestMethod]
        public virtual void Select1Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.One, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select all by id test.
        /// </summary>
        [TestMethod]
        public virtual void GetByIdTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select only part of columns.
        /// </summary>
        [TestMethod]
        public virtual void GetPartTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestIDClass>(c => new object[] { c.Id, c.Text }, q => q.Id == 1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select id by id test.
        /// </summary>
        [TestMethod]
        public virtual void GetByIdColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectById<TestIDClass>(1, q => q.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Relation where test.
        /// </summary>
        [TestMethod]
        public virtual void WhereByReferenceTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<DeviceGroup3>(q => q.Id, _cache);
            arg.Where.And<DeviceGroup3>(q => q.Id == 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public virtual void CountTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(GXSql.One), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public virtual void CountTest2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q.Id), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public virtual void CountTest3(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.Count(q.Id), _cache);
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Distinct count test.
        /// </summary>
        [TestMethod]
        public virtual void DistinctCountTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Supplier>(q => GXSql.DistinctCount(q.Id), _cache);
            arg.Joins.AddInnerJoin<Supplier, Product>(j => j.Id, j => j.SupplierID);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Count test.
        /// </summary>
        [TestMethod]
        public virtual void CountWhereTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), q => q.Id == 1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select single column test.
        /// </summary>
        [TestMethod]
        public virtual void SelectSingleColumnTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Text, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        public virtual void SelectColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid, x.Text }, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        public virtual void SelectSubItemsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => q.Name, _cache);
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(x => x.Country, y => y.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select sub items test.
        /// </summary>
        [TestMethod]
        public virtual void SelectSubItemsTest2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Product2>(q => q.Id, _cache);
            arg.Columns.Add<Supplier>(q => q.Id);
            arg.Joins.AddInnerJoin<Product2, Supplier>(x => x.Supplier, y => y.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Limit test.
        /// </summary>
        [TestMethod]
        public virtual void LimitTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Index = 1;
            arg.Count = 2;
            arg.OrderBy.Add<TestClass>(x => x.Guid);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Distinct test.
        /// </summary>
        [TestMethod]
        public virtual void SelectDistinctTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Distinct = true;
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select two tables test.
        /// </summary>
        [TestMethod]
        public virtual void SelectTablesTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass2>(q => q.Parent, _cache);
            arg.Columns.Clear();
            arg.Columns.Add<TestClass2>(q => q.Name);
            arg.Columns.Add<TestClass>(q => q.Guid);
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select all columns from one table when multiple tables are used.
        /// </summary>
        [TestMethod]
        public virtual void SelectOneTableFromManyTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass2>(q => "*", _cache);
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }


        /// <summary>
        /// Delete by primary key test.
        /// </summary>
        [TestMethod]
        public virtual void DeleteByPrimaryKeyTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXDeleteArgs arg = GXDeleteArgs.DeleteById<TestClass>(1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        public virtual void DeleteByGuidPrimaryKeyTest(string expected)
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.NewGuid();
            GXDeleteArgs arg = GXDeleteArgs.Delete(t, _cache);
            AssertSqlEqual(ResolveExpected(expected, t.Id), arg.ToString(false));
        }

        /// <summary>
        /// Delete by Guid primary key test.
        /// </summary>
        [TestMethod]
        public virtual void DeleteByGuidRangeTest(string[] guids, string expected)
        {
            GuidTestClass t = new GuidTestClass();
            t.Id = Guid.Parse(guids[0]);
            GuidTestClass t2 = new GuidTestClass();
            t2.Id = Guid.Parse(guids[1]);
            GXDeleteArgs arg = GXDeleteArgs.DeleteRange([t, t2], _cache);
            AssertSqlEqual(ResolveExpected(expected, guids[0], guids[1]), arg.ToString(false));
        }

        /// <summary>
        /// Delete using where.
        /// </summary>
        [TestMethod]
        public virtual void DeleteByWhereTest(string expected)
        {
            GXDeleteArgs del = GXDeleteArgs.Delete<TestClass>(q => q.Text == "Gurux", _cache);
            AssertSqlEqual(expected, del.ToString(false));
        }

        /// <summary>
        /// Delete using select.
        /// </summary>
        [TestMethod]
        public virtual void DeleteBySelectTest(string expected)
        {
            GXSelectArgs sel = GXSelectArgs.Select<TestClass>(q => GXSql.One, q => q.Text == "Gurux", _cache);
            GXDeleteArgs del = GXDeleteArgs.Delete<TestClass>(a => GXSql.Exists(sel), _cache);
            AssertSqlEqual(expected, del.ToString(false));
        }

        /// <summary>
        /// Delete using list.
        /// </summary>
        [TestMethod]
        public virtual void DeleteByListTest(string expected)
        {
            Parent2 p = new Parent2()
            {
                Id = 1,
            };
            p.Childrens = new[] {
                    new Child2()
            {
                Id = 2,
                Parent = p,
            },
            new Child2()
            {
                Id = 3,
                Parent = p,
            },
            new Child2()
            {
                Id = 4,
                Parent = p,
            }};
            List<Parent2> list = new List<Parent2>();
            list.Add(p);
            GXDeleteArgs del = GXDeleteArgs.Delete<Child2>(w => list.Contains(w.Parent), _cache);
            AssertSqlEqual(expected, del.ToString(false));
        }

        /// <summary>
        /// Select two columns test.
        /// </summary>
        [TestMethod]
        public virtual void GetFieldsTest(string expected)
        {
            AssertSqlEqual(expected, string.Join(",", GXSqlBuilder.GetFields<TestClass>()));
        }

        /// <summary>
        /// Right join test
        /// </summary>
        [TestMethod]
        public virtual void RightJoinTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>(_cache);
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddRightJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Left join test
        /// </summary>
        [TestMethod]
        public virtual void LeftJoinTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>(_cache);
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddLeftJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Full join test
        /// </summary>
        [TestMethod]
        public virtual void FullJoinTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestClass>(_cache);
            arg.Columns.Add<TestClass2>();
            arg.Joins.AddFullJoin<TestClass2, TestClass>(x => x.Parent, x => x.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        public virtual void WhereSimpleTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Id == 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Time where Datetime is bigger Min date time and Datetime is smaller than max date time and text is not empty.
        /// </summary>
        [TestMethod]
        public virtual void WhereDateTimeTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Time, _cache);
            arg.Where.And<TestClass>(q => q.Time > DateTime.MinValue && q.Time < DateTime.MaxValue);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        public virtual void WhereStringEmptyTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text, _cache);
            arg.Where.And<TestClass>(q => q.Text != string.Empty);
            arg.Where.And<TestClass>(q => q.Text != null);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Text where string is empty or null.
        /// </summary>
        [TestMethod]
        public virtual void WhereStringIsNullOrEmptyTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text, _cache);
            arg.Where.And<TestClass>(q => string.IsNullOrEmpty(q.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Text where string is not empty or null.
        /// </summary>
        [TestMethod]
        public virtual void WhereStringNotIsNullOrEmptyTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Text, _cache);
            arg.Where.And<TestClass>(q => !string.IsNullOrEmpty(q.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Enum is string.
        /// </summary>
        [TestMethod]
        public virtual void WhereEnumTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Settings.UseEnumStringValue = true;
            arg.Where.And<TestClass>(q => q.Status == State.OK);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Enum is saved as int.
        /// </summary>
        [TestMethod]
        public virtual void WhereEnumAsIntTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Settings.UseEnumStringValue = false;
            arg.Where.And<TestClass>(q => q.Status == State.OK);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        public virtual void WhereClassTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Id, _cache);
            arg.Where.And<TestClass>(q => q.Id == t.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where class is given as parameter.
        /// </summary>
        [TestMethod]
        public virtual void WhereClassTest2(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Id, _cache);
            arg.Where.And<TestClass>(q => t);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where class array is given as parameter.
        /// </summary>
        [TestMethod]
        public virtual void WhereClassArrayTest(string expected)
        {
            TestClass[] list = new TestClass[] { new TestClass(), new TestClass() };
            list[0].Id = 1;
            list[1].Id = 2;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => list);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = 1.
        /// </summary>
        [TestMethod]
        public virtual void WhereSimple2Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Id == t.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select all by string test.
        /// </summary>
        [TestMethod]
        public virtual void WhereExactString(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<TestIDClass>(q => q.Text == "Gurux", _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereStartsWithTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Text.StartsWith("Gurux"));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereStartsWith2Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Text.StartsWith(t.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEndsWithTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Text.EndsWith("Gurux"));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text ends with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Text!.Contains("Gurux"));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text contains upper Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsUpperTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            string name = "Gurux";
            arg.Where.And<TestClass>(q => q.Text!.Contains(name.ToUpper()));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text starts with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereStartsWithUpperTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            string name = "Gurux";
            arg.Where.And<TestClass>(q => q.Text.StartsWith(name.ToUpper()));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text ends with upper Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEndsWithUpperTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            string name = "Gurux";
            arg.Where.And<TestClass>(q => q.Text.EndsWith(name.ToUpper()));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where list contains Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereContains2Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            List<string> list = ["Gurux"];
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => list.Contains(q.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text contains with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereContains3Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Text.Contains(t.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where list contains -1.
        /// </summary>
        [TestMethod]
        public virtual void WhereContains5Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            List<int> list = new List<int>();
            list.Add(1);
            list.Add(-1);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => list.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsListGuidTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            List<Guid> list = [Guid.Empty];
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => list.Contains(q.Guid));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where IEnumerable contains Guid.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsIEnumerableGuidTest(string[] guids, string expected)
        {
            var guidsList = guids.Select(a => Guid.Parse(a)).ToList();
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => guidsList.Contains(q.Guid));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where list contains Guid.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsListGuidTest(string[] guids, string expected)
        {
            var guidsList = guids.Select(Guid.Parse).ToList();
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => guidsList.Contains(q.Guid));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where list contains Guid but list is empty.
        /// </summary>
        [TestMethod]
        public virtual void WhereContainsEmptyTest()
        {
            List<TestClass> tmp = [];
            IEnumerable<Guid> list = tmp.Select(s => s.Guid);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, w => list.Contains(w.Guid), _cache);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = arg.ToString(false);
            }, "Where IN expression empty.");
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEqualsTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Text.Equals("Gurux", StringComparison.OrdinalIgnoreCase));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEquals2Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Text.Equals(t.Text, StringComparison.OrdinalIgnoreCase));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEquals3Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Text == t.Text);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEquals4Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Id == t.Id && q.Text!.Equals(t.Text));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where Text equals with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereEquals5Test(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 1;
            t.Text = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Text.Equals(t.Text) && q.Id == t.Id);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        public virtual void WhereOrTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Id == 1 || q.Id == 2 || q.Id == 3);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = -1.
        /// </summary>
        [TestMethod]
        public virtual void WhereMinusTest(string expected)
        {
            int value = -1919693511;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, q => q.Id == value, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = 1 2, or 3.
        /// </summary>
        [TestMethod]
        public virtual void WhereOr2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Id == 1 || q.Id == 2);
            arg.Where.Or<TestClass>(q => q.Id == 3);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID = 1 or text starts with Gurux.
        /// </summary>
        [TestMethod]
        public virtual void WhereOr3Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Id == 1);
            arg.Where.Or<TestClass>(x => x.Text.StartsWith("Gurux"));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        public virtual void WhereAndTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => new { x.Guid }, _cache);
            arg.Where.And<TestClass>(q => q.Id > 1 && q.Id != 2);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID > 1 and not 2.
        /// </summary>
        [TestMethod]
        public virtual void WhereAnd2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => q.Id > 1);
            arg.Where.And<TestClass>(q => q.Id != 2);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlInTest(string expected)
        {

            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => new int[] { 1, 2, 3 }.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlInTest1(string expected)
        {

            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => new byte[] { 1, 2, 3 }.Contains((byte)q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlInTest1_1(string expected)
        {

            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => new long[] { 1, 2, 3 }.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }


        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlInTest2(string expected)
        {
            List<int> list = new List<int> { 1, 2, 3 };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => list.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlInTest3(string expected)
        {
            List<Guid> list = new List<Guid> { Guid.Empty };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => list.Contains(q.Guid));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlNotInTest(string expected)
        {
            int[] list = new int[] { 1, 2, 3 };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => !list.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID not in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlNotInTest2(string expected)
        {
            List<int> list = new List<int> { 1, 2, 3 };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => !list.Contains(q.Id));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlNotInTest3(string expected)
        {
            List<Guid> list = new List<Guid> { Guid.Empty };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => !list.Contains(q.Guid));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public virtual void SqlOrderTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id, _cache);
            arg.OrderBy.Add<TestClass>(q => new { q.Id, q.Guid });
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public virtual void SqlOrder2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id, _cache);
            arg.OrderBy.Add<TestClass>(q => q.Id);
            arg.OrderBy.Add<TestClass>(q => q.Guid);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Order by test.
        /// </summary>
        [TestMethod]
        public virtual void SqlOrder3Test(string expected)
        {
            string value = "ID";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id, _cache);
            arg.OrderBy.Add<TestClass>(value);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Order desc test.
        /// </summary>
        [TestMethod]
        public virtual void SqlOrderDescTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Id, _cache);
            arg.OrderBy.Add<TestClass>(q => q.Id);
            arg.Descending = true;
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public virtual void InsertAllTest(string expected)
        {
            Country c = new Country();
            c.Name = "Finland";
            GXInsertArgs args = GXInsertArgs.Insert(c, _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert name only test.
        /// </summary>
        [TestMethod]
        public virtual void InsertNameOnlyTest(string expected)
        {
            Country c = new Country();
            c.Name = "Finland";
            GXInsertArgs args = GXInsertArgs.Insert(c, i => new { i.Name }, _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert range test.
        /// </summary>
        [TestMethod]
        public virtual void InsertRangeTest(string expected, string expected2)
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
            GXInsertArgs args = GXInsertArgs.InsertRange(list, _cache);
            AssertSqlEqual(expected, args.ToString(false));
            args = GXInsertArgs.InsertRange(list, c => new { c.Name, c.Value }, _cache);
            AssertSqlEqual(expected2, args.ToString(false));
        }

        /// <summary>
        /// Insert emptyrange test.
        /// </summary>
        [TestMethod]
        public virtual void InsertEmptyRangeTest(string expected)
        {
            List<Parameter2> list = new List<Parameter2>();
            GXInsertArgs args = GXInsertArgs.InsertRange(list, _cache);
            Assert.AreEqual(expected, args.ToString(false));
        }


        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public virtual void InsertTest(string expected)
        {
            TestClass t = new TestClass();
            t.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(t, x => new { x.Text, x.Guid }, _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public virtual void InsertTest2(string expected)
        {
            Supplier supplier = new Supplier();
            supplier.Text = "Gurux";
            supplier.NewProducts.Add(new Product2() { Text = "Virtual-serial" });
            GXInsertArgs args = GXInsertArgs.Insert(supplier, _cache);
            args.ToString(false);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Create table test.
        /// </summary>
        [TestMethod]
        public virtual void CreateNullableTableTest(string expected)
        {
            NullableTestClass c = new NullableTestClass();
            c.Active = false;
            c.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(c, _cache);
            string str = args.ToString(false);
            AssertSqlEqual(ResolveExpected(expected, GuidToString(c.Id)), str);
        }

        /// <summary>
        /// Insert test.
        /// </summary>
        [TestMethod]
        public virtual void InsertNullableTest(string expected)
        {
            NullableTestClass c = new NullableTestClass();
            c.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(c, _cache);
            string str = args.ToString(false);
            AssertSqlEqual(ResolveExpected(expected, GuidToString(c.Id)), str);
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public virtual void UpdateTest(string expected)
        {
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Guid, x.Time }, _cache);
            AssertSqlEqual(string.Format(expected, GuidToString(t.Guid), DateTimeToString(t.Time)), args.ToString(false));
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public virtual void UpdateTest2(string expected)
        {
            Guid id = Guid.NewGuid();
            DateTime dt = new DateTime(2014, 1, 2);
            GuidTestClass t = new GuidTestClass();
            t.Id = id;
            t.Time = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, u => u.Time, _cache);
            string format = DateTimeToString(dt);
            string guid = GuidToString(id);
            AssertSqlEqual(ResolveExpected(expected, format, guid), args.ToString(false));
        }

        /// <summary>
        /// Update using where.
        /// </summary>
        [TestMethod]
        public virtual void UpdateWhereTest(string expected)
        {
            DateTime dt = new DateTime(2014, 1, 2);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Guid, x.Time }, _cache);
            args.Where.And<TestClass>(q => q.Text == "Gurux");
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Update default null test.
        /// </summary>
        [TestMethod]
        public virtual void UpdateDefaultNullTest(string expected, string expected2, string expected3)
        {
            Guid id = Guid.NewGuid();
            NullableTestClass value = new NullableTestClass();
            value.Id = id;
            GXUpdateArgs args = GXUpdateArgs.Update(value, _cache);
            AssertSqlEqual(ResolveExpected(expected, GuidToString(id)), args.ToString(false));
            value.Active = true;
            args = GXUpdateArgs.Update(value, _cache);
            AssertSqlEqual(ResolveExpected(expected2, GuidToString(id)), args.ToString(false));
            value.Active = false;
            args = GXUpdateArgs.Update(value, _cache);
            AssertSqlEqual(ResolveExpected(expected3, GuidToString(id)), args.ToString(false));
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public virtual void EpochTimeFormatTest(string expected)
        {
            DateTime dt = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            TestClass t = new TestClass();
            t.Time = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            t.Id = 1;
            GXUpdateArgs args = GXUpdateArgs.Update(t, _cache);
            args.Settings.UseEpochTimeFormat = true;
            args.Add<TestClass>(t, x => x.Time);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public virtual void TableNameTest(string expected)
        {
            GXSqlBuilder parser = new GXSqlBuilder(DatabaseType.MySQL, null);
            Assert.AreEqual(expected, parser.GetTableName<TestClass>());
        }

        /// <summary>
        /// Update test.
        /// </summary>
        [TestMethod]
        public virtual void TableNamePrefixTest(string expected)
        {
            GXSqlBuilder parser = new GXSqlBuilder(DatabaseType.MySQL, "gx_");
            Assert.AreEqual(expected, parser.GetTableName<TestClass>());
        }

        /// <summary>
        /// Where string is null.
        /// </summary>
        [TestMethod]
        public virtual void WhereStringIsNullTest(string expected, string expected2)
        {
            string? text = null;
            GXSelectArgs args = GXSelectArgs.SelectAll<TestClass>(q => q.Text == text, _cache);
            string actual = args.ToString(false);
            AssertSqlEqual(expected, actual);
            args = GXSelectArgs.SelectAll<TestClass>(q => q.Text.Equals(text), _cache);
            actual = args.ToString(false);
            AssertSqlEqual(expected2, actual);
        }

        /// <summary>
        /// Where string is empty.
        /// </summary>
        [TestMethod]
        public virtual void WhereStringIsEmptyTest(string expected)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<TestClass>(q => string.IsNullOrEmpty(q.Text), _cache);
            string actual = args.ToString(false);
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlIn2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlIn3Test(string expected)
        {
            GXSelectArgs sub = GXSelectArgs.Select<Country>(x => x.Id, w => w.Name == "Finland", _cache);
            GXSelectArgs arg = GXSelectArgs.SelectAll<Company>(_cache);
            arg.Where.And<Company>(q => GXSql.In(q.Country, sub));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select Guid where ID in array.
        /// </summary>
        [TestMethod]
        public virtual void SqlNotIn2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.And<TestClass>(q => !GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Select all countries where company exists. 
        /// </summary>
        [TestMethod]
        public virtual void ExistsTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => GXSql.One, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>(_cache);
            arg.Where.And<Country>(q => GXSql.Exists<Country, Company>(b => b.Id, a => a.Country, arg2));
            string actual = arg.ToString(false);
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        public virtual void Exists2Test(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>(_cache);
            arg.Where.And<Country>(q => GXSql.Exists(arg2));
            string actual = arg.ToString(false);
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is in the array.
        /// </summary>
        [TestMethod]
        public virtual void Exists3Test(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => GXSql.One, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>(_cache);
            arg.Where.And<Country>(q => GXSql.Exists(arg2));
            string actual = arg.ToString(false);
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        public virtual void NotExistsTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>(_cache);
            arg.Where.And<Country>(q => !GXSql.Exists<Company, Country>(a => a.Country, b => b.Id, arg2));
            string actual = arg.ToString(false);
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Select Guid where ID is not in the array.
        /// </summary>
        [TestMethod]
        public virtual void NotExists2Test(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.SelectAll<Country>(_cache);
            arg.Where.And<Country>(q => !GXSql.Exists(arg2));
            string actual = arg.ToString(false);
            AssertSqlEqual(expected, actual);
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
            } = default!;
        }

        /// <summary>
        /// Create simple view where data is retreaved from one table.
        /// </summary>
        [TestMethod]
        public virtual void CreateSimpleViewTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => q.Id, _cache);
            arg2.Where.And<Company>(q => q.Name.Equals("Gurux"));
            GXSelectArgs arg = GXSelectArgs.Select<Country>(q => new { q.Id, q.Name }, _cache);
            arg.Where.And<Country>(q => !GXSql.Exists<Company, Country>(a => a.Country, b => b.Id, arg2));
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            string actual = view.ToString();
            AssertSqlEqual(expected, actual);
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
            } = default!;

            public string CountryName
            {
                get;
                set;
            } = default!;
        }

        /// <summary>
        /// Create simple view where data is retreaved from two table.
        /// </summary>
        [TestMethod]
        public virtual void CreateSimpleViewTest2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => new { q.Id, q.Name }, _cache);
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(a => a.Country, b => b.Id);
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            string actual = view.ToString();
            AssertSqlEqual(expected, actual);
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
            } = default!;

            public string Name
            {
                get;
                set;
            } = default!;
        }

        /// <summary>
        /// Create simple view where data is map from two table.
        /// </summary>
        [TestMethod]
        public virtual void CreateSimpleViewTest3(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Company>(q => new { q.Id, q.Name }, _cache);
            arg.Columns.Add<Country>(q => q.Name);
            arg.Joins.AddInnerJoin<Company, Country>(a => a.Country, b => b.Id);
            GXCreateViewArgs view = GXCreateViewArgs.Create<CountriesView>(arg);
            view.Maps.AddMap<CompaniesView2, Company>(t => t.CompanyName, s => s.Name);
            view.Maps.AddMap<CompaniesView2, Country>(t => t.Name, s => s.Name);
            string actual = view.ToString();
            AssertSqlEqual(expected, actual);
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeUpdateTest(string expected)
        {
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            DateTime dt = DateTime.ParseExact("2014-01-02 00:00:00.000", format, CultureInfo.CurrentCulture);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = DateTime.SpecifyKind(new DateTime(2014, 1, 2), DateTimeKind.Utc);
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Id, x.Guid, x.Time }, _cache);
            args.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            AssertSqlEqual(string.Format(expected, GuidToString(t.Guid), DateTimeToString(t.Time)), args.ToString(false));
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeUpdateTest2(string expected)
        {
            DateTime dt = new DateTime(2014, 1, 2);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Time = dt;
            GXUpdateArgs args = GXUpdateArgs.Update(t, _cache);
            args.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            AssertSqlEqual(string.Format(expected, GuidToString(t.Guid), DateTimeToString(t.Time)), args.ToString(false));
        }

        /// <summary>
        /// Exclude update test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeUpdateTest3(string expected)
        {
            DateTime dt = new DateTime(2014, 1, 2);
            TestClass t = new TestClass();
            t.Id = 2;
            t.Text = t.Text2 = "Gurux";
            t.Time = dt;
            GXUpdateArgs args = GXUpdateArgs.Update(t, x => new { x.Id, x.Guid, x.Time }, _cache);
            args.Exclude<TestClass>(x => new { x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            args.Exclude<TestClass>(x => x.Text);
            AssertSqlEqual(string.Format(expected, GuidToString(t.Guid), DateTimeToString(t.Time)), args.ToString(false));
        }

        /// <summary>
        /// Exclude insert test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeInsertTest(string expected)
        {
            TestClass t = new TestClass();
            t.Text = "Gurux";
            GXInsertArgs args = GXInsertArgs.Insert(t, _cache);
            args.Exclude<TestClass>(x => new { x.Time, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Append where test.
        /// </summary>
        [TestMethod]
        public virtual void SelectGuidWhereTest(string expected)
        {
            GXSelectArgs append = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            append.Where.And<TestClass>(q => GXSql.In(q.Id, new int[] { 1, 2, 3 }));
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.Append(append.Where);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        public virtual void FilterByTest(string expected)
        {
            TestClass filter = new TestClass();
            filter.Text2 = "More";
            filter.Text3 = "Gurux";
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.FilterBy(filter);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Filter by test.
        /// </summary>
        [TestMethod]
        public virtual void FilterByTest2(string expected)
        {
            TestClass filter = new TestClass();
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.FilterBy(filter);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Filter by status.
        /// </summary>
        [TestMethod]
        public virtual void FilterByStatus(string expected)
        {
            TestClass filter = new TestClass();
            filter.Status = State.Failed;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.FilterBy(filter);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Filter by date-time.
        /// </summary>
        [TestMethod]
        public virtual void FilterByDateTime(string expected)
        {
            DateTime dt = new DateTime(2014, 1, 2);
            TestClass filter = new TestClass();
            filter.Status = State.OK;
            filter.Time = dt;
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(x => x.Guid, _cache);
            arg.Where.FilterBy(filter);
            AssertSqlEqual(string.Format(expected, DateTimeToString(dt)), arg.ToString(false));
        }

        /// <summary>
        /// Find Empty Guid.
        /// </summary>
        [TestMethod]
        public virtual void FindEmptyGuid(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Guid.Equals(null), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find Not Empty Guid.
        /// </summary>
        [TestMethod]
        public virtual void FindNotEmptyGuid(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => !x.Guid.Equals(null), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        public virtual void FindEmptyDateTime(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Time == null, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        public virtual void FindEmptyDateTime2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Time.Equals(null), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
            arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => x.Time == null, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find not empty date time values.
        /// </summary>
        [TestMethod]
        public virtual void FindNotEmptyDateTime2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, x => !x.Time.Equals(null), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find Empty guid values.
        /// </summary>
        [TestMethod]
        public virtual void EmptyGuidTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => q.Guid.Equals(null) || q.Guid.Equals(Guid.Empty), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find Empty date time values.
        /// </summary>
        [TestMethod]
        public virtual void EmptyDateTimeTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => q.Time.Equals(null) || q.Time.Equals(DateTime.MinValue), _cache);
            AssertSqlEqual(string.Format(expected, DateTimeToString(DateTime.MinValue)), arg.ToString(false));
        }

        /// <summary>
        /// Guid in test.
        /// </summary>
        [TestMethod]
        public virtual void GuidInTest(string expected)
        {
            List<Guid> list = new List<Guid>();
            list.Add(Guid.Empty);
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Guid), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }


        /// <summary>
        /// DateTime in test.
        /// </summary>
        [TestMethod]
        public virtual void DateTimeInTest(string expected)
        {
            List<DateTime> list = new List<DateTime> { DateTime.MinValue };
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Time), _cache);
            AssertSqlEqual(string.Format(expected, DateTimeToString(list[0])), arg.ToString(false));
        }

        /// <summary>
        /// string in test.
        /// </summary>
        [TestMethod]
        public virtual void StringInTest(string expected)
        {
            List<string> list = new List<string>();
            list.Add("Gurux");
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => q.Guid, q => list.Contains(q.Text), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeSelectTest(string expected)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<TestClass>(_cache);
            args.Columns.Exclude<TestClass>(x => new { x.Id, x.Time, x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Exclude select test.
        /// </summary>
        [TestMethod]
        public virtual void ExcludeSelectTest2(string expected)
        {
            GXSelectArgs args = GXSelectArgs.Select<TestClass>(q => new { q.Guid, q.Text }, _cache);
            args.Columns.Exclude<TestClass>(x => new { x.Text, x.Text2, x.Text3, x.Text4, x.BooleanTest, x.IntTest, x.DoubleTest, x.FloatTest, x.Span, x.Object, x.Status });
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public virtual void IsEmptyTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.IsEmpty<TestClass>(_cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public virtual void IsEmpty1Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.IsEmpty<TestClass>(a => a.Id == 1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public virtual void IsEmpty2Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.IsEmpty(q), a => a.Id == 1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Is result empty.
        /// </summary>
        [TestMethod]
        public virtual void IsEmpty3Test(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), a => a.Id == 1, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find rows where Id count is greater than 1.
        /// </summary>
        [TestMethod]
        public virtual void WhereCountTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(g => g.Guid);
            arg.Having.And<TestClass>(q => GXSql.Count(q.Id) > 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find rows where Id count is equal to 1.
        /// </summary>
        [TestMethod]
        public virtual void WhereCountTest2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(g => g.Guid);
            arg.Having.And<TestClass>(q => GXSql.Count(q.Id) == 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        public virtual void HavingTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => a.Guid, _cache);
            arg.GroupBy.Add<TestClass>(g => g.Text);
            arg.Having.And<TestClass>(q => GXSql.Count(q.Id) > 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Find rows that have the same value in the column.
        /// </summary>
        [TestMethod]
        public virtual void HavingTest2(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(a => new { a.Guid, a.Text, a.Status }, _cache);
            arg.GroupBy.Add<TestClass>(q => new { q.Text, q.Status });
            arg.Having.And<TestClass>(q => GXSql.Count(1) > 1);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Name, _cache);
            GXInsertArgs args = GXInsertArgs.Insert<Country>(arg2);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest2(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Country>(_cache);
            GXInsertArgs args = GXInsertArgs.Insert<Country>(arg2);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest3(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Company>(_cache);
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest4(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company>(q => new { q.Name, q.Country }, _cache);
            GXInsertArgs args = GXInsertArgs.Insert<Company2>(arg2, q => new { q.Name, q.Country });
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Copy values from one table to other.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest5(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Company2>(q => new { q.Name, q.Country }, _cache);
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2, q => new { q.Name, q.Country });
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Copy test.
        /// </summary>
        [TestMethod]
        public virtual void CopyTest6(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.SelectAll<Company>(_cache);
            arg2.Joins.AddInnerJoin<Company, Country>(q => q.Country, x => x.Id);
            GXInsertArgs args = GXInsertArgs.Insert<Company>(arg2);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public virtual void InsertSelectedValueTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, _cache);
            Company comp = new Company() { Name = "Gurux" };
            GXInsertArgs args = GXInsertArgs.Insert<Company>(comp, q => new { q.Name, q.Country });
            args.Add<Company>(arg2, q => q.Country);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public virtual void InsertSelectedValue2Test(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, _cache);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField }, _cache);
            args.Add<Company2>(arg2, q => q.Country);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public virtual void InsertSelectedValue3Test(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, _cache);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField }, _cache);
            args.Add<Company2>(arg2, q => q.Country);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public virtual void InsertSelectedValue4Test(string expected)
        {
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => new { q.Name, q.ExtraField }, _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Insert value where data is retreaved from other table.
        /// </summary>
        [TestMethod]
        public virtual void InsertSelectedValue5Test(string expected)
        {
            List<string> list = new List<string>();
            list.Add("Finland");
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, q => list.Contains(q.Name), _cache);
            Company comp = new Company() { Name = "Gurux" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, q => q.Name, _cache);
            args.Add<Company>(arg2, q => q.Country);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// The purpose of this test is check that old column is overrided 
        /// and CountryID is not twice.
        /// </summary>
        [TestMethod]
        public virtual void UpdateInsertParameterTest(string expected)
        {
            GXSelectArgs arg2 = GXSelectArgs.Select<Country>(q => q.Id, _cache);
            Company2 comp = new Company2() { Name = "Gurux", ExtraField = "Extra" };
            GXInsertArgs args = GXInsertArgs.Insert(comp, _cache);
            args.Add<Company2>(arg2, q => q.Country);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        public virtual void UpdateSelectedValueTest(string expected)
        {
            GXSelectArgs sel = GXSelectArgs.Select<Country>(q => q.Id, q => q.Id == 1, _cache);
            Company comp = new Company() { Name = "Gurux" };
            GXUpdateArgs update = GXUpdateArgs.Update<Company>(comp, q => q.Name, _cache);
            update.Where.And<Company>(a => GXSql.Exists(sel));
            AssertSqlEqual(expected, update.ToString(false));
        }

        /// <summary>
        /// Where is used in update syntax.
        /// </summary>
        [TestMethod]
        public virtual void UpdateParameterCollectionTest(string expected, string expected2)
        {
            User2 user = new User2() { Id = 2, Name = "User1" };
            UserGroup2 ug = new UserGroup2() { Id = 1, Name = "Gurux" };
            ug.Users = new User2[] { user };
            GXInsertArgs i = GXInsertArgs.Insert(ug, _cache);
            AssertSqlEqual(expected, i.ToString(false));
            i = GXInsertArgs.Insert(ug, q => q.Users, _cache);
            AssertSqlEqual(expected2, i.ToString(false));
        }

        /// <summary>
        /// Data quota where test.
        /// </summary>
        [TestMethod]
        public virtual void DataQuotaWhereTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Count(q), q => q.Text == "Gurux'", _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Data quota insert test.
        /// </summary>
        [TestMethod]
        public virtual void DataQuotaInsertTest(string expected)
        {
            User2 user = new User2() { Name = "Gurux'" };
            GXInsertArgs i = GXInsertArgs.Insert(user, _cache);
            AssertSqlEqual(expected, i.ToString(false));
        }

        /// <summary>
        /// Data quota update test.
        /// </summary>
        [TestMethod]
        public virtual void DataQuotaUpdateTest(string expected)
        {
            User2 user = new User2() { Id = 2, Name = "Gurux'" };
            GXUpdateArgs args = GXUpdateArgs.Update(user, x => x.Name, _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Data quota delete test.
        /// </summary>
        [TestMethod]
        public virtual void DataQuotaDeleteTest(string expected)
        {
            GXDeleteArgs args = GXDeleteArgs.Delete<TestClass>(q => q.Text == "Gurux'", _cache);
            AssertSqlEqual(expected, args.ToString(false));
        }

        /// <summary>
        /// Sum test.
        /// </summary>
        [TestMethod]
        public virtual void SumTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Sum(q.DoubleTest), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Sum columns test.
        /// </summary>
        [TestMethod]
        public virtual void SumColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Sum(new { q.DoubleTest, q.FloatTest }), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }


        /// <summary>
        /// Min test.
        /// </summary>
        [TestMethod]
        public virtual void MinTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Min(q.DoubleTest), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Min columns test.
        /// </summary>
        [TestMethod]
        public virtual void MinColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Min(new { q.DoubleTest, q.FloatTest }), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Max test.
        /// </summary>
        [TestMethod]
        public virtual void MaxTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Max(q.DoubleTest), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Max columns test.
        /// </summary>
        [TestMethod]
        public virtual void MaxColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Max(new { q.DoubleTest, q.FloatTest }), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Average test.
        /// </summary>
        [TestMethod]
        public virtual void AverageTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Avg(q.DoubleTest), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Average columns test.
        /// </summary>
        [TestMethod]
        public virtual void AverageColumnsTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(q => GXSql.Avg(new { q.DoubleTest, q.FloatTest }), _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Bitwice test.
        /// </summary>
        [TestMethod]
        public virtual void BitwiseTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<TestClass>(s => s.Id, w => (w.IntTest & 1) != 0, _cache);
            AssertSqlEqual(expected, arg.ToString(false));
        }

        /// <summary>
        /// Verify that a query using LEFT JOIN combined with a WHERE Company.Id IS NULL filter 
        /// returns only the rows from the left table that have no matching row in the joined (right) table.
        /// </summary>
        [TestMethod]
        public virtual void NotExistOnJoinTableTest(string expected)
        {
            GXSelectArgs arg = GXSelectArgs.Select<Country>(q => q.Name, _cache);
            arg.Joins.AddInnerJoin<Country, Company>(y => y.Id, x => x.Country);
            arg.Where.And<Company>(w => w.Id == null);
            AssertSqlEqual(expected, arg.ToString(false));
        }

    }
}




