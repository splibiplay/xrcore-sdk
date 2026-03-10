# XRCore SDK

Framework modular para Unity orientado a **XR + AI Agents**.

## Instalacion del SDK (UPM por Git)

Este repositorio expone el paquete en `Assets/XRCore`.

En cualquier proyecto Unity:

1. Abre `Window -> Package Manager`
2. Pulsa `+ -> Add package from git URL...`
3. Pega:

`https://github.com/<usuario>/<repo>.git?path=/Assets/XRCore`

> Ejemplo: `https://github.com/acme/xrcore-sdk.git?path=/Assets/XRCore`

## Contenido principal

- `Assets/XRCore`: paquete UPM (`com.xrcore.sdk`)
- `Assets/XRCoreSamples`: escenas de demo y utilidades de ejemplo
- `Assets/XRCoreInternal`: backlog y notas internas de migracion

## Features clave del SDK

- Arquitectura modular (`Core`, `Interaction`, `Tasks`, `Vision`, `Agents`, `UI`)
- `XRCoreEventBus` desacoplado y optimizado para bajo GC
- Providers de vision intercambiables (`IXRDetectionProvider`)
- Reasoners de agente intercambiables (`IXRAgentReasoner`)
- Setup de escena en 1 click:
  - `GameObject -> XRCore -> Setup XR Assistant`
  - `Tools -> XRCore -> Setup Wizard`

## Publicar en GitHub

1. Inicializa el repo Git (si aun no existe)
2. Commit de todo el proyecto
3. Crea repo remoto en GitHub
4. Push a `main`
5. Usa el URL UPM anterior para instalar

Si quieres, en el siguiente paso te ejecuto yo los comandos (`git init`, commit, crear remoto con `gh`, push).
