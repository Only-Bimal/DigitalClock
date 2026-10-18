using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using static DigitalClock.NativeMethods;

namespace DigitalClock
{
    public enum ClockBackgroundStyle
    {
        Transparent = 0,
        Acrylic = 1,
        DarkFrostedPill = 2,
        LightFrostedPill = 3
    }

    public static class AeroGlassService
    {
        // Configurable blur background properties (BGR color format matching user sample)
        public static uint BlurOpacity { get; set; } = 0x40;
        public static uint BlurBackgroundColor { get; set; } = 0x1E1E1E; /* BGR color format */

        public static void ApplyAcrylic(Window window, bool enable)
        {
            var windowHelper = new WindowInteropHelper(window);
            if (windowHelper.Handle == IntPtr.Zero) return;

            try
            {
                if (enable)
                {
                    EnableBlur(windowHelper.Handle);
                    ApplyRoundedWindowRegion(window, 18);
                }
                else
                {
                    DisableBlur(windowHelper.Handle);
                    ClearWindowRegion(window);
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        internal static void EnableBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy
            {
                // Use ACCENT_ENABLE_BLURBEHIND as specified in user's implementation
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                GradientColor = (int)((BlurOpacity << 24) | (BlurBackgroundColor & 0xFFFFFF))
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        internal static void DisableBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED,
                GradientColor = 0
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public static void RefreshAcrylic(Window window)
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero) return;

            try
            {
                SetWindowPos(helper.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER |
                    SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
            catch { }
        }

        public static void ApplyRoundedWindowRegion(Window window, double cornerRadius)
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero) return;

            try
            {
                var dpi = VisualTreeHelper.GetDpi(window);
                double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                int w = (int)Math.Ceiling(window.ActualWidth * dpiX);
                int h = (int)Math.Ceiling(window.ActualHeight * dpiY);
                int r = (int)Math.Round(cornerRadius * Math.Max(dpiX, dpiY));

                if (w > 0 && h > 0)
                {
                    IntPtr hRgn = CreateRoundRectRgn(0, 0, w + 1, h + 1, r * 2, r * 2);
                    SetWindowRgn(helper.Handle, hRgn, true);
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        public static void ClearWindowRegion(Window window)
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero) return;

            try
            {
                SetWindowRgn(helper.Handle, IntPtr.Zero, true);
            }
            catch
            {
                // Fallback gracefully
            }
        }

        // Backward compatibility helpers
        public static void ApplyAeroGlass(Window window, bool enable) => ApplyAcrylic(window, enable);
        public static void RefreshAeroGlass(Window window, double cornerRadius) => RefreshAcrylic(window);
    }
}
