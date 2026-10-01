using System;
using Godot;
using DesktopFarmIdle.Core.Config;
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
    private CheckBox? _chkAutoPlow;
    private CheckBox? _chkAutoSow;
    private CheckBox? _chkAutoHarvest;
    private CheckBox? _chkAutoRefuel;

    public void BindSimulation(GameSimulation simulation)
    {
        _simulation = simulation;
        RefreshUI();
    }

    public override void _Ready()
    {
        Title = "🌾 Desktop Farm Idle — Management Control";
        Size = new Vector2I(820, 520);
        Position = new Vector2I(150, 150);
        Exclusive = false;
        Transient = false;
        Visible = false;

        CloseRequested += () => Hide();

        BuildInterface();
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
            CustomMinimumSize = new Vector2(800, 500)
        };
        rootPanel.AddChild(vbox);

        // Header bar
        var header = new HBoxContainer();
        _lblBalance = new Label { Text = "💰 Balance: 500 Coins" };
        _lblBalance.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_lblBalance);

        header.AddChild(new VSeparator());

        _lblStorage = new Label { Text = "🏚️ Silo: 0.0 / 25.0 t" };
        _lblStorage.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_lblStorage);

        header.AddChild(new VSeparator());

        _lblCropStatus = new Label { Text = "🌾 Wheat: Growing (45%)" };
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
        BuildDevTab();
    }

    private void BuildDashboardTab()
    {
        var dash = new VBoxContainer { Name = "📊 Dashboard" };

        var lblWelcome = new Label
        {
            Text = "Welcome to Desktop Farm Idle!\nYour autonomous ambient farm operates quietly while you work."
        };
        dash.AddChild(lblWelcome);

        dash.AddChild(new HSeparator());

        var btnSellAll = new Button { Text = "💰 Sell All Stored Crops at Market Value" };
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
        var mach = new VBoxContainer { Name = "🚜 Machinery" };

        var lblInfo = new Label { Text = "Farm Fleet Status & Workshop Maintenance:" };
        mach.AddChild(lblInfo);

        var btnRefuel = new Button { Text = "⛽ Refuel All Machinery (Top Up Tanks)" };
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

        var btnRepair = new Button { Text = "🔧 Service & Repair All Vehicles (Workshop Overhaul)" };
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
        var market = new VBoxContainer { Name = "📈 Market & Silo" };

        var lblPrices = new Label { Text = "Commodity Trading & Prices (per ton):" };
        market.AddChild(lblPrices);

        foreach (CropType crop in Enum.GetValues<CropType>())
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = $"🌾 {crop}: ", CustomMinimumSize = new Vector2(120, 0) });

            var btnSell = new Button { Text = $"Sell {crop} Stock" };
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
        var auto = new VBoxContainer { Name = "⚙️ Automation AI" };

        _chkAutoPlow = new CheckBox { Text = "Auto-Plow when field is cleared", ButtonPressed = true };
        _chkAutoPlow.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoPlow = v; };
        auto.AddChild(_chkAutoPlow);

        _chkAutoSow = new CheckBox { Text = "Auto-Sow selected seed variety", ButtonPressed = true };
        _chkAutoSow.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoSow = v; };
        auto.AddChild(_chkAutoSow);

        _chkAutoHarvest = new CheckBox { Text = "Auto-Harvest when crops mature", ButtonPressed = true };
        _chkAutoHarvest.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoHarvest = v; };
        auto.AddChild(_chkAutoHarvest);

        _chkAutoRefuel = new CheckBox { Text = "Auto-Refuel at low fuel warning", ButtonPressed = true };
        _chkAutoRefuel.Toggled += v => { if (_simulation != null) _simulation.AutomationSystem.AutoRefuel = v; };
        auto.AddChild(_chkAutoRefuel);

        _tabs?.AddChild(auto);
    }

    private void BuildDevTab()
    {
        var dev = new VBoxContainer { Name = "🛠️ Dev Cheats" };

        var btnAddCoins = new Button { Text = "+5,000 Coins" };
        btnAddCoins.Pressed += () =>
        {
            _simulation?.EconomySystem.AddIncome(5000f, "Dev grant");
            RefreshUI();
        };
        dev.AddChild(btnAddCoins);

        var btnInstaGrow = new Button { Text = "Instant Crop Maturation (100% Growth)" };
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

        var btnRain = new Button { Text = "Trigger Rain Storm" };
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
            _lblBalance.Text = $"💰 {_simulation.EconomySystem.Balance:F0} Coins";

        if (_lblStorage != null)
            _lblStorage.Text = $"🏚️ Silo: {_simulation.StorageSystem.GetTotalStoredTons():F1} / {_simulation.StorageSystem.MaxCapacityTons:F0} t";

        if (_lblCropStatus != null)
        {
            float avgGrowth = _simulation.FieldSystem.GetAverageFieldGrowth(1);
            _lblCropStatus.Text = $"🌾 Field Growth: {avgGrowth * 100:F0}%";
        }
    }
}
