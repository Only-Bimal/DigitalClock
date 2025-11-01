// Ignore Spelling: HSB

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DigitalClock
{
    public static class ColorUtilities
    {
        /// <summary>
        /// From a given color it works out a suitable color that will sit on top of
        /// it so that the contrast is suitable for readability.
        /// </summary>
        /// <param name="baseColor">Color to get the contrasting complement of</param>
        /// <returns>Contrasting color</returns>
        public static Color GetContrastingColor(this Color baseColor)
        {
            HSB baseHsb = ColorToHsb(baseColor);
            int newSaturation = baseHsb.Saturation;
            int newBrightness = baseHsb.Brightness;

            if ((baseHsb.Saturation >= 40 && baseHsb.Saturation <= 60) && (baseHsb.Brightness >= 40 && baseHsb.Brightness <= 60))
            {
                newSaturation = (baseHsb.Saturation <= 50 ? 100 : 0);
                newBrightness = (baseHsb.Brightness <= 50 ? 100 : 0);
            }
            else if (baseHsb.Saturation >= 40 && baseHsb.Saturation <= 60)
            {
                newSaturation = (baseHsb.Saturation <= 50 ? 100 : 0);
            }
            else if (baseHsb.Brightness >= 40 && baseHsb.Brightness <= 60)
            {
                newBrightness = (baseHsb.Brightness <= 50 ? 100 : 0);
            }
            else
            {
                newSaturation = 100 - baseHsb.Saturation;
                newBrightness = 100 - baseHsb.Brightness;
            }

            if (baseHsb.Saturation == 0)
            {
                newSaturation = 0;
            }

            HSB newHsb = new HSB(baseHsb.Hue, newSaturation, newBrightness);
            return HsbToColor(newHsb);
        }
        // Add the following helper methods and the HSB struct to resolve the errors related to 'ColorToHsb' and 'HSB'.

        /// <summary>
        /// Converts a Color to an HSB representation.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>An HSB representation of the color.</returns>
        private static HSB ColorToHsb(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue = 0;
            if (delta > 0)
            {
                if (max == r)
                {
                    hue = (g - b) / delta;
                }
                else if (max == g)
                {
                    hue = 2 + (b - r) / delta;
                }
                else
                {
                    hue = 4 + (r - g) / delta;
                }
                hue *= 60;
                if (hue < 0) hue += 360;
            }

            double saturation = max == 0 ? 0 : (delta / max) * 100;
            double brightness = max * 100;

            return new HSB((int)hue, (int)saturation, (int)brightness);
        }

        /// <summary>
        /// Converts an HSB representation to a Color.
        /// </summary>
        /// <param name="hsb">The HSB representation.</param>
        /// <returns>A Color object.</returns>
        private static Color HsbToColor(HSB hsb)
        {
            double h = hsb.Hue / 360.0;
            double s = hsb.Saturation / 100.0;
            double v = hsb.Brightness / 100.0;

            int hi = (int)(h * 6) % 6;
            double f = (h * 6) - hi;
            double p = v * (1 - s);
            double q = v * (1 - f * s);
            double t = v * (1 - (1 - f) * s);

            double r = 0, g = 0, b = 0;
            switch (hi)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }

            return Color.FromArgb(128, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        public static Color ToMediaColor(this System.Drawing.Color color) => Color.FromArgb(color.A, color.R, color.G, color.B);
        public static System.Drawing.Color ToDrawingColor(this Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    /// <summary>
    /// Represents a color in the HSB (Hue, Saturation, Brightness) color space.
    /// </summary>
    internal struct HSB
    {
        public int Hue { get; }
        public int Saturation { get; }
        public int Brightness { get; }

        public HSB(int hue, int saturation, int brightness)
        {
            Hue = hue;
            Saturation = saturation;
            Brightness = brightness;
        }
    }

}

