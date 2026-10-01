using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
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

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("dwmapi.dll")]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    #endregion

    private IntPtr _hwnd = IntPtr.Zero;
    public bool IsInteractive { get; private set; } = false;
    public int ConfiguredLogicalHeight { get; set; } = 120; // Default diorama height: 120 px
    public int BottomOffset { get; set; } = 0; // Configurable bottom margin (0-100px)

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
            // 1. Extend DWM frame for true hardware-accelerated desktop transparency
            MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            int hr = DwmExtendFrameIntoClientArea(_hwnd, ref margins);
            if (hr != 0)
            {
                GameLogger.Instance.Warning($"DwmExtendFrameIntoClientArea returned HRESULT 0x{hr:X}", "WindowsOverlayService");
            }

            // 2. Configure Win32 Extended Window Styles
            IntPtr currentExStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
            long ex = currentExStyle.ToInt64();

            // Add WS_EX_TOOLWINDOW (hides from taskbar and Alt+Tab)
            // Add WS_EX_LAYERED, WS_EX_NOACTIVATE
            // Remove WS_EX_APPWINDOW
            ex |= WS_EX_LAYERED;
            ex |= WS_EX_TOOLWINDOW;
            ex |= WS_EX_NOACTIVATE;
            ex &= ~WS_EX_APPWINDOW;

            if (!IsInteractive)
            {
                ex |= WS_EX_TRANSPARENT; // Click-through enabled
            }
            else
            {
                ex &= ~WS_EX_TRANSPARENT; // Interactive mode
            }

            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(ex));

            // Z-Order: Use HWND_NOTOPMOST so regular applications can cover it, but desktop shows it
            SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

            ShowWindow(_hwnd, SW_SHOWNA);

            GameLogger.Instance.Info($"Applied Window Styles. ClickThrough: {!IsInteractive}, ToolWindow: True, Height: {ConfiguredLogicalHeight}", "WindowsOverlayService");
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error setting window styles: {ex.Message}", ex, "WindowsOverlayService");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    public int TargetMonitorIndex { get; set; } = 0; // 0 = Primary Monitor

    public (int x, int y, int width, int height) CalculateOverlayBounds()
    {
        // Pure Win32 monitor & work area resolution (zero WinForms dependency)
        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

        // Primary monitor is guaranteed at point (0, 0)
        IntPtr hMonitor = TargetMonitorIndex == 0
            ? MonitorFromPoint(new POINT { x = 0, y = 0 }, MONITOR_DEFAULTTOPRIMARY)
            : MonitorFromWindow(_hwnd != IntPtr.Zero ? _hwnd : IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);

        RECT monitor;
        RECT work;

        if (GetMonitorInfo(hMonitor, ref mi))
        {
            monitor = mi.rcMonitor;
            work = mi.rcWork;
        }
        else
        {
            monitor = new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
            work = new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1040 };
        }

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
            // Taskbar is at bottom: overlay sits directly above taskbar, accounting for BottomOffset
            y = work.Bottom - height - BottomOffset;
        }
        else if (work.Top > monitor.Top)
        {
            // Taskbar is at top
            y = monitor.Bottom - height - BottomOffset;
        }
        else
        {
            // Left or right taskbar
            y = work.Bottom - height - BottomOffset;
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
            $"  Bottom Offset: {BottomOffset}px\n" +
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
