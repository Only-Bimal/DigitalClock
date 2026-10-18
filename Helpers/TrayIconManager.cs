using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DigitalClock
{
    public class TrayIconManager : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly NotifyIcon _notifyIcon;

        private readonly ToolStripMenuItem _headerItem;
        private readonly ToolStripMenuItem _alwaysOnTopItem;
        private readonly ToolStripMenuItem _lockPositionItem;
        private readonly ToolStripMenuItem _clickThroughItem;
        private readonly ToolStripMenuItem _startWithWindowsItem;

        private readonly ToolStripMenuItem _format12HourItem;
        private readonly ToolStripMenuItem _format24HourItem;
        private readonly ToolStripMenuItem _showSecondsItem;

        public TrayIconManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            Icon? appIcon = null;
            try
            {
                var iconUri = new Uri("pack://application:,,,/accessories-clock.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using var stream = streamInfo.Stream;
                    appIcon = new Icon(stream);
                }
            }
            catch { }

            if (appIcon == null)
            {
                try
                {
                    string directIco = Path.Combine(AppContext.BaseDirectory, "accessories-clock.ico");
                    if (File.Exists(directIco))
                    {
                        appIcon = new Icon(directIco);
                    }
                }
                catch { }
            }

            if (appIcon == null)
            {
                try
                {
                    string? exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DigitalClock.exe");
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        appIcon = Icon.ExtractAssociatedIcon(exePath);
                    }
                }
                catch { }
            }

            appIcon ??= SystemIcons.Application;

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "Digital Clock",
                Visible = SettingsManager.Current.Settings.Behavior.ShowTrayIcon
            };

            // Build Context Menu
            var menu = new ContextMenuStrip();

            _headerItem = new ToolStripMenuItem("Digital Clock")
            {
                Font = new Font(System.Drawing.SystemFonts.MenuFont ?? new Font("Segoe UI", 9f), System.Drawing.FontStyle.Bold)
            };
            _headerItem.Click += (sender, e) => _mainWindow.ToggleVisibility();
            menu.Items.Add(_headerItem);

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("⚙ Settings...", null, (sender, e) => _mainWindow.OpenSettings());
            menu.Items.Add(settingsItem);

            var toggleVisibilityItem = new ToolStripMenuItem("👁 Show / Hide Clock", null, (sender, e) => _mainWindow.ToggleVisibility());
            menu.Items.Add(toggleVisibilityItem);

            menu.Items.Add(new ToolStripSeparator());

            // Quick Toggles
            _alwaysOnTopItem = new ToolStripMenuItem("📌 Always on Top", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.Behavior.AlwaysOnTop = !settings.Behavior.AlwaysOnTop;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            menu.Items.Add(_alwaysOnTopItem);

            _lockPositionItem = new ToolStripMenuItem("🔒 Lock Position", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.Behavior.LockPosition = !settings.Behavior.LockPosition;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            menu.Items.Add(_lockPositionItem);

            _clickThroughItem = new ToolStripMenuItem("👻 Click-Through Mode", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.Behavior.ClickThrough = !settings.Behavior.ClickThrough;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            menu.Items.Add(_clickThroughItem);

            // Time Format Submenu
            var formatMenu = new ToolStripMenuItem("⏱ Time Format");
            _format12HourItem = new ToolStripMenuItem("12-Hour Format (AM/PM)", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.General.Is24HourFormat = false;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            _format24HourItem = new ToolStripMenuItem("24-Hour Format", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.General.Is24HourFormat = true;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            _showSecondsItem = new ToolStripMenuItem("Show Seconds", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.General.ShowSeconds = !settings.General.ShowSeconds;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });

            formatMenu.DropDownItems.Add(_format12HourItem);
            formatMenu.DropDownItems.Add(_format24HourItem);
            formatMenu.DropDownItems.Add(new ToolStripSeparator());
            formatMenu.DropDownItems.Add(_showSecondsItem);
            menu.Items.Add(formatMenu);

            // Position & Monitor Submenu
            var posMenu = new ToolStripMenuItem("🖥 Position & Snap");
            posMenu.DropDownItems.Add(new ToolStripMenuItem("🎯 Snap to Primary Screen", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.Center, System.Windows.Forms.Screen.PrimaryScreen)));
            posMenu.DropDownItems.Add(new ToolStripSeparator());
            posMenu.DropDownItems.Add(new ToolStripMenuItem("↺ Reset Size to Screen Border", null, (sender, e) => _mainWindow.ResetSizeToScreenDefault()));
            posMenu.DropDownItems.Add(new ToolStripSeparator());
            posMenu.DropDownItems.Add(new ToolStripMenuItem("✛ Center Screen", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.Center)));
            posMenu.DropDownItems.Add(new ToolStripMenuItem("↗ Snap Top-Right", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.TopRight)));
            posMenu.DropDownItems.Add(new ToolStripMenuItem("↘ Snap Bottom-Right", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.BottomRight)));
            posMenu.DropDownItems.Add(new ToolStripMenuItem("↖ Snap Top-Left", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.TopLeft)));
            posMenu.DropDownItems.Add(new ToolStripMenuItem("↙ Snap Bottom-Left", null, (sender, e) => _mainWindow.SnapTo(SnapPosition.BottomLeft)));
            menu.Items.Add(posMenu);

            _startWithWindowsItem = new ToolStripMenuItem("🚀 Start with Windows", null, (sender, e) =>
            {
                var settings = SettingsManager.Current.Settings;
                settings.Behavior.StartWithWindows = !settings.Behavior.StartWithWindows;
                SettingsManager.Current.Save();
                _mainWindow.ApplySettings(settings);
            });
            menu.Items.Add(_startWithWindowsItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("❌ Exit DigitalClock", null, (sender, e) => _mainWindow.ExitApp());
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;

            // Single / Double click
            _notifyIcon.DoubleClick += (sender, e) => _mainWindow.ToggleVisibility();
            _notifyIcon.Click += (sender, e) =>
            {
                if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
                {
                    _mainWindow.ToggleVisibility();
                }
            };

            UpdateMenuItems(SettingsManager.Current.Settings);
        }

        public void UpdateMenuItems(AppSettings settings)
        {
            _notifyIcon.Visible = settings.Behavior.ShowTrayIcon;
            _alwaysOnTopItem.Checked = settings.Behavior.AlwaysOnTop;
            _lockPositionItem.Checked = settings.Behavior.LockPosition;
            _clickThroughItem.Checked = settings.Behavior.ClickThrough;
            _startWithWindowsItem.Checked = settings.Behavior.StartWithWindows;

            _format12HourItem.Checked = !settings.General.Is24HourFormat;
            _format24HourItem.Checked = settings.General.Is24HourFormat;
            _showSecondsItem.Checked = settings.General.ShowSeconds;
        }

        public void RecreateTrayIcon()
        {
            try
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Visible = SettingsManager.Current.Settings.Behavior.ShowTrayIcon;
            }
            catch { }
        }

        public void UpdateTimeText(string timeText)
        {
            _headerItem.Text = $"🕒 {timeText}";
            string tip = $"Digital Clock - {timeText}";
            if (tip.Length > 63) tip = tip.Substring(0, 63);
            if (_notifyIcon.Text != tip)
            {
                _notifyIcon.Text = tip;
            }
        }

        public void ShowNotification(string title, string text)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
            }
            catch { }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
