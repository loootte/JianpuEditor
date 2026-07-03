using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace JianpuEditor.Rendering
{
    public static class AppTheme
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JianpuEditor",
            "settings.json");

        public static bool IsDarkMode { get; private set; }

        public static bool FillMeasurePlaceholdersOnAdd { get; private set; } = true;

        public static event Action ThemeChanged;

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return;
                }

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonConvert.DeserializeObject<ThemeSettings>(json);
                IsDarkMode = settings?.DarkMode ?? false;
                FillMeasurePlaceholdersOnAdd = settings?.FillMeasurePlaceholdersOnAdd ?? true;
            }
            catch
            {
                IsDarkMode = false;
                FillMeasurePlaceholdersOnAdd = true;
            }
        }

        public static void SetDarkMode(bool enabled, bool persist = true)
        {
            if (IsDarkMode == enabled)
            {
                return;
            }

            IsDarkMode = enabled;
            if (persist)
            {
                Save();
            }

            ThemeChanged?.Invoke();
        }

        public static void SetFillMeasurePlaceholdersOnAdd(bool enabled, bool persist = true)
        {
            if (FillMeasurePlaceholdersOnAdd == enabled)
            {
                return;
            }

            FillMeasurePlaceholdersOnAdd = enabled;
            if (persist)
            {
                Save();
            }
        }

        public static Color FormBackground
        {
            get { return IsDarkMode ? Color.FromArgb(32, 32, 36) : SystemColors.Control; }
        }

        public static Color FormForeground
        {
            get { return IsDarkMode ? Color.FromArgb(230, 230, 230) : SystemColors.ControlText; }
        }

        public static Color InputBackground
        {
            get { return IsDarkMode ? Color.FromArgb(45, 45, 50) : SystemColors.Window; }
        }

        public static Color InputForeground
        {
            get { return IsDarkMode ? Color.FromArgb(235, 235, 235) : SystemColors.WindowText; }
        }

        public static Color CanvasChrome
        {
            get { return IsDarkMode ? Color.FromArgb(28, 28, 32) : Color.FromArgb(245, 245, 245); }
        }

        public static Color ScorePaper
        {
            get { return IsDarkMode ? Color.FromArgb(24, 24, 28) : Color.White; }
        }

        public static Color PrimaryText
        {
            get { return IsDarkMode ? Color.FromArgb(230, 230, 230) : Color.Black; }
        }

        public static Color SecondaryText
        {
            get { return IsDarkMode ? Color.FromArgb(170, 170, 175) : Color.DimGray; }
        }

        public static Color TieActive
        {
            get { return Color.FromArgb(255, 41, 98, 255); }
        }

        public static Color TieInactive
        {
            get { return IsDarkMode ? Color.FromArgb(200, 200, 205) : Color.Black; }
        }

        public static Color ChordBackground
        {
            get { return IsDarkMode ? Color.FromArgb(40, 40, 48) : Color.FromArgb(248, 248, 252); }
        }

        public static Color ChordSelectedBackground
        {
            get { return IsDarkMode ? Color.FromArgb(55, 52, 38) : Color.FromArgb(255, 255, 240); }
        }

        public static Color ChordBorder
        {
            get { return IsDarkMode ? Color.FromArgb(120, 120, 135) : Color.FromArgb(180, 160, 174, 192); }
        }

        public static Color ChordSelectedBorder
        {
            get { return Color.FromArgb(220, 41, 98, 255); }
        }

        public static Color InlineEditorBackground
        {
            get { return IsDarkMode ? Color.FromArgb(55, 52, 38) : Color.FromArgb(255, 255, 240); }
        }

        public static Color TieModeButtonBackground
        {
            get { return IsDarkMode ? Color.FromArgb(70, 65, 35) : Color.FromArgb(255, 255, 200); }
        }

        public static Color Separator
        {
            get { return IsDarkMode ? Color.FromArgb(70, 70, 78) : Color.LightGray; }
        }

        public static Color GetScoreBackground(bool respectTheme)
        {
            return respectTheme && IsDarkMode ? ScorePaper : Color.White;
        }

        private static void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(
                    new ThemeSettings
                    {
                        DarkMode = IsDarkMode,
                        FillMeasurePlaceholdersOnAdd = FillMeasurePlaceholdersOnAdd
                    },
                    Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Theme persistence is best-effort.
            }
        }

        private sealed class ThemeSettings
        {
            public bool DarkMode { get; set; }

            public bool FillMeasurePlaceholdersOnAdd { get; set; } = true;
        }
    }
}
