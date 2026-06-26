using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Win32;

using MessageBox = System.Windows.MessageBox;

namespace DigitalClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // Constants for registry keys
        private const string OnTop = "OnTop";
        private const string NotifyIconVisibleKey = "NotifyIconVisible";
        private readonly string DigitalClock = "DigitalClock" + Prefix;
        private string runLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private string appSettingsLocation = @"Software\DigitalClock" + Prefix;
        private static SolidColorBrush AccentColor => (SolidColorBrush)SystemParameters.WindowGlassBrush;

        /// <summary>Gets a value indicating whether the assembly was built in debug mode.</summary>
        public static bool IsDebug
        {
            get
            {
                bool isDebug = false;

#if (DEBUG)
                isDebug = true;
#else
                isDebug = false;
#endif

                return isDebug;
            }
        }

        /// <summary>Gets a value indicating whether the assembly was built in release mode.</summary>
        public bool IsRelease
        {
            get { return !IsDebug; }
        }

        private static string Prefix
        {
            get
            {
                return IsDebug ? "\\Debug" : "\\Release";
            }
        }

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            // Set Location
            SetLocation();
            UpdateNotifyIconMenuHeader();

            // Attach ContextMenuOpening event for Grid
            var grid = this.Content as System.Windows.Controls.Grid;
            if (grid != null)
                grid.ContextMenuOpening += Grid_ContextMenuOpening;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                ClockTimes.Content = DateTime.Now.ToString("HH:mm");
                ClockDates.Content = DateTime.Now.ToString("dddd, dd MMMM yyyy");
                //ClockTimes.Foreground = AccentColor;
                //ClockDates.Foreground = AccentColor;
                UpdateColor();
            };
            _timer.Start();

            // do not make this program crash
            try
            {
                // Auto Start with Windows
                using (var key = Registry.CurrentUser.CreateSubKey(runLocation, true))
                {
                    var startWithWindowsMenu = (System.Windows.Controls.MenuItem)FindName("StartWithWindows");
                    if (startWithWindowsMenu != null)
                        startWithWindowsMenu.IsChecked = (key?.GetValue(DigitalClock, null) != null);
                }
                // Display on top
                using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
                {
                    var displayOnTopMenu = (System.Windows.Controls.MenuItem)FindName("DisplayOnTop");
                    if (displayOnTopMenu != null)
                    {
                        displayOnTopMenu.IsChecked = Convert.ToBoolean(key?.GetValue(OnTop, "False"));
                        Topmost = displayOnTopMenu.IsChecked == true;
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show("Unable to set start-up topValue to true.\n" + ex.Message);
            }

            // Initialize system tray icon
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            bool notifyIconVisible = true;
            using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
            {
                object value = key?.GetValue(NotifyIconVisibleKey, "True");
                notifyIconVisible = Convert.ToBoolean(value);
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = notifyIconVisible,
                Text = "Digital Clock"
            };

            // Create context menu for tray icon
            var trayMenu = new ContextMenuStrip();

            // Add menu items (reuse WPF context menu items)
            trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            trayMenu.Items.Add(new ToolStripSeparator());

            // Add your existing context menu items
            trayMenu.Items.Add("Change Color", null, (s, e) => Dispatcher.Invoke(() => ChangeColor_Click(null, null)));
            trayMenu.Items.Add("Display On Top", null, (s, e) => Dispatcher.Invoke(() => {
                var menu = (System.Windows.Controls.MenuItem)FindName("DisplayOnTop");
                if (menu != null) menu.IsChecked = !menu.IsChecked;
            }));

            trayMenu.Items.Add("Start With Windows", null, (s, e) => Dispatcher.Invoke(() => {
                var menu = (System.Windows.Controls.MenuItem)FindName("StartWithWindows");
                if (menu != null) menu.IsChecked = !menu.IsChecked;
            }));

            trayMenu.Items.Add(new ToolStripSeparator());
            // Add toggle notification icon menu item
            var toggleNotifyIconItem = new ToolStripMenuItem(notifyIconVisible ? "Hide Notification Icon" : "Show Notification Icon");
            toggleNotifyIconItem.Click += (s, e) => ToggleNotifyIconVisibility(toggleNotifyIconItem);
            trayMenu.Items.Add(toggleNotifyIconItem);
            trayMenu.Items.Add("Close", null, (s, e) => Dispatcher.Invoke(() => Close()));

            _notifyIcon.ContextMenuStrip = trayMenu;

            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ToggleNotifyIconVisibility(ToolStripMenuItem menuItem)
        {
            bool newVisible = !_notifyIcon.Visible;
            _notifyIcon.Visible = newVisible;
            // Update tray menu text if applicable
            if (menuItem != null)
                menuItem.Text = newVisible ? "Hide Notification Icon" : "Show Notification Icon";
            // Update WPF context menu text
            UpdateNotifyIconMenuHeader();
            // Save to registry
            using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
            {
                key?.SetValue(NotifyIconVisibleKey, newVisible);
            }
        }

        private void ShowWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                }
                else
                {
                    Show();
                    Activate();
                }
            });
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }

        // drag the digital clock
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // close context menu
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeColor_Click(object sender, RoutedEventArgs e)
        {
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (!(SystemParameters.WindowGlassBrush is SolidColorBrush accentColor))
            {
                Console.WriteLine("Unable to get accent color." + SystemParameters.WindowGlassBrush);
                return;
            }

            Color contrastColor = accentColor.Color.GetContrastingColor();
            contrastColor.A = 200; // Set alpha to 255 for full opacity


            ClockTimes.Foreground = new SolidColorBrush(contrastColor);
            ClockDates.Foreground = new SolidColorBrush(contrastColor);
        }

        // start with windows context menu unchecked event
        private void StartWithWindows_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(runLocation, true))
                {
                    key?.DeleteValue(DigitalClock, false);
                }
            }
            catch { MessageBox.Show("Unable to set StartWithWindows to false."); }
        }

        // start with windows context menu checked event
        private void StartWithWindows_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(runLocation, true))
                {
                    key?.SetValue(DigitalClock, "\"" + Assembly.GetExecutingAssembly().Location + "\"");
                }
            }
            catch { MessageBox.Show("Unable to set StartWithWindows to true."); }
        }

        private void DisplayOnTop_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(appSettingsLocation, true))
                {
                    key?.SetValue(OnTop, true);
                    Topmost = true;
                }
            }
            catch { MessageBox.Show("Unable to set Display On Top to true."); }
        }
        private void DisplayOnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(appSettingsLocation, true))
                {
                    key?.DeleteValue(OnTop, false);
                    Topmost = false;
                }
            }
            catch { MessageBox.Show("Unable to set Display On Top to true."); }
        }

        // close digital clock
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //Save the location and size
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
                {
                    key?.SetValue("Top", Top);
                    key?.SetValue("Left", Left);
                    key?.SetValue("Width", Width);
                    key?.SetValue("Height", Height);
                }
            }
            catch { MessageBox.Show("Unable to set Values."); }
        }

        private void SetLocation()
        {
            var screens = Screen.AllScreens;
            Screen targetScreen = null;
            if (screens.Length == 1)
            {
                // Only one monitor: use the primary
                targetScreen = screens[0];
            }
            else
            {
                // More than one monitor: use the first secondary (not primary)
                foreach (var screen in screens)
                {
                    if (!screen.Primary)
                    {
                        targetScreen = screen;
                        break;
                    }
                }
                // Fallback: if all are primary (should not happen), use the first
                if (targetScreen == null)
                    targetScreen = screens[0];
            }

            WindowStartupLocation = WindowStartupLocation.Manual;

            // Optionally, you can still restore size if you want:
            using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
            {
                object heightValue = key?.GetValue("Height", Height);
                Height = Convert.ToDouble(heightValue);

                object widthValue = key?.GetValue("Width", Width);
                Width = Convert.ToDouble(widthValue);
            }

            // Set Width/Height if not already set (fallback to default)
            if (double.IsNaN(Width) || Width == 0)
                Width = 800;
            if (double.IsNaN(Height) || Height == 0)
                Height = 400;

            // Always center the window on the target screen (ignore saved position)
            Left = targetScreen.Bounds.Left + (targetScreen.Bounds.Width - Width) / 2;
            Top = targetScreen.Bounds.Top + (targetScreen.Bounds.Height - Height) / 2;

            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            // Re-center after possible resize
            SetLocation();
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ToggleNotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            // Use the same logic as tray icon toggle
            ToggleNotifyIconVisibility(null);

            // The header is now updated by UpdateNotifyIconMenuHeader
        }

        private void UpdateNotifyIconMenuHeader()
        {
            // Update Window.ContextMenu
            var wpfMenuItem = (System.Windows.Controls.MenuItem)FindName("ToggleNotifyIcon");
            if (wpfMenuItem != null)
                wpfMenuItem.Header = _notifyIcon != null && _notifyIcon.Visible ? "Hide Notification Icon" : "Show Notification Icon";

            // Update Grid.ContextMenu (if present)
            var grid = this.Content as System.Windows.Controls.Grid;
            if (grid != null)
            {
                var gridContextMenu = grid.ContextMenu;
                if (gridContextMenu != null)
                {
                    foreach (var item in gridContextMenu.Items)
                    {
                        var menuItem = item as System.Windows.Controls.MenuItem;
                        if (menuItem != null && (menuItem.Name == "ToggleNotifyIcon" || menuItem.Header.ToString().Contains("Notification Icon")))
                        {
                            menuItem.Header = _notifyIcon != null && _notifyIcon.Visible ? "Hide Notification Icon" : "Show Notification Icon";
                        }
                    }
                }
            }
        }

        private void Grid_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            var grid = sender as System.Windows.Controls.Grid;
            if (grid != null && grid.ContextMenu != null)
            {
                foreach (var item in grid.ContextMenu.Items)
                {
                    var menuItem = item as System.Windows.Controls.MenuItem;
                    if (menuItem != null && (menuItem.Name == "ToggleNotifyIcon" || menuItem.Header.ToString().Contains("Notification Icon")))
                    {
                        menuItem.Header = _notifyIcon != null && _notifyIcon.Visible ? "Hide Notification Icon" : "Show Notification Icon";
                    }
                }
            }
        }
    }
}