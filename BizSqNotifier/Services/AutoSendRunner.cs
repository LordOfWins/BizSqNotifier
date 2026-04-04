using System;
using BizSqNotifier.Config;

namespace BizSqNotifier.Services
{
    /// <summary>Task Scheduler /autosend 모드 또는 /run 모드용 일괄 실행기.</summary>
    public sealed class AutoSendRunner
    {
        public void ExecuteAll()
        {
            var hour = DateTime.Now.Hour;
            if (hour >= 9 && hour < 12) Execute09();
            else if (hour >= 13 && hour < 16) Execute13();
            else { Execute09(); Execute13(); }
        }

        public void Execute09()
        {
            var s = UserSettings.Current;
            AppLog.Info("── 09:00 그룹 시작 ──");

            try { var r = new MoveInService().ProcessAll(s.PrinterLoginId, s.PrinterLoginPw);
                AppLog.Info($"[입주] 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}"); }
            catch (Exception ex) { AppLog.Error("[입주] 실패", ex); }

            try { var r = new MoveOutService().ProcessAll(s.MoveOutDays);
                AppLog.Info($"[퇴실] 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}"); }
            catch (Exception ex) { AppLog.Error("[퇴실] 실패", ex); }

            try { var r = new RenewalAutoService().ProcessAll(s.RenewalAutoDays);
                AppLog.Info($"[갱신자동] 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}"); }
            catch (Exception ex) { AppLog.Error("[갱신자동] 실패", ex); }

            AppLog.Info("── 09:00 그룹 완료 ──");
        }

        public void Execute13()
        {
            AppLog.Info("── 13:00 그룹 시작 ──");
            try
            {
                var results = new UnpaidService().ProcessAll();
                foreach (var kv in results)
                    AppLog.Info($"[{kv.Key}] 전체={kv.Value.Total} 성공={kv.Value.Success} 실패={kv.Value.Fail} SKIP={kv.Value.Skip}");
            }
            catch (Exception ex) { AppLog.Error("[미납] 실패", ex); }
            AppLog.Info("── 13:00 그룹 완료 ──");
        }
    }
}
