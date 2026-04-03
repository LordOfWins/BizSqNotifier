using System;
using System.Collections.Generic;
using System.Threading;
using BizSqNotifier.Config;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 내부 타이머 기반 스케줄러.
    /// 매 60초마다 현재 시각을 체크하여 발송 시각 도달 시 서비스 호출.
    /// 자정 경과 시 실행 기록 자동 초기화.
    /// </summary>
    public sealed class SchedulerService : IDisposable
    {
        private Timer _timer;
        private DateTime _lastCheckedDate;
        private readonly HashSet<string> _executedToday = new HashSet<string>();

        /// <summary>스케줄러 실행 상태</summary>
        public bool IsRunning { get; private set; }

        /// <summary>로그 이벤트 (UI에서 구독 가능)</summary>
        public event Action<string> OnLog;

        /// <summary>작업 완료 이벤트: taskName, total, success, fail, skip</summary>
        public event Action<string, int, int, int, int> OnTaskCompleted;

        public SchedulerService()
        {
            _lastCheckedDate = DateTime.Today;
        }

        #region Start / Stop

        /// <summary>스케줄러 시작 (60초 간격)</summary>
        public void Start()
        {
            if (IsRunning) return;
            _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
            IsRunning = true;
            Log("스케줄러 시작됨 (60초 간격 체크)");
        }

        /// <summary>스케줄러 중지</summary>
        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            IsRunning = false;
            Log("스케줄러 중지됨");
        }

        #endregion

        #region Timer Tick

        private void OnTick(object state)
        {
            try
            {
                var now = DateTime.Now;

                // 자정 경과 → 실행 기록 초기화
                if (now.Date > _lastCheckedDate)
                {
                    _executedToday.Clear();
                    _lastCheckedDate = now.Date;
                    Log("자정 경과 — 실행 기록 초기화");
                }

                var settings = UserSettings.Current;

                // ── 일반 발송 (입주/퇴실/갱신자동) ──
                var generalTime = settings.GeneralSendTimeSpan;
                if (IsTimeToRun(now, generalTime, "General"))
                {
                    MarkExecuted("General", generalTime);
                    RunGeneralTasks(settings);
                }

                // ── 미납 발송 (1차/2차/최종) ──
                var unpaidTime = settings.UnpaidSendTimeSpan;
                if (IsTimeToRun(now, unpaidTime, "Unpaid"))
                {
                    MarkExecuted("Unpaid", unpaidTime);
                    RunUnpaidTasks(settings);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("[스케줄러] Tick 오류", ex);
            }
        }

        #endregion

        #region 시각 체크

        private bool IsTimeToRun(DateTime now, TimeSpan targetTime, string taskKey)
        {
            var key = $"{taskKey}_{targetTime.Hours:D2}{targetTime.Minutes:D2}";
            if (_executedToday.Contains(key))
                return false;

            return (now.Hour == targetTime.Hours && now.Minute == targetTime.Minutes);
        }

        private void MarkExecuted(string taskKey, TimeSpan time)
        {
            var key = $"{taskKey}_{time.Hours:D2}{time.Minutes:D2}";
            _executedToday.Add(key);
        }

        #endregion

        #region General Tasks (기본 09:00)

        private void RunGeneralTasks(UserSettings settings)
        {
            Log("═══ 일반 발송 시작 (입주/퇴실/갱신자동) ═══");

            // ① 입주 안내
            SafeRun("입주", () =>
            {
                var svc = new MoveInService();
                return svc.ProcessAll(settings.PrinterLoginId, settings.PrinterLoginPw);
            });

            // ② 퇴실 안내
            SafeRun("퇴실", () =>
            {
                var svc = new MoveOutService();
                return svc.ProcessAll(settings.MoveOutDays);
            });

            // ③ 갱신자동
            SafeRun("갱신자동", () =>
            {
                var svc = new RenewalAutoService();
                return svc.ProcessAll(settings.RenewalAutoDays);
            });

            Log("═══ 일반 발송 완료 ═══");
        }

        #endregion

        #region Unpaid Tasks (기본 13:00)

        private void RunUnpaidTasks(UserSettings settings)
        {
            Log("═══ 미납 발송 시작 (1차/2차/최종) ═══");

            try
            {
                var svc = new UnpaidService();
                var results = svc.ProcessAll();

                foreach (var kv in results)
                {
                    var r = kv.Value;
                    Log($"[{kv.Key}] 완료 — 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}");
                    OnTaskCompleted?.Invoke(kv.Key, r.Total, r.Success, r.Fail, r.Skip);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("[스케줄러] 미납 안내 실행 실패", ex);
            }

            Log("═══ 미납 발송 완료 ═══");
        }

        #endregion

        #region RunAllNow — 즉시 전체 실행

        /// <summary>시각 무관 즉시 전체 실행 (/run 모드 또는 관리자 수동 실행)</summary>
        public void RunAllNow()
        {
            Log("═══ 즉시 전체 실행 (RunAllNow) ═══");
            var settings = UserSettings.Current;
            RunGeneralTasks(settings);
            RunUnpaidTasks(settings);
            Log("═══ 즉시 전체 실행 완료 ═══");
        }

        #endregion

        #region 헬퍼

        private void SafeRun(string taskName, Func<(int Total, int Success, int Fail, int Skip)> action)
        {
            try
            {
                var r = action();
                Log($"[{taskName}] 완료 — 전체={r.Total} 성공={r.Success} 실패={r.Fail} SKIP={r.Skip}");
                OnTaskCompleted?.Invoke(taskName, r.Total, r.Success, r.Fail, r.Skip);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[스케줄러] {taskName} 실행 실패", ex);
            }
        }

        private void Log(string message)
        {
            AppLog.Info("[스케줄러] " + message);
            OnLog?.Invoke(message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        #endregion
    }
}
