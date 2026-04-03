using System;
using BizSqNotifier.Config;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 자동 발송 일괄 실행기.
    /// Task Scheduler /autosend 모드 및 내부 스케줄러에서 호출합니다.
    ///
    /// 발송 시각별 분기:
    ///   09:00 → 입주/퇴실/갱신자동
    ///   13:00 → 미납 1차/2차/최종
    ///
    /// ExecuteAll()은 현재 시각에 관계없이 모든 유형을 처리합니다.
    /// Task Scheduler에서 시각별로 분리 호출하려면 Execute09() / Execute13()을 사용합니다.
    /// </summary>
    public sealed class AutoSendRunner
    {
        /// <summary>
        /// 모든 자동 발송 유형을 일괄 실행합니다.
        /// /autosend 커맨드라인 모드에서 호출됩니다.
        /// </summary>
        public void ExecuteAll()
        {
            var now = DateTime.Now;
            var hour = now.Hour;

            // Task Scheduler 시각별 분기
            // 09시대 실행 → 입주/퇴실/갱신자동
            // 13시대 실행 → 미납
            // 그 외 시간 → 전부 실행 (수동 테스트 등)

            if (hour >= 9 && hour < 12)
            {
                Execute09();
            }
            else if (hour >= 13 && hour < 16)
            {
                Execute13();
            }
            else
            {
                // 시간 외 실행 → 전체
                Execute09();
                Execute13();
            }
        }

        /// <summary>
        /// 09:00 발송 그룹: 입주 안내 / 퇴실 안내 / 갱신 자동(adBox/회원제)
        /// </summary>
        public void Execute09()
        {
            var settings = UserSettings.Current;

            AppLog.Info("── 09:00 그룹 자동 발송 시작 ──");

            // ① 입주 안내
            try
            {
                var moveInSvc = new MoveInService();
                var result = moveInSvc.ProcessAll(
                    settings.PrinterLoginId,
                    settings.PrinterLoginPw);
                AppLog.Info($"[입주] 결과: 전체={result.Total} 성공={result.Success} 실패={result.Fail} SKIP={result.Skip}");
            }
            catch (Exception ex)
            {
                AppLog.Error("[입주] 자동 발송 실패", ex);
            }

            // ② 퇴실 안내
            try
            {
                var moveOutSvc = new MoveOutService();
                var result = moveOutSvc.ProcessAll(settings.MoveOutDays);
                AppLog.Info($"[퇴실] 결과: 전체={result.Total} 성공={result.Success} 실패={result.Fail} SKIP={result.Skip}");
            }
            catch (Exception ex)
            {
                AppLog.Error("[퇴실] 자동 발송 실패", ex);
            }

            // ③ 갱신 자동 (adBox/회원제)
            try
            {
                var renewalAutoSvc = new RenewalAutoService();
                var result = renewalAutoSvc.ProcessAll(settings.RenewalAutoDays);
                AppLog.Info($"[갱신자동] 결과: 전체={result.Total} 성공={result.Success} 실패={result.Fail} SKIP={result.Skip}");
            }
            catch (Exception ex)
            {
                AppLog.Error("[갱신자동] 자동 발송 실패", ex);
            }

            AppLog.Info("── 09:00 그룹 자동 발송 완료 ──");
        }

        /// <summary>
        /// 13:00 발송 그룹: 미납 1차 / 2차 / 최종
        /// </summary>
        public void Execute13()
        {
            AppLog.Info("── 13:00 그룹 자동 발송 시작 ──");

            try
            {
                var unpaidSvc = new UnpaidService();
                var results = unpaidSvc.ProcessAll();

                foreach (var kv in results)
                {
                    AppLog.Info($"[{kv.Key}] 결과: 전체={kv.Value.Total} 성공={kv.Value.Success} 실패={kv.Value.Fail} SKIP={kv.Value.Skip}");
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("[미납] 자동 발송 실패", ex);
            }

            AppLog.Info("── 13:00 그룹 자동 발송 완료 ──");
        }
    }
}
