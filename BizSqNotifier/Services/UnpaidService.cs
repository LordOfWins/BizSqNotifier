using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>미납 안내 비즈니스 로직 (초기 골격).</summary>
    public sealed class UnpaidService
    {
        public void Process(UnpaidInfo info)
        {
            if (info == null) return;
            AppLog.Info("UnpaidService.Process MoveInId=" + info.MoveInId);
        }
    }
}
