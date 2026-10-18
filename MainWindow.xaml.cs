using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.IO;
using System.Windows.Threading;

namespace DigitalClock
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer = new();
        private TrayIconManager? _trayIconManager;
        private bool _colonVisible = true;
        private Brush _cachedTextBrush = Brushes.Cyan;
        private Color _cachedColor = Colors.Cyan;
        private SettingsWindow? _activeSettingsWindow;
        private AppSettings _effectiveSettings = SettingsManager.Current.Settings;
        private string _currentScreenDeviceName = string.Empty;

        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private bool _isResizing;
        private ResizeDirection _currentResizeDir = ResizeDirection.None;
        private Point _startScreenPoint;
        private double _startLeft;
        private double _startTop;
        private double _startWidth;
        private double _startHeight;
        private bool _hasUserResized;
        private const double ResizeBorderThickness = 16.0;

        public double? PlacementWidth => SettingsManager.Current.Settings.Placement.Width;
        public double? PlacementHeight => SettingsManager.Current.Settings.Placement.Height;

        public MainWindow()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            SizeChanged += MainWindow_SizeChanged;
            LocationChanged += MainWindow_LocationChanged;

            // Observe system glass color changes and desktop changes
            SystemEvents_UserPreferenceChanged();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_effectiveSettings.Appearance.BackgroundStyle == ClockBackgroundStyle.Acrylic ||
                _effectiveSettings.Appearance.BackgroundStyle == ClockBackgroundStyle.DarkFrostedPill ||
                _effectiveSettings.Appearance.BackgroundStyle == ClockBackgroundStyle.LightFrostedPill)
            {
                AeroGlassService.ApplyRoundedWindowRegion(this, 18);
                if (_effectiveSettings.Appearance.BackgroundStyle == ClockBackgroundStyle.Acrylic)
                {
                    AeroGlassService.RefreshAcrylic(this);
                }
            }
            else
            {
                AeroGlassService.ClearWindowRegion(this);
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var screen = Screen.FromHandle(hwnd);
            if (screen.DeviceName != _currentScreenDeviceName)
            {
                _currentScreenDeviceName = screen.DeviceName;
                RefreshWindowBackdrop();
            }
        }

        private void SystemEvents_UserPreferenceChanged()
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshWindowBackdrop();

                    // If settings dialog is currently open, do NOT overwrite its live preview!
                    if (_activeSettingsWindow != null && _activeSettingsWindow.IsLoaded)
                    {
                        return;
                    }

                    if (SettingsManager.Current.Settings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent)
                    {
                        ApplySettings(SettingsManager.Current.Settings);
                    }
                });
            };
        }

        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private int _taskbarCreatedMsg;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_taskbarCreatedMsg != 0 && msg == _taskbarCreatedMsg)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _trayIconManager?.RecreateTrayIcon();
                });
            }
            else if (msg == WM_SETTINGCHANGE || msg == WM_DISPLAYCHANGE)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    RefreshWindowBackdrop();
                    if (_effectiveSettings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent &&
                        (_activeSettingsWindow == null || !_activeSettingsWindow.IsLoaded))
                    {
                        ApplySettings(_effectiveSettings);
                    }
                });
            }
            else if (msg == WM_EXITSIZEMOVE)
            {
                _hasUserResized = true;
                SettingsManager.Current.Settings.Placement.Left = Left;
                SettingsManager.Current.Settings.Placement.Top = Top;
                SettingsManager.Current.Settings.Placement.Width = ActualWidth;
                SettingsManager.Current.Settings.Placement.Height = ActualHeight;
                SettingsManager.Current.Save();
                RefreshWindowBackdrop();
            }
            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register TaskbarCreated message to survive Explorer restarts
            _taskbarCreatedMsg = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            // Initialize Tray Icon
            _trayIconManager = new TrayIconManager(this);

            // Hook window messages for wallpaper/display changes
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);

            // Restore Position
            RestoreSavedPosition();

            // Apply configuration
            ApplySettings(SettingsManager.Current.Settings);

            // Initial tick
            Timer_Tick(null, EventArgs.Empty);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateClockDisplay(DateTime.Now);
        }

        private void UpdateClockDisplay(DateTime now)
        {
            var settings = _effectiveSettings;

            // Handle Colon Blinking
            if (settings.General.BlinkColon)
            {
                _colonVisible = !_colonVisible;
            }
            else
            {
                _colonVisible = true;
            }

            string colon = _colonVisible ? ":" : " ";

            // Format Time
            string timeString;
            if (settings.General.Is24HourFormat)
            {
                string format = settings.General.ShowSeconds ? $"HH{colon}mm{colon}ss" : $"HH{colon}mm";
                timeString = now.ToString(format);
            }
            else
            {
                string format = settings.General.ShowSeconds ? $"hh{colon}mm{colon}ss tt" : $"hh{colon}mm tt";
                timeString = now.ToString(format);
            }

            ClockTimes.Text = timeString;

            // Format Date
            if (settings.General.ShowDate)
            {
                try
                {
                    ClockDates.Text = now.ToString(settings.General.DateFormat);
                }
                catch
                {
                    ClockDates.Text = now.ToString("dddd, dd MMMM yyyy");
                }
            }

            // Update tray tooltip & header text
            _trayIconManager?.UpdateTimeText(timeString);
        }

        public void ApplySettings(AppSettings settings)
        {
            _effectiveSettings = settings;

            // 1. Topmost
            Topmost = settings.Behavior.AlwaysOnTop;

            // 2. Click-Through
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.SetClickThrough(hwnd, settings.Behavior.ClickThrough);
            }

            // 3. Text Color
            Color textColor;
            if (settings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent)
            {
                textColor = ColorUtilities.GetWindowsAccentColor();
            }
            else
            {
                textColor = ColorUtilities.ParseHexColor(settings.Appearance.CustomHexColor, Colors.White);
            }

            // Boost visibility for dark background
            if (settings.Appearance.BackgroundStyle == ClockBackgroundStyle.DarkFrostedPill)
            {
                textColor = ColorUtilities.EnsureVisibleOnDark(textColor);
            }

            if (_cachedColor != textColor)
            {
                _cachedColor = textColor;
                var brush = new SolidColorBrush(textColor);
                brush.Freeze();
                _cachedTextBrush = brush;
            }

            ClockTimes.Foreground = _cachedTextBrush;
            ClockDates.Foreground = _cachedTextBrush;

            // 4. Opacity
            ClockTimes.Opacity = settings.Appearance.Opacity;
            ClockDates.Opacity = Math.Min(1.0, settings.Appearance.Opacity * 0.9);

            // 5. Typography
            var font = FontService.ResolveFontFamily(settings.Appearance.FontFamily);
            ClockTimes.FontFamily = font;
            ClockDates.FontFamily = font;

            // 6. Drop Shadow (illumination from top-left, casting shadow towards bottom-right)
            if (settings.Appearance.EnableDropShadow)
            {
                TimeShadowEffect.Color = Colors.Black;
                TimeShadowEffect.ShadowDepth = 4;
                TimeShadowEffect.BlurRadius = 9;
                TimeShadowEffect.Direction = 315;
                TimeShadowEffect.Opacity = 0.70;

                DateShadowEffect.Color = Colors.Black;
                DateShadowEffect.ShadowDepth = 4;
                DateShadowEffect.BlurRadius = 9;
                DateShadowEffect.Direction = 315;
                DateShadowEffect.Opacity = 0.70;
            }
            else
            {
                TimeShadowEffect.Opacity = 0.0;
                DateShadowEffect.Opacity = 0.0;
            }

            // 7. Date Visibility
            ClockDates.Visibility = settings.General.ShowDate ? Visibility.Visible : Visibility.Collapsed;

            // 8. Background Style & Acrylic
            ApplyBackgroundStyle(settings.Appearance.BackgroundStyle);

            // 9. Update Context Menu checks
            MenuAlwaysOnTop.IsChecked = settings.Behavior.AlwaysOnTop;
            MenuLockPosition.IsChecked = settings.Behavior.LockPosition;
            MenuClickThrough.IsChecked = settings.Behavior.ClickThrough;
            MenuStartWithWindows.IsChecked = settings.Behavior.StartWithWindows;

            // 10. Update Tray Menu
            _trayIconManager?.UpdateMenuItems(settings);

            // 11. Immediate Live Refresh
            UpdateClockDisplay(DateTime.Now);
        }

        private void ApplyBackgroundStyle(ClockBackgroundStyle style)
        {
            switch (style)
            {
                case ClockBackgroundStyle.Acrylic:
                    AeroGlassService.ApplyAcrylic(this, true);
                    BackplateBorder.Background = new SolidColorBrush(Color.FromArgb(9, 255, 255, 255));
                    BackplateBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    BackplateBorder.BorderThickness = new Thickness(1);
                    break;

                case ClockBackgroundStyle.DarkFrostedPill:
                    AeroGlassService.ApplyAcrylic(this, false);
                    AeroGlassService.ApplyRoundedWindowRegion(this, 18);
                    BackplateBorder.Background = new SolidColorBrush(Color.FromArgb(103, 20, 20, 24));
                    BackplateBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    BackplateBorder.BorderThickness = new Thickness(1);
                    break;

                case ClockBackgroundStyle.LightFrostedPill:
                    AeroGlassService.ApplyAcrylic(this, false);
                    AeroGlassService.ApplyRoundedWindowRegion(this, 18);
                    BackplateBorder.Background = new SolidColorBrush(Color.FromArgb(180, 240, 240, 245));
                    BackplateBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
                    BackplateBorder.BorderThickness = new Thickness(1);
                    break;

                case ClockBackgroundStyle.Transparent:
                default:
                    AeroGlassService.ApplyAcrylic(this, false);
                    AeroGlassService.ClearWindowRegion(this);
                    BackplateBorder.Background = Brushes.Transparent;
                    BackplateBorder.BorderBrush = Brushes.Transparent;
                    BackplateBorder.BorderThickness = new Thickness(0);
                    break;
            }
        }

        public void RefreshWindowBackdrop()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            if (_effectiveSettings.Appearance.BackgroundStyle == ClockBackgroundStyle.Acrylic)
            {
                AeroGlassService.ApplyAcrylic(this, true);
                AeroGlassService.RefreshAcrylic(this);
            }
            else
            {
                NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER |
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
            }

            InvalidateVisual();
        }

        public Screen GetCurrentScreen()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                return Screen.FromHandle(hwnd);
            }

            if (!double.IsNaN(Left) && !double.IsNaN(Top))
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
                int px = (int)((Left + (ActualWidth > 0 ? ActualWidth : 774) / 2) * dpiX);
                int py = (int)((Top + (ActualHeight > 0 ? ActualHeight : 225) / 2) * dpiY);
                return Screen.FromPoint(new System.Drawing.Point(px, py));
            }

            return Screen.PrimaryScreen ?? Screen.AllScreens[0];
        }

        public void ResetSizeToScreenDefault(Screen? targetScreen = null)
        {
            _hasUserResized = false;
            SettingsManager.Current.Settings.Placement.Width = null;
            SettingsManager.Current.Settings.Placement.Height = null;
            SettingsManager.Current.Save();

            var screen = targetScreen ?? GetCurrentScreen();

            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            var workArea = screen.WorkingArea;
            double screenLeft = workArea.Left / dpiX;
            double screenTop = workArea.Top / dpiY;
            double screenWidth = workArea.Width / dpiX;
            double screenHeight = workArea.Height / dpiY;

            var settings = _effectiveSettings;
            double timeRatio = settings.General.ShowSeconds
                ? (settings.General.Is24HourFormat ? 4.0 : 4.6)
                : (settings.General.Is24HourFormat ? 2.6 : 3.2);

            double combinedRatio = settings.General.ShowDate ? (timeRatio * 0.81) : timeRatio;

            bool isWidthLarger = screenWidth >= screenHeight;
            double targetWidth;
            double targetHeight;
            double targetLeft;
            double targetTop;

            if (isWidthLarger)
            {
                // Larger side is Width: span border to border horizontally
                targetWidth = screenWidth;
                double idealHeight = Math.Round(targetWidth / combinedRatio);
                targetHeight = Math.Min(idealHeight, screenHeight);
                targetLeft = screenLeft;
                targetTop = screenTop + (screenHeight - targetHeight) / 2.0;
            }
            else
            {
                // Larger side is Height: span border to border vertically
                targetHeight = screenHeight;
                double idealWidth = Math.Round(targetHeight * combinedRatio);
                targetWidth = Math.Min(idealWidth, screenWidth);
                targetTop = screenTop;
                targetLeft = screenLeft + (screenWidth - targetWidth) / 2.0;
            }

            SizeToContent = SizeToContent.Manual;
            Width = Math.Max(MinWidth, targetWidth);
            Height = Math.Max(MinHeight, targetHeight);
            Left = targetLeft;
            Top = targetTop;

            RefreshWindowBackdrop();
        }

        private void RestoreSavedPosition()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            SizeToContent = SizeToContent.Manual;

            var placement = SettingsManager.Current.Settings.Placement;
            bool hasSavedSize = placement.Width.HasValue && placement.Height.HasValue &&
                                placement.Width.Value > 50 && placement.Height.Value > 30;

            Screen? screenForPlacement = null;

            if (placement.Left.HasValue && placement.Top.HasValue)
            {
                double left = placement.Left.Value;
                double top = placement.Top.Value;

                foreach (var screen in Screen.AllScreens)
                {
                    var dpi = VisualTreeHelper.GetDpi(this);
                    double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                    double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                    var wa = screen.WorkingArea;
                    double sLeft = wa.Left / dpiX;
                    double sTop = wa.Top / dpiY;
                    double sRight = wa.Right / dpiX;
                    double sBottom = wa.Bottom / dpiY;

                    if (left >= sLeft && left < sRight && top >= sTop && top < sBottom)
                    {
                        screenForPlacement = screen;
                        Left = left;
                        Top = top;
                        break;
                    }
                }
            }

            if (hasSavedSize)
            {
                Width = placement.Width!.Value;
                Height = placement.Height!.Value;

                if (screenForPlacement == null)
                {
                    SnapTo(SnapPosition.Center, Screen.PrimaryScreen);
                }
            }
            else
            {
                // No previous size saved:
                // Initial size takes screen size on which it is starting into account
                // and resizes itself from border to border on the larger side.
                var startingScreen = screenForPlacement ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];
                ResetSizeToScreenDefault(startingScreen);
            }
        }

        public void RestorePlacement(WindowPlacementSettings placement)
        {
            if (placement.Width.HasValue && placement.Height.HasValue)
            {
                Width = placement.Width.Value;
                Height = placement.Height.Value;
                if (placement.Left.HasValue) Left = placement.Left.Value;
                if (placement.Top.HasValue) Top = placement.Top.Value;
            }
            else
            {
                ResetSizeToScreenDefault();
            }
        }

        public void SnapTo(SnapPosition position, Screen? targetScreen = null)
        {
            UpdateLayout();

            Screen currentScreen;
            if (targetScreen != null)
            {
                currentScreen = targetScreen;
            }
            else if (!double.IsNaN(Left) && !double.IsNaN(Top))
            {
                currentScreen = GetCurrentScreen();
            }
            else
            {
                currentScreen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            }

            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            var workArea = currentScreen.WorkingArea;
            double screenLeft = workArea.Left / dpiX;
            double screenTop = workArea.Top / dpiY;
            double screenWidth = workArea.Width / dpiX;
            double screenHeight = workArea.Height / dpiY;

            double windowWidth = ActualWidth > 0 ? ActualWidth : 774;
            double windowHeight = ActualHeight > 0 ? ActualHeight : 225;
            double margin = 9.0;

            switch (position)
            {
                case SnapPosition.TopLeft:
                    Left = screenLeft + margin;
                    Top = screenTop + margin;
                    break;

                case SnapPosition.TopCenter:
                    Left = screenLeft + (screenWidth - windowWidth) / 2;
                    Top = screenTop + margin;
                    break;

                case SnapPosition.TopRight:
                    Left = screenLeft + screenWidth - windowWidth - margin;
                    Top = screenTop + margin;
                    break;

                case SnapPosition.Center:
                    Left = screenLeft + (screenWidth - windowWidth) / 2;
                    Top = screenTop + (screenHeight - windowHeight) / 2;
                    break;

                case SnapPosition.BottomLeft:
                    Left = screenLeft + margin;
                    Top = screenTop + screenHeight - windowHeight - margin;
                    break;

                case SnapPosition.BottomCenter:
                    Left = screenLeft + (screenWidth - windowWidth) / 2;
                    Top = screenTop + screenHeight - windowHeight - margin;
                    break;

                case SnapPosition.BottomRight:
                    Left = screenLeft + screenWidth - windowWidth - margin;
                    Top = screenTop + screenHeight - windowHeight - margin;
                    break;
            }

            // Save new position
            SettingsManager.Current.Settings.Placement.Left = Left;
            SettingsManager.Current.Settings.Placement.Top = Top;
            if (_hasUserResized || SettingsManager.Current.Settings.Placement.Width.HasValue)
            {
                SettingsManager.Current.Settings.Placement.Width = ActualWidth;
                SettingsManager.Current.Settings.Placement.Height = ActualHeight;
            }
            SettingsManager.Current.Save();

            RefreshWindowBackdrop();
        }

        private void RootGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_effectiveSettings.Behavior.LockPosition) return;

            var pos = e.GetPosition(this);
            var dir = GetResizeDirection(pos);
            if (dir != ResizeDirection.None)
            {
                _isResizing = true;
                _currentResizeDir = dir;

                _startLeft = Left;
                _startTop = Top;
                _startWidth = ActualWidth;
                _startHeight = ActualHeight;
                _startScreenPoint = GetScreenPointDips(pos);

                Mouse.Capture(RootGrid);
                e.Handled = true;
            }
        }

        private void RootGrid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isResizing)
            {
                var currentScreenPoint = GetScreenPointDips(e.GetPosition(this));
                double dx = currentScreenPoint.X - _startScreenPoint.X;
                double dy = currentScreenPoint.Y - _startScreenPoint.Y;

                double newWidth = _startWidth;
                double newHeight = _startHeight;
                double newLeft = _startLeft;
                double newTop = _startTop;

                // Horizontal resize
                switch (_currentResizeDir)
                {
                    case ResizeDirection.Right:
                    case ResizeDirection.TopRight:
                    case ResizeDirection.BottomRight:
                        newWidth = Math.Max(MinWidth, _startWidth + dx);
                        break;

                    case ResizeDirection.Left:
                    case ResizeDirection.TopLeft:
                    case ResizeDirection.BottomLeft:
                        double potentialWidth = _startWidth - dx;
                        if (potentialWidth >= MinWidth)
                        {
                            newWidth = potentialWidth;
                            newLeft = _startLeft + dx;
                        }
                        else
                        {
                            newWidth = MinWidth;
                            newLeft = _startLeft + (_startWidth - MinWidth);
                        }
                        break;
                }

                // Vertical resize
                switch (_currentResizeDir)
                {
                    case ResizeDirection.Bottom:
                    case ResizeDirection.BottomLeft:
                    case ResizeDirection.BottomRight:
                        newHeight = Math.Max(MinHeight, _startHeight + dy);
                        break;

                    case ResizeDirection.Top:
                    case ResizeDirection.TopLeft:
                    case ResizeDirection.TopRight:
                        double potentialHeight = _startHeight - dy;
                        if (potentialHeight >= MinHeight)
                        {
                            newHeight = potentialHeight;
                            newTop = _startTop + dy;
                        }
                        else
                        {
                            newHeight = MinHeight;
                            newTop = _startTop + (_startHeight - MinHeight);
                        }
                        break;
                }

                Left = newLeft;
                Top = newTop;
                Width = newWidth;
                Height = newHeight;
            }
            else
            {
                var dir = GetResizeDirection(e.GetPosition(this));
                UpdateMouseCursor(dir);
            }
        }

        private void RootGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _hasUserResized = true;
                Mouse.Capture(null);
                UpdateMouseCursor(ResizeDirection.None);

                // Persist new size and position
                SettingsManager.Current.Settings.Placement.Left = Left;
                SettingsManager.Current.Settings.Placement.Top = Top;
                SettingsManager.Current.Settings.Placement.Width = ActualWidth;
                SettingsManager.Current.Settings.Placement.Height = ActualHeight;
                SettingsManager.Current.Save();

                RefreshWindowBackdrop();
                e.Handled = true;
            }
        }

        private void RootGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isResizing)
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private ResizeDirection GetResizeDirection(Point p)
        {
            if (_effectiveSettings.Behavior.LockPosition)
                return ResizeDirection.None;

            double w = ActualWidth;
            double h = ActualHeight;
            double border = ResizeBorderThickness;

            // 1. Check window outer borders
            bool winLeft = p.X <= border;
            bool winRight = p.X >= w - border;
            bool winTop = p.Y <= border;
            bool winBottom = p.Y >= h - border;

            if (winTop && winLeft) return ResizeDirection.TopLeft;
            if (winTop && winRight) return ResizeDirection.TopRight;
            if (winBottom && winLeft) return ResizeDirection.BottomLeft;
            if (winBottom && winRight) return ResizeDirection.BottomRight;
            if (winLeft) return ResizeDirection.Left;
            if (winRight) return ResizeDirection.Right;
            if (winTop) return ResizeDirection.Top;
            if (winBottom) return ResizeDirection.Bottom;

            // 2. Also check visible BackplateBorder edges when clock has empty margins
            if (BackplateBorder != null && BackplateBorder.IsVisible && BackplateBorder.ActualWidth > 0 && BackplateBorder.ActualHeight > 0)
            {
                try
                {
                    var bounds = BackplateBorder.TransformToAncestor(this).TransformBounds(new Rect(0, 0, BackplateBorder.ActualWidth, BackplateBorder.ActualHeight));

                    bool nearLeft = Math.Abs(p.X - bounds.Left) <= border && p.Y >= bounds.Top - border && p.Y <= bounds.Bottom + border;
                    bool nearRight = Math.Abs(p.X - bounds.Right) <= border && p.Y >= bounds.Top - border && p.Y <= bounds.Bottom + border;
                    bool nearTop = Math.Abs(p.Y - bounds.Top) <= border && p.X >= bounds.Left - border && p.X <= bounds.Right + border;
                    bool nearBottom = Math.Abs(p.Y - bounds.Bottom) <= border && p.X >= bounds.Left - border && p.X <= bounds.Right + border;

                    if (nearTop && nearLeft) return ResizeDirection.TopLeft;
                    if (nearTop && nearRight) return ResizeDirection.TopRight;
                    if (nearBottom && nearLeft) return ResizeDirection.BottomLeft;
                    if (nearBottom && nearRight) return ResizeDirection.BottomRight;
                    if (nearLeft) return ResizeDirection.Left;
                    if (nearRight) return ResizeDirection.Right;
                    if (nearTop) return ResizeDirection.Top;
                    if (nearBottom) return ResizeDirection.Bottom;
                }
                catch { }
            }

            return ResizeDirection.None;
        }

        private void UpdateMouseCursor(ResizeDirection dir)
        {
            switch (dir)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    Cursor = System.Windows.Input.Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    Cursor = System.Windows.Input.Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    Cursor = System.Windows.Input.Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    Cursor = System.Windows.Input.Cursors.SizeNESW;
                    break;
                default:
                    Cursor = System.Windows.Input.Cursors.Arrow;
                    break;
            }
        }

        private Point GetScreenPointDips(Point windowPoint)
        {
            Point screenPixels = PointToScreen(windowPoint);
            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            return new Point(screenPixels.X / dpiX, screenPixels.Y / dpiY);
        }

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_effectiveSettings.Behavior.LockPosition && e.ButtonState == MouseButtonState.Pressed && !_isResizing)
            {
                DragMove();
                SettingsManager.Current.Settings.Placement.Left = Left;
                SettingsManager.Current.Settings.Placement.Top = Top;
                if (_hasUserResized || SettingsManager.Current.Settings.Placement.Width.HasValue)
                {
                    SettingsManager.Current.Settings.Placement.Width = ActualWidth;
                    SettingsManager.Current.Settings.Placement.Height = ActualHeight;
                }
                SettingsManager.Current.Save();

                RefreshWindowBackdrop();
            }
        }

        public void OpenSettings()
        {
            if (_activeSettingsWindow != null && _activeSettingsWindow.IsLoaded)
            {
                _activeSettingsWindow.Activate();
                return;
            }

            _activeSettingsWindow = new SettingsWindow(this);
            _activeSettingsWindow.Closed += (s, e) => _activeSettingsWindow = null;
            _activeSettingsWindow.Show();
        }

        public void ToggleVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                Topmost = true;
                Topmost = _effectiveSettings.Behavior.AlwaysOnTop;
            }
        }

        public void ExitApp()
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                SettingsManager.Current.Settings.Placement.Left = Left;
                SettingsManager.Current.Settings.Placement.Top = Top;

                if (_hasUserResized || SettingsManager.Current.Settings.Placement.Width.HasValue)
                {
                    SettingsManager.Current.Settings.Placement.Width = ActualWidth;
                    SettingsManager.Current.Settings.Placement.Height = ActualHeight;
                }

                SettingsManager.Current.Save();
            }

            _trayIconManager?.Dispose();
        }

        // Context Menu Event Handlers
        private void OpenSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

        private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Settings;
            s.Behavior.AlwaysOnTop = MenuAlwaysOnTop.IsChecked;
            SettingsManager.Current.Save();
            ApplySettings(s);
        }

        private void MenuLockPosition_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Settings;
            s.Behavior.LockPosition = MenuLockPosition.IsChecked;
            SettingsManager.Current.Save();
            ApplySettings(s);
        }

        private void MenuClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Settings;
            s.Behavior.ClickThrough = MenuClickThrough.IsChecked;
            SettingsManager.Current.Save();
            ApplySettings(s);
        }

        private void MenuStartWithWindows_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current.Settings;
            s.Behavior.StartWithWindows = MenuStartWithWindows.IsChecked;
            SettingsManager.Current.Save();
            ApplySettings(s);
        }

        private void MenuSnapPrimary_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.Center, Screen.PrimaryScreen);
        private void MenuResetSize_Click(object sender, RoutedEventArgs e) => ResetSizeToScreenDefault();
        private void MenuSnapCenter_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.Center);
        private void MenuSnapTopRight_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.TopRight);
        private void MenuSnapBottomRight_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.BottomRight);
        private void MenuSnapTopLeft_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.TopLeft);
        private void MenuSnapBottomLeft_Click(object sender, RoutedEventArgs e) => SnapTo(SnapPosition.BottomLeft);

        private void MenuClose_Click(object sender, RoutedEventArgs e) => ExitApp();
    }
}