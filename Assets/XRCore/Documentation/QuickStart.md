# XRCore Quick Start

## 1) Import the package

Import `Assets/XRCore` into your project.

## 2) Open the sample scene

Open `Assets/XRCore/Samples/Demo_XR_Assistant/Demo_XR_Assistant.unity`.

## 3) Press Play (expected flow)

Press Play. The sample loop is:

1. `lookAtCubeClip` plays.
2. Look at `Cube_A`: the cube highlights and `objectDetectedClip` plays.
3. The sample emits `vision.detect.object_a`, then `XRTaskRunner` advances the active step.
4. After `requiredGazeSeconds`, `taskCompletedClip` plays and highlight turns off.
5. When looking away, loop resets and starts again.

## 4) Required scene wiring (checklist)

| GameObject | Component | Required references |
|---|---|---|
| `TaskRunner` | `XRTaskRunner` | `taskDefinition = Demo_VisionTask` |
| `InteractionEmitter` | `XRInteractionSignalEmitter` | Default settings are valid |
| `VisionToTaskBridge` | `VisionDetectionToSignalBridge` | `emitter = InteractionEmitter`, `targetLabel = object_a`, `signalToEmit = vision.detect.object_a` |
| `GuideAgent` | `XRGuideAgent` | `behaviours` includes `Demo_XRDemoFlowBehaviour` |
| `InstructionAndDiagnostics` | `XRCoreDiagnosticsOverlay` | Optional `runtimeStats` reference |
| `Main Camera` | `DemoMouseLook` | Active camera with forward raycast |

## 5) Troubleshooting

- **Task does not advance**
  - Check `VisionToTaskBridge.emitter` points to `XRInteractionSignalEmitter`.
  - Check `Demo_VisionTask.expectedSignal` equals `vision.detect.object_a`.
  - Ensure camera can raycast `Cube_A`.
- **Detections stay at zero in diagnostics**
  - Ensure `VisionDetectionToSignalBridge` is enabled.
  - Keep `enableRaycastFallback` enabled for the sample.
- **No instruction/audio feedback**
  - Verify `XRGuideInstructionPresenter`, `XRGuideInstructionUI`, and `XRGuideInstructionAudio` are enabled.
  - Verify `GuideAgent.behaviours` includes `Demo_XRDemoFlowBehaviour`.

## 6) Setup Wizard

For framework bootstrap in other scenes:

- `Tools -> XRCore -> Setup Wizard`
- `GameObject -> XRCore -> Setup XR Assistant`
- `Tools -> XRCore -> Setup Wizard -> Apply Demo Preset -> Beginner/Strict/Fast`

### Demo presets (what they do)

| Preset | Goal | Bridge timing (`cooldown/acquire/lost`) | Agent tick | UI visibility |
|---|---|---|---|---|
| `Beginner` | Maximum stability/readability | `1.2 / 0.20 / 0.35` | `0.25s` | `5s` |
| `Strict` | Balanced production-like behavior | `0.8 / 0.12 / 0.20` | `0.18s` | `3s` |
| `Fast` | Maximum responsiveness | `0.35 / 0.05 / 0.08` | `0.10s` | `2s` |

Notes:
- Presets also configure diagnostics visibility and repeated-instruction cooldown.
- Presets are tuning profiles (no architecture changes).

### Instruction routing mode

`VisionDetectionToSignalBridge` exposes:
- `Use Agent Only`: only instructions emitted by `XRGuideAgent` are shown.
- `Use Bridge Fallback`: bridge emits instruction fallback events (`Look at the cube.` / `You are looking at the cube. Look away.`) for deterministic demos.

## 7) Extending the demo

- Add a new object and signal in `XRCoreSignalRegistry`.
- Duplicate a task step in `Demo_VisionTask.asset` with the new expected signal.
- Add/adjust a provider or bridge rule to emit that signal.
- Keep contracts (`IXRDetectionProvider`, `IXRAgentReasoner`) unchanged so providers/reasoners remain interchangeable.
