# XRCore Agents

Base inicial del modulo de agentes guiados.

## Runtime

- `XRCoreContext`: estado agregado interno del sistema.
- `XRCoreContextSnapshot`: vista read-only para behaviours.
- `XRGuideAgent`: escucha eventos del bus y emite instrucciones.
- `XRGuideAgentEvents`: contrato de salida (`AgentInstructionEvent`).
- `XRGuideAgentBehaviour`: punto de extension para comportamientos.

## Reglas de evaluacion

- El agente evalua por tick (`evaluationTickRateSeconds`).
- Los behaviours se ordenan por `Priority` (mayor primero).
- Cada behaviour puede definir `CooldownSeconds` para evitar spam.
- El agente puede suprimir mensajes repetidos (`repeatedMessageCooldownSeconds`).

## Behaviours incluidos

- `TaskInstructionBehaviour`
- `DetectionCommentBehaviour`
- `ErrorCorrectionBehaviour`
