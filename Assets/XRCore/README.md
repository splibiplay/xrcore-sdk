# XRCore SDK (`com.xrcore.sdk`)

Framework modular de XR + AI para Unity.

## Instalacion por Git URL (UPM)

1. Publica este repositorio en GitHub.
2. En tu proyecto Unity: `Window -> Package Manager -> + -> Add package from git URL...`
3. Usa esta URL (cambia usuario/repositorio):

`https://github.com/<usuario>/<repo>.git?path=/Assets/XRCore`

## Modulos

- `Core`: settings, debug, versionado, event bus.
- `Interaction`: emision de senales.
- `Tasks`: flujo paso a paso y eventos de ciclo de vida.
- `Vision`: contratos/providers de deteccion y publicacion de eventos.
- `Agents`: behaviours y reasoners intercambiables.
- `UI`: presentacion de instrucciones, audio y diagnostico.

## Setup rapido en escena

- `GameObject -> XRCore -> Setup XR Assistant`
- `Tools -> XRCore -> Setup Wizard` (con presets de Provider y Reasoner)

## Reasoners soportados

- `RuleEngineReasoner`
- `StateMachineReasoner`
- `LocalLlmReasoner`
- `ApiLlmReasoner`

Contrato: `IXRAgentReasoner`.

## Providers de Vision soportados

- `RaycastDetectionProvider`
- `SimulationDetectionProvider`
- `SentisDetectionProvider`
- `VisionApiDetectionProvider`

Contrato: `IXRDetectionProvider`.

## Notas

- Los ejemplos/demo viven fuera del paquete principal, en `Assets/XRCoreSamples`.
- Si vas a publicar a terceros, actualiza `package.json` (`author.url`) y licencia del repositorio.
