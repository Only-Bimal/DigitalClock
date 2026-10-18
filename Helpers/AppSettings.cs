using System;

namespace DigitalClock
{
    public enum ColorSelectionMode
    {
        WindowsAccent,
        Preset,
        Custom
    }

    public class GeneralSettings
    {
        public bool Is24HourFormat { get; set; } = true;
        public bool ShowSeconds { get; set; } = false;
        public bool BlinkColon { get; set; } = false;
        public bool ShowDate { get; set; } = true;
        public string DateFormat { get; set; } = "dddd, dd MMMM yyyy";
        public string TimezoneId { get; set; } = "Local";
    }

    public class AppearanceSettings
    {
        public ColorSelectionMode ColorMode { get; set; } = ColorSelectionMode.WindowsAccent;
        public string CustomHexColor { get; set; } = "#00D4FF";
        public double Opacity { get; set; } = 0.95;
        public double Scale { get; set; } = 1.0;
        public string FontFamily { get; set; } = "DSEG7 Classic";
        public bool EnableDropShadow { get; set; } = true;
        public ClockBackgroundStyle BackgroundStyle { get; set; } = ClockBackgroundStyle.Transparent;
    }

    public class BehaviorSettings
    {
        public bool AlwaysOnTop { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public bool LockPosition { get; set; } = false;
        public bool ClickThrough { get; set; } = false;
        public bool ShowTrayIcon { get; set; } = true;
    }

    public class WindowPlacementSettings
    {
        public double? Left { get; set; }
        public double? Top { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public int TargetScreenIndex { get; set; } = -1; // -1 means auto / primary
    }

    public class AppSettings
    {
        public GeneralSettings General { get; set; } = new();
        public AppearanceSettings Appearance { get; set; } = new();
        public BehaviorSettings Behavior { get; set; } = new();
        public WindowPlacementSettings Placement { get; set; } = new();

        public AppSettings Clone()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
    }
}
