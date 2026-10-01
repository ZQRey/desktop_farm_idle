using System;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Economy;
using DesktopFarmIdle.Simulation.Field;
using DesktopFarmIdle.Simulation.Storage;

namespace DesktopFarmIdle.Simulation.Save;

public record OfflineProgressResult(
    TimeSpan OfflineDuration,
    int CyclesCompleted,
    float TonsHarvested,
    float CoinsEarned,
    float FuelConsumedLitres
);

public static class OfflineProgressCalculator
{
    public const double MaxOfflineHours = 8.0;

    public static OfflineProgressResult Calculate(
        DateTime lastSaveUtc,
        DateTime nowUtc,
        FieldSystem fieldSystem,
        StorageSystem storageSystem,
        EconomySystem economySystem)
    {
        var rawDuration = nowUtc - lastSaveUtc;
        if (rawDuration < TimeSpan.FromMinutes(2))
        {
            return new OfflineProgressResult(rawDuration, 0, 0f, 0f, 0f);
        }

        // Cap at MaxOfflineHours
        var effectiveDuration = rawDuration.TotalHours > MaxOfflineHours
            ? TimeSpan.FromHours(MaxOfflineHours)
            : rawDuration;

        // An 18-minute cycle produces ~3 tons of grain on default field
        double cycleDurationSeconds = GameBalanceConfig.TargetHarvestCycleSeconds;
        int completedCycles = (int)(effectiveDuration.TotalSeconds / cycleDurationSeconds);

        if (completedCycles <= 0)
        {
            return new OfflineProgressResult(effectiveDuration, 0, 0f, 0f, 0f);
        }

        // Approximate yields and fuel burn
        float tonsPerCycle = 3.0f * fieldSystem.Fields.Count;
        float totalTons = completedCycles * tonsPerCycle;
        float fuelPerCycle = 15.0f * fieldSystem.Fields.Count; // Litres
        float totalFuel = completedCycles * fuelPerCycle;

        // Auto-store or auto-sell
        float stored = storageSystem.StoreCrop(CropType.Wheat, totalTons);
        float surplus = totalTons - stored;
        float coinsEarned = 0f;

        if (surplus > 0f)
        {
            // Sell surplus immediately
            coinsEarned = surplus * GameBalanceConfig.Crops[CropType.Wheat].BaseSellPricePerTon;
            economySystem.AddIncome(coinsEarned, $"Offline sale: {surplus:F1}t Wheat");
        }

        return new OfflineProgressResult(
            effectiveDuration,
            completedCycles,
            totalTons,
            coinsEarned,
            totalFuel
        );
    }
}
