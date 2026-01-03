# ScanRecolorPlus

Advanced scan recolor mod for **Lethal Company**.

This mod is based on the original **ScanRecolor**, but rewritten and extended to provide
more visual control, animation, and customization — while keeping compatibility with the
vanilla scan system.

---

## ⚠️ IMPORTANT NOTICE (READ BEFORE USING)

**After creating a lobby or connecting to another lobby, the scan color may reset to the vanilla default.**

If this happens, **this is NOT a crash** and **the mod is still loaded**.

### ✅ Temporary workaround (REQUIRED)
Open the config and do **any one** of the following:
- Toggle `General → Enabled` **OFF and ON**
- OR change **any setting** (for example, change `Preset` and change it back)

This will immediately re-apply the scan color.

⚠️ This is a known issue related to HUD re-initialization in the game.  
🛠 It **may be fixed in a future update**.

---

## ✨ Features

- Full scan color customization
- Presets + custom HEX / RGB / HSV colors
- Animated scan effects:
  - Alpha pulse
  - Gradient color cycling
- Randomized scan colors (per scan):
  - Full random
  - Hue-only random
  - Palette-based
- Scan post-processing control:
  - Vignette color & intensity
  - Bloom tint & strength
  - Scan line (dirt texture) recoloring
- Works without hotkeys (config-based)

---

## 🎨 Available Presets

Cyan
Green
Red
Purple
Amber
White
Pink
Default (vanilla)

You can also use:
- HEX: `#RRGGBB` / `#RRGGBBAA`
- CSV: `R,G,B` or `R,G,B,A`

---

## 🔄 Differences from the original ScanRecolor

| Original ScanRecolor | ScanRecolorPlus |
|---------------------|-----------------|
| Simple static color | Animated & dynamic |
| RGB only | Presets / RGB / HSV / HEX |
| No animation | Pulse, gradient, random |
| Limited config | Full visual control |
| Minimal HDRP control | Bloom, vignette, scan lines |

ScanRecolorPlus **does not replace** the scan system — it enhances it.

---

## 🧪 Known Issues

- Scan color may reset after:
  - Creating a lobby
  - Joining another lobby
- Requires manual config toggle to re-apply (see warning above)

---

## 📌 Notes

- This mod only affects **visual scan effects**
- Safe to use in multiplayer (client-side)
- No keybinds
- No gameplay changes

---

## ❤️ Credits

- Original concept inspired by **ScanRecolor**
- Rework, extensions, and stability fixes by **HoppinHauler**

