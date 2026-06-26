using Gurux.Service.Orm;
using MySqlConnector;
namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// docker run -e MARIADB_ROOT_PASSWORD=Gurux123 -p 3307:3307 -d  mariadb
    /// mariadb -u root -p
    /// CREATE DATABASE GuruxAmi;
    /// USE GuruxAmi;
    /// CREATE USER 'GuruxAmiUser'@'%' IDENTIFIED BY 'Gurux123';
    /// GRANT ALL PRIVILEGES ON *.* TO 'GuruxAmiUser'@'%';
    /// Change port from 3306 to 3307
    /// apt install nano
    /// nano /etc/mysql/my.cnf
    /// uncomment port=3307
    /// </summary>
    [TestClass]
    public class MariaDbSQLTest : GXBaseTest
    {
        public MariaDbSQLTest(): base(new GXDbConnection(
            new MySqlConnection("Server=localhost;Port=3307;Database=GuruxAmi;UID=GuruxAmiUser;Password=Gurux123"), null))
        {
        }
    }
}