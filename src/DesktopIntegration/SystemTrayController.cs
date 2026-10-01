using System;
using System.Runtime.InteropServices;
using System.Threading;
using DesktopFarmIdle.Core.Logging;

namespace DesktopFarmIdle.DesktopIntegration;

public class SystemTrayController : IDisposable
{
    private const int WM_USER = 0x0400;
    public const int WM_TRAYICON = WM_USER + 1;

    public const int NIM_ADD = 0x00000000;
    public const int NIM_MODIFY = 0x00000001;
    public const int NIM_DELETE = 0x00000002;

    public const int NIF_MESSAGE = 0x00000001;
    public const int NIF_ICON = 0x00000002;
    public const int NIF_TIP = 0x00000004;

    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_RBUTTONUP = 0x0205;

    public const uint TPM_BOTTOMALIGN = 0x0020;
    public const uint TPM_RETURNCMD = 0x0100;

    public const uint MF_STRING = 0x00000000;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint MF_DISABLED = 0x00000002;
    public const uint MF_CHECKED = 0x00000008;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lpTPMParams);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    private readonly IntPtr _hwnd;
    private NOTIFYICONDATA _nid;
    private bool _isAdded = false;

    // Callbacks for tray menu actions
    public Action? OnToggleInteractive { get; set; }
    public Action? OnOpenManagement { get; set; }
    public Action? OnTogglePause { get; set; }
    public Action? OnExit { get; set; }

    // Dynamic status getters
    public Func<string>? GetFarmStatusText { get; set; }
    public Func<string>? GetFieldCropText { get; set; }
    public Func<string>? GetGrowthText { get; set; }
    public Func<string>? GetTimeToHarvestText { get; set; }
    public Func<bool>? IsInteractiveActive { get; set; }
    public Func<bool>? IsPausedActive { get; set; }

    public SystemTrayController(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void Initialize()
    {
        if (_hwnd == IntPtr.Zero) return;

        IntPtr defaultIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1001,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = defaultIcon,
            szTip = "Desktop Farm Idle"
        };

        _isAdded = Shell_NotifyIcon(NIM_ADD, ref _nid);
        GameLogger.Instance.Info($"System Tray icon initialized: {_isAdded}", "SystemTray");
    }

    public void UpdateTooltip(string tooltip)
    {
        if (!_isAdded) return;
        _nid.szTip = tooltip.Length > 127 ? tooltip[..127] : tooltip;
        _nid.uFlags = NIF_TIP;
        Shell_NotifyIcon(NIM_MODIFY, ref _nid);
    }

    public void ShowContextMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            // Title
            AppendMenu(hMenu, MF_DISABLED | MF_STRING, new IntPtr(1), "🌾 Desktop Farm Idle");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);

            // Dynamic Farm Status
            string status = GetFarmStatusText?.Invoke() ?? "Ферма: работает";
            string crop = GetFieldCropText?.Invoke() ?? "Поле: Пшеница";
            string growth = GetGrowthText?.Invoke() ?? "Рост: 65%";
            string remaining = GetTimeToHarvestText?.Invoke() ?? "До урожая: 05:42";

            AppendMenu(hMenu, MF_DISABLED | MF_STRING, new IntPtr(2), status);
            AppendMenu(hMenu, MF_DISABLED | MF_STRING, new IntPtr(3), crop);
            AppendMenu(hMenu, MF_DISABLED | MF_STRING, new IntPtr(4), growth);
            AppendMenu(hMenu, MF_DISABLED | MF_STRING, new IntPtr(5), remaining);
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);

            // Interactive Controls
            AppendMenu(hMenu, MF_STRING, new IntPtr(100), "📋 Открыть управление");

            bool isInteractive = IsInteractiveActive?.Invoke() ?? false;
            uint checkFlag = isInteractive ? MF_CHECKED : 0;
            AppendMenu(hMenu, MF_STRING | checkFlag, new IntPtr(101), "🖱️ Интерактивный режим");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);

            // Subsections
            AppendMenu(hMenu, MF_STRING, new IntPtr(201), "🌱 Поля");
            AppendMenu(hMenu, MF_STRING, new IntPtr(202), "🚜 Техника");
            AppendMenu(hMenu, MF_STRING, new IntPtr(203), "👨‍🌾 Рабочие");
            AppendMenu(hMenu, MF_STRING, new IntPtr(204), "🏚️ Склад");
            AppendMenu(hMenu, MF_STRING, new IntPtr(205), "💰 Рынок");
            AppendMenu(hMenu, MF_STRING, new IntPtr(206), "⛅ Погода");
            AppendMenu(hMenu, MF_STRING, new IntPtr(207), "📊 Статистика");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);

            bool isPaused = IsPausedActive?.Invoke() ?? false;
            AppendMenu(hMenu, MF_STRING, new IntPtr(301), isPaused ? "▶️ Возобновить" : "⏸️ Пауза");
            AppendMenu(hMenu, MF_STRING, new IntPtr(302), "⚙️ Настройки");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, string.Empty);

            AppendMenu(hMenu, MF_STRING, new IntPtr(999), "❌ Выход");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hwnd);

            int cmd = TrackPopupMenuEx(hMenu, TPM_BOTTOMALIGN | TPM_RETURNCMD, pt.X, pt.Y, _hwnd, IntPtr.Zero);
            HandleMenuCommand(cmd);
        }
        finally
        {
            DestroyMenu(hMenu);
        }
    }

    private void HandleMenuCommand(int cmd)
    {
        switch (cmd)
        {
            case 100: // Open Management
            case 201: // Fields
            case 202: // Machinery
            case 203: // Workers
            case 204: // Silo
            case 205: // Market
            case 206: // Weather
            case 207: // Stats
            case 302: // Settings
                OnOpenManagement?.Invoke();
                break;

            case 101: // Toggle interactive
                OnToggleInteractive?.Invoke();
                break;

            case 301: // Pause
                OnTogglePause?.Invoke();
                break;

            case 999: // Exit
                OnExit?.Invoke();
                break;
        }
    }

    public void Dispose()
    {
        if (_isAdded)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            _isAdded = false;
        }
    }
}
