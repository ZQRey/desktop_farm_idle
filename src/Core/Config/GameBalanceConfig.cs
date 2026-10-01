using System.Collections.Generic;

namespace DesktopFarmIdle.Core.Config;

public enum SoilState
{
    Stubble = 0,
    Plowed = 1,
    Cultivated = 2,
    Seeded = 3,
    Germinating = 4,
    GrowthStage1 = 5,
    GrowthStage2 = 6,
    GrowthStage3 = 7,
    ReadyToHarvest = 8,
    Harvested = 9
}

public enum CropType
{
    Wheat = 0,
    Barley = 1,
    Corn = 2,
    Sunflower = 3,
    Potato = 4
}

public enum VehicleType
{
    TractorClassic = 0,
    TractorUniversal = 1,
    TractorHeavy = 2,
    TractorApexPro = 3,
    HarvesterCompact = 4,
    HarvesterModern = 5,
    GrainTruck = 6,
    ServiceVan = 7
}

public enum EquipmentType
{
    None = 0,
    Plow = 1,
    Cultivator = 2,
    Seeder = 3,
    FertilizerSpreader = 4,
    Sprayer = 5,
    Trailer = 6
}

public enum WeatherType
{
    Clear = 0,
    Cloudy = 1,
    LightRain = 2,
    Rain = 3,
    HeavyRain = 4,
    Storm = 5,
    Hail = 6,
    Hot = 7,
    HeatWave = 8,
    Wind = 9,
    StrongWind = 10,
    Snow = 11,
    Fog = 12
}

public record CropConfig(
    CropType Type,
    string Name,
    float BaseGrowthSeconds,
    float SeedCostPerCell,
    float ExpectedYieldPerCell,
    float BaseSellPricePerTon,
    float WaterDemand,
    float HeatResistance,
    float ColdResistance,
    float DiseaseResistance
);

public record VehicleConfig(
    VehicleType Type,
    string Name,
    string Category,
    float Price,
    float Horsepower,
    float MaxTravelSpeed,
    float WorkSpeed,
    float FuelCapacity,
    float FuelConsumptionRate,
    float Reliability
);

public record EquipmentConfig(
    EquipmentType Type,
    string Name,
    float Price,
    float WorkSpeedModifier,
    float EfficiencyBonus
);

public static class GameBalanceConfig
{
    public const float StartingCoins = 500.0f;
    public const int DefaultFieldCellCount = 60; // 60 cells across the panorama field
    public const float FieldCellWidthWorldUnits = 30.0f; // Each cell represents 30px
    public const float TargetHarvestCycleSeconds = 1080.0f; // 18 minutes baseline

    public static readonly Dictionary<CropType, CropConfig> Crops = new()
    {
        [CropType.Wheat] = new(
            CropType.Wheat, "Wheat",
            BaseGrowthSeconds: 720.0f,
            SeedCostPerCell: 1.0f,
            ExpectedYieldPerCell: 0.05f, // ~3 tons total for 60 cells
            BaseSellPricePerTon: 180.0f,
            WaterDemand: 0.5f,
            HeatResistance: 0.8f,
            ColdResistance: 0.7f,
            DiseaseResistance: 0.85f
        ),
        [CropType.Barley] = new(
            CropType.Barley, "Barley",
            BaseGrowthSeconds: 640.0f,
            SeedCostPerCell: 0.8f,
            ExpectedYieldPerCell: 0.045f,
            BaseSellPricePerTon: 165.0f,
            WaterDemand: 0.4f,
            HeatResistance: 0.85f,
            ColdResistance: 0.85f,
            DiseaseResistance: 0.9f
        ),
        [CropType.Corn] = new(
            CropType.Corn, "Corn",
            BaseGrowthSeconds: 900.0f,
            SeedCostPerCell: 1.5f,
            ExpectedYieldPerCell: 0.08f,
            BaseSellPricePerTon: 150.0f,
            WaterDemand: 0.8f,
            HeatResistance: 0.9f,
            ColdResistance: 0.4f,
            DiseaseResistance: 0.75f
        ),
        [CropType.Sunflower] = new(
            CropType.Sunflower, "Sunflower",
            BaseGrowthSeconds: 960.0f,
            SeedCostPerCell: 1.8f,
            ExpectedYieldPerCell: 0.06f,
            BaseSellPricePerTon: 240.0f,
            WaterDemand: 0.6f,
            HeatResistance: 0.95f,
            ColdResistance: 0.5f,
            DiseaseResistance: 0.8f
        ),
        [CropType.Potato] = new(
            CropType.Potato, "Potato",
            BaseGrowthSeconds: 840.0f,
            SeedCostPerCell: 1.2f,
            ExpectedYieldPerCell: 0.12f,
            BaseSellPricePerTon: 110.0f,
            WaterDemand: 0.75f,
            HeatResistance: 0.6f,
            ColdResistance: 0.75f,
            DiseaseResistance: 0.7f
        )
    };

