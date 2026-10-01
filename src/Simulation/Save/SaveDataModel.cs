using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Vehicles;

namespace DesktopFarmIdle.Simulation.Save;

public class FieldCellSaveData
{
    public int State { get; set; }
    public int Crop { get; set; }
    public float Growth { get; set; }
    public float Moisture { get; set; }
}

public class FieldSaveData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FieldCellSaveData> Cells { get; set; } = new();
}

public class VehicleSaveData
{
    public string Id { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AttachedEquipment { get; set; }
    public int CropToSow { get; set; }
    public float FuelLevel { get; set; }
    public float Condition { get; set; }
    public int CurrentTask { get; set; }
    public float PositionX { get; set; }
}

public class SaveDataModel
{
    public int SaveVersion { get; set; } = 1;
    public DateTime SaveTimestampUtc { get; set; } = DateTime.UtcNow;
    public float Balance { get; set; } = 500f;
    public float TotalEarnings { get; set; } = 0f;
    public float SiloCapacityTons { get; set; } = 25f;
    public Dictionary<string, float> InventoryTons { get; set; } = new();
    public List<FieldSaveData> Fields { get; set; } = new();
    public List<VehicleSaveData> Vehicles { get; set; } = new();
}
