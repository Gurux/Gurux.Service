using Gurux.Service.Orm;
using IBM.Data.Db2;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    ///docker run -d --name db2 --privileged=true -p 50000:50000 -e LICENSE=accept -e DB2INST1_PASSWORD=Gurux123 -e DBNAME=SAMPLE icr.io/db2_community/db2
    ///
    /// su - db2inst1
    /// db2
    /// drop database SAMPLE
    /// create database SAMPLE
    /// </summary>
    [TestClass]
    public class Db2SqlTests : GXBaseTest
    {
        public Db2SqlTests() : base(new GXDbConnection(
            new DB2Connection("Server=localhost:50000;Database=SAMPLE;UID=db2inst1;PWD=Gurux123;"), null))
        {
        }
    }
}