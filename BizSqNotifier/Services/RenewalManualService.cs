using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>갱신 수동 메일 비즈니스 로직 (초기 골격).</summary>
    public sealed class RenewalManualService
    {
        public void Process(RenewalInfo info)
        {
            if (info == null) return;
            AppLog.Info("RenewalManualService.Process MoveInId=" + info.MoveInId);
        }
    }
}
