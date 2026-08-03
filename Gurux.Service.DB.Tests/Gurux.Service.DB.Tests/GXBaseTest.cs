using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Model;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection;

namespace Gurux.Service.DB.Tests
{
    abstract public class GXBaseTest
    {
        protected readonly GXDbConnection _connection;
        protected readonly GXSchemaManager _manager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection settings.</param>
        public GXBaseTest(GXDbConnection connection)
        {
            _connection = connection;
            _manager = new GXSchemaManager(_connection);
            _connection.OnSqlExecuted += (sender, sql, executionTime) =>
            {
                System.Diagnostics.Debug.WriteLine(sql);
            };
        }

        private void DateTimeTest(DateTime dt, DateTimeOffset dto)
        {
            try
            {
                if (dto != DateTimeOffset.MinValue && dto != DateTimeOffset.MaxValue)
                {
                    dto = dto.AddMilliseconds(-dto.Millisecond);
                }
                _manager.DropTable<DateTimeTestData>(false);
                _manager.CreateTable<DateTimeTestData>(false, true);
                var args = GXInsertArgs.Insert(new DateTimeTestData()
                {
                    Milliseconds = dt,
                    Seconds = dt,
                    MillisecondsWithTimeZone = dto,
                    SecondsWithTimeZone = dto,
                });
                _connection.Insert(args);
                var all = _connection.SelectAll<DateTimeTestData>().ToList();
                if (all[0].Milliseconds.Hour != dt.Hour ||
                    all[0].Milliseconds.Minute != dt.Minute ||
                    all[0].Milliseconds.Second != dt.Second ||
                    all[0].Milliseconds.Millisecond != dt.Millisecond)
                {
                    Assert.Fail("Milliseconds not stored correctly.");
                }
                if (all[0].Seconds.Hour != dt.Hour ||
                    all[0].Seconds.Minute != dt.Minute ||
                    all[0].Seconds.Second != dt.Second)
                {
                    Assert.Fail("Seconds not stored correctly.");
                }
                if (all[0].MillisecondsWithTimeZone.Hour != dto.Hour ||
                    all[0].MillisecondsWithTimeZone.Minute != dto.Minute ||
                    all[0].MillisecondsWithTimeZone.Second != dto.Second ||
                    all[0].MillisecondsWithTimeZone.Millisecond != dto.Millisecond)
                {
                    Assert.Fail("Milliseconds not stored correctly.");
                }
                if (all[0].SecondsWithTimeZone.Hour != dto.Hour ||
                    all[0].SecondsWithTimeZone.Minute != dto.Minute ||
                    all[0].SecondsWithTimeZone.Second != dto.Second)
                {
                    Assert.Fail("Seconds not stored correctly.");
                }
            }
            finally
            {
                _manager.DropTable<DateTimeTestData>(false);
            }
        }

        /// <summary>
        /// Date-time test.
        /// </summary>
        [TestMethod]
        public void DateTimeTest()
        {
            DateTimeTest(DateTime.Now, DateTimeOffset.Now);
            DateTimeTest(DateTime.MinValue, DateTimeOffset.MinValue);
            DateTimeTest(DateTime.MaxValue, DateTimeOffset.MaxValue);
        }

        /// <summary>
        /// Auto-increment test.
        /// </summary>
        [TestMethod]
        public void AutoIncrementTest()
        {
            AutoIncrementTest<AutoIncrementSByteData, sbyte>();
            AutoIncrementTest<AutoIncrementInt16Data, short>();
            AutoIncrementTest<AutoIncrementInt32Data, int>();
            AutoIncrementTest<AutoIncrementInt64Data, long>();
            AutoIncrementTest<AutoIncrementByteData, byte>();
            AutoIncrementTest<AutoIncrementUInt16Data, ushort>();
            AutoIncrementTest<AutoIncrementUInt32Data, uint>();
            AutoIncrementTest<AutoIncrementUInt64Data, ulong>();
        }

