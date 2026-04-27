# XRCore — AI Agent Framework for Unity XR

[![Unity](https://img.shields.io/badge/Unity-2022%2B%20%7C%20Unity%206-black)](https://unity.com/)
[![XR](https://img.shields.io/badge/XR-Framework-blue)](#)
[![AI](https://img.shields.io/badge/AI-Agent%20Architecture-purple)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

XRCore is a modular framework for building spatial AI agents in Unity XR.
It connects perception, events, reasoning, and behaviour through a clean event-driven architecture so teams can build scalable XR assistants and guided workflows without hard coupling.

## Demo

- Local demo video in this repository: `XRCore_Demo.mp4`
- Public link: [XRCore Demo (video)](https://github.com/splibiplay/xrcore-sdk/raw/main/XRCore_Demo.mp4)

Demo flow:

1. The user looks at an object.
2. A perception event is generated.
3. The agent consumes context and reasoning.
4. Instructions and scene behaviour are triggered.

## Why XRCore

Most XR projects mix interaction, logic, and feedback directly in scene scripts.
That approach works for prototypes, but it becomes difficult to maintain and reuse.

XRCore separates responsibilities into reusable modules:

- perception providers,
- event bus and signals,
- interchangeable reasoners,
- behaviour output layer.

This keeps projects extensible and lets you swap detection and reasoning strategies without rewriting the whole scene flow.

## Core Features

- Modular architecture for XR AI agents
- Event-driven perception-to-action pipeline
- Pluggable detection providers
- Interchangeable reasoner implementations
- Lightweight runtime suitable for XR apps
- Editor setup tooling and deterministic demo presets

## Architecture Overview

```text
Perception Layer
        ↓
Event System (XRCoreEventBus)
        ↓
Agent Reasoning (XRGuideAgent + reasoner)
        ↓
Behaviour Execution (UI / audio / actions)
```

## Main Building Blocks

### Perception Layer

Detection providers publish structured events:

- `RaycastDetectionProvider`
- `SimulationDetectionProvider`
- `SentisDetectionProvider`
- `VisionApiDetectionProvider`

### Event System

`XRCoreEventBus` decouples publishers and consumers.

Example topics:

```text
vision.detect.object
task.step.changed
agent.instruction
```

### Agent Layer

`XRGuideAgent` consumes events and context snapshots, then executes a reasoner strategy.

Included reasoner options:

- `RuleEngineReasoner`
- `StateMachineReasoner`
- `LocalLlmReasoner`
- `ApiLlmReasoner`

### Behaviour Layer

Reasoning output can drive:

- instruction UI,
- audio cues,
- task progression,
- scene interactions.

## Toolkits Built on XRCore

Higher-level products can be layered on XRCore while keeping the base framework focused.

- **XRCore Training Toolkit**
  - Repository: [xrcore-training-toolkit](https://github.com/splibiplay/xrcore-training-toolkit)
  - Adds scenario-driven training, validators, and guided feedback UX on top of XRCore.

## Installation

XRCore SDK is distributed through the Unity Asset Store.

This repository is documentation-focused and hosts product information, positioning, and demo media.

To use XRCore in production projects, import the official package from the Asset Store.

## Package Contents

- `XRGuideAgent`
- `XRTaskRunner`
- Detection event pipeline
- Vision providers
- Setup wizard tools
- XR Assistant sample
- Demo presets (`Beginner`, `Strict`, `Fast`)

## Use Cases

- XR training simulations
- Spatial AI assistants
- Real-time guided workflows
- Context-aware XR UX
- Spatial computing R&D

## Requirements

- Unity 2022+ or Unity 6
- Built-in / URP / HDRP compatible
- No external services required for the base sample

## Documentation

- Product overview in this `README.md`
- Demo video: `XRCore_Demo.mp4`
- Related toolkit repository: [xrcore-training-toolkit](https://github.com/splibiplay/xrcore-training-toolkit)

## License

MIT License
