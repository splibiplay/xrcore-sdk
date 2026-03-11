# XRCore — AI Agent Framework for Unity XR

XRCore is a modular framework for building **spatial AI agents** in Unity XR applications.

It provides an **event-driven architecture** that connects perception, reasoning, and interaction, enabling developers to build intelligent XR assistants, task guidance systems, and context-aware behaviours inside immersive environments.

---
## Demo

[![XRCore Demo](https://img.youtube.com/vi/glK53YQSdys/maxresdefault.jpg)](https://youtu.be/glK53YQSdys)

---

## Features

- Modular architecture for XR AI agents
- Event-driven perception pipeline
- Pluggable detection providers
- Multiple reasoning strategies
- Lightweight runtime suitable for XR
- Scene setup automation tools
- Demo XR Assistant included

---

## Architecture Overview

XRCore is organized around four main layers:

```
Perception Layer
        ↓
Event System
        ↓
Agent Reasoning
        ↓
Behaviour Execution
```

### 1. Perception Layer

Detection providers generate structured perception events.

Examples:

- RaycastDetectionProvider
- SimulationDetectionProvider
- SentisDetectionProvider
- VisionApiDetectionProvider

These providers can represent:

- simple interaction detection
- simulated perception
- computer vision
- external AI APIs

---

### 2. Event System

XRCore uses a central event bus architecture.

```
XRCoreEventBus
```

Events such as detections or interactions are published and consumed by different systems.

Example events:

```
vision.detect.object
task.step.changed
agent.instruction
```

This architecture decouples perception systems from agent logic.

---

### 3. Agent Layer

The core agent system is:

```
XRGuideAgent
```

The agent processes perception events and determines the appropriate behaviour.

Possible reasoning systems include:

- RuleEngineReasoner
- StateMachineReasoner
- LocalLlmReasoner
- ApiLlmReasoner

This allows XRCore to support both deterministic systems and AI-driven reasoning.

---

### 4. Behaviour Layer

Agent decisions trigger XR behaviours such as:

- UI instructions
- audio feedback
- task progression
- environment interaction

This enables the creation of spatial AI assistants.

---

## Demo

The Unity package includes a demo scene:

```
Assets/XRCore/Samples/Demo_XR_Assistant
```

Example interaction flow:

1. User looks at an object
2. Detection event is generated
3. XR agent processes the event
4. Agent provides instruction or behaviour

---

## Installation

XRCore is distributed through the **Unity Asset Store**.

After importing the package:

1. Open the demo scene:

```
Assets/XRCore/Samples/Demo_XR_Assistant
```

2. Press **Play** to test the interaction.

---

## Package Contents

The package includes:

- XRGuideAgent
- XRTaskRunner
- Detection Event Pipeline
- Vision Providers
- Scene Setup Wizard
- XR Assistant Demo

---

## Use Cases

XRCore can be used for:

- XR training simulations
- spatial AI assistants
- interactive XR guidance systems
- intelligent museum experiences
- experimental spatial AI research

---

## Requirements

- Unity 2022+ or Unity 6
- Compatible with Built-in, URP and HDRP pipelines
- No external services required for the basic demo

---

## Documentation

Architecture documentation can be found in:

```
docs/architecture.md
```

---

## License

MIT License

Copyright (c) XRCore
