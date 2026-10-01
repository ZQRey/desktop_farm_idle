# Desktop Farm Idle — Windows Integration & WinAPI Architecture

## 1. Overview
The desktop integration layer bridges the cross-platform Godot engine with Windows 10/11 desktop APIs to achieve:
1. **Seamless Desktop Placement**: Sits directly atop the taskbar across arbitrary screen resolutions, aspect ratios, and multi-monitor setups.
2. **Transparent Borderless Window**: Eliminates titlebars, borders, and window frames while preserving alpha transparency for rendered sprites.
3. **Click-Through Operation**: Lets user mouse clicks pass completely through to underlying desktop icons, folders, and applications.
4. **Taskbar & Alt+Tab Exclusion**: Doesn't crowd the user's taskbar with an application button or clutter the Alt+Tab switching carousel.
5. **Native System Tray Integration**: Provides quick-glance status, quick toggles (Interactive Mode, Pause), and launches the full Management Window.

---

## 2. WinAPI Architecture & Interop

All Windows native calls are encapsulated in `DesktopFarm.DesktopIntegration.Win32.NativeMethods`:

```csharp
[DllImport("user32.dll", SetLastError = true)]
public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

[DllImport("user32.dll", SetLastError = true)]
public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

[DllImport("user32.dll", SetLastError = true)]
public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);
```

### 2.1 Window Extended Styles (`GWL_EXSTYLE = -20`)
- `WS_EX_LAYERED` (`0x00080000`): Enables per-pixel alpha transparency and layered window composition.
- `WS_EX_TRANSPARENT` (`0x00000020`): Permits mouse input to pass directly through the window to the window beneath it (Desktop / Explorer / Browser).
- `WS_EX_TOOLWINDOW` (`0x00000080`): Marks window as a utility floating tool window, hiding it from the Windows Taskbar and Alt+Tab list.
- `WS_EX_NOACTIVATE` (`0x08000000`): Prevents the window from stealing keyboard focus when clicked or updated.

---

## 3. Click-Through vs. Interactive Mode

Toggling modes dynamically updates the window style without reconstructing the window:

```csharp
public void SetInteractiveMode(bool interactive)
{
    IntPtr exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
    long styleValue = exStyle.ToInt64();

    if (interactive)
    {
        // Remove WS_EX_TRANSPARENT -> Window receives mouse clicks
        styleValue &= ~NativeMethods.WS_EX_TRANSPARENT;
    }
    else
    {
        // Add WS_EX_TRANSPARENT -> Mouse clicks pass through
        styleValue |= NativeMethods.WS_EX_TRANSPARENT;
    }

    NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(styleValue));
    NativeMethods.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
}
```

---

## 4. Desktop Positioning Directly Above Taskbar

To ensure pixel-perfect positioning directly above the taskbar on any monitor and DPI scale:
1. `SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0)` is invoked to query the usable desktop bounds (which excludes the taskbar regardless of whether it is positioned at the bottom, top, left, or right).
2. Given a desired overlay height (e.g., `120px` to `180px`), the window rectangle is computed:
   - `X = workArea.Left`
   - `Y = workArea.Bottom - overlayHeight`
   - `Width = workArea.Right - workArea.Left`
   - `Height = overlayHeight`
3. `SetWindowPos` places the window with `HWND_TOPMOST` (`-1`) or `HWND_NOTOPMOST` based on user preferences.

---

## 5. System Tray & Context Menu

The system tray icon provides a rich ambient interface:
- **Hover Tooltip**: Displays current farm activity (e.g. *"Desktop Farm — Wheat: 72% | 1 Tractor Working"*).
- **Context Menu Hierarchy**:
  - `Header`: Farm Status (Running / Paused)
  - `Crop Status`: Current field stage & time remaining
  - `Separator`
  - `Interactive Mode` (Checkbox toggle)
  - `Open Management Window` (Opens full dashboard)
  - `Separator`
  - `Pause / Resume`
  - `Settings`
  - `Exit Desktop Farm`
