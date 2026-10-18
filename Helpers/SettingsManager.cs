using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace DigitalClock
{
    public class SettingsManager
    {
        private static readonly Lazy<SettingsManager> _lazy = new(() => new SettingsManager());
        public static SettingsManager Current => _lazy.Value;

        private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "DigitalClock";

        public AppSettings Settings { get; private set; } = new();
        public string ConfigFilePath { get; private set; } = string.Empty;
        public bool IsPortableMode { get; private set; }

        public event Action<AppSettings>? SettingsChanged;

        private SettingsManager()
        {
            DetermineConfigPath();
            Load();
        }

        private void DetermineConfigPath()
        {
            string appDir = AppContext.BaseDirectory;
            if (HasWriteAccessToDirectory(appDir))
            {
                ConfigFilePath = Path.Combine(appDir, "settings.json");
                IsPortableMode = true;
            }
            else
            {
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DigitalClock");
                ConfigFilePath = Path.Combine(appDataDir, "settings.json");
                IsPortableMode = false;
            }
        }

        private static bool HasWriteAccessToDirectory(string directoryPath)
        {
            try
            {
                string testFile = Path.Combine(directoryPath, $".perm_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        Settings = loaded;
                        return;
                    }
                }
            }
            catch
            {
                // Fall through to migration/default
            }

            // If file does not exist, try importing legacy registry values if present
            Settings = new AppSettings();
            TryMigrateLegacyRegistrySettings(Settings);
            Save();
        }

        private static void TryMigrateLegacyRegistrySettings(AppSettings settings)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\DigitalClock\Release") 
                             ?? Registry.CurrentUser.OpenSubKey(@"Software\DigitalClock\Debug");
                if (key != null)
                {
                    var onTopVal = key.GetValue("OnTop");
                    if (onTopVal != null) settings.Behavior.AlwaysOnTop = Convert.ToBoolean(onTopVal);

                    var notifyVal = key.GetValue("NotifyIconVisible");
                    if (notifyVal != null) settings.Behavior.ShowTrayIcon = Convert.ToBoolean(notifyVal);

                    var leftVal = key.GetValue("Left");
                    if (leftVal != null) settings.Placement.Left = Convert.ToDouble(leftVal);

                    var topVal = key.GetValue("Top");
                    if (topVal != null) settings.Placement.Top = Convert.ToDouble(topVal);

                    var widthVal = key.GetValue("Width");
                    if (widthVal != null) settings.Placement.Width = Convert.ToDouble(widthVal);

                    var heightVal = key.GetValue("Height");
                    if (heightVal != null) settings.Placement.Height = Convert.ToDouble(heightVal);
                }

                // Check Run key
                using var runKey = Registry.CurrentUser.OpenSubKey(RunRegistryKey);
                if (runKey?.GetValue(AppName) != null || runKey?.GetValue("DigitalClock\\Release") != null)
                {
                    settings.Behavior.StartWithWindows = true;
                }
            }
            catch
            {
                // Silently ignore migration issues
            }
        }

        public void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(ConfigFilePath, json);

                // Synchronize Windows Startup Run key
                SyncWindowsStartup(Settings.Behavior.StartWithWindows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            Settings = newSettings;
            Save();
            NotifySettingsChanged();
        }

        public void NotifySettingsChanged()
        {
            SettingsChanged?.Invoke(Settings);
        }

        public void ResetToDefaults()
        {
            Settings = new AppSettings();
            Save();
            NotifySettingsChanged();
        }

        public static void SyncWindowsStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DigitalClock.exe");
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    key.DeleteValue("DigitalClock\\Release", false);
                    key.DeleteValue("DigitalClock\\Debug", false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to update Windows startup: {ex.Message}");
            }
        }
    }
}
