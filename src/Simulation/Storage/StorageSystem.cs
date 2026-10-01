using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;

namespace DesktopFarmIdle.Simulation.Storage;

public class StorageSystem
{
    private readonly IEventBus _eventBus;
    public float MaxCapacityTons { get; set; } = 25.0f; // Starter capacity
    public Dictionary<CropType, float> InventoryTons { get; } = new();

    public StorageSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
        foreach (CropType crop in Enum.GetValues<CropType>())
        {
            InventoryTons[crop] = 0f;
        }
    }

    public float GetTotalStoredTons()
    {
        float total = 0f;
        foreach (var val in InventoryTons.Values)
        {
            total += val;
        }
        return total;
    }

    public float GetRemainingCapacity() => Math.Max(0f, MaxCapacityTons - GetTotalStoredTons());

    public float StoreCrop(CropType crop, float amountTons)
    {
        float available = GetRemainingCapacity();
        float toStore = Math.Min(available, amountTons);
        InventoryTons[crop] += toStore;

        if (GetTotalStoredTons() >= MaxCapacityTons * 0.95f)
        {
            _eventBus.Publish(new NotificationRequestedEvent("Storage Near Capacity", "Grain silos are 95% full. Sell or upgrade silos.", "Warning", DateTime.Now));
        }

        return toStore;
    }

    public bool TryRetrieve(CropType crop, float amountTons)
    {
        if (InventoryTons.TryGetValue(crop, out float current) && current >= amountTons)
        {
            InventoryTons[crop] -= amountTons;
            return true;
        }
        return false;
    }
}
