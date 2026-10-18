using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DigitalClock
{
    public class FontOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public string FontFamilyKey { get; set; } = string.Empty;
        public bool IsCurated { get; set; }

        public override string ToString() => DisplayName;
    }

    public static class FontService
    {
        public const string BundledDigitalKey = "DSEG7 Classic";
        public const string BundledTechnologyKey = "Technology";
        private static readonly FontFamily BundledDigitalFont = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/Fonts/#DSEG7 Classic");
        private static readonly FontFamily BundledTechnologyFont = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/Fonts/#Technology");

        public static List<FontOption> GetAvailableFonts()
        {
            var list = new List<FontOption>
            {
                new FontOption { DisplayName = "Digital 7-Segment (Bundled)", FontFamilyKey = BundledDigitalKey, IsCurated = true },
                new FontOption { DisplayName = "Technology (Bundled)", FontFamilyKey = BundledTechnologyFont.Source, IsCurated = true },
                new FontOption { DisplayName = "Segoe UI Variable (System)", FontFamilyKey = "Segoe UI Variable Display", IsCurated = true },
                new FontOption { DisplayName = "Segoe UI (System)", FontFamilyKey = "Segoe UI", IsCurated = true },
                new FontOption { DisplayName = "Bahnschrift (System)", FontFamilyKey = "Bahnschrift", IsCurated = true },
                new FontOption { DisplayName = "Cascadia Code (System)", FontFamilyKey = "Cascadia Code", IsCurated = true },
                new FontOption { DisplayName = "Consolas (System)", FontFamilyKey = "Consolas", IsCurated = true },
                new FontOption { DisplayName = "Arial (System)", FontFamilyKey = "Arial", IsCurated = true }
            };

            // Add all other system font families sorted alphabetically
            try
            {
                var existingKeys = new HashSet<string>(list.Select(x => x.FontFamilyKey), StringComparer.OrdinalIgnoreCase);
                var systemFonts = Fonts.SystemFontFamilies
                    .Select(f => f.Source)
                    .Where(s => !string.IsNullOrWhiteSpace(s) && !existingKeys.Contains(s))
                    .OrderBy(s => s)
                    .Select(s => new FontOption { DisplayName = s, FontFamilyKey = s, IsCurated = false });

                list.AddRange(systemFonts);
            }
            catch
            {
                // Fallback gracefully if system font querying encounters restrictions
            }

            return list;
        }

        public static FontFamily ResolveFontFamily(string? fontKey)
        {
            if (string.IsNullOrWhiteSpace(fontKey) ||
                string.Equals(fontKey, BundledDigitalKey, StringComparison.OrdinalIgnoreCase) ||
                fontKey.Contains("DSEG7", StringComparison.OrdinalIgnoreCase) ||
                fontKey.Contains("Digital", StringComparison.OrdinalIgnoreCase))
            {
                return BundledDigitalFont;
            }
            else if (string.Equals(fontKey, BundledTechnologyKey, StringComparison.OrdinalIgnoreCase) ||
                fontKey.Contains("Technology", StringComparison.OrdinalIgnoreCase))
            {
                return BundledTechnologyFont;
            }

            try
            {
                return new FontFamily(fontKey);
            }
            catch
            {
                return new FontFamily("Segoe UI");
            }
        }
    }
}
