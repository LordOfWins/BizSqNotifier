using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using BizSqNotifier.Config;

namespace BizSqNotifier
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ConfigureTraceLogging();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void ConfigureTraceLogging()
        {
            Trace.AutoFlush = true;
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logDir = Path.Combine(baseDir, AppSettings.LogFilePath);
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, "app_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                Trace.Listeners.Add(new TextWriterTraceListener(logFile));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("로그 파일 리스너 추가 실패: " + ex.Message);
            }

            Trace.WriteLine("=== BizSqNotifier 시작 " + DateTime.Now.ToString("o") + " ===");
        }
    }
}
