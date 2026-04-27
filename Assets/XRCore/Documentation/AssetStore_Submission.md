# XRCore Asset Store Submission

## Pre-submit checklist

- `Assets/XRCore` is the only package root.
- Demo scene opens and runs:
  `Assets/XRCore/Samples/Demo_XR_Assistant/Demo_XR_Assistant.unity`
- Console is clean on Play (no errors).
- `QuickStart.md` and `Architecture.md` are present.
- `README.md` and `package.json` exist in `Assets/XRCore`.
- No `Library`, `Temp`, `Logs`, or build artifacts inside package content.
- English-only verification passed on code, docs, and sample-facing strings.
- Sample includes diagnostics overlay that shows detections, signals, and task status.
- Setup wizard preset entries are visible and functional:
  - `Beginner`, `Strict`, `Fast`
- Instruction routing mode validated in demo:
  - `Use Agent Only`
  - `Use Bridge Fallback`

## Export `.unitypackage`

In Unity Editor:

1. Right click `Assets/XRCore`.
2. Select `Export Package...`.
3. Keep `Include dependencies` enabled.
4. Review file list and remove unrelated external assets.
5. Export as `XRCore_SDK.unitypackage`.

## Publishing Tools flow

Use Unity's package:
[Asset Store Publishing Tools](https://assetstore.unity.com/packages/tools/utilities/asset-store-publishing-tools-115)

1. Install the publishing tools in your publisher project.
2. Open `Window -> Asset Store Tools`.
3. Sign in with your Unity Publisher account.
4. Create/Select the draft package.
5. Upload `XRCore_SDK.unitypackage`.
6. Fill metadata (title, description, category, keywords, version, supported Unity versions).
7. Upload artwork:
   - Icon: `128x128`
   - Card: `420x280`
   - Screenshots: `1920x1080`
8. Submit for review.

## Recommended listing text (short)

XRCore is a modular Unity framework for spatial agents. It includes an event bus, task runner, interchangeable detection providers, interchangeable reasoners, setup wizard tooling, and a working XR assistant sample scene.

## Final validation before submit

- Import `XRCore_SDK.unitypackage` into a clean Unity project.
- Open sample scene and verify expected demo loop.
- Confirm no missing scripts/references.
- Confirm setup wizard menu entries work.
- Confirm demo preset application updates bridge/agent/UI timing.
- Confirm diagnostics overlay shows:
  - `Detections > 0` when looking at `Cube_A`.
  - `Last signal = vision.detect.object_a`.
  - Task transitions to completed.
