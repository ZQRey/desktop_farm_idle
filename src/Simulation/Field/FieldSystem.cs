using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;

namespace DesktopFarmIdle.Simulation.Field;

public class FieldSystem
{
    private readonly IEventBus _eventBus;
    public List<FieldModel> Fields { get; } = new();

    public FieldSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
        // Start with 1 field by default
        Fields.Add(new FieldModel(1));
    }

    public void AddField(string name)
    {
        int newId = Fields.Count + 1;
        var field = new FieldModel(newId) { Name = name };
        Fields.Add(field);
    }

    public void ModifyCellSoilState(int fieldId, int cellIndex, SoilState newState, CropType? crop = null)
    {
        var field = Fields.Find(f => f.Id == fieldId);
        if (field == null || cellIndex < 0 || cellIndex >= field.CellCount)
            return;

        var cell = field.Cells[cellIndex];
        if (cell.State != newState)
        {
            cell.State = newState;
            if (crop.HasValue)
            {
                cell.CurrentCrop = crop.Value;
            }

            if (newState == SoilState.Seeded)
            {
                cell.GrowthProgress = 0.0f;
                cell.Health = 1.0f;
            }
            else if (newState == SoilState.Stubble || newState == SoilState.Harvested)
            {
                cell.GrowthProgress = 0.0f;
            }

            _eventBus.Publish(new SoilStateChangedEvent(fieldId, cellIndex, (int)newState, DateTime.Now));
        }
    }

    public void Update(float deltaSimulationTime, float weatherMoistureEffect, float temperatureModifier)
    {
        foreach (var field in Fields)
        {
            for (int i = 0; i < field.CellCount; i++)
            {
                var cell = field.Cells[i];

                // Moisture dynamics
                cell.Moisture = Math.Clamp(cell.Moisture + weatherMoistureEffect * deltaSimulationTime * 0.01f - 0.0005f * deltaSimulationTime, 0.1f, 1.0f);

                // If seeded or growing, progress growth
                if (cell.State >= SoilState.Seeded && cell.State < SoilState.ReadyToHarvest)
                {
                    if (GameBalanceConfig.Crops.TryGetValue(cell.CurrentCrop, out var cropConfig))
                    {
                        // Growth rate depends on moisture and temperature
                        float moistureFactor = cell.Moisture switch
                        {
                            < 0.25f => 0.5f,
                            > 0.85f => 0.8f,
                            _ => 1.0f
                        };

                        float baseDuration = cropConfig.BaseGrowthSeconds;
                        float growthDelta = (deltaSimulationTime / baseDuration) * moistureFactor * temperatureModifier;
                        cell.GrowthProgress = Math.Clamp(cell.GrowthProgress + growthDelta, 0.0f, 1.0f);

                        SoilState targetState = cell.GrowthProgress switch
                        {
                            < 0.08f => SoilState.Seeded,
                            < 0.20f => SoilState.Germinating,
                            < 0.45f => SoilState.GrowthStage1,
                            < 0.70f => SoilState.GrowthStage2,
                            < 0.95f => SoilState.GrowthStage3,
                            _ => SoilState.ReadyToHarvest
                        };

                        if (cell.State != targetState)
                        {
                            cell.State = targetState;
                            _eventBus.Publish(new SoilStateChangedEvent(field.Id, i, (int)targetState, DateTime.Now));
                            _eventBus.Publish(new CropStageChangedEvent(field.Id, (int)cell.CurrentCrop, (int)targetState, cell.GrowthProgress, DateTime.Now));
                        }
                    }
                }
            }
        }
    }

    public float GetAverageFieldGrowth(int fieldId)
    {
        var field = Fields.Find(f => f.Id == fieldId);
        if (field == null || field.CellCount == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < field.CellCount; i++)
        {
            sum += field.Cells[i].GrowthProgress;
        }
        return sum / field.CellCount;
    }

    public bool IsFieldReadyForHarvest(int fieldId)
    {
        var field = Fields.Find(f => f.Id == fieldId);
        if (field == null) return false;

        int readyCount = 0;
        for (int i = 0; i < field.CellCount; i++)
        {
            if (field.Cells[i].State == SoilState.ReadyToHarvest)
                readyCount++;
        }
        return readyCount >= field.CellCount * 0.85f;
    }
}
