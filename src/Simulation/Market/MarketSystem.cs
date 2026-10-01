using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;
using DesktopFarmIdle.Simulation.Economy;
using DesktopFarmIdle.Simulation.Storage;

namespace DesktopFarmIdle.Simulation.Market;

public class MarketSystem
{
    private readonly IEventBus _eventBus;
    private readonly EconomySystem _economy;
    private readonly StorageSystem _storage;
    private readonly Random _random = new();

    public Dictionary<CropType, float> CurrentPrices { get; } = new();
    private float _priceUpdateTimer = 0f;
    private const float PriceUpdateInterval = 60.0f; // Every 60s simulated time

    public MarketSystem(IEventBus eventBus, EconomySystem economy, StorageSystem storage)
    {
        _eventBus = eventBus;
        _economy = economy;
        _storage = storage;

        foreach (var (crop, config) in GameBalanceConfig.Crops)
        {
            CurrentPrices[crop] = config.BaseSellPricePerTon;
        }
    }

    public void Update(float deltaSimulationTime)
    {
        _priceUpdateTimer += deltaSimulationTime;
        if (_priceUpdateTimer >= PriceUpdateInterval)
        {
            _priceUpdateTimer = 0f;
            RecalculatePrices();
        }
    }

    private void RecalculatePrices()
    {
        foreach (var (crop, config) in GameBalanceConfig.Crops)
        {
            // +/- 10% price fluctuations
            float factor = 1.0f + (float)(_random.NextDouble() * 0.20 - 0.10);
            float newPrice = MathF.Round(config.BaseSellPricePerTon * factor, 1);
            float delta = newPrice - CurrentPrices[crop];
            CurrentPrices[crop] = newPrice;

            _eventBus.Publish(new MarketPriceChangedEvent(config.Name, newPrice, delta, DateTime.Now));
        }
    }

    public float SellCrop(CropType crop, float tons)
    {
        if (tons <= 0f) return 0f;
        if (_storage.TryRetrieve(crop, tons))
        {
            float price = CurrentPrices.TryGetValue(crop, out float p) ? p : 100f;
            float total = price * tons;
            _economy.AddIncome(total, $"Sold {tons:F1}t of {crop} @ {price:F0} Coins/t");
            return total;
        }
        return 0f;
    }

    public float SellAll()
    {
        float grandTotal = 0f;
        foreach (CropType crop in Enum.GetValues<CropType>())
        {
            float amount = _storage.InventoryTons[crop];
            if (amount > 0f)
            {
                grandTotal += SellCrop(crop, amount);
            }
        }
        return grandTotal;
    }
}
