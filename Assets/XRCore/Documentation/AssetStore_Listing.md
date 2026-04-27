# Asset Store Listing Draft

## Title

XRCore SDK - Spatial Agents Framework (XR + AI)

## Short Description

Build XR assistant workflows faster with a modular event bus, task system, interchangeable detection providers, interchangeable reasoners, setup wizard tooling, and a ready-to-run sample scene.

## Key Features

- Modular runtime architecture (`Core`, `Interaction`, `Tasks`, `Vision`, `Agents`, `UI`)
- Event-driven communication via `XRCoreEventBus`
- Interchangeable detection providers via `IXRDetectionProvider`
- Interchangeable reasoning strategies via `IXRAgentReasoner`
- Editor setup tooling:
  - `Tools -> XRCore -> Setup Wizard`
  - `GameObject -> XRCore -> Setup XR Assistant`
- One-click demo tuning presets:
  - `Beginner`, `Strict`, `Fast`
- Configurable instruction routing:
  - `Use Agent Only` or `Use Bridge Fallback`
- Included sample scene with audio-guided gaze loop
- End-to-end diagnostics overlay for live pipeline visibility

## Included Content

- Runtime systems
- Editor tools
- Providers
- Sample scene (`Demo_XR_Assistant`)
- Documentation (`QuickStart`, `Architecture`, submission guide)

## Why It Is Useful

- Separate perception, decision, and execution with explicit contracts.
- Replace detection providers without rewriting task logic.
- Replace reasoning strategy without rewriting scene wiring.
- Start from a working scene and adapt it to your own interactions.

## Keywords

XR, AI, spatial computing, agent, task runner, event bus, Unity, vision, reasoner

## Supported Unity Version

Unity 6.3 LTS (`6000.3`)

## Compatibility Notes

- Works in Built-in/URP/HDRP when scene materials and camera are configured.
- Sample defaults are optimized for desktop/editor iteration with XR-ready architecture.
