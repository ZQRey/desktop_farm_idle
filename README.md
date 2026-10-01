# Desktop Farm Idle 🌾🚜

**Desktop Farm Idle** is an ambient agricultural idle management game for Windows 10/11. It lives gracefully on your desktop directly above the taskbar as a transparent, click-through overlay with native Windows System Tray integration.

---

## Key Features

- **Ambient Desktop Overlay**: Sits directly above your taskbar without stealing clicks or cluttering your taskbar.
- **True Click-Through by Default**: Seamlessly interact with desktop shortcuts, files, and background windows.
- **Interactive Mode on Demand**: One-click toggle from System Tray to inspect machinery, plow fields, and direct workers.
- **Rich Physical Field System**: Discrete soil grid (`Stubble` ➔ `Plowed` ➔ `Cultivated` ➔ `Seeded` ➔ `Growing` ➔ `Ready to Harvest` ➔ `Harvested`). Machinery actually transforms the ground beneath its wheels and implements in real time.
- **Layered Vehicles & Implement Mechanics**:
  - Independent rotating tires, bouncy terrain suspension, exhaust smoke puffs, and earthen furrow particle sprays.
  - Classic Utility Tractor, Universal Medium Tractor, Heavy Field Tractor, Combine Harvesters, Grain Trucks, and Service Vans.
  - Modular implements: Plows, Cultivators, Seeders, Fertilizer Spreaders, Sprayers, and Trailers.
- **Complete Realistic Agricultural Cycle**:
  - Plowing ➔ Cultivation ➔ Seeding ➔ Vegetative Growth ➔ Harvesting ➔ Grain Auger Transfer to Truck ➔ Silo Storage & Market Sale.
- **Dynamic Weather & Day/Night**: Rain, storms, sunshine, heatwaves, and local Windows clock-synced diurnal cycle.
- **Decoupled Architecture & Fixed Timestep**:
  - Simulation logic runs at a fixed 10 Hz rate completely isolated from rendering.
  - Offline progression accurately simulates up to 8 hours of idle farming.
- **Low Power & Minimal Resource Footprint**:
  - Dynamically throttles rendering (10–20 FPS in idle ambient mode) keeping CPU usage under 1.5%.

---

## Quick Start

### Build & Run
```powershell
# Build C# solution
dotnet build

# Launch Desktop Farm Idle
& "C:\Users\guita\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path .
```

---

## Documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) — Subsystem breakdown, WinAPI architecture, and data contracts.
- [GAME_DESIGN.md](GAME_DESIGN.md) — Crop balancing, vehicle statistics, worker progression, and mechanics.
- [ASSET_GUIDE.md](ASSET_GUIDE.md) — Visual design rules, layered sprites, and AI generation pipelines.
- [WINDOWS_INTEGRATION.md](WINDOWS_INTEGRATION.md) — Win32 interop, window extended styles, and tray menus.
- [BUILD.md](BUILD.md) — Detailed build instructions and test commands.