        private void AutoIncrementTest<T, TId>()
            where T : IAutoIncrementData<TId>, new()
            where TId : struct, IConvertible
        {
            try
            {
                _manager.DropTable<T>(false);
                _manager.CreateTable<T>(false, true);
                var expected1 = new T()
                {
                    Name = "Value 1"
                };
                var i = GXInsertArgs.Insert(expected1);
                _connection.Insert(i);
                var actual = _connection.SelectById<T>(Convert.ToUInt64(expected1.Id));
                Assert.AreEqual(expected1.Id, actual.Id);

                var expected2 = new T()
                {
                    Name = "Value 2"
                };
                var expected3 = new T()
                {
                    Name = "Value 3"
                };
                i = GXInsertArgs.InsertRange([expected2, expected3]);
                _connection.Insert(i);
                Assert.AreEqual(ToId<TId>(1 + Convert.ToUInt64(expected1.Id)), expected2.Id);
                Assert.AreEqual(ToId<TId>(1 + Convert.ToUInt64(expected2.Id)), expected3.Id);

                actual = _connection.SelectById<T>(Convert.ToUInt64(expected2.Id));
                Assert.AreEqual(expected2.Id, actual.Id);
                actual = _connection.SelectById<T>(Convert.ToUInt64(expected3.Id));
                Assert.AreEqual(expected3.Id, actual.Id);
            }
            finally
            {
                _manager.DropTable<T>(false);
            }
        }

        private static TId ToId<TId>(ulong value)
            where TId : struct, IConvertible
        {
            return (TId)Convert.ChangeType(value, typeof(TId));
        }
        /// <summary>
        /// Byte array data test.
        /// </summary>
        [TestMethod]
        public void ByteArrayDataTest()
        {
            try
            {
                DateTime dt = DateTime.Now;
                _manager.DropTable<ByteArrayData>(false);
                _manager.CreateTable<ByteArrayData>(false, true);
                var expected = new ByteArrayData()
                {
                    Value = [1, 2, 3, 4]
                };

                var i = GXInsertArgs.Insert(expected);
                _connection.Insert(i);
                var args = GXSelectArgs.SelectAll<ByteArrayData>(w => w.Id == expected.Id);
                var actual = _connection.SingleOrDefault<ByteArrayData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.HasCount(expected.Value.Length, actual.Value);
                if (actual.Value[0] != expected.Value[0] ||
                    actual.Value[1] != expected.Value[1] ||
                    actual.Value[2] != expected.Value[2] ||
                    actual.Value[3] != expected.Value[3])
                {
                    Assert.Fail("Byte array not stored correctly.");
                }
                expected.Value = [5, 6];
                var u = GXUpdateArgs.Update(expected);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<ByteArrayData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.HasCount(expected.Value.Length, actual.Value);
                if (actual.Value[0] != expected.Value[0] ||
                    actual.Value[1] != expected.Value[1])
                {
                    Assert.Fail("Byte array not stored correctly.");
                }
                var d = GXDeleteArgs.Delete(expected);
                _connection.Delete(d);
            }
            finally
            {
                _manager.DropTable<ByteArrayData>(false);
            }
        }

