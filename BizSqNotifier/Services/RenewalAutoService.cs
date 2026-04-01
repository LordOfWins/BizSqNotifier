using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>갱신 자동 메일 비즈니스 로직 (초기 골격).</summary>
    public sealed class RenewalAutoService
    {
        public void Process(RenewalInfo info)
        {
            if (info == null) return;
            AppLog.Info("RenewalAutoService.Process MoveInId=" + info.MoveInId);
        }
    }
}
