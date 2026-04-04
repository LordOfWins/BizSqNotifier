using System;
using System.Collections.Generic;
using System.Threading;
using BizSqNotifier.Config;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 내부 타이머 기반 스케줄러.
    /// 매 60초마다 현재 시각 체크 → 발송 시각 도달 시 서비스 호출.
    /// </summary>
    public sealed class SchedulerService : IDisposable
    {
        private Timer _timer;
        private DateTime _lastCheckedDate;
        private readonly HashSet<string> _executedToday = new HashSet<string>();

        public bool IsRunning { get; private set; }
        public event Action<string> OnLog;
        public event Action<string, int, int, int, int> OnTaskCompleted;

        public SchedulerService() { _lastCheckedDate = DateTime.Today; }

        public void Start()
        {
            if (IsRunning) return;
            _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
            IsRunning = true;
            Log("스케줄러 시작 (60초 간격)");
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            IsRunning = false;
            Log("스케줄러 중지");
        }

        private void OnTick(object state)
        {
            try
            {
                var now = DateTime.Now;
                if (now.Date > _lastCheckedDate)
                {
                    _executedToday.Clear();
                    _lastCheckedDate = now.Date;
                    Log("자정 경과 — 실행 기록 초기화");
                }

                var settings = UserSettings.Current;

                var generalTime = settings.GeneralSendTimeSpan;
                if (IsTimeToRun(now, generalTime, "General"))
                { MarkExecuted("General", generalTime); RunGeneralTasks(settings); }

                var unpaidTime = settings.UnpaidSendTimeSpan;
                if (IsTimeToRun(now, unpaidTime, "Unpaid"))
                { MarkExecuted("Unpaid", unpaidTime); RunUnpaidTasks(); }
            }
            catch (Exception ex) { AppLog.Error("[스케줄러] Tick 오류", ex); }
        }

        private bool IsTimeToRun(DateTime now, TimeSpan target, string key)
        {
            var k = $"{key}_{target.Hours:D2}{target.Minutes:D2}";
            return !_executedToday.Contains(k) && now.Hour == target.Hours && now.Minute == target.Minutes;
        }

        private void MarkExecuted(string key, TimeSpan time)
        { _executedToday.Add($"{key}_{time.Hours:D2}{time.Minutes:D2}"); }

        private void RunGeneralTasks(UserSettings settings)
        {
            Log("═══ 일반 발송 시작 (입주/퇴실/갱신자동) ═══");
            SafeRun("입주", () => new MoveInService().ProcessAll(settings.PrinterLoginId, settings.PrinterLoginPw));
            SafeRun("퇴실", () => new MoveOutService().ProcessAll(settings.MoveOutDays));
            SafeRun("갱신자동", () => new RenewalAutoService().ProcessAll(settings.RenewalAutoDays));
            Log("═══ 일반 발송 완료 ═══");
        }

        private void RunUnpaidTasks()
        {
            Log("═══ 미납 발송 시작 ═══");
            try
            {
                // ProcessAll()은 내부에서 UserSettings.Current 기준일을 읽음
                var results = new UnpaidService().ProcessAll();
                foreach (var kv in results)
                {
                    var r = kv.Value;
                    Log($"[{kv.Key}] 완료 — 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}");
                    OnTaskCompleted?.Invoke(kv.Key, r.Total, r.Success, r.Fail, r.Skip);
                }
            }
            catch (Exception ex) { AppLog.Error("[스케줄러] 미납 실행 실패", ex); }
            Log("═══ 미납 발송 완료 ═══");
        }

        public void RunAllNow()
        {
            Log("═══ 즉시 전체 실행 ═══");
            RunGeneralTasks(UserSettings.Current);
            RunUnpaidTasks();
            Log("═══ 즉시 전체 실행 완료 ═══");
        }

        private void SafeRun(string name, Func<(int Total, int Success, int Fail, int Skip)> action)
        {
            try
            {
                var r = action();
                Log($"[{name}] 완료 — 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}");
                OnTaskCompleted?.Invoke(name, r.Total, r.Success, r.Fail, r.Skip);
            }
            catch (Exception ex) { AppLog.Error($"[스케줄러] {name} 실패", ex); }
        }

        private void Log(string msg) { AppLog.Info("[스케줄러] " + msg); OnLog?.Invoke(msg); }

        public void Dispose() { _timer?.Dispose(); _timer = null; }
    }
}
