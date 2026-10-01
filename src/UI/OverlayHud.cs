using System;
using System.Collections.Generic;
using Godot;
using DesktopFarmIdle.Core.Events;
using DesktopFarmIdle.Simulation;

namespace DesktopFarmIdle.UI;

public partial class OverlayHud : Control
{
    private GameSimulation? _simulation;
    private readonly Queue<(string text, float duration)> _pendingToasts = new();
    private Label? _toastLabel;
    private PanelContainer? _toastPanel;
    private float _toastTimer = 0f;
    private float _currentDuration = 0f;

    public void BindSimulation(GameSimulation simulation)
    {
        _simulation = simulation;
        _simulation.EventBus.Subscribe<VehicleBrokenEvent>(OnVehicleBroken);
        _simulation.EventBus.Subscribe<HarvestCompletedEvent>(OnHarvestCompleted);
        _simulation.EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
    }

    public override void _Ready()
    {
        // Zero persistent HUD. Completely pass-through mouse filter.
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorRight = 1.0f;
        AnchorBottom = 1.0f;

        // Subtle contextual toast bubble that appears only on events and fades out
        _toastPanel = new PanelContainer
        {
            Position = new Vector2(50, 16),
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, 0) // Hidden by default
        };
        AddChild(_toastPanel);

        _toastLabel = new Label
        {
            Text = string.Empty,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _toastLabel.AddThemeFontSizeOverride("font_size", 13);
        _toastLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.95f, 0.90f, 0.95f));
        _toastPanel.AddChild(_toastLabel);
    }

    private void OnVehicleBroken(VehicleBrokenEvent e)
    {
        ShowToast($"⚠️ Трактор: {e.BreakdownReason}", 4.0f);
    }

    private void OnHarvestCompleted(HarvestCompletedEvent e)
    {
        ShowToast($"🌾 Собрано {e.TonsHarvested:F1} т урожая пшеницы!", 4.0f);
    }

    private void OnWeatherChanged(WeatherChangedEvent e)
    {
        ShowToast($"⛅ Погода изменилась: {e.WeatherName}", 3.0f);
    }

    public void ShowToast(string message, float duration = 3.5f)
    {
        _pendingToasts.Enqueue((message, duration));
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_toastTimer > 0f)
        {
            _toastTimer -= dt;
            if (_toastTimer <= 0f)
            {
                if (_toastPanel != null) _toastPanel.Modulate = new Color(1, 1, 1, 0);
            }
            else
            {
                // Smooth fade in / out
                float alpha = 1.0f;
                if (_toastTimer < 0.8f)
                    alpha = _toastTimer / 0.8f;
                else if (_toastTimer > (_currentDuration - 0.5f))
                    alpha = (_currentDuration - _toastTimer) / 0.5f;

                if (_toastPanel != null)
                    _toastPanel.Modulate = new Color(1, 1, 1, Math.Clamp(alpha, 0f, 1f));
            }
        }
        else if (_pendingToasts.Count > 0)
        {
            var (text, duration) = _pendingToasts.Dequeue();
            if (_toastLabel != null) _toastLabel.Text = text;
            _currentDuration = duration;
            _toastTimer = duration;
            if (_toastPanel != null) _toastPanel.Modulate = new Color(1, 1, 1, 0.01f);
        }
    }
}
