# Desktop Farm Idle — Architecture Specification

## 1. Overview & Architectural Principles

**Desktop Farm Idle** is an ambient desktop idle farming game designed specifically for Windows 10/11. The application lives directly above the Windows taskbar as a borderless, transparent, click-through overlay, accompanied by a native Windows System Tray presence and an on-demand Management Window.

### Core Architectural Principles:
1. **Decoupled Simulation & Rendering**: The gameplay/simulation domain logic has zero dependency on Godot rendering or Windows API. The simulation can execute in headless mode, run in unit tests, or fast-forward for deterministic offline calculation.
2. **Fixed Timestep Simulation**: Simulation ticks run at a deterministic rate (5 to 10 ticks per second), decoupled from the rendering frame rate (which adjusts dynamically between 10 FPS in low-power idle mode and 60 FPS in interactive mode).
3. **Event-Driven Communication**: Subsystems communicate via a centralized `EventBus` (`IGameEventBus`) using strongly-typed event records (`CropStageChangedEvent`, `VehicleBrokenEvent`, `WeatherChangedEvent`, etc.).
4. **Native Windows Integration**: Windows-specific functionalities (window styles, click-through toggle, taskbar exclusion, tray icon, work area calculation) are encapsulated in the `DesktopIntegration` layer using WinAPI (`user32.dll`, `shell32.dll`).
5. **Robust Data & Persistence**: Atomic versioned JSON save system with primary and backup files, schema validation, and deterministic catch-up calculation for offline progression.

---

## 2. Solution Structure

```
d:/Project/idle_game/
├── .gitignore
├── README.md
├── ARCHITECTURE.md
├── GAME_DESIGN.md
├── ASSET_GUIDE.md
├── WINDOWS_INTEGRATION.md
├── BUILD.md
├── project.godot
├── DesktopFarmIdle.sln
├── src/
│   ├── Core/
│   │   ├── Logging/              # ILogger, LogLevel, StructuredLogger
│   │   ├── Events/               # EventBus, IEvent, GameEvents
│   │   ├── Time/                 # SimulationClock, TimeSpan extensions
│   │   └── Config/               # GameBalanceConfig, SettingsConfig
│   ├── Simulation/
│   │   ├── GameSimulation.cs     # Simulation container & coordinator
│   │   ├── Field/                # FieldModel, FieldCell, SoilState, FieldSystem
│   │   ├── Crops/                # CropDefinition, CropInstance, CropSystem
│   │   ├── Vehicles/             # VehicleModel, VehicleType, VehicleState, VehicleSystem
│   │   ├── Equipment/            # EquipmentModel, EquipmentType, ToolSystem
│   │   ├── Workers/              # WorkerModel, WorkerRole, FatigueSystem, WorkerSystem
│   │   ├── Weather/              # WeatherType, WeatherForecast, WeatherSystem
│   │   ├── Economy/              # Wallet, Transactions, EconomySystem
│   │   ├── Market/               # MarketItem, PriceDynamics, MarketSystem
│   │   ├── Storage/              # Silo, Warehouse, StorageSystem
│   │   ├── Contracts/            # ContractModel, ContractSystem
│   │   ├── Automation/           # FarmManagerAI, AutomationSystem
│   │   └── Save/                 # SaveDataModel, SaveMigrator, SaveManager, OfflineProgressCalculator
│   ├── DesktopIntegration/
│   │   ├── Win32/                # WinApi NativeMethods, Constants (WS_EX, SWP, etc.)
│   │   ├── WindowController.cs   # Transparent borderless window, click-through, positioning above taskbar
│   │   ├── SystemTrayController.cs # Windows system tray icon, dynamic menu, tooltips
│   │   └── PowerManager.cs       # Active / Idle / Low-Power FPS throttling & fullscreen detection
│   ├── Rendering/
│   │   ├── FieldRenderer.cs      # Progressive field cell visual updates
│   │   ├── VehicleView.cs        # Layered vehicle rendering (body, rotating wheels, exhaust, tool)
│   │   ├── WorkerView.cs         # 2D worker animation controller
│   │   ├── WeatherView.cs        # Rain/snow particles, day/night lighting modulate
│   │   └── WorldScene.cs         # Main viewport and panorama horizon above taskbar
│   └── UI/
│       ├── OverlayHud.cs         # Subtle minimal status pills (temporary notifications)
│       └── Management/           # Tabbed management window (Dashboard, Farm, Market, etc.)
├── tests/
│   └── DesktopFarmIdle.Tests/    # xUnit / NUnit simulation tests
└── assets/
    ├── vehicles/
    ├── equipment/
    ├── crops/
    ├── environment/
    ├── ui/
    └── sfx/
```

