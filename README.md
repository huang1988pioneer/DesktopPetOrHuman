# DesktopPetOrHuman

Avalonia desktop pet app with draggable, always-on-top SVG characters.

## Run

```powershell
dotnet run --project DesktopPetOrHuman\DesktopPetOrHuman.csproj
```

## Features

- Transparent borderless desktop pet window
- Drag to move
- Double-click for a happy reaction
- Right-click menu for resize, sleep, reset, topmost toggle, and character switching
- 12 selectable characters: 喵白白, 喵布布, 小塗, Old Wang Cat, 鋒兄, 鋒哥, 小英, 喵娘, 塗哥, 牙妹, 魚妹, ググガガ
- 36 original SVG illustrations: idle, happy, and sleeping poses for every character
- Vector rendering at all five pet sizes and display scales, with transparent backgrounds

## Character artwork

The app embeds `DesktopPetOrHuman/Assets/Characters/*_idle.svg`, `*_happy.svg`,
and `*_sleep.svg`. The SVGs contain editable paths and shapes, with a consistent
320 × 320 viewBox and padding for the idle animation. They have no embedded
bitmap images, scripts, external assets, or font requirements.

`CharacterArtwork.cs` loads the SVGs through
[Svg.Controls.Avalonia](https://wieslawsoltes.github.io/Svg.Skia/articles/packages/svg-controls-avalonia/).
The window caches each character's three vector images when first selected.
Existing PNG artwork is retained as reference material and is not embedded in the app.

To change the artwork, edit the palettes and named drawing functions in
`tools/draw_characters.py`, then regenerate (Python 3.10+, no extra modules):

```powershell
python tools/draw_characters.py
```

To verify the embedded SVGs using the app's actual Avalonia renderer:

```powershell
dotnet run --project tools/CharacterArtCheck/CharacterArtCheck.csproj
```

This headless check renders all 36 states at every pet size at 1× and 2× DPI,
checks for empty images, clipped edges, opaque backgrounds, and duplicate poses,
and exercises the existing mood, size, and artwork cache behavior. It writes
light/dark contact sheets and a character lineup to `artifacts/character-review/`
without opening desktop windows or changing saved preferences.
