using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace hoppinhauler.ScanRecolorRework
{
    [HarmonyPatch]
    internal static class HUDManagerPatch
    {
        // Approx. scan duration in vanilla (used only for fade curve progress).
        private const float ScanDuration = 1.3f;

        // Cached HUD references
        private static global::UnityEngine.MeshRenderer _scanRenderer;

        private static Volume _scanVolume;
        private static Vignette _scanVignette;
        private static Bloom _scanBloom;

        // Cached base texture (readable copy)
        private static global::UnityEngine.Texture2D _baseScanLinesTextureReadable;

        // State for random/gradient
        private static global::UnityEngine.Color? _currentRandomColor;
        private static float _gradientT;

        // Reflection cache
        private static FieldInfo _fiPlayerPingingScan;
        private static MethodInfo _miCanPlayerScan;

        // Apply queue (config changes / scene changes)
        private static bool _applyQueued;

        // ---- Public hook for config changes ----
        internal static void RequestApply()
        {
            _applyQueued = true;
            // We can try immediate apply (safe); if HUD isn't ready, it will retry later.
            ApplyAllIfPossible();
        }

        // ---- Core reference getters (like the working mod) ----

        private static bool HasScanMaterial
        {
            get
            {
                var r = ScanRenderer;
                return r != null && r.material != null;
            }
        }

        private static global::UnityEngine.MeshRenderer ScanRenderer
        {
            get
            {
                if (_scanRenderer != null && _scanRenderer.material != null)
                    return _scanRenderer;

                var hud = HUDManager.Instance;
                if (hud == null || hud.scanEffectAnimator == null)
                    return null;

                global::UnityEngine.MeshRenderer mr;
                if (!hud.scanEffectAnimator.TryGetComponent<global::UnityEngine.MeshRenderer>(out mr))
                    return null;

                _scanRenderer = mr;
                return _scanRenderer;
            }
        }

        private static Volume ScanVolume
        {
            get
            {
                if (_scanVolume != null)
                    return _scanVolume;

                try
                {
                    // Find ScanVolume profile used by scan post-processing
                    var volumes = global::UnityEngine.Object.FindObjectsByType<Volume>((FindObjectsSortMode)0);
                    if (volumes != null)
                    {
                        _scanVolume = volumes.FirstOrDefault(v =>
                        {
                            if (v == null) return false;
                            var p = v.profile;
                            if (p == null) return false;
                            var n = ((global::UnityEngine.Object)p).name;
                            return !string.IsNullOrEmpty(n) && n.StartsWith("ScanVolume", StringComparison.OrdinalIgnoreCase);
                        });
                    }
                }
                catch { /* ignored */ }

                return _scanVolume;
            }
        }

        private static Vignette ScanVignette
        {
            get
            {
                if (_scanVignette != null)
                    return _scanVignette;

                var vol = ScanVolume;
                if (vol == null || vol.profile == null || vol.profile.components == null)
                    return null;

                _scanVignette = vol.profile.components.FirstOrDefault(c =>
                {
                    if (c == null) return false;
                    var n = ((global::UnityEngine.Object)c).name;
                    return !string.IsNullOrEmpty(n) && n.StartsWith("Vignette", StringComparison.OrdinalIgnoreCase);
                }) as Vignette;

                return _scanVignette;
            }
        }

        private static Bloom ScanBloom
        {
            get
            {
                if (_scanBloom != null)
                    return _scanBloom;

                var vol = ScanVolume;
                if (vol == null || vol.profile == null || vol.profile.components == null)
                    return null;

                _scanBloom = vol.profile.components.FirstOrDefault(c =>
                {
                    if (c == null) return false;
                    var n = ((global::UnityEngine.Object)c).name;
                    return !string.IsNullOrEmpty(n) && n.StartsWith("Bloom", StringComparison.OrdinalIgnoreCase);
                }) as Bloom;

                return _scanBloom;
            }
        }

        // ---- Reflection helpers (avoid compile-time dependency) ----

        private static bool TryGetPlayerPingingScan(out float value)
        {
            value = -1f;

            var hud = HUDManager.Instance;
            if (hud == null)
                return false;

            try
            {
                if (_fiPlayerPingingScan == null)
                {
                    _fiPlayerPingingScan = typeof(HUDManager).GetField(
                        "playerPingingScan",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (_fiPlayerPingingScan == null)
                    return false;

                object raw = _fiPlayerPingingScan.GetValue(hud);
                if (raw is float f)
                {
                    value = f;
                    return true;
                }
            }
            catch { /* ignored */ }

            return false;
        }

        private static bool IsScanActive()
        {
            float t;
            return TryGetPlayerPingingScan(out t) && t > -1f;
        }

        private static float ScanProgress01()
        {
            float t;
            if (!TryGetPlayerPingingScan(out t))
                return 0f;

            // Working mod used: (playerPingingScan + 1) / ScanDuration
            float k = (t + 1f) / ScanDuration;
            if (k < 0f) k = 0f;
            if (k > 1f) k = 1f;
            return k;
        }

        private static bool CanPlayerScanSafe()
        {
            var hud = HUDManager.Instance;
            if (hud == null)
                return false;

            try
            {
                if (_miCanPlayerScan == null)
                    _miCanPlayerScan = AccessTools.Method(typeof(HUDManager), "CanPlayerScan");

                if (_miCanPlayerScan == null)
                    return false;

                object r = _miCanPlayerScan.Invoke(hud, null);
                return r is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        // ---- Color building (features from rework) ----

        private static global::UnityEngine.Color GetEffectiveScanColor(bool includeAnimatedAlpha)
        {
            global::UnityEngine.Color baseColor = ModConfig.GetBaseScanColor();
            global::UnityEngine.Color result = baseColor;

            // Random / Gradient selection
            if (ModConfig.RandomModePerScan.Value == RandomMode.Gradient)
            {
                global::UnityEngine.Color ca = ResolveColorOrFallback(ModConfig.GradientA.Value, baseColor);
                global::UnityEngine.Color cb = ResolveColorOrFallback(ModConfig.GradientB.Value, baseColor);

                float speed = ModConfig.GradientSpeed.Value;
                if (speed <= 0f)
                {
                    result = ca;
                }
                else
                {
                    _gradientT += global::UnityEngine.Time.deltaTime * speed;

                    float t = _gradientT;
                    float frac = t - (float)Math.Floor(t);

                    if (ModConfig.GradientPingPong.Value)
                    {
                        float tri = frac < 0.5f ? (frac * 2f) : (2f - frac * 2f);
                        result = global::UnityEngine.Color.Lerp(ca, cb, tri);
                    }
                    else
                    {
                        result = global::UnityEngine.Color.Lerp(ca, cb, frac);
                    }
                }

                result.a = baseColor.a;
            }
            else if (ModConfig.RandomModePerScan.Value != RandomMode.Off)
            {
                if (_currentRandomColor.HasValue)
                    result = _currentRandomColor.Value;
                else
                    result = baseColor;
            }

            // Alpha animation only while scan active
            result.a = baseColor.a;

            if (includeAnimatedAlpha && IsScanActive())
            {
                float a = baseColor.a;

                if (ModConfig.FadeEnabled.Value)
                {
                    // Use progress based on ScanDuration and configured curve
                    float k = ScanProgress01();
                    a *= ModConfig.ApplyCurve(k, ModConfig.FadeCurveMode.Value);
                }

                if (ModConfig.PulseEnabled.Value)
                {
                    float s = ModConfig.PulseStrength.Value;
                    float spd = ModConfig.PulseSpeed.Value;

                    if (s > 0f && spd > 0f)
                    {
                        float wave = (float)(Math.Sin(global::UnityEngine.Time.time * spd * Math.PI * 2.0) * 0.5 + 0.5);
                        float mult = 1f - s + (s * wave);
                        a *= mult;
                    }
                }

                result.a = ModConfig.Clamp01(a);
            }

            // Clamp brightness
            result = ModConfig.ApplyMaxBrightness(result, ModConfig.Clamp01(ModConfig.MaxBrightness.Value));
            return result;
        }

        private static global::UnityEngine.Color ResolveColorOrFallback(string s, global::UnityEngine.Color fallback)
        {
            global::UnityEngine.Color c;
            if (ModConfig.TryParseColorString(s, out c))
                return c;
            return fallback;
        }

        private static global::UnityEngine.Color BuildRandomFull()
        {
            var c = new global::UnityEngine.Color(
                global::UnityEngine.Random.Range(0f, 1f),
                global::UnityEngine.Random.Range(0f, 1f),
                global::UnityEngine.Random.Range(0f, 1f),
                1f);

            c.a = ModConfig.ResolveRandomAlpha();
            return c;
        }

        private static global::UnityEngine.Color BuildRandomHueOnly()
        {
            global::UnityEngine.Color baseColor = ModConfig.GetBaseScanColor();

            float h, s, v;
            global::UnityEngine.Color.RGBToHSV(baseColor, out h, out s, out v);

            float jitter = ModConfig.RandomHueJitter.Value / 360f;
            float dh = global::UnityEngine.Random.Range(-jitter, jitter);

            float dsv = ModConfig.Clamp01(ModConfig.RandomSVJitter.Value);
            float ds = global::UnityEngine.Random.Range(-dsv, dsv);
            float dv = global::UnityEngine.Random.Range(-dsv, dsv);

            float nh = h + dh;
            while (nh < 0f) nh += 1f;
            while (nh > 1f) nh -= 1f;

            float ns = ModConfig.Clamp01(s + ds);
            float nv = ModConfig.Clamp01(v + dv);

            global::UnityEngine.Color c = global::UnityEngine.Color.HSVToRGB(nh, ns, nv);
            c.a = ModConfig.ResolveRandomAlpha();
            return c;
        }

        private static global::UnityEngine.Color BuildRandomFromPalette()
        {
            var list = ModConfig.ParsePaletteOrFallback();
            int idx = global::UnityEngine.Random.Range(0, list.Count);
            global::UnityEngine.Color c = list[idx];
            c.a = ModConfig.ResolveRandomAlpha();
            return c;
        }

        // ---- Apply (like the working mod) ----

        private static void ApplyAllIfPossible()
        {
            if (!ModConfig.Enabled.Value)
                return;

            // Need scan renderer to do anything visible. If not ready yet, keep queued.
            if (ScanRenderer == null)
            {
                _applyQueued = true;
                return;
            }

            // Apply now
            _applyQueued = false;

            var col = GetEffectiveScanColor(includeAnimatedAlpha: true);

            ApplyScanOverlayColor(col);
            ApplyVignette(col);
            ApplyBloom(col);
            ApplyScanLines(col);
        }

        private static void ApplyScanOverlayColor(global::UnityEngine.Color col)
        {
            if (!HasScanMaterial)
                return;

            try
            {
                // This is the "known-working" path.
                ScanRenderer.material.color = col;
            }
            catch { /* ignored */ }
        }

        private static void ApplyVignette(global::UnityEngine.Color scanColor)
        {
            if (!ModConfig.VignetteEnabled.Value)
                return;

            var v = ScanVignette;
            if (v == null)
                return;

            try
            {
                v.intensity.value = ModConfig.VignetteIntensity.Value;

                global::UnityEngine.Color vcol;
                if (ModConfig.VignetteUseScanColor.Value)
                {
                    vcol = scanColor;
                    vcol.a = 1f;
                }
                else
                {
                    vcol = ResolveColorOrFallback(ModConfig.VignetteColor.Value, scanColor);
                    vcol.a = ModConfig.Clamp01(ModConfig.VignetteAlpha.Value);
                }

                v.color.value = vcol;
            }
            catch { /* ignored */ }
        }

        private static void ApplyBloom(global::UnityEngine.Color scanColor)
        {
            if (!ModConfig.BloomEnabled.Value)
                return;

            var b = ScanBloom;
            if (b == null)
                return;

            try
            {
                global::UnityEngine.Color bcol = ModConfig.BloomUseScanColor.Value
                    ? scanColor
                    : ResolveColorOrFallback(ModConfig.BloomColor.Value, scanColor);

                bcol.a = ModConfig.Clamp01(ModConfig.BloomAlpha.Value);

                float mult = ModConfig.BloomTintStrength.Value;
                bcol.r = ModConfig.Clamp01(bcol.r * mult);
                bcol.g = ModConfig.Clamp01(bcol.g * mult);
                bcol.b = ModConfig.Clamp01(bcol.b * mult);

                b.tint.Override(bcol);
            }
            catch { /* ignored */ }
        }

        private static void ApplyScanLines(global::UnityEngine.Color scanColor)
        {
            if (!ModConfig.ScanLinesEnabled.Value)
                return;

            var b = ScanBloom;
            if (b == null || b.dirtTexture == null)
                return;

            if (!ModConfig.RecolorScanLines.Value)
            {
                RevertScanLinesTexture();
                return;
            }

            try
            {
                EnsureBaseScanLinesReadable();
                if (_baseScanLinesTextureReadable == null || !_baseScanLinesTextureReadable.isReadable)
                    return;

                float strength = ModConfig.Clamp01(ModConfig.ScanLinesRecolorStrength.Value);
                if (strength <= 0.001f)
                {
                    RevertScanLinesTexture();
                    return;
                }

                var tex = new global::UnityEngine.Texture2D(
                    _baseScanLinesTextureReadable.width,
                    _baseScanLinesTextureReadable.height,
                    _baseScanLinesTextureReadable.format,
                    false);

                tex.SetPixels(_baseScanLinesTextureReadable.GetPixels());
                tex.Apply(false, false);

                TextureUtils.RecolorTextureInPlace(
                    tex,
                    scanColor,
                    strength,
                    ModConfig.Clamp01(ModConfig.ScanLinesMinLuma.Value),
                    ModConfig.Clamp01(ModConfig.ScanLinesMinAlpha.Value));

                tex.Apply(false, false);

                b.dirtTexture.Override(global::UnityEngine.Object.Instantiate(tex));
            }
            catch { /* ignored */ }
        }

        private static void EnsureBaseScanLinesReadable()
        {
            if (_baseScanLinesTextureReadable != null)
                return;

            var b = ScanBloom;
            if (b == null || b.dirtTexture == null)
                return;

            try
            {
                var t = b.dirtTexture.value;
                if (t == null)
                    return;

                var t2 = t as global::UnityEngine.Texture2D;
                if (t2 != null && t2.isReadable)
                {
                    _baseScanLinesTextureReadable = t2;
                    return;
                }

                _baseScanLinesTextureReadable = TextureUtils.MakeReadableTexture(t);
            }
            catch { /* ignored */ }
        }

        private static void RevertScanLinesTexture()
        {
            try
            {
                var b = ScanBloom;
                if (b == null || b.dirtTexture == null)
                    return;

                if (_baseScanLinesTextureReadable != null)
                    b.dirtTexture.Override(_baseScanLinesTextureReadable);
            }
            catch { /* ignored */ }
        }

        // ---- Harmony hooks (string names, no compile-time member refs) ----

        [HarmonyPatch(typeof(HUDManager), "Start")]
        [HarmonyPostfix]
        private static void HUDStartPostfix()
        {
            // Reset caches on round change / lobby rejoin
            _scanRenderer = null;
            _scanVolume = null;
            _scanVignette = null;
            _scanBloom = null;

            _fiPlayerPingingScan = null;
            _miCanPlayerScan = null;

            _currentRandomColor = null;
            _gradientT = 0f;

            _applyQueued = true;
            ApplyAllIfPossible();
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        private static void HUDUpdatePostfix()
        {
            if (!ModConfig.Enabled.Value)
                return;

            // Retry queued apply when HUD becomes ready
            if (_applyQueued && (global::UnityEngine.Time.frameCount % 10 == 0))
                ApplyAllIfPossible();


            // Only animate while scan active
            if (!IsScanActive())
                return;

            var col = GetEffectiveScanColor(includeAnimatedAlpha: true);
            ApplyScanOverlayColor(col);
            ApplyVignette(col);
            ApplyBloom(col);
        }

        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPostfix]
        private static void HUDAwakePostfix()
        {
            
            ResetForNewHudInstance();
        }

        [HarmonyPatch(typeof(HUDManager), "OnEnable")]
        [HarmonyPostfix]
        private static void HUDOnEnablePostfix()
        {
            ResetForNewHudInstance();
        }

        
        private static void ResetForNewHudInstance()
        {
            _scanRenderer = null;
            _scanVolume = null;
            _scanVignette = null;
            _scanBloom = null;

            _fiPlayerPingingScan = null;
            _miCanPlayerScan = null;

            _currentRandomColor = null;
            _gradientT = 0f;

            _applyQueued = true;     
            ApplyAllIfPossible();    
        }


        [HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        [HarmonyPrefix]
        private static void PingScanPerformedPrefix(object __0)
        {
            if (!ModConfig.Enabled.Value)
                return;

            if (!CanPlayerScanSafe())
                return;

            switch (ModConfig.RandomModePerScan.Value)
            {
                case RandomMode.Off:
                    _currentRandomColor = null;
                    break;

                case RandomMode.Full:
                    _currentRandomColor = BuildRandomFull();
                    break;

                case RandomMode.HueOnly:
                    _currentRandomColor = BuildRandomHueOnly();
                    break;

                case RandomMode.Palette:
                    _currentRandomColor = BuildRandomFromPalette();
                    break;

                case RandomMode.Gradient:
                    _currentRandomColor = null;
                    _gradientT = 0f;
                    break;
            }

            // Apply immediately on scan start
            _applyQueued = true;
            ApplyAllIfPossible();
        }
    }
}
