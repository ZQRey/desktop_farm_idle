using System;
using System.IO;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;
using DesktopFarmIdle.Simulation;
using DesktopFarmIdle.Simulation.Economy;
using DesktopFarmIdle.Simulation.Field;
using DesktopFarmIdle.Simulation.Market;
using DesktopFarmIdle.Simulation.Save;
using DesktopFarmIdle.Simulation.Storage;
using DesktopFarmIdle.Simulation.Vehicles;
using Xunit;

namespace DesktopFarmIdle.Tests;

public class SimulationTests
{
    [Fact]
    public void FieldSystem_ModifiesCellState_AndPublishesEvent()
    {
        var bus = new GameEventBus();
        var fieldSystem = new FieldSystem(bus);

        SoilStateChangedEvent? receivedEvent = null;
        bus.Subscribe<SoilStateChangedEvent>(e => receivedEvent = e);

        fieldSystem.ModifyCellSoilState(1, 5, SoilState.Plowed);

        Assert.Equal(SoilState.Plowed, fieldSystem.Fields[0].Cells[5].State);
        Assert.NotNull(receivedEvent);
        Assert.Equal(5, receivedEvent.CellIndex);
        Assert.Equal((int)SoilState.Plowed, receivedEvent.NewState);
    }

    [Fact]
    public void CropGrowth_AdvancesThroughStages()
    {
        var bus = new GameEventBus();
        var fieldSystem = new FieldSystem(bus);

        // Seed cell 0 with Wheat
        fieldSystem.ModifyCellSoilState(1, 0, SoilState.Seeded, CropType.Wheat);

        // Advance field by 300 simulation seconds
        fieldSystem.Update(300.0f, weatherMoistureEffect: 0f, temperatureModifier: 1.0f);

        var cell = fieldSystem.Fields[0].Cells[0];
        Assert.True(cell.GrowthProgress > 0.3f, "Crop should have progressed growth");
        Assert.True(cell.State > SoilState.Seeded, "Soil state should advance past Seeded");
    }

    [Fact]
    public void Vehicle_ConsumesFuel_WhenWorking()
    {
        var bus = new GameEventBus();
        var fieldSystem = new FieldSystem(bus);
        var vehicleSystem = new VehicleSystem(bus, fieldSystem);

        var tractor = vehicleSystem.Vehicles[0];
        float initialFuel = tractor.FuelLevel;

        tractor.IsWorking = true;
        tractor.CurrentTask = VehicleTaskType.Plowing;

        // Simulate 10 seconds of vehicle work
        vehicleSystem.Update(10.0f, weatherFrictionModifier: 1.0f);

        Assert.True(tractor.FuelLevel < initialFuel, "Tractor should consume fuel while working");
    }

    [Fact]
    public void Economy_CreditsAndDebits_Accurately()
    {
        var bus = new GameEventBus();
        var economy = new EconomySystem(bus);

        economy.SetBalance(100.0f);
        Assert.True(economy.CanAfford(50.0f));
        Assert.False(economy.CanAfford(150.0f));

        bool spent = economy.TrySpend(40.0f, "Bought seeds");
        Assert.True(spent);
        Assert.Equal(60.0f, economy.Balance);

        economy.AddIncome(200.0f, "Sold crops");
        Assert.Equal(260.0f, economy.Balance);
    }

    [Fact]
    public void Storage_EnforcesCapacity_AndRetrieval()
    {
        var bus = new GameEventBus();
        var storage = new StorageSystem(bus) { MaxCapacityTons = 10.0f };

        float stored = storage.StoreCrop(CropType.Wheat, 7.0f);
        Assert.Equal(7.0f, stored);
        Assert.Equal(3.0f, storage.GetRemainingCapacity());

        // Storing more than remaining capacity clamps to max
        float overflowStored = storage.StoreCrop(CropType.Wheat, 5.0f);
        Assert.Equal(3.0f, overflowStored);
        Assert.Equal(10.0f, storage.GetTotalStoredTons());

        bool retrieved = storage.TryRetrieve(CropType.Wheat, 4.0f);
        Assert.True(retrieved);
        Assert.Equal(6.0f, storage.GetTotalStoredTons());
    }

    [Fact]
    public void Market_SellsStoredCrops_AndCreditsEconomy()
    {
        var bus = new GameEventBus();
        var economy = new EconomySystem(bus);
        var storage = new StorageSystem(bus);
        var market = new MarketSystem(bus, economy, storage);

        economy.SetBalance(0f);
        storage.StoreCrop(CropType.Wheat, 5.0f);

        float earned = market.SellCrop(CropType.Wheat, 5.0f);
        Assert.True(earned > 0f, "Should earn money from sale");
        Assert.Equal(earned, economy.Balance);
        Assert.Equal(0f, storage.InventoryTons[CropType.Wheat]);
    }

    [Fact]
    public void OfflineProgress_CalculatesCorrectly()
    {
        var bus = new GameEventBus();
        var fieldSystem = new FieldSystem(bus);
        var storage = new StorageSystem(bus);
        var economy = new EconomySystem(bus);

        // 36 minutes offline = 2 complete 18-minute cycles
        var lastSave = DateTime.UtcNow.AddMinutes(-36);
        var now = DateTime.UtcNow;

        var result = OfflineProgressCalculator.Calculate(lastSave, now, fieldSystem, storage, economy);

        Assert.Equal(2, result.CyclesCompleted);
        Assert.True(result.TonsHarvested > 0f);
    }

    [Fact]
    public void SaveManager_WritesAndLoads_StateCorrectly()
    {
        var bus = new GameEventBus();
        var economy = new EconomySystem(bus);
        var storage = new StorageSystem(bus);
        var fieldSystem = new FieldSystem(bus);
        var vehicleSystem = new VehicleSystem(bus, fieldSystem);
        var saveManager = new SaveManager();

        economy.SetBalance(1234.5f);
        fieldSystem.ModifyCellSoilState(1, 2, SoilState.Plowed);

        bool saved = saveManager.Save(economy, storage, fieldSystem, vehicleSystem);
        Assert.True(saved, "Save should succeed");

        var loaded = saveManager.Load();
        Assert.NotNull(loaded);
        Assert.Equal(1234.5f, loaded.Balance);
        Assert.Equal((int)SoilState.Plowed, loaded.Fields[0].Cells[2].State);
    }
}
