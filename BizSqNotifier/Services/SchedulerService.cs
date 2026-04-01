using System;
using System.Timers;

namespace BizSqNotifier.Services
{
    /// <summary>주기 작업 스케줄러 (초기 골격).</summary>
    public sealed class SchedulerService : IDisposable
    {
        private readonly Timer _timer;

        public SchedulerService()
        {
            _timer = new Timer(60_000) { AutoReset = true };
            _timer.Elapsed += (_, __) => AppLog.Info("Scheduler tick (idle)");
        }

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
