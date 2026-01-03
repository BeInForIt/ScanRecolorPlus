using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;

namespace hoppinhauler.ScanRecolorRework
{
    internal enum ColorMode
    {
        Preset = 0,
        RGB = 1,
        HSV = 2
    }

    internal enum RandomMode
    {
        Off = 0,
        Full = 1,
        HueOnly = 2,
        Palette = 3,
        Gradient = 4
    }

    internal enum FadeCurve
    {
        Linear = 0,
        EaseIn = 1,
        EaseOut = 2,
        EaseInOut = 3,
        Exponential = 4
    }

    internal static class ModConfig
    {
        // General
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ResetOnSceneLoad;

        // Base color
        public static ConfigEntry<ColorMode> BaseColorMode;
        public static ConfigEntry<string> Preset;
        public static ConfigEntry<int> Red;
        public static ConfigEntry<int> Green;
        public static ConfigEntry<int> Blue;

        public static ConfigEntry<float> Hue;
        public static ConfigEntry<float> Saturation;
        public static ConfigEntry<float> Value;

        public static ConfigEntry<float> Alpha;
        public static ConfigEntry<float> MaxBrightness;

        // Random / Gradient
        public static ConfigEntry<RandomMode> RandomModePerScan;

        public static ConfigEntry<bool> RandomFixedAlpha;
        public static ConfigEntry<float> RandomAlpha;
        public static ConfigEntry<float> RandomAlphaMin;
        public static ConfigEntry<float> RandomAlphaMax;

        public static ConfigEntry<string> RandomPalette;
        public static ConfigEntry<float> RandomHueJitter;
        public static ConfigEntry<float> RandomSVJitter;

        public static ConfigEntry<string> GradientA;
        public static ConfigEntry<string> GradientB;
        public static ConfigEntry<float> GradientSpeed;
        public static ConfigEntry<bool> GradientPingPong;

        // Fade / Pulse
        public static ConfigEntry<bool> FadeEnabled;
        public static ConfigEntry<FadeCurve> FadeCurveMode;

        public static ConfigEntry<bool> PulseEnabled;
        public static ConfigEntry<float> PulseSpeed;
        public static ConfigEntry<float> PulseStrength;

        // Vignette
        public static ConfigEntry<bool> VignetteEnabled;
        public static ConfigEntry<float> VignetteIntensity;
        public static ConfigEntry<bool> VignetteUseScanColor;
        public static ConfigEntry<string> VignetteColor;
        public static ConfigEntry<float> VignetteAlpha;

        // Bloom
        public static ConfigEntry<bool> BloomEnabled;
        public static ConfigEntry<float> BloomTintStrength;
        public static ConfigEntry<bool> BloomUseScanColor;
        public static ConfigEntry<string> BloomColor;
        public static ConfigEntry<float> BloomAlpha;

        // ScanLines (Bloom dirt texture)
        public static ConfigEntry<bool> ScanLinesEnabled;
        public static ConfigEntry<bool> RecolorScanLines;
        public static ConfigEntry<float> ScanLinesRecolorStrength;
        public static ConfigEntry<float> ScanLinesMinLuma;
        public static ConfigEntry<float> ScanLinesMinAlpha;

        private static readonly Dictionary<string, string> Presets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cyan",    "#00E5FFFF" },
            { "Green",   "#00FF55FF" },
            { "Red",     "#FF2A2AFF" },
            { "Purple",  "#B000FFFF" },
            { "Amber",   "#FFB000FF" },
            { "White",   "#FFFFFFFF" },
            { "Pink",    "#FF4FD8FF" },
            { "Default", "#000CFFFF" },
        };

