# Pocket Drift Overdrive

Pocket Drift: Overdrive is a miniature arcade racing project set on a workshop bench. It is created by Casper of Branded Creations.

The v3.1 Windows public beta is available from this repository.

## Download and install the beta

1. Download [`Pocket-Drift-Overdrive-v3.1-Beta-Windows.zip`](Releases/Pocket-Drift-Overdrive-v3.1-Beta-Windows.zip).
2. Extract the entire ZIP file to a new folder. Do not run the game from inside the ZIP.
3. Keep `Pocket Drift Overdrive.exe`, `Pocket Drift Overdrive_Data`, `UnityPlayer.dll`, and `MonoBleedingEdge` together.
4. Run `Pocket Drift Overdrive.exe` on a 64-bit Windows 10 or Windows 11 computer.
5. This independent beta is unsigned. If Windows SmartScreen appears, verify the download came from this official repository before selecting **More info** and **Run anyway**.

Download size: 38.6 MB. SHA-256: `30E040571A91FF2941297E3F12E8979699EDA58796DC03EF3D8FDBDBD754B9B6` ([checksum file](Releases/Pocket-Drift-Overdrive-v3.1-Beta-Windows.sha256)).

### Controls

- `WASD` or arrow keys: accelerate, reverse, and steer.
- `Space` or `Left Shift`: hold to drift; release to trigger a charged boost.
- `E`, `Enter`, or left mouse button: use the equipped weapon.
- `R`: respawn the car.
- Xbox/XInput controller: left stick, triggers or A, RB/LB, and X/B.

Beta feedback is especially useful for steering feel, collision recovery, controller compatibility, and frame rate. This package is an early test build and may contain unfinished gameplay or visual issues.

## Project layout

- `PocketDrift_UnityProject/` contains the Unity 6 project, including Assets, Packages, and ProjectSettings.
- `Blender/` contains the source scene, generation scripts, and development renders.
- `Scripts/`, `Editor/`, and `Models/` contain standalone gameplay, tooling, and model sources.
- `website/` contains the static Pocket Drift promotional website.
- `press-kit/` contains the v3.1 beta information packet and its editable source.

## Open the game project

Open `PocketDrift_UnityProject` in Unity 6000.4.0f1. The primary generated scene is `Assets/Scenes/WorkshopPro.unity`.

The repository excludes Unity's generated Library, Temp, Logs, UserSettings, and build directories. Unity recreates those locally when the project opens.

See `WORKSHOP-PRO.md` for the documented scene-build process and current verification boundaries.

## Creator

-= Casper of Branded Creations, 2026  =-
=-  Public contact: `404kidzworld` -=
