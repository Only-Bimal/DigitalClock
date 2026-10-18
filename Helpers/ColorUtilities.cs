using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DigitalClock
{
    public static class ColorUtilities
    {
        public static readonly List<string> ColorPresets = new()
        {
            "#00E5FF", // Neon Cyan
            "#00E676", // Emerald Green
            "#FFD600", // Amber Gold
            "#FFFFFF", // Pure White
            "#D500F9", // Neon Violet
            "#FF1744", // Electric Coral
            "#FF9100", // Sunset Orange
            "#40C4FF"  // Sky Blue
        };

        public static Color GetWindowsAccentColor()
        {
            if (SystemParameters.WindowGlassBrush is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Color.FromRgb(0, 120, 215); // Default Windows Blue
        }

        public static Color ParseHexColor(string? hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;

            string clean = hex.Trim().TrimStart('#');
            try
            {
                if (clean.Length == 6)
                {
                    byte r = byte.Parse(clean.Substring(0, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(clean.Substring(2, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(clean.Substring(4, 2), NumberStyles.HexNumber);
                    return Color.FromRgb(r, g, b);
                }
                if (clean.Length == 8)
                {
                    byte a = byte.Parse(clean.Substring(0, 2), NumberStyles.HexNumber);
                    byte r = byte.Parse(clean.Substring(2, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(clean.Substring(4, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(clean.Substring(6, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch
            {
                // Fallback on invalid format
            }

            return fallback;
        }

        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static double GetRelativeLuminance(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
            /*
             * The hardcoded numbers 0.2126, 0.7152, and 0.0722 are not arbitrary constants. 
             * They represent the perceptual contribution of each color channel to the overall brightness of a color, based on the sRGB color space and the standard D65 white point.
             * 1. What each number represents
             * Constant Color   channel Contribution
             * 0.2126	Red     (r)	    21.26%
             * 0.7152	Green   (g)     71.52%
             * 0.0722	Blue    (b)	    7.22%
             * Total		            100%
             */
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        public static Color EnsureVisibleOnDark(Color color, double minLuminance = 0.45)
        {
            double lum = GetRelativeLuminance(color);
            if (lum >= minLuminance) return color;

            RgbToHsl(color, out double h, out double s, out double l);
            // Boost lightness while keeping hue vibrant
            l = Math.Max(l, 0.72);
            s = Math.Max(s, 0.60);
            return HslToRgb(h, s, l);
        }

        public static void RgbToHsl(Color c, out double h, out double s, out double l)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

                if (max == r)
                    h = ((g - b) / delta + (g < b ? 6.0 : 0.0)) / 6.0;
                else if (max == g)
                    h = ((b - r) / delta + 2.0) / 6.0;
                else
                    h = ((r - g) / delta + 4.0) / 6.0;
            }
        }

        public static Color HslToRgb(double h, double s, double l)
        {
            byte r, g, b;

            if (s == 0)
            {
                r = g = b = (byte)(l * 255);
            }
            else
            {
                double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
                double p = 2.0 * l - q;

                r = (byte)Math.Round(HueToRgb(p, q, h + 1.0 / 3.0) * 255);
                g = (byte)Math.Round(HueToRgb(p, q, h) * 255);
                b = (byte)Math.Round(HueToRgb(p, q, h - 1.0 / 3.0) * 255);
            }

            return Color.FromRgb(r, g, b);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1.0;
            if (t > 1) t -= 1.0;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }
    }
}
