# XRCore Architecture

## Overview

XRCore is an event-driven runtime for Unity XR that separates perception, reasoning, and execution.

Primary pipeline:

`Perception -> Event Bus -> Agent Reasoning -> Actions/Behaviours`

Design goal: reusable orchestration for XR assistants, guided flows, and task-based experiences without hard coupling between systems.

## Runtime Modules

- **Core**
  - `XRCoreEventBus`
  - `XRCoreSettings`
  - Debug/diagnostics base
- **Interaction**
  - User/system signal emission
- **Tasks**
  - Step-by-step task definition and execution
- **Vision**
  - Detection contracts and events
- **Agents**
  - Context, behaviors, and reasoners
- **UI**
  - Instruction presentation (overlay/audio)

## Key Contracts

- `IXRDetectionProvider`
  - Interchangeable detection providers.
- `IXRAgentReasoner`
  - Interchangeable decision engine for agents.

## Mental Model

- Providers detect.
- Event bus distributes.
- Agents/reasoners decide.
- Behaviours execute.
- Tasks structure the user journey.

## Event Pipeline

1. Vision/Interaction publishes events in `XRCoreEventBus`.
2. Tasks consume signals and publish lifecycle events.
3. Agent consumes context/events and decides instructions.
4. UI consumes `AgentInstructionEvent` and renders output.

## Instruction Routing Strategy

XRCore supports two instruction routing modes for demos and integrations:

- `Use Agent Only`
  - Instructions are emitted only by `XRGuideAgent` (behaviours/reasoner).
  - Recommended when validating pure agent orchestration.
- `Use Bridge Fallback`
  - `VisionDetectionToSignalBridge` can emit fallback instruction events.
  - Recommended for deterministic demos and onboarding scenes.

Both modes keep the same event contracts and can be switched from inspector without scene rewiring.

## Package Boundaries

- `com.xrcore.sdk` (this package)
  - Core runtime, providers, tasks, agents, UI bridge, editor setup tools, and sample scene.
- Toolkit packages (recommended)
  - Domain-specific layers such as training/assessment should depend on XRCore, not the other way around.

## Extensibility

- Providers:
  - `RaycastDetectionProvider`
  - `SimulationDetectionProvider`
  - `SentisDetectionProvider`
  - `VisionApiDetectionProvider`
- Reasoners:
  - `RuleEngineReasoner`
  - `StateMachineReasoner`
  - `LocalLlmReasoner`
  - `ApiLlmReasoner`

## Sample Reference Flow

1. User looks at target object (`Cube_A` in sample).
2. Provider/bridge emits detection and signal.
3. `XRTaskRunner` advances the expected step.
4. `XRGuideAgent` evaluates context and can emit instruction events.
5. UI/audio presenters render instruction feedback.

## Editor tooling

- `Tools -> XRCore -> Setup Wizard`
- `GameObject -> XRCore -> Setup XR Assistant`
- `Tools -> XRCore -> Setup Wizard -> Apply Demo Preset -> Beginner/Strict/Fast`

Enables fast and consistent bootstrapping for demos and production projects.
