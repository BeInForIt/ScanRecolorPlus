using UnityEngine;

namespace hoppinhauler.ScanRecolorRework
{
    internal static class TextureUtils
    {
        public static global::UnityEngine.Texture2D MakeReadableTexture(global::UnityEngine.Texture original)
        {
            if (original == null) return null;

            global::UnityEngine.RenderTexture rt = null;
            global::UnityEngine.RenderTexture prev = null;

            try
            {
                rt = global::UnityEngine.RenderTexture.GetTemporary(
                    original.width,
                    original.height,
                    0,
                    global::UnityEngine.RenderTextureFormat.ARGB32,
                    global::UnityEngine.RenderTextureReadWrite.Linear);

                global::UnityEngine.Graphics.Blit(original, rt);

                prev = global::UnityEngine.RenderTexture.active;
                global::UnityEngine.RenderTexture.active = rt;

                var tex = new global::UnityEngine.Texture2D(original.width, original.height, global::UnityEngine.TextureFormat.RGBA32, false);
                tex.ReadPixels(new global::UnityEngine.Rect(0f, 0f, rt.width, rt.height), 0, 0);
                tex.Apply(false, false);
                return tex;
            }
            catch
            {
                return null;
            }
            finally
            {
                try { global::UnityEngine.RenderTexture.active = prev; } catch { }
                try { if (rt != null) global::UnityEngine.RenderTexture.ReleaseTemporary(rt); } catch { }
            }
        }

        public static void RecolorTextureInPlace(
            global::UnityEngine.Texture2D texture,
            global::UnityEngine.Color targetColor,
            float strength,
            float minLuma,
            float minAlpha)
        {
            if (texture == null) return;

            strength = Clamp01(strength);
            if (strength <= 0f) return;

            global::UnityEngine.Color[] pixels;
            try { pixels = texture.GetPixels(); }
            catch { return; }

            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a < minAlpha) continue;

                float l = (p.r + p.g + p.b) / 3f;
                if (l < minLuma) continue;

                var recol = new global::UnityEngine.Color(
                    targetColor.r * l,
                    targetColor.g * l,
                    targetColor.b * l,
                    p.a);

                pixels[i] = global::UnityEngine.Color.Lerp(p, recol, strength);
            }

            try { texture.SetPixels(pixels); }
            catch { }
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
