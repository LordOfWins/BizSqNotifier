using System;
using System.Diagnostics;

namespace BizSqNotifier.Services
{
    /// <summary>진단·테스트용 로그 (Trace).</summary>
    public static class AppLog
    {
        public static void Info(string message) => Trace.WriteLine("[INFO] " + DateTime.Now.ToString("o") + " " + message);

        public static void Warn(string message) => Trace.WriteLine("[WARN] " + DateTime.Now.ToString("o") + " " + message);

        public static void Error(string message, Exception ex = null)
        {
            Trace.WriteLine("[ERR ] " + DateTime.Now.ToString("o") + " " + message);
            if (ex != null)
                Trace.WriteLine(ex.ToString());
        }
    }
}
