using System.Collections.Generic;
using UnityEngine;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.Agents
{
    /// <summary>
    /// Vista read-only del estado del sistema para behaviours.
    /// </summary>
    public readonly struct XRCoreContextSnapshot
    {
        public XRTaskDefinition CurrentTaskDefinition { get; }
        public int CurrentStepIndex { get; }
        public XRTaskStep CurrentStep { get; }

        public string LastSignal { get; }
        public GameObject LastSignalSource { get; }

        public float LastDetectionTimestamp { get; }
        public string LastDetectionSource { get; }
        public IReadOnlyList<DetectionResult> LastDetections { get; }

        public float LastUpdateTime { get; }

        public XRCoreContextSnapshot(
            XRTaskDefinition currentTaskDefinition,
            int currentStepIndex,
            XRTaskStep currentStep,
            string lastSignal,
            GameObject lastSignalSource,
            float lastDetectionTimestamp,
            string lastDetectionSource,
            IReadOnlyList<DetectionResult> lastDetections,
            float lastUpdateTime)
        {
            CurrentTaskDefinition = currentTaskDefinition;
            CurrentStepIndex = currentStepIndex;
            CurrentStep = currentStep;
            LastSignal = lastSignal;
            LastSignalSource = lastSignalSource;
            LastDetectionTimestamp = lastDetectionTimestamp;
            LastDetectionSource = lastDetectionSource;
            LastDetections = lastDetections;
            LastUpdateTime = lastUpdateTime;
        }
    }
}
