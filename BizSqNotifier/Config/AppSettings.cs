using System.Configuration;

namespace BizSqNotifier.Config
{
    /// <summary>App.config 기반 애플리케이션 설정.</summary>
    public static class AppSettings
    {
        public const string ConnectionStringName = "MosDB";

        public static string SettingsFilePath =>
            ConfigurationManager.AppSettings["SettingsFilePath"] ?? "settings.json";

        public static string LogFilePath =>
            ConfigurationManager.AppSettings["LogFilePath"] ?? "Logs";
    }
}
