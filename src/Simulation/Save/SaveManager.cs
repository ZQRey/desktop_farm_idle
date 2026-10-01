using System;
using System.IO;
using System.Text.Json;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Logging;
using DesktopFarmIdle.Simulation.Economy;
using DesktopFarmIdle.Simulation.Field;
using DesktopFarmIdle.Simulation.Storage;
using DesktopFarmIdle.Simulation.Vehicles;

namespace DesktopFarmIdle.Simulation.Save;

public class SaveManager
{
    private readonly string _saveFilePath;
    private readonly string _backupFilePath;
    private readonly string _tempFilePath;

    public SaveManager()
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savedata");
        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch { }

        _saveFilePath = Path.Combine(dir, "savegame.json");
        _backupFilePath = Path.Combine(dir, "savegame.bak");
        _tempFilePath = Path.Combine(dir, "savegame.tmp");
    }

    public bool Save(
        EconomySystem economy,
        StorageSystem storage,
        FieldSystem fieldSystem,
        VehicleSystem vehicleSystem)
    {
        try
        {
            var data = new SaveDataModel
            {
                SaveVersion = 1,
                SaveTimestampUtc = DateTime.UtcNow,
                Balance = economy.Balance,
                TotalEarnings = economy.TotalEarnings,
                SiloCapacityTons = storage.MaxCapacityTons
            };

            foreach (var (crop, tons) in storage.InventoryTons)
            {
                data.InventoryTons[crop.ToString()] = tons;
            }

            foreach (var f in fieldSystem.Fields)
            {
                var fData = new FieldSaveData { Id = f.Id, Name = f.Name };
                foreach (var c in f.Cells)
                {
                    fData.Cells.Add(new FieldCellSaveData
                    {
                        State = (int)c.State,
                        Crop = (int)c.CurrentCrop,
                        Growth = c.GrowthProgress,
                        Moisture = c.Moisture
                    });
                }
                data.Fields.Add(fData);
            }

            foreach (var v in vehicleSystem.Vehicles)
            {
                data.Vehicles.Add(new VehicleSaveData
                {
                    Id = v.Id,
                    Type = (int)v.Type,
                    Name = v.Name,
                    AttachedEquipment = (int)v.AttachedEquipment,
                    CropToSow = (int)v.CropToSow,
                    FuelLevel = v.FuelLevel,
                    Condition = v.Condition,
                    CurrentTask = (int)v.CurrentTask,
                    PositionX = v.PositionX
                });
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            // Atomic file write: write temp -> replace
            File.WriteAllText(_tempFilePath, json);

            if (File.Exists(_saveFilePath))
            {
                File.Copy(_saveFilePath, _backupFilePath, true);
            }

            File.Move(_tempFilePath, _saveFilePath, true);
            GameLogger.Instance.Info("Game saved successfully.", "SaveManager");
            return true;
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Failed to save game: {ex.Message}", ex, "SaveManager");
            return false;
        }
    }

    public SaveDataModel? Load()
    {
        string? targetFile = null;
        if (File.Exists(_saveFilePath))
        {
            targetFile = _saveFilePath;
        }
        else if (File.Exists(_backupFilePath))
        {
            targetFile = _backupFilePath;
            GameLogger.Instance.Warning("Primary save missing. Falling back to backup.", "SaveManager");
        }

        if (targetFile == null)
            return null;

        try
        {
            string json = File.ReadAllText(targetFile);
            var data = JsonSerializer.Deserialize<SaveDataModel>(json);
            return data;
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Corrupt save in {targetFile}: {ex.Message}", ex, "SaveManager");
            return null;
        }
    }
}
