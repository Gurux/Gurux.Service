using Gurux.Service.Orm;
using Npgsql;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// docker run -d -e POSTGRES_USER=gurux -e POSTGRES_PASSWORD=Gurux123 -e POSTGRES_DB=guruxamidb -p 5432:5432 postgres
    /// 
    /// psql -h localhost -p 5432 -U gurux -d guruxamidb
    /// DROP SCHEMA public CASCADE;
    /// CREATE SCHEMA public;
    /// </summary>
    /// 
    [TestClass]
    public class PostgreSqlTests : GXBaseTest
    {
        public PostgreSqlTests(): base(new GXDbConnection(
            new NpgsqlConnection("Host=localhost;Port=5432;Database=guruxamidb;Username=gurux;Password=Gurux123"), null))
        {
        }
    }
}