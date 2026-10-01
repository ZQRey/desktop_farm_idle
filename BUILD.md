# Desktop Farm Idle — Build & Development Guide

## Prerequisites

- **Windows 10 / 11 x64**
- **.NET SDK 8.0+** (`dotnet --version`)
- **Godot Engine 4.x Mono (C#)** (`4.7.2.stable.mono` or compatible)

---

## 1. Project Build & Compilation

To build the C# solution using .NET CLI:

```powershell
dotnet build DesktopFarmIdle.sln -c Debug
```

To run unit tests:

```powershell
dotnet test tests/DesktopFarmIdle.Tests/DesktopFarmIdle.Tests.csproj
```

---

## 2. Launching from CLI

To launch the project in Godot engine:

```powershell
& "C:\Users\guita\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path .
```

---

## 3. Configuration & Flags

- `--interactive`: Starts the desktop overlay in interactive mode directly for testing.
- `--benchmark`: Runs headless simulation ticks at 100x speed for stress testing.
- `--dev`: Enables Developer panel with instant cheat buttons (fast-forward, grant coins, change weather).
