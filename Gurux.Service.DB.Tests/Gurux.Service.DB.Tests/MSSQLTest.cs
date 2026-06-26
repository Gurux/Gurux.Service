using Gurux.Service.Orm;
using Microsoft.Data.SqlClient;

namespace Gurux.Service.DB.Tests
{
    /// <summary>
    /// docker run -d --env=MSSQL_SA_PASSWORD=Gurux123 --env=MSSQL_PID=Evaluation --env=ACCEPT_EULA=Y -p 1433:1433 mcr.microsoft.com/mssql/server:2022-CU23-ubuntu-22.04
    /// vanha: docker run -d --env=MSSQL_SA_PASSWORD=Gurux123 --env=MSSQL_PID=Evaluation --env=ACCEPT_EULA=Y -p 1433:1433 mcr.microsoft.com/mssql/server:2022-preview-ubuntu-22.04
    /// 
    /// docker run -d --rm -it -e "Database:Type=MSSQL" -e "Database:Settings=Server=192.168.68.55;Database=msdb;User ID=sa;Password=Gurux123;TrustServerCertificate=True" -e "IdentityServer:Key:Type=Development" -p 8000:80 -p 8001:443 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=8001 -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_Kestrel__Certificates__Default__Password="Gurux123" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/Gurux.DLMS.AMI.Server.pfx -v %USERPROFILE%\.aspnet\https:/https/ guruxorg/guruxdlmsamiserver:latest
    /// </summary>
    [TestClass]
    public class GXMSSQLTest : GXBaseTest
    {
        public GXMSSQLTest() : base(new GXDbConnection(new SqlConnection("Server=localhost;Database=msdb;User ID=sa;Password=Gurux123;TrustServerCertificate=True"), null))
        {
        }
    }
}