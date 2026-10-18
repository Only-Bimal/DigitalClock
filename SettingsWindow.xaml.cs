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
        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _originalSettings = SettingsManager.Current.Settings.Clone();
            _tempSettings = SettingsManager.Current.Settings.Clone();

            InitializeComponent();
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

            // About & Reset
            var appVersion = typeof(SettingsWindow).Assembly.GetName().Version;
            TxtAppTitle.Text = appVersion != null
                ? $"Digital Clock v{appVersion.Major}.{appVersion.Minor}"
                : "Digital Clock v2.6";

            TxtStorageMode.Text = SettingsManager.Current.IsPortableMode
                ? "Portable Mode (beside application executable)"
                : "Standard Mode (%AppData% folder)";
            TxtStorageMode.Foreground = SettingsManager.Current.IsPortableMode
                ? new SolidColorBrush(Color.FromRgb(0, 230, 118))
                : new SolidColorBrush(Color.FromRgb(64, 196, 255));

            TxtConfigPath.Text = SettingsManager.Current.ConfigFilePath;
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

        private void SelectDateFormat(string format)
        {
            for (int i = 0; i < CmbDateFormat.Items.Count; i++)
            {
                if (CmbDateFormat.Items[i] is ComboBoxItem item &&
                    item.Content.ToString()!.StartsWith(format))
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
            _mainWindow.ApplySettings(_tempSettings);
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == NavGeneral) SettingsTabControl.SelectedIndex = 0;
            else if (sender == NavAppearance) SettingsTabControl.SelectedIndex = 1;
            else if (sender == NavBehavior) SettingsTabControl.SelectedIndex = 2;
            else if (sender == NavAbout) SettingsTabControl.SelectedIndex = 3;
        }

        private void OnShowDateToggled(object sender, RoutedEventArgs e)
        {
            CmbDateFormat.IsEnabled = ChkShowDate.IsChecked == true;
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
        private void SnapTopLeft_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.TopLeft);
        private void SnapTopCenter_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.TopCenter);
        private void SnapTopRight_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.TopRight);
        private void SnapCenter_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.Center);
        private void SnapBottomLeft_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.BottomLeft);
        private void SnapBottomCenter_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.BottomCenter);
        private void SnapBottomRight_Click(object sender, RoutedEventArgs e) => _mainWindow.SnapTo(SnapPosition.BottomRight);

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
            _mainWindow.ResetSizeToScreenDefault();
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to restore all settings back to factory defaults?\nYour custom styles and positions will be reset.",
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

                _mainWindow.ApplySettings(SettingsManager.Current.Settings);
                _mainWindow.ResetSizeToScreenDefault();
            }
        }

        private bool _isSaved = false;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _isSaved = true;

            // Update placement from current window position
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

            SettingsManager.Current.UpdateSettings(_tempSettings);
            _mainWindow.ApplySettings(_tempSettings);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Revert live preview back to original placement and settings
            _mainWindow.RestorePlacement(_originalSettings.Placement);
            _mainWindow.ApplySettings(_originalSettings);
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (!_isSaved)
            {
                _mainWindow.RestorePlacement(_originalSettings.Placement);
                _mainWindow.ApplySettings(_originalSettings);
            }
        }
    }
}
