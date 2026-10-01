using System;

namespace DesktopFarmIdle.Core.Events;

public record SoilStateChangedEvent(
    int FieldId,
    int CellIndex,
    int NewState,
    DateTime Timestamp
) : IGameEvent;

public record CropStageChangedEvent(
    int FieldId,
    int CropType,
    int Stage,
    float Progress,
    DateTime Timestamp
) : IGameEvent;

public record VehicleStateChangedEvent(
    string VehicleId,
    string StateName,
    float PositionX,
    float FuelLevel,
    float Condition,
    DateTime Timestamp
) : IGameEvent;

public record VehicleBrokenEvent(
    string VehicleId,
    string BreakdownReason,
    DateTime Timestamp
) : IGameEvent;

public record VehicleRepairedEvent(
    string VehicleId,
    DateTime Timestamp
) : IGameEvent;

public record HarvestCompletedEvent(
    int FieldId,
    string CropName,
    float TonsHarvested,
    float Quality,
    DateTime Timestamp
) : IGameEvent;

public record FieldTaskCompletedEvent(
    int FieldId,
    string TaskType,
    DateTime Timestamp
) : IGameEvent;

public record WeatherChangedEvent(
    int NewWeatherType,
    string WeatherName,
    float Intensity,
    DateTime Timestamp
) : IGameEvent;

public record MarketPriceChangedEvent(
    string CropName,
    float NewPricePerTon,
    float PriceDelta,
    DateTime Timestamp
) : IGameEvent;

public record WorkerLevelUpEvent(
    string WorkerId,
    string WorkerName,
    int NewLevel,
    DateTime Timestamp
) : IGameEvent;

public record WorkerFatiguedEvent(
    string WorkerId,
    string WorkerName,
    float FatiguePercent,
    DateTime Timestamp
) : IGameEvent;

public record EconomyTransactionEvent(
    float Amount,
    float NewBalance,
    string Description,
    bool IsIncome,
    DateTime Timestamp
) : IGameEvent;

public record NotificationRequestedEvent(
    string Title,
    string Message,
    string NotificationType,
    DateTime Timestamp
) : IGameEvent;

public record InteractiveModeToggledEvent(
    bool IsInteractive,
    DateTime Timestamp
) : IGameEvent;
