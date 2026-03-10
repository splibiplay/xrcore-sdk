# XRCore SDK

Framework para **agents en spatial computing (XR + AI)**.

## Instalacion (Unity Package Manager)

Este repo publica el SDK en `Assets/XRCore`.

En Unity:
1. `Window -> Package Manager`
2. `+ -> Add package from git URL...`
3. URL:

`https://github.com/<usuario>/<repo>.git?path=/Assets/XRCore`

## Estructura del repo

- `Assets/XRCore` -> SDK instalable (`com.xrcore.sdk`)
- `Assets/DemoScene/Demo_XR_Assistant.unity` -> escena demo principal
- `Assets/Scripts/Demo` -> scripts demo
- `media/` -> video/gif para mostrar el resultado

## Qué incluye XRCore

- Event Bus desacoplado
- Task Runner
- Agent + Behaviours + Reasoners (`IXRAgentReasoner`)
- Vision Providers (`IXRDetectionProvider`)
- Installer y Setup Wizard de editor
- Capa UI/audio para instrucciones

## Demo visual

Coloca tu demo en:
- `media/demo.mp4`
- `media/demo.gif`

Y referencia en este README:

`![Demo](media/demo.gif)`
