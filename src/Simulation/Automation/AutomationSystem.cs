using System.Linq;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Field;
using DesktopFarmIdle.Simulation.Market;
using DesktopFarmIdle.Simulation.Storage;
using DesktopFarmIdle.Simulation.Vehicles;

namespace DesktopFarmIdle.Simulation.Automation;

public class AutomationSystem
{
    private readonly FieldSystem _fieldSystem;
    private readonly VehicleSystem _vehicleSystem;
    private readonly StorageSystem _storageSystem;
    private readonly MarketSystem _marketSystem;

    public bool AutoPlow { get; set; } = true;
    public bool AutoCultivate { get; set; } = true;
    public bool AutoSow { get; set; } = true;
    public bool AutoHarvest { get; set; } = true;
    public bool AutoRefuel { get; set; } = true;
    public bool AutoRepair { get; set; } = true;
    public bool AutoSell { get; set; } = false;

    private float _automationTimer = 0f;
    private const float CheckInterval = 2.0f; // Check every 2 simulation seconds

    public AutomationSystem(
        FieldSystem fieldSystem,
        VehicleSystem vehicleSystem,
        StorageSystem storageSystem,
        MarketSystem marketSystem)
    {
        _fieldSystem = fieldSystem;
        _vehicleSystem = vehicleSystem;
        _storageSystem = storageSystem;
        _marketSystem = marketSystem;
    }

    public void Update(float deltaSimulationTime)
    {
        _automationTimer += deltaSimulationTime;
        if (_automationTimer < CheckInterval)
            return;

        _automationTimer = 0f;
        RunAutomationCycle();
    }

    private void RunAutomationCycle()
    {
        foreach (var vehicle in _vehicleSystem.Vehicles)
        {
            // Auto-refuel when low
            if (AutoRefuel && vehicle.FuelLevel < vehicle.FuelCapacity * 0.20f)
            {
                _vehicleSystem.RefuelVehicle(vehicle.Id);
            }

            // Auto-repair when damaged
            if (AutoRepair && (vehicle.IsBroken || vehicle.Condition < 0.30f))
            {
                _vehicleSystem.RepairVehicle(vehicle.Id);
            }

            // Empty combine bunker if full
            if (vehicle.CurrentGrainTons > 0f)
            {
                float stored = _storageSystem.StoreCrop(vehicle.CropToSow, vehicle.CurrentGrainTons);
                vehicle.CurrentGrainTons -= stored;
            }
        }

        // Automatic Field Phase Progression for Field 1
        var field = _fieldSystem.Fields.FirstOrDefault();
        if (field == null) return;

        var tractor = _vehicleSystem.Vehicles.FirstOrDefault();
        if (tractor == null) return;

        // Check dominant soil state
        int stubbleOrHarvestedCount = field.Cells.Count(c => c.State == SoilState.Stubble || c.State == SoilState.Harvested);
        int plowedCount = field.Cells.Count(c => c.State == SoilState.Plowed);
        int cultivatedCount = field.Cells.Count(c => c.State == SoilState.Cultivated);
        int readyToHarvestCount = field.Cells.Count(c => c.State == SoilState.ReadyToHarvest);

        if (AutoPlow && stubbleOrHarvestedCount >= field.CellCount * 0.75f)
        {
            if (tractor.CurrentTask != VehicleTaskType.Plowing)
            {
                tractor.AttachedEquipment = EquipmentType.Plow;
                tractor.CurrentTask = VehicleTaskType.Plowing;
                tractor.IsWorking = true;
            }
        }
        else if (AutoCultivate && plowedCount >= field.CellCount * 0.75f)
        {
            if (tractor.CurrentTask != VehicleTaskType.Cultivating)
            {
                tractor.AttachedEquipment = EquipmentType.Cultivator;
                tractor.CurrentTask = VehicleTaskType.Cultivating;
                tractor.IsWorking = true;
            }
        }
        else if (AutoSow && cultivatedCount >= field.CellCount * 0.75f)
        {
            if (tractor.CurrentTask != VehicleTaskType.Sowing)
            {
                tractor.AttachedEquipment = EquipmentType.Seeder;
                tractor.CropToSow = CropType.Wheat;
                tractor.CurrentTask = VehicleTaskType.Sowing;
                tractor.IsWorking = true;
            }
        }
        else if (AutoHarvest && readyToHarvestCount >= field.CellCount * 0.75f)
        {
            if (tractor.CurrentTask != VehicleTaskType.Harvesting)
            {
                tractor.AttachedEquipment = EquipmentType.None;
                tractor.CurrentTask = VehicleTaskType.Harvesting;
                tractor.IsWorking = true;
            }
        }

        if (AutoSell)
        {
            _marketSystem.SellAll();
        }
    }
}
