# XRCore SDK

[![Unity](https://img.shields.io/badge/Unity-2022%2B%20%7C%20Unity%206-black)](https://unity.com/)
[![Package](https://img.shields.io/badge/Package-Foundation%20SDK-2563eb)](#)
[![Architecture](https://img.shields.io/badge/Architecture-Event--Driven-0ea5e9)](#)
[![Ecosystem](https://img.shields.io/badge/Ecosystem-XRCore%20Modules-7a3cff)](https://github.com/splibiplay/splibiplay)
[![License](https://img.shields.io/badge/License-MIT-22c55e)](LICENSE)

The official foundation for XRCore products in Unity XR.

XRCore SDK provides the runtime contract and event infrastructure used by Context, Toolkit, Assessment, Authoring, Voice, VisionPlus, LLBridge, and Analytics.

## Value in 2 Minutes

1. Import XRCore SDK into a Unity 2022+ project.
2. Open the included demo scene.
3. Run a perception event flow end-to-end.
4. Confirm agent-driven output through event topics.

## Architecture Snapshot

`Perception -> Event Bus -> Reasoner -> Action`

Core runtime blocks:
- `XRCoreEventBus`
- `XRGuideAgent`
- perception providers
- modular reasoner adapters

## Ecosystem Position

```text
XRCore SDK (base)
      ↓
XRCore Context (infrastructure)
      ↓
Training Toolkit + Assessment + Authoring
      ↓
Voice + VisionPlus + LLBridge
      ↓
Analytics
```

## Related XRCore Modules

- Hub: [splibiplay](https://github.com/splibiplay/splibiplay)
- Context: [xrcore-context](https://github.com/splibiplay/xrcore-context)
- Training Toolkit: [xrcore-training-toolkit](https://github.com/splibiplay/xrcore-training-toolkit)
- Training Assessment: [xrcore-assessment](https://github.com/splibiplay/xrcore-assessment)
- Training Authoring: [xrcore-training-authoring](https://github.com/splibiplay/xrcore-training-authoring)
- Voice: [xrcore-voice](https://github.com/splibiplay/xrcore-voice)
- VisionPlus: [xrcore-visionplus](https://github.com/splibiplay/xrcore-visionplus)
- LLBridge: [xrcore-llbridge](https://github.com/splibiplay/xrcore-llbridge)
- Analytics: [xrcore-analytics](https://github.com/splibiplay/xrcore-analytics)

## Demo

- Video: [XRCore SDK Demo](https://github.com/splibiplay/xrcore-sdk/raw/main/XRCore_Demo.mp4)

## Commercial Packaging

- Sold as standalone foundation module.
- Included in XRCore Complete Pack.
- Best first purchase before extension modules.

## License

MIT License.
