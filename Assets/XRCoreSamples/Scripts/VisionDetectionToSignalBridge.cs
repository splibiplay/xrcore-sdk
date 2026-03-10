using UnityEngine;
using XRCore.Core;
using XRCore.Interaction;
using XRCore.Vision;

namespace XRCore.Samples
{
    /// <summary>
    /// Convierte una deteccion objetivo en una senal de interaccion para avanzar tareas.
    /// </summary>
    public sealed class VisionDetectionToSignalBridge : MonoBehaviour
    {
        [SerializeField] private XRInteractionSignalEmitter emitter;
        [SerializeField] private string targetLabel = "objeto_a";
        [SerializeField] private string signalToEmit = XRCoreSignalRegistry.VisionDetectObjetoA;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 0.8f;
        [SerializeField] private bool logEmittedSignals = true;

        private float _lastEmitTime = -999f;

        private void OnEnable()
        {
            XRCoreEventBus.Subscribe<XRDetectionEvent>(OnDetectionEvent);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(OnDetectionEvent);
        }

        private void OnDetectionEvent(XRDetectionEvent evt)
        {
            if (emitter == null || string.IsNullOrWhiteSpace(targetLabel) || string.IsNullOrWhiteSpace(signalToEmit))
            {
                return;
            }

            if (Time.time - _lastEmitTime < cooldownSeconds)
            {
                return;
            }

            if (evt.Detections == null || evt.Count <= 0)
            {
                return;
            }

            int count = Mathf.Min(evt.Count, evt.Detections.Length);
            for (int i = 0; i < count; i++)
            {
                if (!string.Equals(evt.Detections[i].Label, targetLabel, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _lastEmitTime = Time.time;
                emitter.RaiseSignal(signalToEmit);
                if (logEmittedSignals)
                {
                    XRCoreDebug.Log($"[VisionDetectionToSignalBridge] {targetLabel} -> {signalToEmit}");
                }
                return;
            }
        }
    }
}
