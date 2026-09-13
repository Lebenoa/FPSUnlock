# FPSUnlock

A [MelonLoader](https://melonwiki.xyz/) mod that removes the frame rate cap for **IL2CPP** builds of Unity, allowing unlimited or custom target frame rates.

**One universal DLL, drop-in for any IL2CPP game running MelonLoader 0.7.3.** It
targets only stable engine + MelonLoader APIs — no game-specific code — so the
same `FPSUnlock.dll` works across Unity versions (verified on **2022.3.23f1**
and **6000.0.66f2**). Just drop it into the game's `Mods/` folder; no rebuild,
no per-game step.

Two requirements: the game must be **IL2CPP** and have **MelonLoader 0.7.3 or
newer** (the mod references the MelonLoader 0.7.3 API). Unity version and
scripting details are resolved at runtime.

Open an issue if a game behaves differently.

## Is your game IL2CPP?

This mod targets **IL2CPP** builds. Mono builds are not supported. To check the game's install folder:

- **`GameAssembly.dll`** — an IL2CPP game ships this native library in its root folder; Mono games instead ship `Managed/` assemblies under the `Data` folder.
- **`MelonLoader/Il2CppAssemblies/`** — created after MelonLoader installs on an IL2CPP game.
- **`il2cpp` subfolder** — a `<game>_Data/il2cpp_data/` directory is a strong sign of an IL2CPP build.
- **File size** — IL2CPP native code produces a large `GameAssembly.dll` (tens to hundreds of MB); Mono compiles to smaller managed `.dll` files.

## Features

- **Unlock the FPS cap** — set any target frame rate between 30 and 1000 FPS, or go fully unlimited (`-1`).
- **Disable VSync** — turns off Unity's vertical sync to avoid frame rate clamping to monitor refresh rate.
- **Configurable in-game UI** — an on-screen menu lets you tweak settings live.
- **Persistent configuration** — your settings are saved automatically.

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=automated-installation) for the target game.
2. Copy `FPSUnlock.dll` from the `Release` build output into the game's `Mods` folder.
3. Launch the game. The mod initializes automatically.

## Usage

Press **F6** (configurable) in-game to open the FPS Unlock menu. From there you can:

- Toggle the mod on/off
- Enable/disable VSync
- Drag the slider to adjust the target (30–1000)
- Jump straight to **Unlimited** or **240 FPS**

> **Why no input box?** The menu deliberately has no text field for typing a
> custom FPS. `GUI.TextField` routes through `GUI.DoTextField`, which IL2CPP
> strips from the game image when the game never calls it — invoking it throws
> a `NotSupportedException` (method unstripping failed) and crashes the menu.
> The slider covers every value in range (30–1000), so an input box would be
> redundant even if it were safe.

> **Menu availability** — no Unity IMGUI method is guaranteed across every
> IL2CPP build; IL2CPP strips whatever each game's code never references. The
> menu uses a small set of IMGUI controls that survive broadly (box, label,
> toggle, button, slider), and the mod **probes at runtime**: if the build
> stripped any of them, the menu disables itself instead of crashing and you
> get a notification telling you to configure via MelonLoader settings.

## Configuration

Settings live in MelonLoader's preferences (e.g. `UserData/MelonPreferences.cfg`) under the `FPSUnlock` category, and are also editable from the in-game UI.

| Entry | Default | Description |
|-------|---------|-------------|
| `Enabled` | `true` | Master switch for the mod. |
| `TargetFPS` | `240` | Target frame rate. `-1` means unlimited. |
| `DisableVSync` | `true` | Sets Unity's VSync to off. |
| `ForceEveryFrame` | `false` | Reapplies FPS settings every frame; useful if the game resets them. |
| `ToggleMenuKey` | `F6` | Key that opens/closes the in-game configuration menu. |

## Notes

- Target FPS values are clamped to the **30–1000** range. Use `-1` for unlimited.
- Enabling `ForceEveryFrame` has a small per-frame cost; leave it off unless you need it.
- Frame rates above your monitor's refresh rate will not render more frames than the display supports, but they can reduce input latency.

## Building

- .NET 6 SDK
- Set `GamePath` in `Directory.Build.props` to **any** game's install directory (MelonLoader must be installed there). The build references only the stable MelonLoader + `UnityEngine` APIs, producing **one universal DLL**. Build once, then copy it into every game's `Mods/` folder — do not rebuild per game:

```
dotnet build -c Release
```

Optionally override `GamePath` per build (only needed if you lack a `GamePath` default):

```
dotnet build -c Release -p:GamePath="S:\path\to\game"
```

## License

Provided as-is. Use at your own risk.
