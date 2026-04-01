using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>퇴실 안내 비즈니스 로직 (초기 골격).</summary>
    public sealed class MoveOutService
    {
        public void Process(MoveOutInfo info)
        {
            if (info == null) return;
            AppLog.Info("MoveOutService.Process MoveInId=" + info.MoveInId);
        }
    }
}
