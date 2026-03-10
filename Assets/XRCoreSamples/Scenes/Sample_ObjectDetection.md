# Sample_ObjectDetection

Objetivo:

- Validar `XRCore.Vision` de forma agnostica al dispositivo.

Setup minimo recomendado:

1. Crear escena `Sample_ObjectDetection`.
2. Anadir un provider que implemente `IDetectionProvider` (ej. `SentisDetectionProvider`).
3. Anadir `DetectionStabilizer` y visualizar etiquetas estables en UI simple.
4. Publicar eventos al `XRCoreEventBus` para alimentar Tasks o Agents.
