using System;
using System.Runtime.InteropServices;
using DesktopFarmIdle.Core.Logging;
using DesktopFarmIdle.DesktopIntegration.Win32;

namespace DesktopFarmIdle.DesktopIntegration;

public class WindowController
{
    private IntPtr _hwnd = IntPtr.Zero;
    public bool IsInteractive { get; private set; } = false;
    public int DesiredHeight { get; set; } = 180;

    public bool Initialize(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            _hwnd = NativeMethods.FindWindow(null, "Desktop Farm Idle");
            if (_hwnd == IntPtr.Zero)
            {
                GameLogger.Instance.Warning("Could not find Desktop Farm Idle window handle.", "WindowController");
                return false;
            }
        }
        else
        {
            _hwnd = hwnd;
        }

        GameLogger.Instance.Info($"WindowController attached to HWND 0x{_hwnd.ToInt64():X}", "WindowController");
        ApplyDesktopOverlayStyles();
        PositionDirectlyAboveTaskbar();
        return true;
    }

    public void ApplyDesktopOverlayStyles()
    {
        if (_hwnd == IntPtr.Zero) return;

        try
        {
            IntPtr currentExStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE);
            long exStyle = currentExStyle.ToInt64();

            // Add Layered, ToolWindow (no taskbar / no Alt-Tab), NoActivate
            exStyle |= NativeMethods.WS_EX_LAYERED;
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            exStyle |= NativeMethods.WS_EX_NOACTIVATE;
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;

            // Click-through by default
            if (!IsInteractive)
            {
                exStyle |= NativeMethods.WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            }

            NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

            // Force frame change
            NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);

            GameLogger.Instance.Info($"Applied overlay styles. Interactive: {IsInteractive}", "WindowController");
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error setting overlay styles: {ex.Message}", ex, "WindowController");
        }
    }

    public void PositionDirectlyAboveTaskbar()
    {
        if (_hwnd == IntPtr.Zero) return;

        try
        {
            NativeMethods.RECT workArea = new();
            if (NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref workArea, 0))
            {
                int width = workArea.Width;
                int height = DesiredHeight;
                int x = workArea.Left;
                int y = workArea.Bottom - height; // Sits directly atop the taskbar

                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_TOPMOST,
                    x, y, width, height,
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
                );

                GameLogger.Instance.Info($"Positioned overlay: X={x}, Y={y}, W={width}, H={height}", "WindowController");
            }
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error positioning above taskbar: {ex.Message}", ex, "WindowController");
        }
    }

    public void SetInteractive(bool interactive)
    {
        IsInteractive = interactive;
        ApplyDesktopOverlayStyles();
    }

    public void ToggleInteractive()
    {
        SetInteractive(!IsInteractive);
    }
}
