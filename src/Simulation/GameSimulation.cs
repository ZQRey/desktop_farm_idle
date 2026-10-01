using System;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;
using DesktopFarmIdle.Core.Logging;
using DesktopFarmIdle.Core.Time;
using DesktopFarmIdle.Simulation.Automation;
using DesktopFarmIdle.Simulation.Economy;
using DesktopFarmIdle.Simulation.Field;
using DesktopFarmIdle.Simulation.Market;
using DesktopFarmIdle.Simulation.Save;
using DesktopFarmIdle.Simulation.Storage;
using DesktopFarmIdle.Simulation.Vehicles;
using DesktopFarmIdle.Simulation.Weather;
using DesktopFarmIdle.Simulation.Workers;

namespace DesktopFarmIdle.Simulation;

public class GameSimulation
{
    public IEventBus EventBus { get; }
    public SimulationClock Clock { get; }
    public FieldSystem FieldSystem { get; }
    public VehicleSystem VehicleSystem { get; }
    public WorkerSystem WorkerSystem { get; }
    public WeatherSystem WeatherSystem { get; }
    public EconomySystem EconomySystem { get; }
    public StorageSystem StorageSystem { get; }
    public MarketSystem MarketSystem { get; }
    public AutomationSystem AutomationSystem { get; }
    public SaveManager SaveManager { get; }

    public OfflineProgressResult? PendingOfflineProgress { get; private set; }

    public GameSimulation(IEventBus? eventBus = null)
    {
        EventBus = eventBus ?? GameEventBus.Instance;
        Clock = new SimulationClock();

        FieldSystem = new FieldSystem(EventBus);
        VehicleSystem = new VehicleSystem(EventBus, FieldSystem);
        WorkerSystem = new WorkerSystem(EventBus);
        WeatherSystem = new WeatherSystem(EventBus);
        EconomySystem = new EconomySystem(EventBus);
        StorageSystem = new StorageSystem(EventBus);
        MarketSystem = new MarketSystem(EventBus, EconomySystem, StorageSystem);
        AutomationSystem = new AutomationSystem(FieldSystem, VehicleSystem, StorageSystem, MarketSystem);
        SaveManager = new SaveManager();

        Clock.OnSimulationTick += OnSimulationTick;

        TryLoadAndCatchUp();
    }

    private void TryLoadAndCatchUp()
    {
        var saved = SaveManager.Load();
        if (saved != null)
        {
            EconomySystem.SetBalance(saved.Balance);
            StorageSystem.MaxCapacityTons = saved.SiloCapacityTons;
            foreach (var (cropStr, tons) in saved.InventoryTons)
            {
                if (Enum.TryParse<CropType>(cropStr, out var crop))
                {
                    StorageSystem.InventoryTons[crop] = tons;
                }
            }

            PendingOfflineProgress = OfflineProgressCalculator.Calculate(
                saved.SaveTimestampUtc,
                DateTime.UtcNow,
                FieldSystem,
                StorageSystem,
                EconomySystem
            );

            GameLogger.Instance.Info(
                $"Offline progress calculated: {PendingOfflineProgress.CyclesCompleted} cycles, " +
                $"{PendingOfflineProgress.TonsHarvested:F1}t harvested.", "Simulation");
        }
    }

    public void Update(float realDeltaSeconds)
    {
        Clock.Update(realDeltaSeconds);
    }

    private void OnSimulationTick(float deltaSimTime)
    {
        float moistureEffect = WeatherSystem.GetMoistureEffect();
        float tempGrowthMod = WeatherSystem.GetTemperatureGrowthModifier();
        float frictionMod = WeatherSystem.GetVehicleFrictionModifier();

        FieldSystem.Update(deltaSimTime, moistureEffect, tempGrowthMod);
        VehicleSystem.Update(deltaSimTime, frictionMod);
        WorkerSystem.Update(deltaSimTime);
        WeatherSystem.Update(deltaSimTime);
        MarketSystem.Update(deltaSimTime);
        AutomationSystem.Update(deltaSimTime);
    }

    public void SaveGame()
    {
        SaveManager.Save(EconomySystem, StorageSystem, FieldSystem, VehicleSystem);
    }
}
