using Gurux.Service.Orm;
using MySqlConnector;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// docker run --name=guruxamidb -e MYSQL_ROOT_PASSWORD=Gurux123 -p 3306:3306 -d  mysql
    /// mysql -u root -p
    /// CREATE DATABASE GuruxAmi;
    /// USE GuruxAmi;
    /// CREATE USER 'GuruxAmiUser'@'%' IDENTIFIED BY 'Gurux123';
    /// GRANT ALL PRIVILEGES ON *.* TO 'GuruxAmiUser'@'%';
    /// </summary>
    [TestClass]
    public class GXMySQLTest : GXBaseTest
    {
        public GXMySQLTest(): 
            base(new GXDbConnection(
                new MySqlConnection("Server=localhost;Database=GuruxAmi;UID=GuruxAmiUser;Password=Gurux123"), null))
        {
        }
    }
}