    public static readonly Dictionary<VehicleType, VehicleConfig> Vehicles = new()
    {
        [VehicleType.TractorClassic] = new(
            VehicleType.TractorClassic, "Rusty Classic 25", "Tractor",
            Price: 0.0f, Horsepower: 28.0f, MaxTravelSpeed: 45.0f, WorkSpeed: 25.0f,
            FuelCapacity: 40.0f, FuelConsumptionRate: 0.015f, Reliability: 0.80f
        ),
        [VehicleType.TractorUniversal] = new(
            VehicleType.TractorUniversal, "AgroSteed 70", "Tractor",
            Price: 1200.0f, Horsepower: 75.0f, MaxTravelSpeed: 60.0f, WorkSpeed: 35.0f,
            FuelCapacity: 80.0f, FuelConsumptionRate: 0.022f, Reliability: 0.90f
        ),
        [VehicleType.TractorHeavy] = new(
            VehicleType.TractorHeavy, "Titan 150", "Heavy Tractor",
            Price: 3500.0f, Horsepower: 160.0f, MaxTravelSpeed: 55.0f, WorkSpeed: 42.0f,
            FuelCapacity: 160.0f, FuelConsumptionRate: 0.040f, Reliability: 0.94f
        ),
        [VehicleType.TractorApexPro] = new(
            VehicleType.TractorApexPro, "Apex Pro 280", "Modern Tractor",
            Price: 8500.0f, Horsepower: 290.0f, MaxTravelSpeed: 70.0f, WorkSpeed: 55.0f,
            FuelCapacity: 260.0f, FuelConsumptionRate: 0.055f, Reliability: 0.98f
        ),
        [VehicleType.HarvesterCompact] = new(
            VehicleType.HarvesterCompact, "CerealCut 3", "Combine Harvester",
            Price: 2800.0f, Horsepower: 110.0f, MaxTravelSpeed: 40.0f, WorkSpeed: 22.0f,
            FuelCapacity: 120.0f, FuelConsumptionRate: 0.035f, Reliability: 0.85f
        ),
        [VehicleType.HarvesterModern] = new(
            VehicleType.HarvesterModern, "GrainMaster 9000", "Combine Harvester",
            Price: 12000.0f, Horsepower: 380.0f, MaxTravelSpeed: 50.0f, WorkSpeed: 40.0f,
            FuelCapacity: 350.0f, FuelConsumptionRate: 0.070f, Reliability: 0.96f
        ),
        [VehicleType.GrainTruck] = new(
            VehicleType.GrainTruck, "FarmHauler T8", "Truck",
            Price: 1800.0f, Horsepower: 180.0f, MaxTravelSpeed: 80.0f, WorkSpeed: 60.0f,
            FuelCapacity: 150.0f, FuelConsumptionRate: 0.025f, Reliability: 0.92f
        ),
        [VehicleType.ServiceVan] = new(
            VehicleType.ServiceVan, "Mobile Tech Van", "Service",
            Price: 1500.0f, Horsepower: 120.0f, MaxTravelSpeed: 85.0f, WorkSpeed: 70.0f,
            FuelCapacity: 90.0f, FuelConsumptionRate: 0.020f, Reliability: 0.95f
        )
    };

    public static readonly Dictionary<EquipmentType, EquipmentConfig> Equipment = new()
    {
        [EquipmentType.Plow] = new(EquipmentType.Plow, "3-Furrow Moldboard Plow", 300.0f, 0.9f, 1.0f),
        [EquipmentType.Cultivator] = new(EquipmentType.Cultivator, "Spring Tine Cultivator", 450.0f, 1.1f, 1.0f),
        [EquipmentType.Seeder] = new(EquipmentType.Seeder, "Precision Row Seeder", 600.0f, 1.0f, 1.0f),
        [EquipmentType.FertilizerSpreader] = new(EquipmentType.FertilizerSpreader, "Centrifugal Spreader", 500.0f, 1.2f, 1.25f),
        [EquipmentType.Sprayer] = new(EquipmentType.Sprayer, "Field Boom Sprayer", 750.0f, 1.2f, 1.15f),
        [EquipmentType.Trailer] = new(EquipmentType.Trailer, "Tipping Grain Trailer", 800.0f, 1.0f, 1.0f)
    };
}