        public static void Bind(ConfigFile cfg)
        {
            // General
            Enabled = cfg.Bind("General", "Enabled", true,
                "Enable/disable the mod.");

            ResetOnSceneLoad = cfg.Bind("General", "ResetOnSceneLoad", true,
                "Reset cached references when a scene loads (recommended).");

            // Base color
            BaseColorMode = cfg.Bind("Color", "Mode", ColorMode.Preset,
                "Base color mode: Preset / RGB / HSV.");

            Preset = cfg.Bind("Color", "Preset", "White",
                "Preset name (Cyan, Green, Red, Purple, Amber, White) or hex (#RRGGBB / #RRGGBBAA).");

            Red = cfg.Bind("Color.RGB", "Red", 0,
                new ConfigDescription("Red channel (0..255).",
                    new AcceptableValueRange<int>(0, 255), new object[0]));

            Green = cfg.Bind("Color.RGB", "Green", 229,
                new ConfigDescription("Green channel (0..255).",
                    new AcceptableValueRange<int>(0, 255), new object[0]));

            Blue = cfg.Bind("Color.RGB", "Blue", 255,
                new ConfigDescription("Blue channel (0..255).",
                    new AcceptableValueRange<int>(0, 255), new object[0]));

            Hue = cfg.Bind("Color.HSV", "Hue", 190f,
                new ConfigDescription("Hue (0..360).",
                    new AcceptableValueRange<float>(0f, 360f), new object[0]));

            Saturation = cfg.Bind("Color.HSV", "Saturation", 1f,
                new ConfigDescription("Saturation (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            Value = cfg.Bind("Color.HSV", "Value", 1f,
                new ConfigDescription("Value/Brightness (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            Alpha = cfg.Bind("Color", "Alpha", 0.5636086f,
                new ConfigDescription("Scan overlay alpha/opacity (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            MaxBrightness = cfg.Bind("Color", "MaxBrightness", 1f,
                new ConfigDescription("Clamp overall brightness (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            // Random / Gradient
            RandomModePerScan = cfg.Bind("Random", "Mode", RandomMode.HueOnly,
                "Random color per scan: Off / Full / HueOnly / Palette / Gradient.");

            RandomFixedAlpha = cfg.Bind("Random", "FixedAlpha", true,
                "Use fixed alpha for random colors (otherwise AlphaMin..AlphaMax).");

            RandomAlpha = cfg.Bind("Random", "Alpha", 0.26f,
                new ConfigDescription("Fixed random alpha (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            RandomAlphaMin = cfg.Bind("Random", "AlphaMin", 0.26f,
                new ConfigDescription("Min random alpha when FixedAlpha=false (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            RandomAlphaMax = cfg.Bind("Random", "AlphaMax", 0.65f,
                new ConfigDescription("Max random alpha when FixedAlpha=false (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            RandomPalette = cfg.Bind("Random", "Palette", "Cyan, Purple, Green, Amber",
                "Palette for Random=Palette. Items can be preset names or hex.");

            RandomHueJitter = cfg.Bind("Random", "HueJitter", 9.65953f,
                new ConfigDescription("Hue jitter in degrees for HueOnly (0..180).",
                    new AcceptableValueRange<float>(0f, 180f), new object[0]));

            RandomSVJitter = cfg.Bind("Random", "SVJitter", 0.02869597f,
                new ConfigDescription("Small S/V jitter for HueOnly (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            GradientA = cfg.Bind("Gradient", "ColorA", "Cyan",
                "Gradient color A (preset or hex).");

            GradientB = cfg.Bind("Gradient", "ColorB", "Purple",
                "Gradient color B (preset or hex).");

            GradientSpeed = cfg.Bind("Gradient", "Speed", 0.6f,
                new ConfigDescription("Gradient speed in cycles per second (0..5).",
                    new AcceptableValueRange<float>(0f, 5f), new object[0]));

            GradientPingPong = cfg.Bind("Gradient", "PingPong", true,
                "Ping-pong between A and B instead of looping.");

            // Fade / Pulse
            FadeEnabled = cfg.Bind("Fade", "Enabled", false,
                "Enable fade alpha animation during scan.");

            FadeCurveMode = cfg.Bind("Fade", "Curve", FadeCurve.EaseOut,
                "Fade curve: Linear / EaseIn / EaseOut / EaseInOut / Exponential.");

            PulseEnabled = cfg.Bind("Animation", "Pulse", true,
                "Enable alpha pulsing during scan.");

            PulseSpeed = cfg.Bind("Animation", "PulseSpeed", 3.139388f,
                new ConfigDescription("Pulse frequency in Hz (0..20).",
                    new AcceptableValueRange<float>(0f, 20f), new object[0]));

            PulseStrength = cfg.Bind("Animation", "PulseStrength", 0.2991548f,
                new ConfigDescription("Pulse strength (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            // Vignette
            VignetteEnabled = cfg.Bind("Vignette", "Enabled", true,
                "Control scan vignette settings.");

            VignetteIntensity = cfg.Bind("Vignette", "Intensity", 0.7582871f,
                new ConfigDescription("Vignette intensity (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            VignetteUseScanColor = cfg.Bind("Vignette", "UseScanColor", true,
                "Use scan color for vignette color.");

            VignetteColor = cfg.Bind("Vignette", "Color", "#00E5FFFF",
                "Vignette color when UseScanColor=false (preset or hex).");

            VignetteAlpha = cfg.Bind("Vignette", "Alpha", 1f,
                new ConfigDescription("Vignette alpha when UseScanColor=false (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            // Bloom
            BloomEnabled = cfg.Bind("Bloom", "Enabled", true,
                "Control scan bloom tint settings.");

            BloomTintStrength = cfg.Bind("Bloom", "TintStrength", 1.511349f,
                new ConfigDescription("Bloom tint multiplier (0..2).",
                    new AcceptableValueRange<float>(0f, 2f), new object[0]));

            BloomUseScanColor = cfg.Bind("Bloom", "UseScanColor", true,
                "Use scan color for bloom tint.");

            BloomColor = cfg.Bind("Bloom", "Color", "#00E5FFFF",
                "Bloom tint color when UseScanColor=false (preset or hex).");

            BloomAlpha = cfg.Bind("Bloom", "Alpha", 1f,
                new ConfigDescription("Bloom tint alpha (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            // ScanLines
            ScanLinesEnabled = cfg.Bind("ScanLines", "Enabled", true,
                "Enable/disable scan-lines (bloom dirt texture) handling.");

            RecolorScanLines = cfg.Bind("ScanLines", "Recolor", true,
                "Recolor scan-lines texture using the scan color.");

            ScanLinesRecolorStrength = cfg.Bind("ScanLines", "RecolorStrength", 1f,
                new ConfigDescription("How strongly to recolor scan-lines (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            ScanLinesMinLuma = cfg.Bind("ScanLines", "MinLuma", 0.05f,
                new ConfigDescription("Minimum pixel brightness threshold to recolor (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            ScanLinesMinAlpha = cfg.Bind("ScanLines", "MinAlpha", 0.05f,
                new ConfigDescription("Minimum pixel alpha threshold to recolor (0..1).",
                    new AcceptableValueRange<float>(0f, 1f), new object[0]));

            HookApplyOnChanges();
        }

        private static void HookApplyOnChanges()
        {
            Action apply = HUDManagerPatch.RequestApply;

            Enabled.SettingChanged += delegate { apply(); };
            ResetOnSceneLoad.SettingChanged += delegate { apply(); };

            BaseColorMode.SettingChanged += delegate { apply(); };
            Preset.SettingChanged += delegate { apply(); };
            Red.SettingChanged += delegate { apply(); };
            Green.SettingChanged += delegate { apply(); };
            Blue.SettingChanged += delegate { apply(); };
            Hue.SettingChanged += delegate { apply(); };
            Saturation.SettingChanged += delegate { apply(); };
            Value.SettingChanged += delegate { apply(); };
            Alpha.SettingChanged += delegate { apply(); };
            MaxBrightness.SettingChanged += delegate { apply(); };

            RandomModePerScan.SettingChanged += delegate { apply(); };
            RandomFixedAlpha.SettingChanged += delegate { apply(); };
            RandomAlpha.SettingChanged += delegate { apply(); };
            RandomAlphaMin.SettingChanged += delegate { apply(); };
            RandomAlphaMax.SettingChanged += delegate { apply(); };
            RandomPalette.SettingChanged += delegate { apply(); };
            RandomHueJitter.SettingChanged += delegate { apply(); };
            RandomSVJitter.SettingChanged += delegate { apply(); };

            GradientA.SettingChanged += delegate { apply(); };
            GradientB.SettingChanged += delegate { apply(); };
            GradientSpeed.SettingChanged += delegate { apply(); };
            GradientPingPong.SettingChanged += delegate { apply(); };

            FadeEnabled.SettingChanged += delegate { apply(); };
            FadeCurveMode.SettingChanged += delegate { apply(); };

            PulseEnabled.SettingChanged += delegate { apply(); };
            PulseSpeed.SettingChanged += delegate { apply(); };
            PulseStrength.SettingChanged += delegate { apply(); };

            VignetteEnabled.SettingChanged += delegate { apply(); };
            VignetteIntensity.SettingChanged += delegate { apply(); };
            VignetteUseScanColor.SettingChanged += delegate { apply(); };
            VignetteColor.SettingChanged += delegate { apply(); };
            VignetteAlpha.SettingChanged += delegate { apply(); };

            BloomEnabled.SettingChanged += delegate { apply(); };
            BloomTintStrength.SettingChanged += delegate { apply(); };
            BloomUseScanColor.SettingChanged += delegate { apply(); };
            BloomColor.SettingChanged += delegate { apply(); };
            BloomAlpha.SettingChanged += delegate { apply(); };

            ScanLinesEnabled.SettingChanged += delegate { apply(); };
            RecolorScanLines.SettingChanged += delegate { apply(); };
            ScanLinesRecolorStrength.SettingChanged += delegate { apply(); };
            ScanLinesMinLuma.SettingChanged += delegate { apply(); };
            ScanLinesMinAlpha.SettingChanged += delegate { apply(); };
        }

        // ---- Helpers used by HUDManagerPatch.cs ----

        internal static bool TryParseColorString(string s, out global::UnityEngine.Color color)
        {
            color = default(global::UnityEngine.Color);

            if (string.IsNullOrWhiteSpace(s))
                return false;

            string raw = s.Trim();

            string presetHex;
            if (Presets.TryGetValue(raw, out presetHex))
                raw = presetHex;

            if (raw.StartsWith("#", StringComparison.Ordinal))
                return TryParseHex(raw, out color);

            if (raw.IndexOf(",") >= 0)
                return TryParseCsv(raw, out color);

            return false;
        }

        private static bool TryParseHex(string hex, out global::UnityEngine.Color color)
        {
            color = default(global::UnityEngine.Color);

            string h = hex.Trim();
            if (!h.StartsWith("#", StringComparison.Ordinal))
                return false;

            h = h.Substring(1);
            if (h.Length != 6 && h.Length != 8)
                return false;

            try
            {
                byte r = byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte g = byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte b = byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte a = 255;

                if (h.Length == 8)
                    a = byte.Parse(h.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                color = new global::UnityEngine.Color(r / 255f, g / 255f, b / 255f, a / 255f);
                return true;
            }
            catch { return false; }
        }

        private static bool TryParseCsv(string csv, out global::UnityEngine.Color color)
        {
            color = default(global::UnityEngine.Color);

            string[] parts = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 && parts.Length != 4)
                return false;

            float[] v = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                float f;
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                    return false;
                v[i] = f;
            }

            bool as255 = v.Take(3).Any(x => x > 1.0f);

            float r = as255 ? (v[0] / 255f) : v[0];
            float g = as255 ? (v[1] / 255f) : v[1];
            float b = as255 ? (v[2] / 255f) : v[2];

            float a = 1f;
            if (parts.Length == 4)
                a = v[3] > 1.0f ? (v[3] / 255f) : v[3];

            color = new global::UnityEngine.Color(Clamp01(r), Clamp01(g), Clamp01(b), Clamp01(a));
            return true;
        }

        internal static global::UnityEngine.Color GetBaseScanColor()
        {
            global::UnityEngine.Color c;

            switch (BaseColorMode.Value)
            {
                case ColorMode.RGB:
                    c = new global::UnityEngine.Color(Red.Value / 255f, Green.Value / 255f, Blue.Value / 255f, Alpha.Value);
                    break;

                case ColorMode.HSV:
                    c = global::UnityEngine.Color.HSVToRGB(Clamp01(Hue.Value / 360f), Clamp01(Saturation.Value), Clamp01(Value.Value));
                    c.a = Alpha.Value;
                    break;

                case ColorMode.Preset:
                default:
                    if (!TryParseColorString(Preset.Value, out c))
                        c = new global::UnityEngine.Color(0f, 229f / 255f, 1f, Alpha.Value); // cyan-ish fallback
                    c.a = Alpha.Value;
                    break;
            }

            c = ApplyMaxBrightness(c, Clamp01(MaxBrightness.Value));
            return c;
        }

        internal static List<global::UnityEngine.Color> ParsePaletteOrFallback()
        {
            var list = new List<global::UnityEngine.Color>();

            string raw = RandomPalette.Value ?? string.Empty;
            string[] items = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < items.Length; i++)
            {
                global::UnityEngine.Color c;
                if (TryParseColorString(items[i].Trim(), out c))
                    list.Add(c);
            }

            if (list.Count == 0)
            {
                global::UnityEngine.Color c2;
                if (TryParseColorString("Cyan", out c2)) list.Add(c2);
                if (TryParseColorString("Purple", out c2)) list.Add(c2);
            }

            return list;
        }

        internal static float ResolveRandomAlpha()
        {
            if (RandomFixedAlpha.Value)
                return Clamp01(RandomAlpha.Value);

            float min = Clamp01(RandomAlphaMin.Value);
            float max = Clamp01(RandomAlphaMax.Value);
            if (max < min) { float t = min; min = max; max = t; }

            return global::UnityEngine.Random.Range(min, max);
        }

        internal static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        internal static global::UnityEngine.Color ApplyMaxBrightness(global::UnityEngine.Color c, float maxB)
        {
            if (maxB >= 0.999f)
                return c;

            float h, s, v;
            global::UnityEngine.Color.RGBToHSV(c, out h, out s, out v);
            v = Math.Min(v, maxB);
            global::UnityEngine.Color rgb = global::UnityEngine.Color.HSVToRGB(h, s, v);
            rgb.a = c.a;
            return rgb;
        }

        internal static float ApplyCurve(float x, FadeCurve curve)
        {
            x = Clamp01(x);

            switch (curve)
            {
                case FadeCurve.Linear:
                    return x;

                case FadeCurve.EaseIn:
                    return x * x;

                case FadeCurve.EaseOut:
                    return 1f - (1f - x) * (1f - x);

                case FadeCurve.EaseInOut:
                    return x < 0.5f
                        ? (2f * x * x)
                        : (1f - (float)Math.Pow(-2f * x + 2f, 2f) / 2f);

                case FadeCurve.Exponential:
                    return (float)Math.Pow(x, 3.0);

                default:
                    return x;
            }
        }
    }
}
