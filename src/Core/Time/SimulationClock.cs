using System;

namespace DesktopFarmIdle.Core.Time;

public class SimulationClock
{
    public const float StandardTickDuration = 0.1f; // 10 ticks per second

    public float TimeScale { get; set; } = 1.0f;
    public bool IsPaused { get; set; } = false;
    public bool SyncWithSystemTime { get; set; } = true;

    public double TotalElapsedSimulationSeconds { get; private set; } = 0;
    public long TotalTicks { get; private set; } = 0;

    private float _accumulator = 0f;

    public event Action<float>? OnSimulationTick;

    public void Update(float realDeltaSeconds)
    {
        if (IsPaused)
            return;

        float effectiveDelta = realDeltaSeconds * TimeScale;
        _accumulator += effectiveDelta;

        while (_accumulator >= StandardTickDuration)
        {
            _accumulator -= StandardTickDuration;
            TotalElapsedSimulationSeconds += StandardTickDuration;
            TotalTicks++;
            OnSimulationTick?.Invoke(StandardTickDuration);
        }
    }

    /// <summary>
    /// Returns 0.0 (midnight) to 1.0 (next midnight) representing the diurnal time.
    /// If SyncWithSystemTime is true, returns normalized local system time.
    /// </summary>
    public float GetDayTimeNormalized()
    {
        if (SyncWithSystemTime)
        {
            var now = DateTime.Now;
            return (float)(now.TimeOfDay.TotalSeconds / 86400.0);
        }
        else
        {
            // Continuous game day length = 1200 seconds (20 minutes)
            const double gameDaySeconds = 1200.0;
            return (float)((TotalElapsedSimulationSeconds % gameDaySeconds) / gameDaySeconds);
        }
    }

    public void FastForward(double seconds)
    {
        int ticks = (int)(seconds / StandardTickDuration);
        for (int i = 0; i < ticks; i++)
        {
            TotalElapsedSimulationSeconds += StandardTickDuration;
            TotalTicks++;
            OnSimulationTick?.Invoke(StandardTickDuration);
        }
    }
}
