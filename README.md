# XRCore SDK

Framework for **agents in spatial computing (XR + AI)**.

## Installation (Unity Package Manager)

This repo publishes the SDK in `Assets/XRCore`.

In Unity:
1. `Window -> Package Manager`
2. `+ -> Add package from git URL...`
3. URL:

`https://github.com/splibiplay/xrcore-sdk.git?path=/Assets/XRCore`

## Repository structure

- `Assets/XRCore` -> installable SDK (`com.xrcore.sdk`)
- `Assets/XRCore/Samples/Demo_XR_Assistant` -> demo scene + scripts + prefabs + audio
- `media/` -> video/gif to showcase the result

## What XRCore includes

- Decoupled Event Bus
- Task Runner
- Agent + Behaviours + Reasoners (`IXRAgentReasoner`)
- Vision Providers (`IXRDetectionProvider`)
- Editor installer and setup wizard
- Setup wizard presets (`Beginner` / `Strict` / `Fast`)
- UI/audio layer for instructions
- Functional demo at `Assets/XRCore/Samples/Demo_XR_Assistant`
- Publishing documentation at `Assets/XRCore/Documentation/AssetStore_Submission.md`

## Visual demo

Place your demo in:
- `media/demo.mp4`
- `media/demo.gif`

And reference it in this README:

`![Demo](media/demo.gif)`
