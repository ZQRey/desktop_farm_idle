using System;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;

namespace DesktopFarmIdle.Simulation.Economy;

public class EconomySystem
{
    private readonly IEventBus _eventBus;
    public float Balance { get; private set; } = GameBalanceConfig.StartingCoins;
    public float TotalEarnings { get; private set; } = 0f;
    public float TotalExpenses { get; private set; } = 0f;

    public EconomySystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public bool CanAfford(float amount) => Balance >= amount;

    public bool TrySpend(float amount, string description)
    {
        if (amount <= 0f) return true;
        if (Balance < amount) return false;

        Balance -= amount;
        TotalExpenses += amount;
        _eventBus.Publish(new EconomyTransactionEvent(amount, Balance, description, false, DateTime.Now));
        return true;
    }

    public void AddIncome(float amount, string description)
    {
        if (amount <= 0f) return;
        Balance += amount;
        TotalEarnings += amount;
        _eventBus.Publish(new EconomyTransactionEvent(amount, Balance, description, true, DateTime.Now));
    }

    public void SetBalance(float amount)
    {
        Balance = Math.Max(0f, amount);
    }
}
