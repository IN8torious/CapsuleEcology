using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CapsuleEcology
{
    /// <summary>
    /// The v2 look, natively (2026-11-03): appearance presets +
    /// a per-user accent, persisted in
    /// %LocalAppData%\CapsuleEcology\settings.json.
    /// Presets are Nathan's preview v17 token sets: Obsidian
    /// (warm dark, the Dark theme dictionary), Paper (warm light,
    /// the Light theme dictionary), and Slate (cool dark — applied
    /// as root resource overrides over the Dark dictionary).
    /// One accent hex drives the accent brush family, exactly as
    /// the preview's picker did: accent, accent-dark (×0.76),
    /// rose tint (lerp toward white 0.45), and the line shade
    /// (×0.45). Switching applies to the window root; ThemeResource
    /// lookups below it follow. Content viewports (the world, the
    /// paintings) are not themed — chrome only, per the theme law.
    /// </summary>
    public static class ThemeService
    {
        public const string Obsidian = "Obsidian";
        public const string Slate = "Slate";
        public const string Paper = "Paper";
        public const string DefaultAccent = "#D9777F";

        public static readonly string[] AccentSwatches =
            { "#D9777F", "#DFAE52", "#6AA8D9", "#9A7FD9", "#D96F4E", "#7FAE7A" };

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CapsuleEcology", "settings.json");

        public static string Appearance { get; private set; } = Obsidian;
        public static string Accent { get; private set; } = DefaultAccent;

        private static bool _loaded;
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(SettingsPath)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
                if (doc.RootElement.TryGetProperty("appearance", out var a)) Appearance = a.GetString() ?? Obsidian;
                if (doc.RootElement.TryGetProperty("accent", out var c)) Accent = c.GetString() ?? DefaultAccent;
            }
            catch { /* a broken settings file falls back to defaults — never a crash */ }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(new { appearance = Appearance, accent = Accent }));
            }
            catch { /* persistence is best-effort; the live switch already applied */ }
        }

        public static void SetAppearance(string appearance) { Load(); Appearance = appearance; Save(); }
        public static void SetAccent(string hex) { Load(); Accent = hex; Save(); }

        /// <summary>Apply the persisted choice to a window root element.</summary>
        public static void Apply(FrameworkElement? root)
        {
            if (root is null) return;
            Load();
            root.RequestedTheme = Appearance == Paper ? ElementTheme.Light : ElementTheme.Dark;

            if (Appearance == Slate)
            {
                SetBrush(root, "CapsuleBgBrush", "#12101A");
                SetBrush(root, "CapsulePanelBrush", "#1A1724");
                SetBrush(root, "CapsulePanel2Brush", "#211D2E");
                SetBrush(root, "CapsuleInkBrush", "#E9E4F2");
                SetBrush(root, "CapsuleDimBrush", "#A79FC0");
                SetBrush(root, "CapsuleFaintBrush", "#6F6A8C");
                SetBrush(root, "CapsuleLineBrush", "#353050");
                SetBrush(root, "CapsuleGoldBrush", "#DFAE52");
            }
            else
            {
                // Back to the theme dictionaries: drop any Slate overrides.
                foreach (var key in new[] { "CapsuleBgBrush", "CapsulePanelBrush", "CapsulePanel2Brush",
                                            "CapsuleInkBrush", "CapsuleDimBrush", "CapsuleFaintBrush",
                                            "CapsuleLineBrush", "CapsuleGoldBrush" })
                    root.Resources.Remove(key);
            }

            var accent = ParseHex(Accent);
            SetBrushColor(root, "CapsuleAccentBrush", accent);
            SetBrushColor(root, "CapsuleAccentDarkBrush", Scale(accent, 0.76));
            SetBrushColor(root, "CapsuleRoseBrush", Lerp(accent, ParseHex("#FFFFFF"), 0.45));
            if (Appearance != Slate)
                SetBrushColor(root, "CapsuleLineBrush", Scale(accent, 0.45));
        }

        /// <summary>Apply to the app's main window content, if it exists yet.</summary>
        public static void ApplyToWindow()
        {
            if (App.WindowRef?.Content is FrameworkElement root) Apply(root);
        }

        private static void SetBrush(FrameworkElement root, string key, string hex)
            => SetBrushColor(root, key, ParseHex(hex));

        private static void SetBrushColor(FrameworkElement root, string key, Color color)
            => root.Resources[key] = new SolidColorBrush(color);

        public static Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private static Color Scale(Color c, double f) => Color.FromArgb(255,
            (byte)Math.Round(c.R * f), (byte)Math.Round(c.G * f), (byte)Math.Round(c.B * f));

        private static Color Lerp(Color a, Color b, double t) => Color.FromArgb(255,
            (byte)Math.Round(a.R + (b.R - a.R) * t),
            (byte)Math.Round(a.G + (b.G - a.G) * t),
            (byte)Math.Round(a.B + (b.B - a.B) * t));
    }
}
