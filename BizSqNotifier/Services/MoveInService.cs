using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>입주 안내 비즈니스 로직 (초기 골격).</summary>
    public sealed class MoveInService
    {
        public void Process(MoveInInfo info)
        {
            if (info == null) return;
            AppLog.Info("MoveInService.Process MoveInId=" + info.MoveInId);
        }
    }
}