        /// <summary>
        /// Test data test.
        /// </summary>
        [TestMethod]
        public void TestDataTest()
        {
            try
            {
                DateTime dt = DateTime.Now;
                _manager.DropTable<TestData>(false);
                _manager.CreateTable<TestData>(false, true);
                var expected = new TestData()
                {
                    Name = "Test",
                    Identifier = Guid.NewGuid()
                };
                //Add just id and name.
                var i = GXInsertArgs.Insert(expected, c => new { c.Id, c.Name });
                _connection.Insert(i);
                var args = GXSelectArgs.SelectAll<TestData>(w => w.Id == expected.Id);
                var actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                Assert.AreEqual(null, actual.Identifier);
                var d = GXDeleteArgs.Delete(expected);
                _connection.Delete(d);
                expected = new TestData()
                {
                    Name = "Test",
                    Identifier = Guid.NewGuid()
                };
                i = GXInsertArgs.Insert(expected);
                _connection.Insert(i);
                args = GXSelectArgs.SelectAll<TestData>(w => w.Id == expected.Id);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                Assert.AreEqual(expected.Identifier, actual.Identifier);

                expected.Identifier = null;
                expected.Name = null;
                var u = GXUpdateArgs.Update(expected);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                Assert.AreEqual(expected.Identifier, actual.Identifier);
                expected.Identifier = Guid.NewGuid();
                expected.Name = "c:\\";
                u = GXUpdateArgs.Update(expected);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Name, actual.Name);
                Assert.AreEqual(expected.Identifier, actual.Identifier);
                //Test name update with special character.
                expected.Name = "'��'";
                u = GXUpdateArgs.Update(expected, c => c.Name);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Identifier, actual.Identifier);
                //Test name update with special character.
                expected.Name = "C:\\temp";
                u = GXUpdateArgs.Update(expected, c => c.Name);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Identifier, actual.Identifier);
                //Boolean test.
                expected.Active = true;
                u = GXUpdateArgs.Update(expected, c => c.Active);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Active, actual.Active);
                //Boolean test.
                expected.Active = false;
                u = GXUpdateArgs.Update(expected, c => c.Active);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Active, actual.Active);
                //Boolean test.
                expected.Active = null;
                u = GXUpdateArgs.Update(expected, c => c.Active);
                _connection.Update(u);
                actual = _connection.SingleOrDefault<TestData>(args);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Active, actual.Active);


                d = GXDeleteArgs.Delete(expected);
                _connection.Delete(d);
            }
            finally
            {
                _manager.DropTable<ByteArrayData>(false);
            }
        }

        private string GetUserName(DatabasePermission value)
        {
            if (_connection.DatabaseType == DatabaseType.SapHana ||
                _connection.DatabaseType == DatabaseType.Oracle)
            {
                return "USER_" + value.ToString().ToUpper();
            }
            return "user_" + value.ToString();
        }

        private string GetPassword()
        {
            if (_connection.DatabaseType == DatabaseType.SapHana)
            {
                return "Gurux123!";
            }
            return "Gurux123";
        }

        /// <summary>
        /// Database users test.
        /// </summary>
        [TestMethod]
        public void DatabaseUsersTest()
        {
            if (_connection.DatabaseType == DatabaseType.SqLite ||
                _connection.DatabaseType == DatabaseType.DB2)
            {
                //SQLite and DB2 do not support users.
                return;
            }
            string databaseName = "UsersTestDb";
            try
            {
                if (_connection.DatabaseExists(databaseName))
                {
                    _manager.DropDatabase(databaseName);
                }
                if (_connection.DatabaseType != DatabaseType.Oracle)
                {
                    _manager.CreateDatabase(databaseName);
                }
                else
                {
                    var users = _connection.GetUsers();
                    _connection.AddUsers([new DatabaseUser(databaseName, GetPassword())]);
                }
                //Get database users. Should be empty.
                var dbUsers = _connection.GetUsers(databaseName);
                if (_connection.DatabaseType != DatabaseType.PostgreSQL)
                {
                    //Postgre SQL returns all users as a default.
                    Assert.AreEqual(0, dbUsers.Count());
                }
                //Validate connected user permissions.
                var permissions = _connection.GetUserPermission();
                var userName = _connection.GetCurrentUser();
                var permissions2 = _connection.GetUserPermission(userName);
                Assert.AreEqual(permissions, permissions2);

                //Get all users.
                var allUsers = _connection.GetUsers();
                allUsers = allUsers.Where(it => it.StartsWith("USER_")).ToArray();
                if (allUsers.Any())
                {
                    _connection.RemoveUsers(allUsers);
                }
                allUsers = _connection.GetUsers();
                int count = allUsers.Count();
                Assert.AreNotEqual(0, count);

                foreach (var it in Enum.GetValues<DatabasePermission>())
                {
                    string name = GetUserName(it);
                    if (!allUsers.Contains(name))
                    {
                        _connection.AddUsers(new DatabaseUser(name, GetPassword()));
                        ++count;
                    }
                }
                allUsers = _connection.GetUsers();
                foreach (var it in Enum.GetValues<DatabasePermission>())
                {
                    string name = GetUserName(it);
                    if (!allUsers.Contains(name))
                    {
                        throw new Exception("User " + name + " not found.");
                    }
                }

                Assert.AreEqual(count, allUsers.Count());
                dbUsers = _connection.GetUsers(databaseName);
                if (_connection.DatabaseType != DatabaseType.PostgreSQL)
                {
                    //Postgre SQL returns all users as a default.
                    Assert.AreEqual(0, dbUsers.Count());
                }
                //Add users to database.
                foreach (var it in _manager.AvailablePermissions())
                {
                    string name = GetUserName(it);
                    if (allUsers.Contains(name))
                    {
                        _connection.AddUsersToDatabase(databaseName, it, name);
                    }
                }
                dbUsers = _connection.GetUsers(databaseName);
                if (_connection.DatabaseType != DatabaseType.PostgreSQL)
                {
                    foreach (var it in _manager.AvailablePermissions())
                    {
                        string name = GetUserName(it);
                        var permission = _connection.GetUserPermission(name, databaseName);
                        if (_connection.DatabaseType == DatabaseType.MySQL ||
                            _connection.DatabaseType == DatabaseType.MariaDB)
                        {
                            if (it == DatabasePermission.CreateFunction ||
                                it == DatabasePermission.CreateProcedure)
                            {
                                Assert.AreEqual(it, (permission & it));
                                continue;
                            }
                        }
                        if (it != DatabasePermission.Admin && it != DatabasePermission.Connect)
                        {
                            permission &= ~DatabasePermission.Connect;
                            Assert.AreEqual(it, permission);
                        }
                        else
                        {
                            Assert.AreEqual(it, permission);
                        }
                    }
                }
                foreach (var it in _manager.AvailablePermissions())
                {
                    string name = GetUserName(it);
                    //  if (_connection.DatabaseType != DatabaseType.PostgreSQL)
                    {
                        _connection.RemoveUsersFromDatabase(databaseName, name);
                        dbUsers = _connection.GetUsers(databaseName);


                        if (dbUsers.Contains(name))
                        {
                            throw new Exception("User " + name + " not removed from database.");
                        }
                    }
                    _connection.RemoveUsers([name]);
                    --count;
                }
                dbUsers = _connection.GetUsers(databaseName);
                allUsers = _connection.GetUsers(null);
                Assert.AreEqual(count, allUsers.Count());
            }
            finally
            {
                _manager.DropDatabase(databaseName);
            }
        }

        /// <summary>
        /// Database users test.
        /// </summary>
        [TestMethod]
        public void DatabaseUsersTest2()
        {
            if (_connection.DatabaseType == DatabaseType.SqLite ||
                _connection.DatabaseType == DatabaseType.DB2)
            {
                //SQLite and DB2 do not support users.
                return;
            }
            //Validate connected user permissions.
            var permissions = _connection.GetUserPermission();
            var userName = _connection.GetCurrentUser();
            var permissions2 = _connection.GetUserPermission(userName);
            Assert.AreEqual(permissions, permissions2);

            //Get all users.
            var allUsers = _connection.GetUsers(null);
            allUsers = allUsers.Where(it => it.StartsWith("USER_")).ToArray();
            if (allUsers.Any())
            {
                _connection.RemoveUsers(allUsers);
            }
            allUsers = _connection.GetUsers(null);
            int count = allUsers.Count();
            Assert.AreNotEqual(0, count);
            foreach (var it in Enum.GetValues<DatabasePermission>())
            {
                if (it != DatabasePermission.None)
                {
                    string name = "user_" + it.ToString();
                    if (!allUsers.Contains(name))
                    {
                        _connection.AddUsers(new DatabaseUser(name, GetPassword()));
                        ++count;
                    }
                }
            }
            allUsers = _connection.GetUsers();
            Assert.AreEqual(count, allUsers.Count());
            foreach (var it in Enum.GetValues<DatabasePermission>())
            {
                if (it != DatabasePermission.None)
                {
                    if (_connection.DatabaseType != DatabaseType.SapHana &&
                        _connection.DatabaseType != DatabaseType.Oracle)
                    {
                        string name = "user_" + it.ToString();
                        if (!allUsers.Contains(name))
                        {
                            throw new Exception("User " + name + " not found.");
                        }
                    }
                    else
                    {
                        string name = "USER_" + it.ToString().ToUpper();
                        if (!allUsers.Contains(name))
                        {
                            throw new Exception("User '" + name + "' not found.");
                        }
                    }
                }
            }
            foreach (var it in Enum.GetValues<DatabasePermission>())
            {
                if (it != DatabasePermission.None)
                {
                    string name = "user_" + it.ToString();
                    if (allUsers.Contains(name))
                    {
                        _connection.RemoveUsers([name]);
                        --count;
                    }
                }
            }
            allUsers = _connection.GetUsers(null);
            Assert.AreEqual(count, allUsers.Count());
        }

        [TestMethod]
        public void DatabaseRenameTest()
        {

        }
        [TestMethod]
        public void ColumnRenameTest()
        {
            List<DataTypesData> expected = [];
            EventHandler<GXColumnValueConvertingEventArgs> converter = (sender, e) =>
            {
                if (e.OldType == typeof(string) && e.NewType == typeof(byte[]))
                {
                    e.Value = Convert.FromHexString((string)e.Value);
                    e.IsConverted = true;
                }
            };
            try
            {
                _manager.DropTable<DataTypesData>(false);
                _manager.DropTable<DataTypesStringData>(false);
                _manager.CreateTable<DataTypesStringData>(false, true);
                _manager.RenameTable<DataTypesStringData>("DataTypesData");

                List<DataTypesStringData> rows = [];
                for (int pos = 0; pos != 10; ++pos)
                {
                    byte[] byteArray = new byte[4];
                    Random.Shared.NextBytes(byteArray);
                    Guid id = Guid.NewGuid();
                    string text = Guid.NewGuid().ToString("N")[..16];
                    byte byteValue = (byte)Random.Shared.Next(byte.MinValue, byte.MaxValue + 1);
                    sbyte sbyteValue = (sbyte)Random.Shared.Next(sbyte.MinValue, sbyte.MaxValue + 1);
                    short shortValue = (short)Random.Shared.Next(short.MinValue, short.MaxValue + 1);
                    int intValue = Random.Shared.Next(-1000000000, 1000000000);
                    long longValue = Random.Shared.NextInt64(-1000000000000, 1000000000000);
                    float floatValue = Random.Shared.Next(-100000, 100000);
                    double doubleValue = Random.Shared.Next(-100000, 100000);
                    decimal decimalValue = Random.Shared.Next(-100000, 100000);
                    bool boolValue = Random.Shared.Next(2) != 0;
                    DateTime dateTimeValue = new(2020 + Random.Shared.Next(7),
                        Random.Shared.Next(1, 13), Random.Shared.Next(1, 28),
                        Random.Shared.Next(24), Random.Shared.Next(60), Random.Shared.Next(60));
                    ushort uint16Value = (ushort)Random.Shared.Next(ushort.MinValue, ushort.MaxValue + 1);
                    uint uint32Value = (uint)Random.Shared.NextInt64(uint.MinValue, (long)uint.MaxValue + 1);
                    ulong uint64Value = (ulong)Random.Shared.NextInt64(0, 1000000000000);

                    rows.Add(new DataTypesStringData
                    {
                        Id = id.ToString(),
                        Text1 = text,
                        ByteArray = Convert.ToHexString(byteArray),
                        ByteValue = byteValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        SByteValue = sbyteValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ShortValue = shortValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        IntValue = intValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        LongValue = longValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        FloatValue = floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        DoubleValue = doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        DecimalValue = decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        BoolValue = boolValue.ToString(),
                        DateTimeValue = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture),
                        UInt16Value = uint16Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        UInt32Value = uint32Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        UInt64Value = uint64Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
                    expected.Add(new DataTypesData
                    {
                        Id = id,
                        Text1 = text,
                        ByteArray = byteArray,
                        ByteValue = byteValue,
                        SByteValue = sbyteValue,
                        ShortValue = shortValue,
                        IntValue = intValue,
                        LongValue = longValue,
                        FloatValue = floatValue,
                        DoubleValue = doubleValue,
                        DecimalValue = decimalValue,
                        BoolValue = boolValue,
                        DateTimeValue = dateTimeValue,
                        UInt16Value = uint16Value,
                        UInt32Value = uint32Value,
                        UInt64Value = uint64Value
                    });
                }
                var i = GXInsertArgs.InsertRange(rows);
                _connection.Insert(i);

                _manager.ColumnValueConverting += converter;
                _manager.UpdateTable<DataTypesData>();
                _manager.ColumnValueConverting -= converter;

                List<DataTypesData> actual = _connection.SelectAll<DataTypesData>().ToList();
                Assert.HasCount(expected.Count, actual);
                foreach (DataTypesData item in expected)
                {
                    DataTypesData converted = actual.Single(q => q.Id == item.Id);
                    Assert.AreEqual(item.Text1, converted.Text1);
                    CollectionAssert.AreEqual(item.ByteArray, converted.ByteArray);
                    Assert.AreEqual(item.ByteValue, converted.ByteValue);
                    Assert.AreEqual(item.SByteValue, converted.SByteValue);
                    Assert.AreEqual(item.ShortValue, converted.ShortValue);
                    Assert.AreEqual(item.IntValue, converted.IntValue);
                    Assert.AreEqual(item.LongValue, converted.LongValue);
                    Assert.AreEqual(item.FloatValue, converted.FloatValue);
                    Assert.AreEqual(item.DoubleValue, converted.DoubleValue);
                    Assert.AreEqual(item.DecimalValue, converted.DecimalValue);
                    Assert.AreEqual(item.BoolValue, converted.BoolValue);
                    Assert.AreEqual(item.DateTimeValue, converted.DateTimeValue);
                    Assert.AreEqual(item.UInt16Value, converted.UInt16Value);
                    Assert.AreEqual(item.UInt32Value, converted.UInt32Value);
                    Assert.AreEqual(item.UInt64Value, converted.UInt64Value);
                }
            }
            finally
            {
                _manager.ColumnValueConverting -= converter;
                _manager.DropTable<DataTypesData>(false);
            }
        }

        /// <summary>
        /// Auto-increment test.
        /// </summary>
        [TestMethod]
        public void GenerateDataTypesDataTest()
        {
            try
            {
                _manager.DropTable<DataTypesData>(false);
                _manager.CreateTable<DataTypesData>(false, true);
                GXTableSchema table = _manager.Describe<DataTypesData>();
                PropertyInfo[] properties = typeof(DataTypesData).GetProperties();

                Assert.AreEqual(properties.Length, table.Columns.Count,
                    "Property and column counts do not match.");

                System.ComponentModel.DescriptionAttribute description;
                //SQLIte doesn't support comments.
                if (_connection.DatabaseType != DatabaseType.SqLite)
                {
                    description = typeof(DataTypesData).GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                    if (description != null && description.Description != table.Comment)
                    {
                        Assert.AreEqual(description.Description, table.Comment,
                            $"Comment of column {typeof(DataTypesData).Name} does not match the property description.");
                    }
                }
                for (int pos = 0; pos != properties.Length; ++pos)
                {
                    PropertyInfo property = properties[pos];
                    GXColumnSchema? column = table.Columns.SingleOrDefault(it =>
                        string.Equals(it.Name, property.Name,
                            StringComparison.OrdinalIgnoreCase));
                    Assert.IsNotNull(column,
                        $"Column for property {property.Name} was not found.");

                    Assert.AreEqual(1 + pos, column.Ordinal,
                          $"Ordinal of column {property.Name} does not match the property position.");

                    Type expectedType = Nullable.GetUnderlyingType(property.PropertyType)
                        ?? property.PropertyType;
                    if (_connection.DatabaseType == DatabaseType.PostgreSQL)
                    {
                        if (expectedType == typeof(sbyte) || expectedType == typeof(byte))
                        {
                            //Postgre SQL uses same data type for byte and sbyte.
                            expectedType = typeof(short);
                        }
                        else if (expectedType == typeof(UInt16))
                        {
                            expectedType = typeof(Int32);
                        }
                        else if (expectedType == typeof(UInt32))
                        {
                            expectedType = typeof(Int64);
                        }
                    }
                    else if (_connection.DatabaseType == DatabaseType.Oracle)
                    {
                        if (expectedType == typeof(sbyte))
                        {
                            //Oracle uses same data type for byte and sbyte.
                            expectedType = typeof(byte);
                        }
                        else if (expectedType == typeof(Single))
                        {
                            expectedType = typeof(decimal);
                        }
                        else if (expectedType == typeof(UInt16))
                        {
                            expectedType = typeof(Int16);
                        }
                        else if (expectedType == typeof(UInt32))
                        {
                            expectedType = typeof(Int32);
                        }
                    }
                    else if (_connection.DatabaseType == DatabaseType.SqLite)
                    {
                        if (column.Type == typeof(UInt64))
                        {
                            expectedType = typeof(UInt64);
                        }
                        else if (expectedType == typeof(Guid))
                        {
                            //SQLite don't have data type for Guid.
                            expectedType = typeof(byte[]);
                        }
                        else if (expectedType == typeof(sbyte) || expectedType == typeof(byte)
                            || expectedType == typeof(bool))
                        {
                            //SQLite uses same data type for byte and sbyte.
                            expectedType = typeof(Int32);
                        }
                        else if (expectedType == typeof(UInt16) ||
                            expectedType == typeof(Int16))
                        {
                            expectedType = typeof(Int32);
                        }
                        else if (expectedType == typeof(UInt32))
                        {
                            expectedType = typeof(Int64);
                        }
                        else if (expectedType == typeof(double))
                        {
                            expectedType = typeof(Single);
                        }
                        else if (expectedType == typeof(DateTime))
                        {
                            expectedType = typeof(string);
                        }
                    }
                    else if (_connection.DatabaseType == DatabaseType.SapHana)
                    {
                        if (expectedType == typeof(sbyte))
                        {
                            expectedType = typeof(Int16);
                        }
                        else if (expectedType == typeof(UInt16))
                        {
                            expectedType = typeof(Int32);
                        }
                        else if (expectedType == typeof(UInt32))
                        {
                            expectedType = typeof(Int64);
                        }
                    }
                    Assert.AreEqual(expectedType, column.Type,
                        $"Type of column {property.Name} does not match the property type.");
                    MaxLengthAttribute? maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                    if (maxLength != null)
                    {
                        if (_connection.DatabaseType == DatabaseType.PostgreSQL ||
                            _connection.DatabaseType == DatabaseType.SqLite)
                        {
                            if (column.Type != typeof(byte[]))
                            {
                                //Postgre and SQLite don't save the max length of the byte array to the schema.
                                Assert.AreEqual((long)maxLength.Length, column.MaxLength,
                                    $"Maximum length of column {property.Name} does not match the property.");
                            }
                        }
                        else
                        {
                            Assert.AreEqual((long)maxLength.Length, column.MaxLength,
                                $"Maximum length of column {property.Name} does not match the property.");
                        }
                    }
                    if (_connection.DatabaseType != DatabaseType.SqLite)
                    {
                        description = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                        if (description != null && description.Description != column.Comment)
                        {
                            Assert.AreEqual(description.Description, column.Comment,
                                $"Comment of column {property.Name} does not match the property description.");
                        }
                    }
                    var defaultValue = property.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>();
                    if (defaultValue != null && !Equals(defaultValue.Value, column.DefaultValue))
                    {
                        Assert.AreEqual(defaultValue.Value, column.DefaultValue,
                            $"Default value of column {property.Name} does not match the property default value.");
                    }

                }
            }
            finally
            {
                _manager.DropTable<DataTypesData>(false);
            }
        }

        /// <summary>
        /// Auto-increment test.
        /// </summary>
        [TestMethod]
        public void GenerateDataTypesDataDefaultValueTest()
        {
            try
            {
                _manager.DropTable<DataTypesDataDefaultValue>(false);
                _manager.CreateTable<DataTypesDataDefaultValue>(false, true);
                GXTableSchema table = _manager.Describe<DataTypesDataDefaultValue>();
                PropertyInfo[] properties = typeof(DataTypesDataDefaultValue).GetProperties();

                Assert.AreEqual(properties.Length, table.Columns.Count,
                    "Property and column counts do not match.");

                var description = typeof(DataTypesDataDefaultValue).GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                if (description != null && description.Description != table.Comment)
                {
                    Assert.AreEqual(description.Description, table.Comment,
                        $"Comment of column {typeof(DataTypesData).Name} does not match the property description.");
                }
                for (int pos = 0; pos != properties.Length; ++pos)
                {
                    PropertyInfo property = properties[pos];
                    GXColumnSchema? column = table.Columns.SingleOrDefault(it =>
                        string.Equals(it.Name, property.Name,
                            StringComparison.OrdinalIgnoreCase));
                    Assert.IsNotNull(column,
                        $"Column for property {property.Name} was not found.");

                    Assert.AreEqual(1 + pos, column.Ordinal,
                          $"Ordinal of column {property.Name} does not match the property position.");

                    Type expectedType = Nullable.GetUnderlyingType(property.PropertyType)
                        ?? property.PropertyType;
                    // Assert.AreEqual(expectedType, column.Type,
                    //     $"Type of column {property.Name} does not match the property type.");
                    MaxLengthAttribute? maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                    if (maxLength != null)
                    {
                        Assert.AreEqual((long)maxLength.Length, column.MaxLength,
                            $"Maximum length of column {property.Name} does not match the property.");
                    }
                    description = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                    if (description != null && description.Description != column.Comment)
                    {
                        Assert.AreEqual(description.Description, column.Comment,
                            $"Comment of column {property.Name} does not match the property description.");
                    }
                    var defaultValue = property.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>();
                    if (defaultValue != null && !Equals(defaultValue.Value, column.DefaultValue))
                    {
                        if (_connection.DatabaseType == DatabaseType.PostgreSQL ||
                            _connection.DatabaseType == DatabaseType.Oracle ||
                            _connection.DatabaseType == DatabaseType.SqLite ||
                            _connection.DatabaseType == DatabaseType.SapHana)
                        {
                            //Postgre and Oracle SQL uses same data type for byte and sbyte. So, we need to convert the default value to int for comparison.
                            Assert.AreEqual(Convert.ToInt32(defaultValue.Value), Convert.ToInt32(column.DefaultValue),
                            $"Default value of column {property.Name} does not match the property default value.");
                        }
                        else
                        {
                            Assert.AreEqual(defaultValue.Value, column.DefaultValue,
                                $"Default value of column {property.Name} does not match the property default value.");
                        }
                    }

                }
            }
            finally
            {
                _manager.DropTable<DataTypesDataDefaultValue>(false);
            }
        }
    }
}
