using System;
using Godot;
using DesktopFarmIdle.Simulation;

namespace DesktopFarmIdle.UI;

public partial class OverlayHud : Control
{
    private GameSimulation? _simulation;
    private Label? _statusPill;
    private Label? _interactiveIndicator;
    private float _updateTimer = 0f;

    public void BindSimulation(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorRight = 1.0f;
        AnchorBottom = 1.0f;

        // Top-left subtle ambient status pill
        var panel = new PanelContainer
        {
            Position = new Vector2(40, 10),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(panel);

        _statusPill = new Label
        {
            Text = "🌾 Desktop Farm Idle",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _statusPill.AddThemeFontSizeOverride("font_size", 13);
        _statusPill.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.90f, 0.9f));
        panel.AddChild(_statusPill);

        // Interactive mode indicator (shown only in interactive mode)
        var interPanel = new PanelContainer
        {
            Position = new Vector2(300, 10),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(interPanel);

        _interactiveIndicator = new Label
        {
            Text = "🖱️ [Interactive Mode Active - Clicks Enabled]",
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        _interactiveIndicator.AddThemeFontSizeOverride("font_size", 12);
        _interactiveIndicator.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.4f, 0.95f));
        interPanel.AddChild(_interactiveIndicator);
    }

    public void SetInteractiveDisplay(bool isInteractive)
    {
        if (_interactiveIndicator != null)
        {
            _interactiveIndicator.Visible = isInteractive;
        }
    }

    public override void _Process(double delta)
    {
        _updateTimer += (float)delta;
        if (_updateTimer >= 0.5f)
        {
            _updateTimer = 0f;
            UpdateStatusText();
        }
    }

    private void UpdateStatusText()
    {
        if (_simulation == null || _statusPill == null) return;

        var tractor = _simulation.VehicleSystem.Vehicles[0];
        if (tractor.IsBroken)
        {
            _statusPill.Text = $"⚠️ Tractor Service Required: {tractor.BreakdownReason}";
            return;
        }

        string task = tractor.CurrentTask switch
        {
            Simulation.Vehicles.VehicleTaskType.Plowing => "🚜 Plowing Furrows...",
            Simulation.Vehicles.VehicleTaskType.Cultivating => "🚜 Cultivating Seedbed...",
            Simulation.Vehicles.VehicleTaskType.Sowing => "🌱 Sowing Wheat...",
            Simulation.Vehicles.VehicleTaskType.Harvesting => "🌾 Harvesting Crop...",
            _ => "🌾 Resting"
        };

        float avgGrowth = _simulation.FieldSystem.GetAverageFieldGrowth(1) * 100f;
        _statusPill.Text = $"{task} | Wheat: {avgGrowth:F0}% | 💰 {_simulation.EconomySystem.Balance:F0} Coins";
    }
}
