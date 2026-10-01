using System;
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
    private WindowController? _windowController;
    private SystemTrayController? _trayController;

    private FieldRenderer? _fieldRenderer;
    private VehicleView? _vehicleView;
    private CatView? _catView;
    private WeatherView? _weatherView;
    private OverlayHud? _overlayHud;
    private ManagementWindow? _managementWindow;

    public override void _Ready()
    {
        GameLogger.Instance.Info("MainOverlay _Ready starting...", "MainOverlay");

        // 1. Initialize Core Simulation
        _simulation = new GameSimulation();

        // 2. Set up visual scenery
        BuildScenery();

        // 3. Set up HUD and Management window
        _overlayHud = new OverlayHud();
        _overlayHud.BindSimulation(_simulation);
        AddChild(_overlayHud);

        _managementWindow = new ManagementWindow();
        _managementWindow.BindSimulation(_simulation);
        AddChild(_managementWindow);

        // 4. Windows Desktop Integration
        InitializeWindowsIntegration();

        GameLogger.Instance.Info("MainOverlay initialized successfully.", "MainOverlay");
    }

    private void BuildScenery()
    {
        if (_simulation == null) return;

        // Background Silo Building
        var siloTex = GD.Load<Texture2D>("res://assets/environment/buildings/silo.png");
        var siloSprite = new Sprite2D
        {
            Texture = siloTex,
            Scale = new Vector2(0.13f, 0.13f),
            Position = new Vector2(1820, 100)
        };
        AddChild(siloSprite);

        // Weather effects (rain/snow/lightning)
        _weatherView = new WeatherView();
        _weatherView.BindWeather(_simulation.WeatherSystem);
        AddChild(_weatherView);

        // Field soil & crop renderer
        _fieldRenderer = new FieldRenderer();
        _fieldRenderer.BindField(_simulation.FieldSystem.Fields[0]);
        AddChild(_fieldRenderer);

        // Layered tractor vehicle
        _vehicleView = new VehicleView();
        _vehicleView.BindVehicle(_simulation.VehicleSystem.Vehicles[0]);
        AddChild(_vehicleView);

        // Cozy ambient farm cat
        _catView = new CatView();
        AddChild(_catView);
    }

    private void InitializeWindowsIntegration()
    {
        try
        {
            IntPtr hwnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
            _windowController = new WindowController();
            bool attached = _windowController.Initialize(hwnd);

            if (attached)
            {
                _trayController = new SystemTrayController(hwnd);

                // Wire up tray menu actions
                _trayController.OnToggleInteractive = ToggleInteractiveMode;
                _trayController.OnOpenManagement = () => _managementWindow?.Show();
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
                    return $"До урожая: {remainingSec / 60:D2}:{remainingSec % 60:D2}";
                };

                _trayController.IsInteractiveActive = () => _windowController.IsInteractive;
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
        // Advance simulation clock
        _simulation?.Update((float)delta);
    }

    public void ToggleInteractiveMode()
    {
        if (_windowController == null) return;
        _windowController.ToggleInteractive();
        _overlayHud?.SetInteractiveDisplay(_windowController.IsInteractive);
    }

    public void TogglePause()
    {
        if (_simulation == null) return;
        _simulation.Clock.IsPaused = !_simulation.Clock.IsPaused;
    }

    public void ExitGame()
    {
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
