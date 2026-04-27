using System;
using UnityEngine;
using XRCore.Core;

namespace XRCore.Interaction
{
    /// <summary>
    /// Emite senales de interaccion simples para desacoplar input/objetos del sistema de tareas.
    /// </summary>
    public sealed class XRInteractionSignalEmitter : MonoBehaviour
    {
        public event Action<string, GameObject> SignalRaised;

        [SerializeField] private bool logSignals = false;
        [SerializeField] private bool publishToEventBus = true;
        [SerializeField] private bool warnOnUnknownSignal = true;

        /// <summary>
        /// Llamar desde eventos Unity (botones, interactables, triggers).
        /// </summary>
        public void RaiseSignal(string signal)
        {
            if (string.IsNullOrWhiteSpace(signal))
            {
                return;
            }

            if (logSignals)
            {
                Debug.Log($"[XRInteractionSignalEmitter] Signal={signal} Source={name}");
            }

            if (warnOnUnknownSignal && !XRCoreSignalRegistry.IsKnown(signal))
            {
                XRCoreDebug.Warning($"Unknown signal '{signal}' emitted by '{name}'. Consider registering it in XRCoreSignalRegistry.");
            }

            SignalRaised?.Invoke(signal, gameObject);

            if (publishToEventBus)
            {
                XRCoreEventBus.Publish(new XRSignalEvent(signal, gameObject));
            }
        }
    }
}
