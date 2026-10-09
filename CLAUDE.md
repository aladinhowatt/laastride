# CLAUDE.md

Unity **6000.3.12f1** (URP 3D renderer) prototype for the NEXT-SHAPE "ปั้นโลก" game competition: **"ตุ๊กตุ๊กรอบสุดท้าย / Last Ride Home"** — a cosy 8-bit side-scrolling road-trip game where a tuk-tuk driver delivers ghost passengers to a temple before dawn. The user communicates in Thai. The design lives in the Concept Paper doc (see "Design" below).

Settings that constrain code: **Input System only** (`Keyboard.current`, `Gamepad.current`, `Mouse.current`; legacy `Input.*` throws), sprites render with `Sprites/Default`, no asmdefs (runtime code → `Assembly-CSharp`, `Editor/` → `Assembly-CSharp-Editor`). Text is TextMeshPro (needed for Thai tone marks); essentials are imported by the builder.

## Build / run

Everything is generated from code — **change the generators, not the PNGs / scene**:

| Menu | What it does |
|---|---|
| `Tools/Last Ride/Build Scene` | imports TMP essentials (first run only, async), regenerates art, builds `Assets/LastRide/Scenes/LastRide.unity` (camera + `RideBootstrap`), puts it first in Build Settings |
| `Tools/Last Ride/Regenerate Art` | only redraws the sprites |

Headless (Editor must be closed on the project, or use a copy): `Unity.exe -batchmode -projectPath <p> -executeMethod LastRideBuilder.BuildBatch` (no `-quit`; it exits itself). For screenshots: `-executeMethod LastRideBuilder.PlayForShots -lastride-shots <dir>` runs `ShotBot` (scripted session, writes PNGs, exits). Batch mode does not fire `WaitForEndOfFrame`.

## Architecture

- `Editor/PixelCanvas.cs`, `Editor/LastRideArt.cs` — all pixel art is drawn in code (480×270 virtual screen, 16 px = 1 unit, point filtered) into `Assets/LastRide/Resources/LastRide/*.png`, loaded at runtime with `Resources.Load<Sprite>("LastRide/<name>")`.
- `Scripts/RideBootstrap.cs` builds the **whole world at runtime** (sky, parallax layers, props, events, traffic, tuk-tuk, audio, UI). The scene itself only contains a camera + this component.
- The tuk-tuk never moves on screen. `RideGame.distance` is the world position; `WorldAnchor` objects sit at `TukScreenX + worldX - distance`, snapped to the pixel grid. `RoadBlocker` (LeadCar, Buffalo) = things the tuk-tuk must not hit; the controller only clamps speed + a forgiving "bump" (no game over).
- `EventPoint` = roadside (!) spot; stop beside it, Space → `DialogueUI.Play(DScript)`. All dialogue text is in `Story.cs` (nodes + choices + actions). Endings call `RideGame.EndRide`.
- Night clock (`RideGame.clockTotal`) runs while driving, half-rate when parked, paused in dialogue; reaching it ends the ride ("ฟ้าสาง").
- `ThaiFont` loads `Resources/LastRide/ThaiFontAsset` (baked from any .ttf in `Assets/LastRide/Fonts` by the builder) or falls back to a Windows system font file (LeelawUI/Tahoma). **Ship a free Thai font (e.g. Sarabun, OFL) before distributing** — the system-font fallback is not redistributable.
- Cut-scene art: `Resources/LastRide/AI/{cut_<place>,portrait_<who>}.png` are Higgsfield (gpt_image_2_5) illustrations imported as smooth Sprites by `Editor/LastRideAIImport.cs`; `RideBootstrap.Pick` prefers them and falls back to the code-drawn `cut_*` / `portrait_*` sprites. Mood-board images live in `Docs/MoodBoard`.
- Route = real places (Yaowarat -> Ayutthaya -> Sukhothai -> northern road -> Doi Suthep, `RouteRegions`). The roadside is built from 39 separate AI sprites (`Resources/LastRide/AI/Elements`, listed with heights, per-place sets and landmarks in `ElementCatalog`) by `SceneryField` in 4 depth rows (parallax 1 / 0.8 / 0.6 / 0.25; packing per place in `SceneryField.DensityOf`) on top of the code-drawn sky. `Editor/ElementTrimmer.cs` crops the raw transparent PNGs (bottom edge = ground, faint halo removed) and `LastRideAIImport` sizes each sprite from `ElementCatalog` heights (pivot bottom-centre). Falls back to the code-drawn hills if the pieces are missing. HUD shows a place banner.
- Sprites: never share one runtime Material; always go through `SpriteMats.Apply(sr, sprite)` (one material per texture) — a shared material made most sprites draw the sky texture in the Game view.
- `ChipAudio` synthesises all sound (engine, horn, blips, loop) — no audio assets.

- Traffic & HUD: `TrafficSignal` (stop lines, 4 along the route, `RideBootstrap.BuildSignals`) — `LeadCar` obeys them, running a red calls `Story.Police()`. Motorcycles are scenery from `TrafficDirector.SpawnBike`. We drive in the UPPER lane (keep left); oncoming traffic is the lower lane drawn in front. The HUD is a bottom dashboard (`HudUI.BuildDashboard`, height `RideGameConst.DashPx`); the camera is lifted by `DashUnits` so the road sits above it.

## Gotchas

- `Camera.main` is reconfigured by `RideBootstrap` (orthographic, size 135/16).
- UI is a Screen-Space-Camera canvas (reference 960×540 = 4× the art) so `ShotBot` can capture it with `Camera.Render`.
- TMP has no OpenType substitution: tone marks on upper vowels (หนึ่ง, ที่) vanish. All UI text goes through `ThaiText.Fix()` which lifts them with `<voffset>` (tuned for Leelawadee UI; `ThaiText.Lift` / `ToneFix`). Use `UIKit.Txt` / `ThaiText.Fix` for every new string.
- Thai text has spaces only between phrases; TMP wraps at spaces, so keep a space every few words in `Story.cs`.
