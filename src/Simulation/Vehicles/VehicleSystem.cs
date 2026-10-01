using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;
using DesktopFarmIdle.Simulation.Field;

namespace DesktopFarmIdle.Simulation.Vehicles;

public class VehicleSystem
{
    private readonly IEventBus _eventBus;
    private readonly FieldSystem _fieldSystem;
    private readonly Random _random = new();

    public List<VehicleModel> Vehicles { get; } = new();

    public VehicleSystem(IEventBus eventBus, FieldSystem fieldSystem)
    {
        _eventBus = eventBus;
        _fieldSystem = fieldSystem;

        // Initialize starter tractor
        var starterTractor = CreateVehicle(VehicleType.TractorClassic, "Rusty Classic 25");
        starterTractor.AttachedEquipment = EquipmentType.Plow;
        starterTractor.CurrentTask = VehicleTaskType.Plowing;
        starterTractor.IsWorking = true;
        starterTractor.PositionX = 100.0f;
        Vehicles.Add(starterTractor);
    }

    public VehicleModel CreateVehicle(VehicleType type, string name)
    {
        var config = GameBalanceConfig.Vehicles[type];
        return new VehicleModel
        {
            Type = type,
            Name = name,
            FuelCapacity = config.FuelCapacity,
            FuelLevel = config.FuelCapacity,
            FuelConsumptionRate = config.FuelConsumptionRate,
            TravelSpeed = config.MaxTravelSpeed,
            WorkSpeed = config.WorkSpeed,
            Reliability = config.Reliability,
            Condition = 1.0f
        };
    }

    public void Update(float deltaSimulationTime, float weatherFrictionModifier)
    {
        foreach (var vehicle in Vehicles)
        {
            UpdateVehicle(vehicle, deltaSimulationTime, weatherFrictionModifier);
        }
    }

    private void UpdateVehicle(VehicleModel vehicle, float dt, float weatherFriction)
    {
        if (vehicle.IsBroken)
            return;

        // Turn sequence logic
        if (vehicle.IsTurning)
        {
            vehicle.TurnTimer -= dt;
            if (vehicle.TurnTimer <= 0f)
            {
                vehicle.IsTurning = false;
                vehicle.MoveDirection *= -1.0f; // Reverse direction
                vehicle.IsWorking = true;
            }
            return;
        }

        // Active task movement & field modification
        if (vehicle.CurrentTask != VehicleTaskType.Idle)
        {
            float speed = (vehicle.IsWorking ? vehicle.WorkSpeed : vehicle.TravelSpeed) * weatherFriction;
            vehicle.PositionX += vehicle.MoveDirection * speed * dt;

            // Fuel burn
            float fuelBurn = vehicle.FuelConsumptionRate * (vehicle.IsWorking ? 1.5f : 1.0f) * dt;
            vehicle.FuelLevel = Math.Max(0f, vehicle.FuelLevel - fuelBurn);

            if (vehicle.FuelLevel <= 0f)
            {
                vehicle.IsBroken = true;
                vehicle.BreakdownReason = "Out of Fuel";
                _eventBus.Publish(new VehicleBrokenEvent(vehicle.Id, "Out of Fuel", DateTime.Now));
                return;
            }

            // Wear & tear
            vehicle.Condition = Math.Max(0f, vehicle.Condition - 0.0001f * dt);

            // Rare breakdown chance based on condition & reliability
            if (vehicle.Condition < 0.4f && _random.NextDouble() < 0.0005)
            {
                vehicle.IsBroken = true;
                vehicle.BreakdownReason = "Hydraulic Pressure Failure";
                _eventBus.Publish(new VehicleBrokenEvent(vehicle.Id, vehicle.BreakdownReason, DateTime.Now));
                return;
            }

            // Field boundary checks and turns
            const float fieldMinX = 80.0f;
            float fieldMaxX = fieldMinX + (GameBalanceConfig.DefaultFieldCellCount * GameBalanceConfig.FieldCellWidthWorldUnits) - 100.0f;

            if (vehicle.PositionX >= fieldMaxX && vehicle.MoveDirection > 0)
            {
                StartTurn(vehicle, fieldMaxX);
            }
            else if (vehicle.PositionX <= fieldMinX && vehicle.MoveDirection < 0)
            {
                StartTurn(vehicle, fieldMinX);
            }

            // Apply soil transformation at current position
            if (vehicle.IsWorking)
            {
                ApplyWorkAtPosition(vehicle);
            }
        }
    }

    private void StartTurn(VehicleModel vehicle, float clampX)
    {
        vehicle.PositionX = clampX;
        vehicle.IsTurning = true;
        vehicle.TurnTimer = 2.0f; // 2 seconds turn sequence
        vehicle.IsWorking = false;
    }

    private void ApplyWorkAtPosition(VehicleModel vehicle)
    {
        const float fieldMinX = 80.0f;
        float relativeX = vehicle.PositionX - fieldMinX;
        int cellIndex = (int)(relativeX / GameBalanceConfig.FieldCellWidthWorldUnits);

        var field = _fieldSystem.Fields.Find(f => f.Id == vehicle.TargetFieldId);
        if (field == null || cellIndex < 0 || cellIndex >= field.CellCount)
            return;

        var cell = field.Cells[cellIndex];

        switch (vehicle.CurrentTask)
        {
            case VehicleTaskType.Plowing:
                if (cell.State == SoilState.Stubble || cell.State == SoilState.Harvested)
                {
                    _fieldSystem.ModifyCellSoilState(vehicle.TargetFieldId, cellIndex, SoilState.Plowed);
                }
                break;

            case VehicleTaskType.Cultivating:
                if (cell.State == SoilState.Plowed)
                {
                    _fieldSystem.ModifyCellSoilState(vehicle.TargetFieldId, cellIndex, SoilState.Cultivated);
                }
                break;

            case VehicleTaskType.Sowing:
                if (cell.State == SoilState.Cultivated)
                {
                    _fieldSystem.ModifyCellSoilState(vehicle.TargetFieldId, cellIndex, SoilState.Seeded, vehicle.CropToSow);
                }
                break;

            case VehicleTaskType.Harvesting:
                if (cell.State == SoilState.ReadyToHarvest)
                {
                    _fieldSystem.ModifyCellSoilState(vehicle.TargetFieldId, cellIndex, SoilState.Harvested);
                    // Add grain to bunker
                    if (GameBalanceConfig.Crops.TryGetValue(cell.CurrentCrop, out var cropConfig))
                    {
                        vehicle.CurrentGrainTons += cropConfig.ExpectedYieldPerCell;
                    }
                }
                break;
        }
    }

    public void RefuelVehicle(string vehicleId)
    {
        var vehicle = Vehicles.Find(v => v.Id == vehicleId);
        if (vehicle != null)
        {
            vehicle.FuelLevel = vehicle.FuelCapacity;
            if (vehicle.IsBroken && vehicle.BreakdownReason == "Out of Fuel")
            {
                vehicle.IsBroken = false;
                vehicle.BreakdownReason = string.Empty;
                _eventBus.Publish(new VehicleRepairedEvent(vehicle.Id, DateTime.Now));
            }
        }
    }

    public void RepairVehicle(string vehicleId)
    {
        var vehicle = Vehicles.Find(v => v.Id == vehicleId);
        if (vehicle != null)
        {
            vehicle.Condition = 1.0f;
            vehicle.IsBroken = false;
            vehicle.BreakdownReason = string.Empty;
            _eventBus.Publish(new VehicleRepairedEvent(vehicle.Id, DateTime.Now));
        }
    }
}
