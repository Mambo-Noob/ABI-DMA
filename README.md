# ImGuiOverlay â€? C# / ImGui.NET

A **production-grade ImGui.NET overlay** with full docking, ESP renderer, radar canvas, movable navigation bar, multi-menu system, and a unified live-editable theme/config system.

---

## Architecture

```
ImGuiOverlay/
// Core/
// OverlayApp.cs Silk.NET window, OpenGL, ImGui init (DOCKING ON)
// Theme/
// OverlayTheme.cs ONE FILE for all colors, roundings, thicknesses
// Config/
// ConfigManager.cs JSON save/load for any settings object
// OverlaySettings.cs All feature toggles / behavior values
// Rendering/
// EspRenderer.cs   Boxes, health bars, skeletons, snaplines
// RadarRenderer.cs Mini-map canvas, FOV cone, zoom, rotate
// UI/
// NavigationBar.cs Free-floating movable tab bar
// OverlayWindowManager.cs Dockspace, watermark, crosshair HUD
// Menus/
// AllMenus.cs  ESP / Aimbot / Radar / Visuals panels
// ConfigMenu.cs Live theme editor + profile manager
// configs/
// theme_default.json Default theme (editable externally)
// Program.cs
```

---

## Build & Compatibility Notes

This project targets **ImGui.NET 1.90.8.1** with **Silk.NET 2.21.0** on **.NET 8**.

### Breaking changes handled
| Issue | Fix |
|-------|-----|
| `ImGuiCol.TabSelected/TabDimmed/TabDimmedSelected` renamed in 1.90.9 | Use raw index (`style.Colors[35]` etc.) for cross-version compatibility |
| `ImGuiCol.NavHighlight`  `NavCursor` | Raw index `style.Colors[54]` |
| `ImGuiDockNodeFlags.DockSpace` removed | Use `PassthruCentralNode` instead |
| `Vector4 with { }` â€? `Vector4` is not a record struct | Replaced with explicit `new Vector4(...)` constructors |
| `ImGuiHoveredFlags.DelayNormal` â€? value `2048` | Cast `(ImGuiHoveredFlags)2048` for compatibility |
| `io.ConfigDockingWithShift` â€? removed in newer bindings | Removed; docking works without it |
| Top-level `Program.cs` + `WinExe` entry point ambiguity | Explicit `static void Main()` + `OutputType=Exe` |



### Requirements
- .NET 8 SDK
- Any GPU with OpenGL 3.3+ support

### Build & Run
```bash
cd ImGuiOverlay
dotnet run
```

### Key Bindings
| Key    | Action                     |
|--------|----------------------------|
| INSERT | Toggle menu open/close     |
| END    | Close overlay              |

---

## Features

###  Docking (ImGui Docking Branch)
Enabled via `ImGuiConfigFlags.DockingEnable` in `OverlayApp.cs`. Every window is dockable. The full-screen dockspace uses `PassthruCentralNode` so the game is visible through the center.

Multi-viewport is also enabled (`ImGuiConfigFlags.ViewportsEnable`) â€? windows can be dragged outside the main window.

###  Movable Navigation Bar
`NavigationBar.cs` draws a custom floating tab bar using the **foreground draw list** â€? not a normal ImGui window â€? giving pixel-perfect control over every visual element. Drag it anywhere by clicking the `â‹®` handle on the lABI.

###  ESP Renderer
Zero-GC hot path via `ReadOnlySpan<EspEntity>`. Draws:
- Full box or **corner-style** boxes
- Gradient health bars (greenâ†’yellowâ†’red)
- Skeleton (16 bones, configurable pairs)
- Head circle
- Name + distance labels
- Snaplines to screen center

Replace `_demoEntities` in `OverlayApp.AnimateDemoData()` with your game memory read.

###  Radar / Map Canvas
- Scroll to zoom, drag-to-move
- Rotate with player yaw
- FOV cone
- Grid overlay
- Player triangles pointing in movement direction
- Worldâ†’Radar coordinate transform with optional rotation

###  Unified Theme System
`OverlayTheme.cs` contains **every** visual property:
- All ImGui colors, roundings, border sizes, padding
- NavBar geometry and colors
- ESP line thicknesses and colors
- Radar dot sizes and colors

Call `theme.Apply()` after any change â€? it pushes everything to ImGui's style struct in one go.

###  Config Manager
```csharp
// Save
ConfigManager.Save(myTheme, "my_theme.theme.json");
// Load
var theme = ConfigManager.Load<OverlayTheme>("my_theme.theme.json");
```
Saves to `./configs/` directory as readable, editable JSON.

###  Live Theme Editor (Config Menu)
- Accent color picker with auto-generated hover/active variants
- Per-category color editors (Window, Navbar, Text, etc.)
- Geometry sliders (roundings, border sizes, etc.)
- One-click presets: **Blue / Red / Green / Purple**
- Save/Load/Delete named profiles
- All changes apply instantly

---

## Transparent Overlay (Real Game Overlay)

To use as a transparent borderless overlay over a game, modify `OverlayApp.OnLoad()`:

```csharp
// Silk window options
opts.WindowBorder           = WindowBorder.Hidden;
opts.TransparentFramebuffer = true;

// After window creation (Windows only) â€? P/Invoke:
[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);
[DllImport("user32.dll")]
static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

const int GWL_EXSTYLE   = -20;
const int WS_EX_LAYERED = 0x80000;
const int WS_EX_TRANSPARENT = 0x20;

SetWindowLong(hwnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
```

---

## Adding a New Menu Tab

1. Create class inheriting `MenuBase` in `UI/Menus/`
2. Override `Render()` with your controls
3. Add entry to `_tabs` in `NavigationBar.cs`
4. Add `case N:` in `OverlayWindowManager.RenderMainMenu()`

---

## Performance Notes

- ESP uses `ReadOnlySpan<T>` â€? no heap allocation per frame
- All draw calls go directly to `ImDrawList` native pointers
- `OverlayTheme.ToU32()` is a one-liner `ImGui.ColorConvertFloat4ToU32` â€? no caching needed
- Keep `EspEntity` and `RadarEntity` as `readonly struct` â€? stack allocated, cache friendly
- ImGui dockspace uses `PassthruCentralNode` â€? zero overdraw on game world area

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| ImGui.NET | 1.90.8.1 | ImGui bindings |
| Silk.NET.OpenGL | 2.21.0 | OpenGL |
| Silk.NET.Windowing | 2.21.0 | Window |
| Silk.NET.Input | 2.21.0 | Keyboard/mouse |
| Silk.NET.OpenGL.Extensions.ImGui | 2.21.0 | ImGuiâ†”OpenGL |
| Newtonsoft.Json | 13.0.3 | Config serialization |
