# XRCore SDK (`com.xrcore.sdk`)

XRCore is a modular framework for spatial AI agents in Unity XR.
This package contains the reusable runtime, provider layer, setup tooling, and sample content for the XRCore SDK.

## Package layout

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
  - `SimulationDetectionProvider`
  - `SentisDetectionProvider`
  - `VisionApiDetectionProvider`
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
- In sample bridge inspector: `Instruction Routing -> Use Agent Only` or `Use Bridge Fallback`

## Stable extension contracts

- `IXRDetectionProvider` for interchangeable perception sources.
- `IXRAgentReasoner` for interchangeable reasoning backends.

## Included reasoners

- `RuleEngineReasoner`
- `StateMachineReasoner`
- `LocalLlmReasoner`
- `ApiLlmReasoner`

## Sample demo loop

Scene: `Assets/XRCore/Samples/Demo_XR_Assistant/Demo_XR_Assistant.unity`

Default deterministic loop:

1. `Look at the cube.`
2. User focuses `Cube_A`.
3. `You are looking at the cube. Look away.`
4. User looks away and the loop resets.

## Value proof in 2 minutes

1. Open the sample scene and press Play.
2. Confirm step progression while focusing `Cube_A`.
3. Change one signal label in demo assets.
4. Press Play again and verify the pipeline still works.

This demonstrates modularity: perception, reasoner strategy, and task logic remain decoupled.

## Platform notes

- Unity 2022+ and Unity 6 compatible.
- Works with Built-in, URP, and HDRP (scene configuration dependent).
- Supports desktop and XR targets through interchangeable providers.

## Installation (UPM Git)

`https://github.com/splibiplay/xrcore-sdk.git?path=/Assets/XRCore`
