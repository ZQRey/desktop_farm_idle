using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DesktopFarmIdle.Core.Logging;

namespace DesktopFarmIdle.DesktopIntegration;

public class WindowsOverlayService
{
    private static WindowsOverlayService? _instance;
    public static WindowsOverlayService Instance => _instance ??= new WindowsOverlayService();

    #region Win32 Constants

    public const int GWL_EXSTYLE = -20;
    public const int GWL_STYLE = -16;

    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_APPWINDOW = 0x00040000;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOREDRAW = 0x0008;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public static readonly IntPtr HWND_BOTTOM = new(1);

    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int SW_SHOWNA = 8;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public override string ToString() => $"[{Left},{Top} -> {Right},{Bottom} ({Width}x{Height})]";
    }

    #endregion

    #region Win32 API Imports

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetDpiForSystem();

    #endregion

    private IntPtr _hwnd = IntPtr.Zero;
    public bool IsInteractive { get; private set; } = false;
    public int ConfiguredLogicalHeight { get; set; } = 240;

    public IntPtr HWnd => _hwnd;

    public bool Attach(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            hwnd = Process.GetCurrentProcess().MainWindowHandle;
        }

        if (hwnd == IntPtr.Zero)
        {
            GameLogger.Instance.Warning("Could not determine native window handle for overlay.", "WindowsOverlayService");
            return false;
        }

        _hwnd = hwnd;
        GameLogger.Instance.Info($"WindowsOverlayService attached to HWND 0x{_hwnd.ToInt64():X}", "WindowsOverlayService");

        // Apply extended styles and remove from taskbar
        ApplyWindowStyles();

        // Position overlay directly above taskbar
        PositionOverlayAboveTaskbar();

        return true;
    }

    public void ApplyWindowStyles()
    {
        if (_hwnd == IntPtr.Zero) return;

        try
        {
            IntPtr currentExStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
            long ex = currentExStyle.ToInt64();

            // Hide window momentarily to force Windows Shell to delete taskbar button
            ShowWindow(_hwnd, SW_HIDE);

            // Add WS_EX_TOOLWINDOW (hides from taskbar and Alt+Tab)
            // Add WS_EX_LAYERED, WS_EX_NOACTIVATE
            // Remove WS_EX_APPWINDOW
            ex |= WS_EX_LAYERED;
            ex |= WS_EX_TOOLWINDOW;
            ex |= WS_EX_NOACTIVATE;
            ex &= ~WS_EX_APPWINDOW;

            if (!IsInteractive)
            {
                ex |= WS_EX_TRANSPARENT; // Click-through
            }
            else
            {
                ex &= ~WS_EX_TRANSPARENT; // Interactive
            }

            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(ex));

            // Restore window without activating it
            ShowWindow(_hwnd, SW_SHOWNA);

            // Z-Order: Use HWND_NOTOPMOST or HWND_BOTTOM so regular applications can cover it
            SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            GameLogger.Instance.Info($"Applied Window Styles. ClickThrough: {!IsInteractive}, ToolWindow: True", "WindowsOverlayService");
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error setting window styles: {ex.Message}", ex, "WindowsOverlayService");
        }
    }

    public (int x, int y, int width, int height) CalculateOverlayBounds()
    {
        // Use primary screen for the main overlay
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var monitor = screen.Bounds;
        var work = screen.WorkingArea;

        uint dpi = 96;
        if (_hwnd != IntPtr.Zero)
        {
            try { dpi = GetDpiForWindow(_hwnd); } catch { }
        }
        if (dpi == 0)
        {
            try { dpi = (uint)GetDpiForSystem(); } catch { dpi = 96; }
        }

        float scale = dpi / 96.0f;
        int physicalHeight = (int)(ConfiguredLogicalHeight * scale);

        int x = work.Left;
        int width = work.Width;
        int height = physicalHeight;
        int y;

        // Inferred taskbar position
        if (work.Bottom < monitor.Bottom)
        {
            // Taskbar is at bottom: overlay sits directly above taskbar
            y = work.Bottom - height;
        }
        else if (work.Top > monitor.Top)
        {
            // Taskbar is at top
            y = monitor.Bottom - height;
        }
        else
        {
            // Left or right taskbar
            y = work.Bottom - height;
        }

        string taskbarPos = work.Bottom < monitor.Bottom ? "Bottom" :
                            work.Top > monitor.Top ? "Top" :
                            work.Left > monitor.Left ? "Left" :
                            work.Right < monitor.Right ? "Right" : "Hidden/Autohide";

        GameLogger.Instance.Info(
            $"[WINDOW_DIAGNOSTICS]\n" +
            $"  Monitor Rect: [{monitor.Left},{monitor.Top} -> {monitor.Right},{monitor.Bottom} ({monitor.Width}x{monitor.Height})]\n" +
            $"  Work Area Rect: [{work.Left},{work.Top} -> {work.Right},{work.Bottom} ({work.Width}x{work.Height})]\n" +
            $"  Detected DPI: {dpi} (Scale: {scale:F2})\n" +
            $"  Inferred Taskbar: {taskbarPos}\n" +
            $"  Overlay Logical Height: {ConfiguredLogicalHeight}px\n" +
            $"  Overlay Physical Height: {physicalHeight}px\n" +
            $"  Calculated Overlay Bounds: X={x}, Y={y}, W={width}, H={height}",
            "WindowsOverlayService"
        );

        return (x, y, width, height);
    }

    public void PositionOverlayAboveTaskbar()
    {
        if (_hwnd == IntPtr.Zero) return;

        var (x, y, width, height) = CalculateOverlayBounds();

        try
        {
            SetWindowPos(
                _hwnd,
                HWND_NOTOPMOST,
                x, y, width, height,
                SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED
            );

            GetWindowRect(_hwnd, out RECT actualRect);
            GameLogger.Instance.Info($"Actual HWND Rect after SetWindowPos: {actualRect}", "WindowsOverlayService");
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error in SetWindowPos: {ex.Message}", ex, "WindowsOverlayService");
        }
    }

    public void SetInteractiveMode(bool interactive)
    {
        IsInteractive = interactive;
        ApplyWindowStyles();
    }

    public void ToggleInteractiveMode()
    {
        SetInteractiveMode(!IsInteractive);
    }
}
