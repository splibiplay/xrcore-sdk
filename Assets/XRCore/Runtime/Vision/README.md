# XRCore Vision

Arquitectura recomendada:

- `Runtime`: contratos y logica generica de deteccion.
- `Providers`: adaptadores concretos de modelos (Sentis, YOLO, etc.).
- `Editor`: tooling opcional para validacion y autoria.

Principio clave:

- `Vision` no depende de XR ni del dispositivo.
- Solo produce resultados de deteccion agnosticos.

Evento recomendado en bus:

- `XRDetectionEvent` con `DetectionResult[]`, `Count`, `Timestamp` y `Source`.
