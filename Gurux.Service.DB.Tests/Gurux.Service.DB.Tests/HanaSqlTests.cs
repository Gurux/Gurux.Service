using Gurux.Service.Orm;
using Sap.Data.Hana;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// echo '{ "master_password": "Gurux123!" }' > C:\projects\sap-password.json
    /// docker run -d --name hana  --hostname 25485ace1d4c -p 39015:39015 -p 39013:39013 -p 39017:39017  -p 39041-39045:39041-39045 -v C:\projects:/hana/mounts saplabs/hanaexpress:latest --agree-to-sap-license --passwords-url file:///hana/mounts/sap-password.json
    /// </summary>
    /// 
    [TestClass]
    public class HanaSqlTests : GXBaseTest
    {
        public HanaSqlTests() : base(new GXDbConnection(
            new HanaConnection("Server=localhost:39041;UserID=SYSTEM;Password=Gurux123!"), null))
        {
        }
    }
}