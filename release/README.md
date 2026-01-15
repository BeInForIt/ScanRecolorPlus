# ScanRecolorPlus

Scan recolor mod for **Lethal Company**.

This mod is based on the original **ScanRecolor**, rewritten and extended to provide more
control over scan visuals while remaining compatible with the vanilla scan system.

## Important notice

After creating a lobby or joining another lobby, the scan color may reset to the vanilla default.

This is not a crash and does not mean the mod is unloaded.

### Temporary workaround

Open the config and perform any one of the following actions:
- Toggle `General → Enabled` off and on
- Change any setting (for example, change `Preset` and revert it)

This immediately reapplies the scan color.

This is a known issue related to HUD re-initialization and may be addressed in a future update.

## Features

- Scan color customization
- Presets and custom colors (HEX, RGB, HSV)
- Optional animated effects:
  - Alpha pulse
  - Gradient color cycling
- Randomized scan colors:
  - Fully random
  - Hue-only random
  - Palette-based random
- Scan post-processing control:
  - Vignette color and intensity
  - Bloom tint and strength
  - Scan line texture recoloring
- Configuration-based control (no hotkeys)

## Presets

- Cyan
- Green
- Red
- Purple
- Amber
- White
- Pink
- Default (vanilla)

Custom color formats:
- HEX: `#RRGGBB` / `#RRGGBBAA`
- CSV: `R,G,B` or `R,G,B,A`

## Differences from original ScanRecolor

| ScanRecolor | ScanRecolorPlus |
|------------|-----------------|
| Static color | Dynamic and animated |
| RGB only | Presets, RGB, HSV, HEX |
| No animation | Pulse, gradient, random |
| Limited configuration | Extended visual control |
| Minimal post-processing | Bloom, vignette, scan lines |

ScanRecolorPlus enhances the existing scan system and does not replace it.

## Known issues

- Scan color may reset after:
  - Creating a lobby
  - Joining another lobby
- Manual config change is required to reapply settings

## Notes

- Affects visual scan effects only
- Client-side and multiplayer safe
- No keybinds
- No gameplay changes

## Credits

- Original concept inspired by **ScanRecolor**
- Rewrite and extensions by **HoppinHauler**
