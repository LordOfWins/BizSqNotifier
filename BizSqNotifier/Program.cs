using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    internal static class Program
    {
        private static Mutex _mutex;
        private const string MutexName = "Global\\BizSqNotifier_SingleInstance";

        [STAThread]
        static void Main(string[] args)
        {
            // ── 단일 인스턴스 체크 ──
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "BizSqNotifier가 이미 실행 중입니다.\n시스템 트레이를 확인하세요.",
                    "BizSqNotifier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ConfigureTraceLogging();
            AppLog.Info("=== BizSqNotifier 시작 ===");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var normalizedArgs = args.Select(a => a.ToLowerInvariant().Trim()).ToArray();

            // ── /run 모드: 즉시 전체 발송 1회 후 종료 ──
            if (normalizedArgs.Contains("/run"))
            {
                AppLog.Info("모드: /run — 즉시 실행");
                RunOnceAndExit();
                ReleaseMutex();
                return;
            }

            bool silentMode = normalizedArgs.Contains("/silent");
            AppLog.Info(silentMode ? "모드: /silent — 트레이 전용" : "모드: 일반 — 대시보드");

            // ── 로그인 체크 (/silent 모드에서는 로그인 스킵) ──
            if (!silentMode)
            {
                using (var loginForm = new LoginForm())
                {
                    var loginResult = loginForm.ShowDialog();
                    if (loginResult != DialogResult.OK)
                    {
                        AppLog.Info("로그인 취소 — 프로그램 종료");
                        ReleaseMutex();
                        return;
                    }
                }
            }

            // ── 메인 폼 실행 ──
            Application.Run(new MainForm(silentMode));

            ReleaseMutex();
            AppLog.Info("=== BizSqNotifier 종료 ===");
        }

        private static void RunOnceAndExit()
        {
            try
            {
                var runner = new SchedulerService();
                runner.RunAllNow();
                AppLog.Info("/run 모드 처리 완료");
            }
            catch (Exception ex)
            {
                AppLog.Error("/run 모드 처리 중 오류", ex);
            }
        }

        private static void ReleaseMutex()
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch { /* 무시 */ }
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
        }
    }
}
