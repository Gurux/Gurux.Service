using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using System.Data.Common;

namespace Gurux.Service.DB.Tests
{
    abstract public class GXBaseTest
    {
        protected readonly GXDbConnection _connection;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connection">DB connection settings.</param>
        public GXBaseTest(GXDbConnection connection)
        {
            _connection = connection;
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
                _connection.DropTable<DateTimeTestData>(false);
                _connection.CreateTable<DateTimeTestData>(false, true);
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
                _connection.DropTable<DateTimeTestData>(false);
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
                _connection.DropTable<T>(false);
                _connection.CreateTable<T>(false, true);
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
                _connection.DropTable<T>(false);
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
                _connection.DropTable<ByteArrayData>(false);
                _connection.CreateTable<ByteArrayData>(false, true);
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
                _connection.DropTable<ByteArrayData>(false);
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
                _connection.DropTable<TestData>(false);
                _connection.CreateTable<TestData>(false, true);
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
                _connection.DropTable<ByteArrayData>(false);
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
                    _connection.DropDatabase(databaseName);
                }
                if (_connection.DatabaseType != DatabaseType.Oracle)
                {
                    _connection.CreateDatabase(databaseName);
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
                foreach (var it in _connection.AvailablePermissions())
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
                    foreach (var it in _connection.AvailablePermissions())
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
                foreach (var it in _connection.AvailablePermissions())
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
                _connection.DropDatabase(databaseName);
            }
        }

        /// <summary>
        /// Users test.
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
    }
}