---

## 3. Subsystem Breakdown

### 3.1 TimeSystem & Fixed Timestep
- `SimulationClock`: Tracks total game elapsed time, day/night cycle (optionally synced with Windows local time), and discrete simulation ticks.
- Standard tick frequency: **10 Hz** (`SimulationDeltaTime = 0.1s`).
- All calculations (fuel burn, crop growth, worker stamina depletion, vehicle velocity) are measured in physical game units per second multiplied by `SimulationDeltaTime`.

### 3.2 Field & Crop Systems
- The farm is represented as horizontal contiguous fields composed of linear discrete grid cells (`FieldCell[X]`).
- Each cell maintains:
  - `SoilState`: `Stubble`, `Plowed`, `Cultivated`, `Seeded`, `Growing`, `ReadyToHarvest`, `Harvested`.
  - `Moisture`: 0.0 - 1.0.
  - `NutrientLevel`: 0.0 - 1.0.
  - `WeedInfestation`: 0.0 - 1.0.
  - `CurrentCrop`: Crop ID, growth stage (0 to 100%), health, yield estimate.
- When an active vehicle passes over a cell with attached equipment down:
  - Plow: modifies cell from `Stubble` -> `Plowed`.
  - Cultivator: modifies cell from `Plowed` -> `Cultivated`.
  - Seeder: modifies cell from `Cultivated` -> `Seeded`.
  - Harvester: modifies cell from `ReadyToHarvest` -> `Harvested` / `Stubble`.
- Visual state updates are emitted per modified cell to allow the renderer to update only affected tiles without repainting the entire grid.

### 3.3 Vehicle & Equipment Systems
- Vehicles are modeled with real operational attributes:
  - Engine state (Off, Starting, Idling, Working).
  - Velocity, fuel level, fuel burn rate, condition (0-100%), maintenance state.
  - Connected equipment attachment slot (front/rear hitch).
  - Waypoints along the field: Entry -> Traverse Field Row -> Turn Sequence -> Next Row -> Exit.
- Layered visual representation:
  - Chassis/Body sprite.
  - Wheels (rotated proportionally to distance traversed).
  - Equipment attachment (pivot-linked with lift/lower animation).
  - VFX anchors: Exhaust pipe, wheel dust, plow soil furrow particles.

### 3.4 Worker System
- Workers hold roles: `TractorDriver`, `HarvesterOperator`, `Mechanic`, `Agronomist`, `GeneralWorker`, `FarmManager`.
- Stats: `Fatigue` (0-100), `Morale` (0-100), `XP`, `Level`.
- Automatic resting cycle: Workers walk to the staff quarters/break area when fatigue exceeds threshold, and return to work once rested.

### 3.5 Weather System
- Continuous Markov-chain weather state machine:
  - `Clear` <-> `Cloudy` <-> `Rain` <-> `Storm` <-> `Fog` / `Snow` / `HeatWave`.
- Weather affects field moisture, vehicle traction/speed, and crop quality.

### 3.6 Desktop & Windows API Integration
- `WindowController`:
  - `GetWindowLongPtr` / `SetWindowLongPtr` with `GWL_EXSTYLE`:
    - `WS_EX_LAYERED | WS_EX_TRANSPARENT`: mouse clicks pass through completely to underlying desktop icons and windows.
    - `WS_EX_TOOLWINDOW`: suppresses taskbar entry and Alt+Tab thumbnail.
    - `WS_EX_NOACTIVATE`: ensures clicks don't steal keyboard focus.
  - `SetWindowPos` places the window at `HWND_TOPMOST` or `HWND_BOTTOM` according to user settings, positioned precisely above the Windows taskbar work area (`SystemParametersInfo(SPI_GETWORKAREA)`).
- `SystemTrayController`:
  - Utilizes `Shell_NotifyIcon` with custom icon and context menu.
  - Displays real-time farm status strings directly in tray menu items.

---

## 4. Performance & Resource Profiles

| Profile | Target Render FPS | Simulation Ticks | Use Case |
|---|---|---|---|
| **Active** | 30 - 60 FPS | 10 Hz | User opened Management Window or enabled Interactive Mode |
| **Idle** | 15 - 20 FPS | 10 Hz | Normal ambient desktop background operation |
| **Low-Power** | 5 - 10 FPS | 5 Hz | User is idle or running fullscreen application |
| **Minimized/Hidden** | 0 FPS (Render pause) | 5 Hz | Overlay explicitly hidden, runs background simulation only |

CPU target in Idle mode: **< 1.5%** on modern multicore CPUs.
Memory footprint: **< 120 MB**.
