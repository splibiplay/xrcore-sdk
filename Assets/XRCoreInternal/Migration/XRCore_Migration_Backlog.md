# XRCore Migration Backlog

Estado inicial de clasificacion de scripts actuales en `Assets/Scripts`.

## Avances recientes

- Se anadio `XRCoreEventBus` en `XRCore/Core/Runtime`.
- Se anadieron utilidades base:
  - `XRCoreVersion`
  - `XRCoreDebug`
  - `XRCoreSettings`
  - `XRCoreInstaller`
  - `XRCoreBootstrapMode`
  - `XRCoreDiagnosticsMode`
  - `XRCoreDiagnosticsOverlay`
  - `XRCoreRuntimeStats`
  - `XRCoreSignalRegistry`
- Se prepararon carpetas `Runtime/Editor` en modulos principales.
- Se anadieron carpetas `Samples` en `Tasks` y `Vision`.
- Se creo arquitectura de `XRCore/Vision`:
  - `Runtime`
  - `Providers`
  - `Editor`
- Se creo bootstrap de `Agents`:
  - `XRCoreContext`
  - `XRCoreContextSnapshot`
  - `XRGuideAgent`
  - `XRGuideAgentBehaviour`
  - `AgentInstructionEvent`
- Se anadio capa de presentacion de instrucciones en `UI`:
  - `XRGuideInstructionPresenter`
  - `XRGuideInstructionUI`
  - `XRGuideInstructionAudio`
- Se creo sample integrado:
  - `Sample_AI_Assistant`
  - `Sample_AI_Assistant_VisionTask`
- Se anadio tooling editor:
  - `XRCoreAgentDebuggerWindow`
- Se agregaron samples base:
  - `Sample_TaskTraining`
  - `Sample_ObjectDetection`

## Candidatos claros a modulo reusable

- `Detection.cs` -> `XRCore/Vision`
- `IDetectionProvider.cs` -> `XRCore/Vision`
- `DetectionStabilizer.cs` -> `XRCore/Vision`
- Partes de `RoutineEngine.cs` (pipeline de deteccion y estabilizacion) -> `XRCore/Tasks` + `XRCore/Vision`

## Candidatos parciales (requieren limpieza previa)

- `SentisDetectionProvider.cs`
  - Reusable: bridge de Sentis a proveedor de detecciones.
  - No reusable aun: labels y reglas de sesion acopladas a rutinas concretas.
- `RoutineCatalog.cs`
  - Reusable: normalizacion/evaluacion por reglas.
  - No reusable aun: catalogo ligado a casos "higiene_bucal" y "poner_mesa".

## Especificos de DomusVi (deben salir del framework vendible)

- `DomusViFlowController.cs`
- `DomusViRoutineUI.cs`
- `DomusViStepFlow.cs`
- `DomusViStepUI.cs`
- `DomusViBootUI.cs`
- `UIInputBridge.cs`

## Siguiente paso recomendado

1. Extraer `Vision` base sin romper escenas:
   - crear versiones `XRCore` de `Detection`, `IDetectionProvider` y `DetectionStabilizer`.
   - mantener wrappers temporales en `Assets/Scripts` para compatibilidad.
2. Integrar `XRTaskRunner` en `Sample_TaskTraining`.
3. Integrar pipeline de deteccion en `Sample_ObjectDetection`.
