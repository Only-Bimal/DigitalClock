using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DigitalClock
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _originalSettings;
        private readonly AppSettings _tempSettings;
        private bool _isInitializing = true;
        private bool _hasSnappedWindow = false;
        private bool _isSaved = false;
        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _originalSettings = SettingsManager.Current.Settings.Clone();
            _tempSettings = SettingsManager.Current.Settings.Clone();

            InitializeComponent();
            SettingsTabControl.SelectedIndex = 0;
            NavGeneral.IsChecked = true;
            PopulateControls();
            _isInitializing = false;
        }

        private void PopulateControls()
        {
            // General
            Chk24Hour.IsChecked = _tempSettings.General.Is24HourFormat;
            ChkShowSeconds.IsChecked = _tempSettings.General.ShowSeconds;
            ChkBlinkColon.IsChecked = _tempSettings.General.BlinkColon;
            ChkShowDate.IsChecked = _tempSettings.General.ShowDate;
            CmbDateFormat.IsEnabled = _tempSettings.General.ShowDate;

            // Date format combobox selection
            SelectDateFormat(_tempSettings.General.DateFormat);

            // Background style
            CmbBackgroundStyle.SelectedIndex = (int)_tempSettings.Appearance.BackgroundStyle;

            // Color Mode
            if (_tempSettings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent)
            {
                RbColorAccent.IsChecked = true;
                PanelCustomColors.Visibility = Visibility.Collapsed;
            }
            else
            {
                RbColorCustom.IsChecked = true;
                PanelCustomColors.Visibility = Visibility.Visible;
            }

            TxtHexColor.Text = _tempSettings.Appearance.CustomHexColor;
            UpdateColorPreview(_tempSettings.Appearance.CustomHexColor);

            // Build Color Presets
            WrapPresets.Children.Clear();
            foreach (var hex in ColorUtilities.ColorPresets)
            {
                var btn = new Button
                {
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(0, 0, 9, 9),
                    Background = new SolidColorBrush(ColorUtilities.ParseHexColor(hex, Colors.Cyan)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(160, 130, 130, 130)),
                    BorderThickness = new Thickness(1),
                    Tag = hex,
                    ToolTip = hex
                };

                // Rounded preset swatch
                var style = new Style(typeof(Button));
                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
                borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { Source = btn });
                borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { Source = btn });
                borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { Source = btn });
                template.VisualTree = borderFactory;
                style.Setters.Add(new Setter(Button.TemplateProperty, template));
                btn.Style = style;

                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string clickedHex)
                    {
                        RbColorCustom.IsChecked = true;
                        RbColorAccent.IsChecked = false;
                        _tempSettings.Appearance.ColorMode = ColorSelectionMode.Custom;
                        PanelCustomColors.Visibility = Visibility.Visible;
                        TxtHexColor.Text = clickedHex;
                    }
                };
                WrapPresets.Children.Add(btn);
            }

            // Typography & Opacity
            PopulateFonts();

            SliderOpacity.Value = _tempSettings.Appearance.Opacity;
            LblOpacity.Text = $"{(int)(SliderOpacity.Value * 100)}%";

            ChkDropShadow.IsChecked = _tempSettings.Appearance.EnableDropShadow;

            // Behavior
            ChkAlwaysOnTop.IsChecked = _tempSettings.Behavior.AlwaysOnTop;
            ChkStartWithWindows.IsChecked = _tempSettings.Behavior.StartWithWindows;
            ChkLockPosition.IsChecked = _tempSettings.Behavior.LockPosition;
            ChkClickThrough.IsChecked = _tempSettings.Behavior.ClickThrough;
            ChkShowTrayIcon.IsChecked = _tempSettings.Behavior.ShowTrayIcon;
            UpdateSavedSizeStatus();

            // Reset & Storage
            TxtStorageMode.Text = SettingsManager.Current.IsPortableMode
                ? "Portable Mode (beside application executable)"
                : "Standard Mode (%AppData% folder)";
            TxtStorageMode.Foreground = SettingsManager.Current.IsPortableMode
                ? new SolidColorBrush(Color.FromRgb(0, 140, 60))
                : new SolidColorBrush(Color.FromRgb(0, 120, 215));

            TxtConfigPath.Text = SettingsManager.Current.ConfigFilePath;

            // About
            var appVersion = typeof(SettingsWindow).Assembly.GetName().Version;
            TxtAppTitle.Text = appVersion != null
                ? (appVersion.Build > 0
                    ? $"Digital Clock v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}"
                    : $"Digital Clock v{appVersion.Major}.{appVersion.Minor}")
                : "Digital Clock v2.7.1";

            // Application Icon
            LoadAppIcons();
        }

        private void LoadAppIcons()
        {
            try
            {
                var headerIcon = GetAppIconImageSource(32);
                var aboutIcon = GetAppIconImageSource(48) ?? headerIcon;

                if (headerIcon != null)
                {
                    Icon = headerIcon;
                    if (ImgHeaderIcon != null) ImgHeaderIcon.Source = headerIcon;
                }

                if (aboutIcon != null && ImgAboutIcon != null)
                {
                    ImgAboutIcon.Source = aboutIcon;
                }
            }
            catch { }
        }

        private static ImageSource? GetAppIconImageSource(int targetSize = 48)
        {
            // 1. Check for accessories-clock.ico on disk
            try
            {
                string[] candidates =
                [
                    Path.Combine(AppContext.BaseDirectory, "accessories-clock.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accessories-clock.ico"),
                    Path.Combine(Directory.GetCurrentDirectory(), "accessories-clock.ico")
                ];

                foreach (var path in candidates)
                {
                    if (File.Exists(path))
                    {
                        using var stream = File.OpenRead(path);
                        var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                            stream,
                            System.Windows.Media.Imaging.BitmapCreateOptions.None,
                            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

                        if (decoder.Frames.Count > 0)
                        {
                            System.Windows.Media.Imaging.BitmapFrame best = decoder.Frames[0];
                            int minDiff = int.MaxValue;
                            foreach (var frame in decoder.Frames)
                            {
                                int diff = Math.Abs(frame.PixelWidth - targetSize);
                                if (diff < minDiff)
                                {
                                    minDiff = diff;
                                    best = frame;
                                }
                            }
                            best.Freeze();
                            return best;
                        }
                    }
                }
            }
            catch { }

            // 2. Extract embedded application icon directly from the running executable (.exe)
            try
            {
                string? exePath = Environment.ProcessPath
                    ?? Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppContext.BaseDirectory, "DigitalClock.exe");

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null)
                    {
                        var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        return bs;
                    }
                }
            }
            catch { }

            // 3. Fallback to System Application Icon
            try
            {
                using var sysIcon = System.Drawing.SystemIcons.Application;
                var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    sysIcon.Handle,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            catch { }

            return null;
        }

        private void PopulateFonts()
        {
            var fonts = FontService.GetAvailableFonts();
            CmbFontFamily.ItemsSource = fonts;

            int selectedIndex = 0;
            for (int i = 0; i < fonts.Count; i++)
            {
                if (string.Equals(fonts[i].FontFamilyKey, _tempSettings.Appearance.FontFamily, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
            CmbFontFamily.SelectedIndex = selectedIndex;
        }

        private void SelectDateFormat(string? format)
        {
            if (CmbDateFormat == null) return;
            string target = string.IsNullOrEmpty(format) ? "dddd" : format;
            for (int i = 0; i < CmbDateFormat.Items.Count; i++)
            {
                if (CmbDateFormat.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString()?.StartsWith(target) == true)
                {
                    CmbDateFormat.SelectedIndex = i;
                    return;
                }
            }
            CmbDateFormat.SelectedIndex = 0;
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            // Read values into _tempSettings
            _tempSettings.General.Is24HourFormat = Chk24Hour.IsChecked == true;
            _tempSettings.General.ShowSeconds = ChkShowSeconds.IsChecked == true;
            _tempSettings.General.BlinkColon = ChkBlinkColon.IsChecked == true;
            _tempSettings.General.ShowDate = ChkShowDate.IsChecked == true;

            // Background Style
            if (CmbBackgroundStyle.SelectedIndex >= 0)
            {
                _tempSettings.Appearance.BackgroundStyle = (ClockBackgroundStyle)CmbBackgroundStyle.SelectedIndex;
            }

            // Color Mode
            if (sender == RbColorAccent || sender == RbColorCustom)
            {
                if (RbColorAccent.IsChecked == true)
                {
                    _tempSettings.Appearance.ColorMode = ColorSelectionMode.WindowsAccent;
                    PanelCustomColors.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _tempSettings.Appearance.ColorMode = ColorSelectionMode.Custom;
                    PanelCustomColors.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Preserve existing ColorMode and keep UI in sync
                if (_tempSettings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent)
                {
                    if (RbColorAccent.IsChecked != true) RbColorAccent.IsChecked = true;
                    PanelCustomColors.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (RbColorCustom.IsChecked != true) RbColorCustom.IsChecked = true;
                    PanelCustomColors.Visibility = Visibility.Visible;
                }
            }

            _tempSettings.Appearance.CustomHexColor = TxtHexColor.Text.Trim();

            // Font Family
            if (CmbFontFamily.SelectedItem is FontOption selectedFont)
            {
                _tempSettings.Appearance.FontFamily = selectedFont.FontFamilyKey;
            }

            _tempSettings.Appearance.Opacity = SliderOpacity.Value;
            _tempSettings.Appearance.EnableDropShadow = ChkDropShadow.IsChecked == true;

            // Behavior
            _tempSettings.Behavior.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
            _tempSettings.Behavior.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            _tempSettings.Behavior.LockPosition = ChkLockPosition.IsChecked == true;
            _tempSettings.Behavior.ClickThrough = ChkClickThrough.IsChecked == true;
            _tempSettings.Behavior.ShowTrayIcon = ChkShowTrayIcon.IsChecked == true;

            // Live Preview on MainWindow
            SettingsManager.Current.NotifySettingsChanged();
            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.ApplySettings(_tempSettings);
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsTabControl == null) return;

            if (sender == NavGeneral) SettingsTabControl.SelectedIndex = 0;
            else if (sender == NavAppearance) SettingsTabControl.SelectedIndex = 1;
            else if (sender == NavBehavior) SettingsTabControl.SelectedIndex = 2;
            else if (sender == NavReset) SettingsTabControl.SelectedIndex = 3;
            else if (sender == NavAbout) SettingsTabControl.SelectedIndex = 4;
        }

        private void OnShowDateToggled(object sender, RoutedEventArgs e)
        {
            if (CmbDateFormat != null)
            {
                CmbDateFormat.IsEnabled = ChkShowDate?.IsChecked == true;
            }
            OnSettingChanged(sender, e);
        }

        private void CmbDateFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (CmbDateFormat.SelectedItem is ComboBoxItem item)
            {
                string text = item.Content.ToString() ?? "";
                string pattern = text.Split(' ')[0];
                if (text.Contains("("))
                {
                    int start = text.IndexOf('(');
                    pattern = text.Substring(0, start).Trim();
                }
                _tempSettings.General.DateFormat = pattern;
            }

            OnSettingChanged(sender, e);
        }

        private void TxtHexColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            string hex = TxtHexColor.Text.Trim();
            UpdateColorPreview(hex);

            if (TxtHexColor.IsFocused && RbColorCustom.IsChecked != true)
            {
                RbColorCustom.IsChecked = true;
                RbColorAccent.IsChecked = false;
                _tempSettings.Appearance.ColorMode = ColorSelectionMode.Custom;
                PanelCustomColors.Visibility = Visibility.Visible;
            }

            OnSettingChanged(sender, e);
        }

        private void UpdateColorPreview(string hex)
        {
            var color = ColorUtilities.ParseHexColor(hex, Colors.Cyan);
            ColorPreviewBorder.Background = new SolidColorBrush(color);
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LblOpacity?.Text = $"{(int)(e.NewValue * 100)}%";
            OnSettingChanged(sender, e);
        }

        // Snap Position Handlers
        private void SnapTopLeft_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.TopLeft);
        }
        private void SnapTopCenter_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.TopCenter);
        }
        private void SnapTopRight_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.TopRight);
        }
        private void SnapCenter_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.Center);
        }
        private void SnapBottomLeft_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.BottomLeft);
        }
        private void SnapBottomCenter_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.BottomCenter);
        }
        private void SnapBottomRight_Click(object sender, RoutedEventArgs e)
        {
            _hasSnappedWindow = true;
            if (_mainWindow != null && _mainWindow.IsLoaded) _mainWindow.SnapTo(SnapPosition.BottomRight);
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? folder = Path.GetDirectoryName(SettingsManager.Current.ConfigFilePath);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open folder: {ex.Message}", "DigitalClock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BackupSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Backup Settings File",
                    Filter = "JSON Configuration File (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"DigitalClock-Settings-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.json"
                };

                if (sfd.ShowDialog(this) == true)
                {
                    // Update placement in _tempSettings from current window
                    try
                    {
                        if (_mainWindow != null && _mainWindow.IsLoaded)
                        {
                            _tempSettings.Placement.Left = _mainWindow.Left;
                            _tempSettings.Placement.Top = _mainWindow.Top;

                            if (_mainWindow.PlacementWidth.HasValue)
                            {
                                _tempSettings.Placement.Width = _mainWindow.ActualWidth;
                                _tempSettings.Placement.Height = _mainWindow.ActualHeight;
                            }
                            else
                            {
                                _tempSettings.Placement.Width = null;
                                _tempSettings.Placement.Height = null;
                            }
                        }
                    }
                    catch { }

                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    string json = System.Text.Json.JsonSerializer.Serialize(_tempSettings, options);
                    File.WriteAllText(sfd.FileName, json);

                    MessageBox.Show(
                        this,
                        $"Settings successfully backed up to:\n{sfd.FileName}",
                        "Backup Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Failed to backup settings: {ex.Message}",
                    "Backup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateSavedSizeStatus()
        {
            if (_tempSettings.Placement.Width.HasValue && _tempSettings.Placement.Height.HasValue)
            {
                TxtSavedSizeStatus.Text = $"{(int)_tempSettings.Placement.Width.Value} × {(int)_tempSettings.Placement.Height.Value} px (Saved)";
                TxtSavedSizeStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 180, 80));
            }
            else
            {
                TxtSavedSizeStatus.Text = "Missing / Null (Screen border-to-border fit)";
                TxtSavedSizeStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 255));
            }
        }

        private void ResetWindowSize_Click(object sender, RoutedEventArgs e)
        {
            _tempSettings.Placement.Width = null;
            _tempSettings.Placement.Height = null;
            UpdateSavedSizeStatus();

            // Immediate live preview on MainWindow: sizes border-to-border on the current screen
            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.ResetSizeToScreenDefault();
            }
        }

        // Granular Reset: Appearance & Styling
        private void ResetAppearance_Click(object sender, RoutedEventArgs e)
        {
            var defaultAppearance = new AppearanceSettings();
            _tempSettings.Appearance = defaultAppearance;

            _isInitializing = true;
            CmbBackgroundStyle.SelectedIndex = (int)_tempSettings.Appearance.BackgroundStyle;
            if (_tempSettings.Appearance.ColorMode == ColorSelectionMode.WindowsAccent)
            {
                RbColorAccent.IsChecked = true;
                PanelCustomColors.Visibility = Visibility.Collapsed;
            }
            else
            {
                RbColorCustom.IsChecked = true;
                PanelCustomColors.Visibility = Visibility.Visible;
            }

            TxtHexColor.Text = _tempSettings.Appearance.CustomHexColor;
            UpdateColorPreview(_tempSettings.Appearance.CustomHexColor);
            PopulateFonts();
            SliderOpacity.Value = _tempSettings.Appearance.Opacity;
            LblOpacity.Text = $"{(int)(SliderOpacity.Value * 100)}%";
            ChkDropShadow.IsChecked = _tempSettings.Appearance.EnableDropShadow;
            _isInitializing = false;

            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.ApplySettings(_tempSettings);
            }
        }

        // Granular Reset: Window Size & Placement
        private void ResetSizeAndPosition_Click(object sender, RoutedEventArgs e)
        {
            _tempSettings.Placement.Width = null;
            _tempSettings.Placement.Height = null;
            _tempSettings.Placement.Left = null;
            _tempSettings.Placement.Top = null;
            _tempSettings.Placement.TargetScreenIndex = -1;
            UpdateSavedSizeStatus();

            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.ResetSizeToScreenDefault();
            }
        }

        // Granular Reset: General & Time Display
        private void ResetGeneral_Click(object sender, RoutedEventArgs e)
        {
            var defaultGeneral = new GeneralSettings();
            _tempSettings.General = defaultGeneral;

            _isInitializing = true;
            Chk24Hour.IsChecked = _tempSettings.General.Is24HourFormat;
            ChkShowSeconds.IsChecked = _tempSettings.General.ShowSeconds;
            ChkBlinkColon.IsChecked = _tempSettings.General.BlinkColon;
            ChkShowDate.IsChecked = _tempSettings.General.ShowDate;
            SelectDateFormat(_tempSettings.General.DateFormat);
            CmbDateFormat.IsEnabled = _tempSettings.General.ShowDate;
            _isInitializing = false;

            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.ApplySettings(_tempSettings);
            }
        }

        // Factory Reset: All Settings
        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to restore all settings back to factory defaults?\nYour custom styles, formats, and positions will be reset.",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SettingsManager.Current.ResetToDefaults();
                _tempSettings.General = SettingsManager.Current.Settings.General;
                _tempSettings.Appearance = SettingsManager.Current.Settings.Appearance;
                _tempSettings.Behavior = SettingsManager.Current.Settings.Behavior;
                _tempSettings.Placement = SettingsManager.Current.Settings.Placement;

                _isInitializing = true;
                PopulateControls();
                _isInitializing = false;

                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.ApplySettings(SettingsManager.Current.Settings);
                    _mainWindow.ResetSizeToScreenDefault();
                }
            }
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Only-Bimal/DigitalClock",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open GitHub repository: {ex.Message}", "Digital Clock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public bool HasUnsavedChanges()
        {
            if (_isSaved) return false;

            if (_hasSnappedWindow)
            {
                try
                {
                    if (_mainWindow != null && _mainWindow.IsLoaded)
                    {
                        if (_mainWindow.Left != _originalSettings.Placement.Left ||
                            _mainWindow.Top != _originalSettings.Placement.Top)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            string origJson = System.Text.Json.JsonSerializer.Serialize(_originalSettings);
            string tempJson = System.Text.Json.JsonSerializer.Serialize(_tempSettings);
            return !string.Equals(origJson, tempJson, StringComparison.Ordinal);
        }

        public bool HandleMainWindowClosing()
        {
            if (!HasUnsavedChanges())
            {
                _isSaved = true;
                Close();
                return true;
            }

            var result = MessageBox.Show(
                this,
                "You have unsaved changes in Settings.\n\nDo you want to save your changes before closing?",
                "Digital Clock Settings",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveInternal();
                return true;
            }
            else if (result == MessageBoxResult.No)
            {
                _isSaved = true;
                try
                {
                    if (_mainWindow != null && _mainWindow.IsLoaded)
                    {
                        _mainWindow.RestorePlacement(_originalSettings.Placement);
                        _mainWindow.ApplySettings(_originalSettings);
                    }
                }
                catch { }
                Close();
                return true;
            }
            else // MessageBoxResult.Cancel
            {
                return false;
            }
        }

        private void SaveInternal()
        {
            _isSaved = true;

            try
            {
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _tempSettings.Placement.Left = _mainWindow.Left;
                    _tempSettings.Placement.Top = _mainWindow.Top;

                    if (_mainWindow.PlacementWidth.HasValue)
                    {
                        _tempSettings.Placement.Width = _mainWindow.ActualWidth;
                        _tempSettings.Placement.Height = _mainWindow.ActualHeight;
                    }
                    else
                    {
                        _tempSettings.Placement.Width = null;
                        _tempSettings.Placement.Height = null;
                    }
                }
            }
            catch { }

            SettingsManager.Current.UpdateSettings(_tempSettings);

            try
            {
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.ApplySettings(_tempSettings);
                }
            }
            catch { }

            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveInternal();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _isSaved = true;
            try
            {
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.RestorePlacement(_originalSettings.Placement);
                    _mainWindow.ApplySettings(_originalSettings);
                }
            }
            catch { }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (!_isSaved)
            {
                try
                {
                    if (_mainWindow != null && _mainWindow.IsLoaded)
                    {
                        _mainWindow.RestorePlacement(_originalSettings.Placement);
                        _mainWindow.ApplySettings(_originalSettings);
                    }
                }
                catch { }
            }
        }
    }
}
