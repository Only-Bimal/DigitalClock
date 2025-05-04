using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using MessageBox = System.Windows.MessageBox;

namespace DigitalClock
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string OnTop = "OnTop";
        private const string DigitalClock = "DigitalClock";
        private bool _isExit;
        private string runLocation = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private string appSettingsLocation = @"Software\DigitalClock";
        private static SolidColorBrush AccentColor => (SolidColorBrush)SystemParameters.WindowGlassBrush;

        public MainWindow()
        {
            InitializeComponent();
            // Set Location
            SetLocation();
            // configure clock
            _clock = new Thread(() =>
            {
                while (true)
                {
                    // update date and time by Realtime
                    ClockTimes.Dispatcher.Invoke(new Action(() => ClockTimes.Content = DateTime.Now.ToString("HH:mm")));
                    ClockDates.Dispatcher.Invoke(new Action(() => ClockDates.Content = DateTime.Now.ToString("dddd, dd MMMM yyyy")));

                    ClockTimes.Dispatcher.Invoke(new Action(() => ClockTimes.Foreground = AccentColor));
                    ClockDates.Dispatcher.Invoke(new Action(() => ClockDates.Foreground = AccentColor));

                    Thread.Sleep(1000); // sleep
                    if (_isExit) { return; }
                }
            });
            // start digital clock
            _clock.Start();

            // do not make this program crash
            try
            {
                // Auto Start with Windows
                using (var key = Registry.CurrentUser.CreateSubKey(runLocation, true))
                {
                    StartWithWindows.IsChecked = (key?.GetValue(DigitalClock, null) != null);
                }
                // Display on top
                using (var key = Registry.CurrentUser.CreateSubKey(appSettingsLocation, true))
                {
                    DisplayOnTop.IsChecked = Convert.ToBoolean(key?.GetValue(OnTop, "False"));
                    Topmost = DisplayOnTop.IsChecked == true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show("Unable to set start-up value to true.\n" + ex.Message);
            }
        }

        // create new thread variable
        private readonly Thread _clock;

        // drag the digital clock
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // close context menu
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _isExit = true;
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
                return;
            }

            ClockTimes.Foreground = accentColor;
            ClockDates.Foreground = accentColor;
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

            _isExit = true;
            _clock.Abort();
        }

        private void SetLocation()
        {
            Screen display;
            var screens = Screen.AllScreens;
            var firstSecondary = screens.FirstOrDefault(s => s.Primary == false);
            if (firstSecondary != null)
            {
                display = firstSecondary;
            }
            else
            {
                display = screens.First();
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            // Ensure Window is minimized on creation
            WindowState = WindowState.Minimized;
            // Define Position on Secondary screen, for "Normal" window-mode
            Left = (display.Bounds.Left / 2) - (Width / 2) - 50;
            Top = (display.Bounds.Height / 2) - (Height / 2) - 100;

            //Get location from registry.
            double top = (double)Registry.CurrentUser.GetValue(appSettingsLocation + "\\Top", Top);
            double left = (double)Registry.CurrentUser.GetValue(appSettingsLocation + "\\Left", Left);
            double width = (double)Registry.CurrentUser.GetValue(appSettingsLocation + "\\Width", Width);
            double height = (double)Registry.CurrentUser.GetValue(appSettingsLocation + "\\Height", Height);
            //Set the location and size
            Top = top;
            Left = left;
            Width = width;
            Height = height;

            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }
    }
}