using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.DesktopIntegration;
using DesktopFarmIdle.Simulation;
using DesktopFarmIdle.Simulation.Vehicles;

namespace DesktopFarmIdle.UI;

public partial class ManagementWindow : Window
{
    private GameSimulation? _simulation;

    private TabContainer? _tabs;
    private Label? _lblBalance;
    private Label? _lblStorage;
    private Label? _lblCropStatus;
    private Label? _lblTractorStatus;
    private CheckBox? _chkAutoPlow;
    private CheckBox? _chkAutoSow;
    private CheckBox? _chkAutoHarvest;
    private CheckBox? _chkAutoRefuel;
    private SpinBox? _spinHeight;

    public void BindSimulation(GameSimulation simulation)
    {
        _simulation = simulation;
        RefreshUI();
    }

    public override void _Ready()
    {
        Title = "🌾 Desktop Farm Idle — Управление фермой";
        Size = new Vector2I(840, 540);
        Position = new Vector2I(200, 200);
        Exclusive = false;
        Transient = false;
        Visible = false;

        CloseRequested += () => Hide();

        BuildInterface();
    }

    public void Open()
    {
        RefreshUI();
        Show();
        GrabFocus();
    }

    private void BuildInterface()
    {
        var rootPanel = new PanelContainer
        {
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f
        };
        AddChild(rootPanel);

        var vbox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(820, 520)
        };
        rootPanel.AddChild(vbox);

