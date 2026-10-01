using System;
using DesktopFarmIdle.Core.Config;

namespace DesktopFarmIdle.Simulation.Vehicles;

public enum VehicleTaskType
{
    Idle = 0,
    Plowing = 1,
    Cultivating = 2,
    Sowing = 3,
    Harvesting = 4,
    Transporting = 5,
    Refueling = 6,
    Repairing = 7
}

public class VehicleModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];
    public VehicleType Type { get; set; } = VehicleType.TractorClassic;
    public string Name { get; set; } = "Classic 25";
    public EquipmentType AttachedEquipment { get; set; } = EquipmentType.Plow;
    public CropType CropToSow { get; set; } = CropType.Wheat;

    public float FuelCapacity { get; set; } = 40.0f;
    public float FuelLevel { get; set; } = 40.0f;
    public float FuelConsumptionRate { get; set; } = 0.015f;

    public float Condition { get; set; } = 1.0f; // 1.0 = 100%
    public float Reliability { get; set; } = 0.85f;
    public bool IsBroken { get; set; } = false;
    public string BreakdownReason { get; set; } = string.Empty;

    public float TravelSpeed { get; set; } = 45.0f;
    public float WorkSpeed { get; set; } = 25.0f;

    // Operational state
    public VehicleTaskType CurrentTask { get; set; } = VehicleTaskType.Idle;
    public int TargetFieldId { get; set; } = 1;

    // Spatial & Animation variables (in world coordinates)
    public float PositionX { get; set; } = 60.0f;
    public float MoveDirection { get; set; } = 1.0f; // 1.0 for right, -1.0 for left
    public bool IsWorking { get; set; } = false;
    public bool IsTurning { get; set; } = false;
    public float TurnTimer { get; set; } = 0f;

    // Combine / Grain Truck storage
    public float GrainCapacity { get; set; } = 3.0f; // in tons
    public float CurrentGrainTons { get; set; } = 0.0f;

    public string? AssignedWorkerId { get; set; }
}
