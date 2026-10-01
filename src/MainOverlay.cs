using System;
using System.Diagnostics;
using Godot;
using DesktopFarmIdle.Core.Logging;
using DesktopFarmIdle.DesktopIntegration;
using DesktopFarmIdle.Rendering;
using DesktopFarmIdle.Simulation;
using DesktopFarmIdle.UI;

namespace DesktopFarmIdle;

public partial class MainOverlay : Node2D
{
    private GameSimulation? _simulation;
    private SystemTrayController? _trayController;

    private FieldRenderer? _fieldRenderer;
    private VehicleView? _vehicleView;
    private CatView? _catView;
    private WeatherView? _weatherView;
    private OverlayHud? _toastOverlay;
    private ManagementWindow? _managementWindow;

    private float _displayCheckTimer = 0f;
    private const float DisplayCheckInterval = 3.0f; // Check for taskbar/display shifts every 3s

    public override void _Ready()
    {
        // 0. Ensure Windows desktop assemblies can resolve
        DesktopFarmIdle.Core.WindowsDesktopResolver.Initialize();

        // 1. Force hardware-transparent clear color and transparent viewport
        RenderingServer.SetDefaultClearColor(new Color(0, 0, 0, 0));
        GetTree().Root.TransparentBg = true;
        GetViewport().TransparentBg = true;

        GameLogger.Instance.Info("MainOverlay _Ready starting...", "MainOverlay");

        // 2. Initialize Core Simulation
        _simulation = new GameSimulation();

        // 3. Build World Scenery (Diorama, 0 clickable UI controls in desktop world!)
        BuildScenery();

        // 4. Transient Toast Overlay (Non-persistent, mouse ignore)
        _toastOverlay = new OverlayHud();
        _toastOverlay.BindSimulation(_simulation);
        AddChild(_toastOverlay);

        // 5. Standalone Management Window
        _managementWindow = new ManagementWindow();
        _managementWindow.BindSimulation(_simulation);
        AddChild(_managementWindow);

        // 6. Windows Desktop Integration
        InitializeWindowsIntegration();

        GameLogger.Instance.Info("MainOverlay initialized successfully.", "MainOverlay");
    }

    private void BuildScenery()
    {
        if (_simulation == null) return;

        // Weather effects (rain, snow, lightning) across viewport
        _weatherView = new WeatherView();
        _weatherView.BindWeather(_simulation.WeatherSystem);
        AddChild(_weatherView);

        // Field soil & crop renderer (Clean diorama soil surface at Y=85, base at Y=120)
        _fieldRenderer = new FieldRenderer();
        _fieldRenderer.BindField(_simulation.FieldSystem.Fields[0]);
        AddChild(_fieldRenderer);

        // Layered tractor vehicle (Rolling on soil surface at Y=85)
        _vehicleView = new VehicleView();
        _vehicleView.BindVehicle(_simulation.VehicleSystem.Vehicles[0]);
        AddChild(_vehicleView);

        // Cozy ambient farm cat (Walking along soil surface at Y=85)
        _catView = new CatView();
        AddChild(_catView);
    }

    private void InitializeWindowsIntegration()
    {
        try
        {
            // Acquire native HWND
            int winId = (int)DisplayServer.MainWindowId;
            IntPtr hwnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, winId);
            if (hwnd == IntPtr.Zero)
            {
                hwnd = Process.GetCurrentProcess().MainWindowHandle;
            }

            // Attach WindowsOverlayService
            bool attached = WindowsOverlayService.Instance.Attach(hwnd);

            // Also configure Godot's internal window position and size to match
            var (x, y, width, height) = WindowsOverlayService.Instance.CalculateOverlayBounds();
            DisplayServer.WindowSetSize(new Vector2I(width, height), winId);
            DisplayServer.WindowSetPosition(new Vector2I(x, y), winId);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, false, winId);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true, winId);
            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Transparent, true, winId);

            if (attached && hwnd != IntPtr.Zero)
            {
                _trayController = new SystemTrayController(hwnd);

                // Wire up tray callbacks
                _trayController.OnToggleInteractive = ToggleInteractiveMode;
                _trayController.OnOpenManagement = () => _managementWindow?.Open();
                _trayController.OnTogglePause = TogglePause;
                _trayController.OnExit = ExitGame;

                // Wire up dynamic tray menu status
                _trayController.GetFarmStatusText = () =>
                    _simulation?.Clock.IsPaused == true ? "Ферма: на паузе" : "Ферма: работает";

                _trayController.GetFieldCropText = () => "Поле: Пшеница";

                _trayController.GetGrowthText = () =>
                {
                    float g = (_simulation?.FieldSystem.GetAverageFieldGrowth(1) ?? 0f) * 100f;
                    return $"Рост: {g:F0}%";
                };

                _trayController.GetTimeToHarvestText = () =>
                {
                    float g = _simulation?.FieldSystem.GetAverageFieldGrowth(1) ?? 0f;
                    int remainingSec = (int)((1.0f - g) * 360f);
                    return $"До уборки: {remainingSec / 60:D2}:{remainingSec % 60:D2}";
                };

                _trayController.IsInteractiveActive = () => WindowsOverlayService.Instance.IsInteractive;
                _trayController.IsPausedActive = () => _simulation?.Clock.IsPaused ?? false;

                _trayController.Initialize();
            }
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Failed to initialize Windows desktop integration: {ex.Message}", ex, "Desktop");
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Advance simulation clock
        _simulation?.Update(dt);

        // Periodically verify taskbar & monitor position
        _displayCheckTimer += dt;
        if (_displayCheckTimer >= DisplayCheckInterval)
        {
            _displayCheckTimer = 0f;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        }
    }

    public void ToggleInteractiveMode()
    {
        WindowsOverlayService.Instance.ToggleInteractiveMode();
        string msg = WindowsOverlayService.Instance.IsInteractive
            ? "🖱️ Интерактивный режим ВКЛЮЧЕН (клики активны)"
            : "🖱️ Режим Click-Through ВКЛЮЧЕН (клики проходят сквозь ферму)";
        _toastOverlay?.ShowToast(msg, 3.0f);
    }

    public void TogglePause()
    {
        if (_simulation == null) return;
        _simulation.Clock.IsPaused = !_simulation.Clock.IsPaused;
        string msg = _simulation.Clock.IsPaused ? "⏸️ Ферма на паузе" : "▶️ Ферма возобновлена";
        _toastOverlay?.ShowToast(msg, 2.5f);
    }

    public void ExitGame()
    {
        GameLogger.Instance.Info("Exiting Desktop Farm Idle...", "MainOverlay");
        _simulation?.SaveGame();
        _trayController?.Dispose();
        GetTree().Quit();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            ExitGame();
        }
    }
}