        // Header bar
        var header = new HBoxContainer();
        _lblBalance = new Label { Text = "💰 Баланс: 500 Монет" };
        _lblBalance.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_lblBalance);

        header.AddChild(new VSeparator());

        _lblStorage = new Label { Text = "🏚️ Элеватор: 0.0 / 25.0 т" };
        _lblStorage.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_lblStorage);

        header.AddChild(new VSeparator());

        _lblCropStatus = new Label { Text = "🌾 Пшеница: Рост (45%)" };
        _lblCropStatus.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_lblCropStatus);

        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        // Tab container
        _tabs = new TabContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(_tabs);

        BuildDashboardTab();
        BuildMachineryTab();
        BuildMarketTab();
        BuildAutomationTab();
        BuildSettingsTab();
        BuildDevTab();
    }

    private void BuildDashboardTab()
    {
        var dash = new VBoxContainer { Name = "📊 Дашборд" };

        var lblWelcome = new Label
        {
            Text = "Добро пожаловать в Desktop Farm Idle!\nСпокойная idle-ферма живёт на рабочем столе и самостоятельно выполняет работы."
        };
        dash.AddChild(lblWelcome);

        dash.AddChild(new HSeparator());

        _lblTractorStatus = new Label { Text = "🚜 Состояние техники: Отличное | Топливо: 100%" };
        dash.AddChild(_lblTractorStatus);

        dash.AddChild(new HSeparator());

        var btnSellAll = new Button { Text = "💰 Продать весь урожай со склада по текущей рыночной цене" };
        btnSellAll.Pressed += () =>
        {
            if (_simulation != null)
            {
                float earned = _simulation.MarketSystem.SellAll();
                RefreshUI();
            }
        };
        dash.AddChild(btnSellAll);

        _tabs?.AddChild(dash);
    }

    private void BuildMachineryTab()
    {
        var mach = new VBoxContainer { Name = "🚜 Техника" };

        var lblInfo = new Label { Text = "Парк сельхозтехники и мастерская обслуживания:" };
        mach.AddChild(lblInfo);

        var btnRefuel = new Button { Text = "⛽ Заправить технику (Полные баки)" };
        btnRefuel.Pressed += () =>
        {
            if (_simulation != null)
            {
                foreach (var v in _simulation.VehicleSystem.Vehicles)
                {
                    _simulation.VehicleSystem.RefuelVehicle(v.Id);
                }
                RefreshUI();
            }
        };
        mach.AddChild(btnRefuel);

        var btnRepair = new Button { Text = "🔧 Провести полное техобслуживание и ремонт" };
        btnRepair.Pressed += () =>
        {
            if (_simulation != null)
            {
                foreach (var v in _simulation.VehicleSystem.Vehicles)
                {
                    _simulation.VehicleSystem.RepairVehicle(v.Id);
                }
                RefreshUI();
            }
        };
        mach.AddChild(btnRepair);

        _tabs?.AddChild(mach);
    }

    private void BuildMarketTab()
    {
        var market = new VBoxContainer { Name = "📈 Рынок и склад" };

        var lblPrices = new Label { Text = "Текущие биржевые цены на урожай (за тонну):" };
        market.AddChild(lblPrices);

        foreach (CropType crop in Enum.GetValues<CropType>())
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = $"🌾 {crop}: ", CustomMinimumSize = new Vector2(130, 0) });

            var btnSell = new Button { Text = $"Продать {crop}" };
            var cropLocal = crop;
            btnSell.Pressed += () =>
            {
                if (_simulation != null)
                {
                    float stock = _simulation.StorageSystem.InventoryTons[cropLocal];
                    _simulation.MarketSystem.SellCrop(cropLocal, stock);
                    RefreshUI();
                }
            };
            row.AddChild(btnSell);
            market.AddChild(row);
        }

        _tabs?.AddChild(market);
    }

    private void BuildAutomationTab()
    {
        var auto = new VBoxContainer { Name = "⚙️ Автоматизация" };

        _chkAutoPlow = new CheckBox { Text = "Автоматическая вспашка поля", ButtonPressed = true };
        _chkAutoPlow.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoPlow = v; };
        auto.AddChild(_chkAutoPlow);

        _chkAutoSow = new CheckBox { Text = "Автоматический посев семян", ButtonPressed = true };
        _chkAutoSow.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoSow = v; };
        auto.AddChild(_chkAutoSow);

        _chkAutoHarvest = new CheckBox { Text = "Автоматическая уборка созревшего урожая", ButtonPressed = true };
        _chkAutoHarvest.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoHarvest = v; };
        auto.AddChild(_chkAutoHarvest);

        _chkAutoRefuel = new CheckBox { Text = "Автоматическая дозаправка техники", ButtonPressed = true };
        _chkAutoRefuel.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoRefuel = v; };
        auto.AddChild(_chkAutoRefuel);

        _tabs?.AddChild(auto);
    }

    private void BuildSettingsTab()
    {
        var settings = new VBoxContainer { Name = "⚙️ Настройки" };

        // 1. Preset Farm Sizes (Compact 90px, Normal 120px, Large 160px)
        var lblSize = new Label { Text = "Размер фермы (Diorama Size):" };
        settings.AddChild(lblSize);

        var rowPresets = new HBoxContainer();
        var btnCompact = new Button { Text = "Компактный (90 px)" };
        btnCompact.Pressed += () =>
        {
            WindowsOverlayService.Instance.ConfiguredLogicalHeight = 90;
            if (_spinHeight != null) _spinHeight.Value = 90;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        };
        rowPresets.AddChild(btnCompact);

        var btnNormal = new Button { Text = "Обычный (120 px) [Рекомендуется]" };
        btnNormal.Pressed += () =>
        {
            WindowsOverlayService.Instance.ConfiguredLogicalHeight = 120;
            if (_spinHeight != null) _spinHeight.Value = 120;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        };
        rowPresets.AddChild(btnNormal);

        var btnLarge = new Button { Text = "Крупный (160 px)" };
        btnLarge.Pressed += () =>
        {
            WindowsOverlayService.Instance.ConfiguredLogicalHeight = 160;
            if (_spinHeight != null) _spinHeight.Value = 160;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        };
        rowPresets.AddChild(btnLarge);
        settings.AddChild(rowPresets);

        // 2. Exact Custom Height
        var rowHeight = new HBoxContainer();
        rowHeight.AddChild(new Label { Text = "Точная высота (px): " });
        _spinHeight = new SpinBox
        {
            MinValue = 80,
            MaxValue = 220,
            Step = 5,
            Value = WindowsOverlayService.Instance.ConfiguredLogicalHeight
        };
        _spinHeight.ValueChanged += val =>
        {
            WindowsOverlayService.Instance.ConfiguredLogicalHeight = (int)val;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        };
        rowHeight.AddChild(_spinHeight);
        settings.AddChild(rowHeight);

        // 3. Bottom Offset (Raise above taskbar)
        var rowOffset = new HBoxContainer();
        rowOffset.AddChild(new Label { Text = "Отступ от панели задач снизу (px): " });
        var spinOffset = new SpinBox
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 5,
            Value = WindowsOverlayService.Instance.BottomOffset
        };
        spinOffset.ValueChanged += val =>
        {
            WindowsOverlayService.Instance.BottomOffset = (int)val;
            WindowsOverlayService.Instance.PositionOverlayAboveTaskbar();
        };
        rowOffset.AddChild(spinOffset);
        settings.AddChild(rowOffset);

        // 4. Interactive Mode
        var chkInteractive = new CheckBox { Text = "Интерактивный режим (принимать клики мыши сквозь окно)" };
        chkInteractive.ButtonPressed = WindowsOverlayService.Instance.IsInteractive;
        chkInteractive.Toggled += val =>
        {
            WindowsOverlayService.Instance.SetInteractiveMode(val);
        };
        settings.AddChild(chkInteractive);

        // 5. Monitor Info
        var lblMonitor = new Label { Text = "🖥️ Дисплей: Основной монитор Windows (Primary Display)" };
        settings.AddChild(lblMonitor);

        _tabs?.AddChild(settings);
    }

    private void BuildDevTab()
    {
        var dev = new VBoxContainer { Name = "🛠️ Чит-панель" };

        var btnAddCoins = new Button { Text = "+5,000 Монет" };
        btnAddCoins.Pressed += () =>
        {
            _simulation?.EconomySystem.AddIncome(5000f, "Dev grant");
            RefreshUI();
        };
        dev.AddChild(btnAddCoins);

        var btnInstaGrow = new Button { Text = "Мгновенное созревание урожая (100% готов к уборке)" };
        btnInstaGrow.Pressed += () =>
        {
            if (_simulation != null)
            {
                var f = _simulation.FieldSystem.Fields[0];
                for (int i = 0; i < f.CellCount; i++)
                {
                    _simulation.FieldSystem.ModifyCellSoilState(1, i, SoilState.ReadyToHarvest, CropType.Wheat);
                }
                RefreshUI();
            }
        };
        dev.AddChild(btnInstaGrow);

        var btnRain = new Button { Text = "Вызвать дождь" };
        btnRain.Pressed += () =>
        {
            _simulation?.WeatherSystem.ForceWeather(WeatherType.Rain, 180f);
        };
        dev.AddChild(btnRain);

        _tabs?.AddChild(dev);
    }

    public void RefreshUI()
    {
        if (_simulation == null) return;

        if (_lblBalance != null)
            _lblBalance.Text = $"💰 {_simulation.EconomySystem.Balance:F0} Монет";

        if (_lblStorage != null)
            _lblStorage.Text = $"🏚️ Элеватор: {_simulation.StorageSystem.GetTotalStoredTons():F1} / {_simulation.StorageSystem.MaxCapacityTons:F0} т";

        if (_lblCropStatus != null)
        {
            float avgGrowth = _simulation.FieldSystem.GetAverageFieldGrowth(1);
            _lblCropStatus.Text = $"🌾 Рост пшеницы: {avgGrowth * 100:F0}%";
        }

        if (_lblTractorStatus != null && _simulation.VehicleSystem.Vehicles.Count > 0)
        {
            var v = _simulation.VehicleSystem.Vehicles[0];
            _lblTractorStatus.Text = $"🚜 Трактор: {v.Name} | Топливо: {v.FuelLevel:F0}/{v.FuelCapacity:F0}L | Состояние: {v.Condition * 100:F0}%";
        }
    }
}
