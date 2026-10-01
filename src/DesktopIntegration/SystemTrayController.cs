using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DesktopFarmIdle.Core.Logging;

namespace DesktopFarmIdle.DesktopIntegration;

public class SystemTrayController : IDisposable
{
    private Thread? _trayThread;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private readonly object _lock = new();

    public Action? OnToggleInteractive { get; set; }
    public Action? OnOpenManagement { get; set; }
    public Action? OnTogglePause { get; set; }
    public Action? OnExit { get; set; }

    public Func<string>? GetFarmStatusText { get; set; }
    public Func<string>? GetFieldCropText { get; set; }
    public Func<string>? GetGrowthText { get; set; }
    public Func<string>? GetTimeToHarvestText { get; set; }
    public Func<bool>? IsInteractiveActive { get; set; }
    public Func<bool>? IsPausedActive { get; set; }

    public SystemTrayController(IntPtr hwnd)
    {
        // hwnd kept for compatibility
    }

    public void Initialize()
    {
        _trayThread = new Thread(RunTrayMessageLoop);
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.IsBackground = true;
        _trayThread.Start();
    }

    private void RunTrayMessageLoop()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Desktop Farm Idle",
                Visible = true
            };

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Opening += (s, e) => RebuildContextMenu();

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => OnOpenManagement?.Invoke();

            GameLogger.Instance.Info("System Tray icon successfully initialized and visible.", "SystemTray");
            Application.Run();
        }
        catch (Exception ex)
        {
            GameLogger.Instance.Error($"Error in System Tray message loop: {ex.Message}", ex, "SystemTray");
        }
    }

    private void RebuildContextMenu()
    {
        if (_contextMenu == null) return;
        _contextMenu.Items.Clear();

        // 1. Title / Header
        var titleItem = new ToolStripMenuItem("🌾 Desktop Farm") { Enabled = false };
        titleItem.Font = new Font(titleItem.Font, FontStyle.Bold);
        _contextMenu.Items.Add(titleItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 2. Dynamic Status Items
        string statusText = GetFarmStatusText?.Invoke() ?? "Ферма: работает";
        string cropText = GetFieldCropText?.Invoke() ?? "Поле: Пшеница";
        string growthText = GetGrowthText?.Invoke() ?? "Рост: 65%";
        string remainingText = GetTimeToHarvestText?.Invoke() ?? "До уборки: 05:42";

        _contextMenu.Items.Add(new ToolStripMenuItem(statusText) { Enabled = false });
        _contextMenu.Items.Add(new ToolStripMenuItem(cropText) { Enabled = false });
        _contextMenu.Items.Add(new ToolStripMenuItem(growthText) { Enabled = false });
        _contextMenu.Items.Add(new ToolStripMenuItem(remainingText) { Enabled = false });
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 3. Open Management Window
        var openItem = new ToolStripMenuItem("📋 Открыть управление", null, (s, e) => OnOpenManagement?.Invoke());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        _contextMenu.Items.Add(openItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 4. Subsections
        _contextMenu.Items.Add(new ToolStripMenuItem("🌱 Поля", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("🚜 Техника", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("👨‍🌾 Рабочие", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("🏚️ Склад", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("💰 Рынок", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("⛅ Погода", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 5. Controls: Interactive mode & Pause
        bool isInteractive = IsInteractiveActive?.Invoke() ?? false;
        var interactiveItem = new ToolStripMenuItem("🖱️ Интерактивный режим", null, (s, e) => OnToggleInteractive?.Invoke())
        {
            Checked = isInteractive
        };
        _contextMenu.Items.Add(interactiveItem);

        bool isPaused = IsPausedActive?.Invoke() ?? false;
        var pauseItem = new ToolStripMenuItem("⏸️ Пауза", null, (s, e) => OnTogglePause?.Invoke())
        {
            Checked = isPaused
        };
        _contextMenu.Items.Add(pauseItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 6. Settings and Exit
        _contextMenu.Items.Add(new ToolStripMenuItem("⚙️ Настройки", null, (s, e) => OnOpenManagement?.Invoke()));
        _contextMenu.Items.Add(new ToolStripMenuItem("❌ Выход", null, (s, e) => OnExit?.Invoke()));
    }

    public void UpdateTooltip(string tooltip)
    {
        if (_notifyIcon != null && _notifyIcon.Visible)
        {
            try
            {
                _notifyIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
            }
            catch { }
        }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        try
        {
            Application.ExitThread();
        }
        catch { }
    }
}
