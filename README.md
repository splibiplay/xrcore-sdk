# XRCore — AI Agent Framework for Unity XR

![Unity](https://img.shields.io/badge/Unity-2022%2B%20%7C%20Unity%206-black)
![XR](https://img.shields.io/badge/XR-Framework-blue)
![AI](https://img.shields.io/badge/AI-Agent%20Architecture-purple)
![License](https://img.shields.io/badge/license-MIT-green)

XRCore is a modular framework for building **spatial AI agents** in Unity XR applications.

It provides an **event-driven architecture** that connects perception, reasoning, and interaction, enabling developers to create intelligent XR assistants, guided experiences, and context-aware behaviours inside immersive environments.

---

# Demo

[![XRCore Demo](https://img.youtube.com/vi/glK53YQSdys/maxresdefault.jpg)](https://youtu.be/glK53YQSdys)

This demo shows a simple XR interaction flow where:

1. The user looks at an object  
2. A perception event is generated  
3. The XR agent processes the event  
4. The agent triggers instructions and behaviour in the scene  

---

# Why XRCore

Building intelligent behaviour in XR often requires connecting multiple systems:

- perception
- interaction
- reasoning
- behaviour execution

Most XR projects implement these systems separately, which leads to tightly coupled and difficult-to-extend architectures.

XRCore provides a **clean modular architecture** where these systems communicate through an **event-driven pipeline**, making it easier to build scalable XR assistants and intelligent interaction systems.

---

# Features

- Modular architecture for XR AI agents
- Event-driven perception pipeline
- Pluggable detection providers
- Multiple reasoning strategies
- Lightweight runtime suitable for XR
- Scene setup automation tools
- Demo XR Assistant included
- Easily extensible architecture

---

# Architecture Overview

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

---

## 1. Perception Layer

Detection providers generate structured perception events.

Examples include:

- RaycastDetectionProvider  
- SimulationDetectionProvider  
- SentisDetectionProvider  
- VisionApiDetectionProvider  

These providers can represent:

- simple interaction detection  
- simulated perception  
- computer vision models  
- external AI APIs  

---

## 2. Event System

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

This decouples perception systems from agent logic.

---

## 3. Agent Layer

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

## 4. Behaviour Layer

Agent decisions trigger XR behaviours such as:

- UI instructions
- audio feedback
- task progression
- environment interaction

This enables the creation of **spatial AI assistants**.

---

# Installation

XRCore is distributed through the **Unity Asset Store**.

After importing the package:

1. Open the demo scene:

```
Assets/XRCore/Samples/Demo_XR_Assistant
```

2. Press **Play** to test the interaction.

---

# Package Contents

The Unity package includes:

- XRGuideAgent
- XRTaskRunner
- Detection Event Pipeline
- Vision Providers
- Scene Setup Wizard
- XR Assistant Demo

---

# Example Interaction Flow

```
User looks at object
        ↓
Detection event generated
        ↓
XRCoreEventBus publishes event
        ↓
XRGuideAgent receives event
        ↓
Reasoner decides behaviour
        ↓
Instruction / behaviour executed
```

---

# Use Cases

XRCore can be used for:

- XR training simulations
- spatial AI assistants
- interactive XR guidance systems
- intelligent museum experiences
- context-aware XR workflows
- spatial computing research

---

# Requirements

- Unity 2022+ or Unity 6
- Compatible with Built-in, URP and HDRP pipelines
- No external services required for the basic demo

---

# Documentation

Architecture documentation is available in:

```
docs/architecture.md
```

---

# Topics

unity  
xr  
ai-agents  
spatial-computing  
unity-xr  
computer-vision  
event-driven  
xr-development  

---

# License

MIT License

Copyright (c) XRCore
