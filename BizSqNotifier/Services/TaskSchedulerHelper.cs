using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// Windows Task Scheduler에 자동 발송 작업을 등록/해제하는 헬퍼.
    /// schtasks.exe CLI를 사용하므로 COM 참조(TaskScheduler COM) 불필요.
    ///
    /// 등록되는 작업:
    ///   1) BizSqNotifier_09 → 매일 09:00 → /autosend
    ///   2) BizSqNotifier_13 → 매일 13:00 → /autosend
    ///   3) BizSqNotifier_Startup → 로그온 시 → /silent (트레이 상주)
    ///
    /// 관리자 권한 필요: schtasks /CREATE 는 관리자 권한에서 실행해야 합니다.
    /// </summary>
    public static class TaskSchedulerHelper
    {
        private const string TaskPrefix = "BizSqNotifier";
        private static readonly string ExePath = Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// 09:00 자동 발송 작업을 등록합니다.
        /// </summary>
        public static (bool Success, string Message) RegisterTask09(string time = "09:00")
        {
            return CreateDailyTask($"{TaskPrefix}_09", time, "/autosend");
        }

        /// <summary>
        /// 13:00 자동 발송 작업을 등록합니다.
        /// </summary>
        public static (bool Success, string Message) RegisterTask13(string time = "13:00")
        {
            return CreateDailyTask($"{TaskPrefix}_13", time, "/autosend");
        }

        /// <summary>
        /// 로그온 시 자동 시작(트레이 상주) 작업을 등록합니다.
        /// </summary>
        public static (bool Success, string Message) RegisterStartupTask()
        {
            var taskName = $"{TaskPrefix}_Startup";
            // ONLOGON 트리거: 사용자 로그온 시 실행
            var args = $"/CREATE /TN \"{taskName}\" " +
                       $"/TR \"\\\"{ExePath}\\\" /silent\" " +
                       "/SC ONLOGON /RL HIGHEST /F";

            return RunSchtasks(args, taskName);
        }

        /// <summary>
        /// 모든 BizSqNotifier 작업을 등록합니다.
        /// </summary>
        public static (bool Success, string Message) RegisterAll(string time09 = "09:00", string time13 = "13:00")
        {
            var results = new System.Text.StringBuilder();
            bool allSuccess = true;

            var r1 = RegisterTask09(time09);
            results.AppendLine($"09시 작업: {(r1.Success ? "성공" : "실패")} — {r1.Message}");
            if (!r1.Success) allSuccess = false;

            var r2 = RegisterTask13(time13);
            results.AppendLine($"13시 작업: {(r2.Success ? "성공" : "실패")} — {r2.Message}");
            if (!r2.Success) allSuccess = false;

            var r3 = RegisterStartupTask();
            results.AppendLine($"시작프로그램: {(r3.Success ? "성공" : "실패")} — {r3.Message}");
            if (!r3.Success) allSuccess = false;

            return (allSuccess, results.ToString().TrimEnd());
        }

        /// <summary>
        /// 특정 이름의 작업을 삭제합니다.
        /// </summary>
        public static (bool Success, string Message) RemoveTask(string taskName)
        {
            var args = $"/DELETE /TN \"{taskName}\" /F";
            return RunSchtasks(args, taskName);
        }

        /// <summary>
        /// 모든 BizSqNotifier 작업을 삭제합니다.
        /// </summary>
        public static void RemoveAll()
        {
            RemoveTask($"{TaskPrefix}_09");
            RemoveTask($"{TaskPrefix}_13");
            RemoveTask($"{TaskPrefix}_Startup");
        }

        /// <summary>
        /// 작업 등록 상태를 확인합니다.
        /// </summary>
        public static bool IsTaskRegistered(string taskName)
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", $"/QUERY /TN \"{taskName}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit(5000);
                    return proc.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        #region Private

        /// <summary>
        /// 매일 실행 작업을 생성합니다.
        /// </summary>
        private static (bool, string) CreateDailyTask(string taskName, string time, string argument)
        {
            var args = $"/CREATE /TN \"{taskName}\" " +
                       $"/TR \"\\\"{ExePath}\\\" {argument}\" " +
                       $"/SC DAILY /ST {time} /RL HIGHEST /F";

            return RunSchtasks(args, taskName);
        }

        /// <summary>
        /// schtasks.exe를 실행합니다.
        /// </summary>
        private static (bool, string) RunSchtasks(string arguments, string taskName)
        {
            try
            {
                AppLog.Info($"schtasks 실행: {arguments}");

                var psi = new ProcessStartInfo("schtasks.exe", arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    var error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10000);

                    if (proc.ExitCode == 0)
                    {
                        AppLog.Info($"작업 '{taskName}' 등록 성공");
                        return (true, $"'{taskName}' 등록 완료");
                    }
                    else
                    {
                        var msg = !string.IsNullOrWhiteSpace(error) ? error.Trim() : output.Trim();
                        AppLog.Error($"작업 '{taskName}' 등록 실패: {msg}");
                        return (false, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Error($"schtasks 실행 오류 — {taskName}", ex);
                return (false, ex.Message);
            }
        }

        #endregion
    }
}
