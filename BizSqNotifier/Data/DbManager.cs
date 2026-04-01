using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using BizSqNotifier.Config;

namespace BizSqNotifier.Data
{
    /// <summary>SQL Server 연결 팩토리.</summary>
    public static class DbManager
    {
        public static SqlConnection CreateConnection()
        {
            var cs = ConfigurationManager.ConnectionStrings[AppSettings.ConnectionStringName];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new ConfigurationErrorsException(
                    "connectionStrings에 '" + AppSettings.ConnectionStringName + "' 항목이 없거나 비어 있습니다.");
            return new SqlConnection(cs.ConnectionString);
        }

        public static IDbConnection CreateOpenConnection()
        {
            var c = CreateConnection();
            c.Open();
            return c;
        }
    }
}
