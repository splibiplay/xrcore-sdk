# XRCoreSamples

Escenas demo para validar y mostrar modulos de `XRCore`.

## Escenas objetivo

- `XRCore_BaseScene`
- `Sample_TaskTraining`
- `Sample_ObjectDetection`
- `Agent_Demo`

## Escenas creadas

- `Scenes/Sample_TaskTraining.unity`
- `Scenes/Sample_ObjectDetection.unity`
- `Scenes/Sample_AI_Assistant.unity`
- `Scenes/Sample_AI_Assistant_VisionTask.unity`

## Build order sugerido

1. `Sample_TaskTraining`
2. `Sample_ObjectDetection`
3. `Sample_AI_Assistant`
4. `Sample_AI_Assistant_VisionTask`

## Validacion rapida

- `Sample_TaskTraining`: pulsa `1` y `2` para emitir senales y completar pasos.
- `Sample_ObjectDetection`: usa provider mock y revisa logs de `SampleDetectionListener`.
- `Sample_AI_Assistant`: combina Tasks + Vision + Agents + Instruction Presenter.
- `Sample_AI_Assistant_VisionTask`: demo del loop Vision -> Agent -> Task.

## Nota para UPM / produccion

Al empaquetar como UPM, mover samples a carpeta con sufijo `~` (por ejemplo `XRCoreSamples~`)
para evitar inclusion accidental en builds de produccion.

## Uso

- Probar integracion de modulos.
- Grabar demos funcionales.
- Verificar regresiones de flujo.
