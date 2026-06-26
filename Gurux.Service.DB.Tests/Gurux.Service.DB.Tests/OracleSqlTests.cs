using Gurux.Service.Orm;
using Oracle.ManagedDataAccess.Client;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// docker run -d -p 1521:1521 container-registry.oracle.com/database/express:latest  

    /// docker run -d -p 1521:1521 container-registry.oracle.com/database/express:21.3.0-xe  
    /// sqlplus / as sysdba
    /// ALTER SESSION SET CONTAINER=XEPDB1;
    /// CREATE USER GuruxAmiUser IDENTIFIED BY Gurux123 DEFAULT TABLESPACE users QUOTA UNLIMITED ON USERS;
    /// GRANT CONNECT, RESOURCE, CREATE SESSION, CREATE TABLE TO GuruxAmiUser;
    /// 
    /// TAI:
    /// ALTER SESSION SET CONTAINER=XEPDB1;
    /// CREATE USER C##GuruxAmiUser IDENTIFIED BY Gurux123 DEFAULT TABLESPACE users QUOTA UNLIMITED ON USERS;
    /// GRANT CONNECT, RESOURCE, CREATE SESSION, CREATE TABLE TO C##GuruxAmiUser;

    // DROP USER GuruxAmiUser CASCADE;

    /// //Näytä taulut kaikista schemoista.
    /// SELECT owner, table_name FROM all_tables WHERE table_name LIKE 'GXUSER' ORDER BY owner, table_name;
    /// select * from GURUXAMIUSER.GXUSER;
    /// select * from GURUXAMIUSER.GXUSERROLE;
    /// select Id, Email from GURUXAMIUSER.GXUSER;
    /// select NAME from GURUXAMIUSER.GXROLE;
    /// select Id, Name, NormalizedName from GURUXAMIUSER.GXROLE;
    /// </summary>
    /// 
    [TestClass]
    public class OracleSqlTests : GXBaseTest
    {
        public OracleSqlTests(): base(new GXDbConnection(
            new OracleConnection("User Id=GuruxAmiUser;Password=Gurux123;Data Source=localhost/XEPDB1"), null))
        {
        }
    }
}