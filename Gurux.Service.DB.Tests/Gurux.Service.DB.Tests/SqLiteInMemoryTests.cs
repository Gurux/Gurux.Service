using Gurux.Service.Orm;
using Microsoft.Data.Sqlite;

namespace Gurux.Service.DB.Tests
{
    [TestClass]
    public class SqLiteInMemoryTests : GXBaseTest
    {
        public SqLiteInMemoryTests(): base(new GXDbConnection(
            new SqliteConnection("Data Source=:memory:"), null))
        {
        }
    }
}