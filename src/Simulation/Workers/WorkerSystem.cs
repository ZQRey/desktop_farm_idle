using System;
using System.Collections.Generic;
using DesktopFarmIdle.Core.Events;

namespace DesktopFarmIdle.Simulation.Workers;

public enum WorkerRole
{
    TractorDriver = 0,
    CombineOperator = 1,
    Mechanic = 2,
    Agronomist = 3,
    TruckDriver = 4,
    GeneralWorker = 5,
    FarmManager = 6
}

public class WorkerModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];
    public string Name { get; set; } = "John Doe";
    public WorkerRole Role { get; set; } = WorkerRole.TractorDriver;
    public int Level { get; set; } = 1;
    public float Experience { get; set; } = 0f;
    public float Fatigue { get; set; } = 0f; // 0 to 100
    public float Morale { get; set; } = 95f;
    public float WagePerMinute { get; set; } = 2.0f;
    public bool IsResting { get; set; } = false;
    public string? AssignedVehicleId { get; set; }

    public float GetEfficiencyMultiplier() => Fatigue switch
    {
        < 40f => 1.0f + (Level - 1) * 0.05f,
        < 70f => 0.9f + (Level - 1) * 0.05f,
        < 90f => 0.7f + (Level - 1) * 0.05f,
        _ => 0.5f
    };
}

public class WorkerSystem
{
    private readonly IEventBus _eventBus;
    public List<WorkerModel> Workers { get; } = new();

    public WorkerSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
        // Starter tractor driver
        Workers.Add(new WorkerModel
        {
            Name = "Vasily Farmer",
            Role = WorkerRole.TractorDriver,
            Level = 1
        });
    }

    public void Update(float deltaSimulationTime)
    {
        foreach (var worker in Workers)
        {
            if (worker.IsResting)
            {
                // Recover fatigue while resting
                worker.Fatigue = Math.Max(0f, worker.Fatigue - 0.1f * deltaSimulationTime);
                if (worker.Fatigue <= 10f)
                {
                    worker.IsResting = false;
                }
            }
            else
            {
                // Slowly accumulate fatigue while working
                worker.Fatigue = Math.Min(100f, worker.Fatigue + 0.02f * deltaSimulationTime);
                worker.Experience += 0.05f * deltaSimulationTime;

                // Level up check: each level requires level * 100 XP
                float neededXp = worker.Level * 100.0f;
                if (worker.Experience >= neededXp)
                {
                    worker.Level++;
                    worker.Experience -= neededXp;
                    _eventBus.Publish(new WorkerLevelUpEvent(worker.Id, worker.Name, worker.Level, DateTime.Now));
                }

                if (worker.Fatigue >= 90f)
                {
                    worker.IsResting = true;
                    _eventBus.Publish(new WorkerFatiguedEvent(worker.Id, worker.Name, worker.Fatigue, DateTime.Now));
                }
            }
        }
    }
}
