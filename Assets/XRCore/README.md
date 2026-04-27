# XRCore SDK (`com.xrcore.sdk`)

Modular framework for **agents in spatial computing (XR + AI)**.

## Asset Store oriented structure

- `Runtime/`
  - `Agents`
  - `Vision`
  - `Tasks`
  - `Interaction`
  - `Core`
  - `UI`
- `Editor/`
  - `SetupWizard`
  - `AgentTools`
- `Providers/`
  - `RaycastDetectionProvider`
  - `SentisDetectionProvider`
  - `VisionApiDetectionProvider`
  - `SimulationDetectionProvider`
- `Samples/Demo_XR_Assistant/`
- `Documentation/`
  - `QuickStart.md`
  - `Architecture.md`
  - `AssetStore_Submission.md`
  - `AssetStore_Listing.md`
- `XRCore.asmdef`
- `package.json`

## Fast setup

- `Tools -> XRCore -> Setup Wizard`
- `GameObject -> XRCore -> Setup XR Assistant`
- `Tools -> XRCore -> Setup Wizard -> Apply Demo Preset -> Beginner/Strict/Fast`
- In sample bridge inspector: `Instruction Routing` -> `Use Agent Only` / `Use Bridge Fallback`

## Core contracts

- `IXRDetectionProvider` for interchangeable providers.
- `IXRAgentReasoner` for interchangeable agent brains.

## Included reasoners

- `RuleEngineReasoner`
- `StateMachineReasoner`
- `LocalLlmReasoner`
- `ApiLlmReasoner`

## Sample demo flow

Scene: `Assets/XRCore/Samples/Demo_XR_Assistant/Demo_XR_Assistant.unity`

The sample includes a deterministic loop:

1. `Look at the cube.`
2. Look at `Cube_A`.
3. `You are looking at the cube. Look away.`
4. Look away to reset loop and repeat.

## Value Proof In 2 Minutes

1. Open the sample scene and press Play.
2. Verify the task advances when looking at `Cube_A`.
3. Change one signal label in `Demo_VisionTask` and `VisionDetectionToSignalBridge`.
4. Press Play again and confirm the same runtime pipeline still works.

This demonstrates that detection source, reasoning strategy, and task logic can be changed without rewriting scene logic.

## Platform and Pipeline Notes

- Designed for Unity 6 (`6000.3`).
- Works with Built-in, URP, and HDRP as long as scene materials/camera are configured.
- Works with desktop and XR targets through interchangeable providers (raycast/simulation/sentis/API).

## Installation via Git URL (UPM)

`https://github.com/splibiplay/xrcore-sdk.git?path=/Assets/XRCore`
