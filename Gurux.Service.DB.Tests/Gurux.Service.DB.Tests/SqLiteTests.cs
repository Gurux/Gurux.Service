using Gurux.Service.Orm;
using Microsoft.Data.Sqlite;

namespace Gurux.Service.DB.Tests
{
    [TestClass]
    public class SqLiteTests : GXBaseTest
    {
        private static string GetConnectionString()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = "test.sqlite",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };
            return builder.ToString();
        }
        public SqLiteTests() : base(new GXDbConnection(
            new SqliteConnection(GetConnectionString()), null))
        {
        }
    }
}