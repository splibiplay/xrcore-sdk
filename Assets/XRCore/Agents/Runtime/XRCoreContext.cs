using System.Collections.Generic;
using UnityEngine;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.Agents
{
    /// <summary>
    /// Estado resumido del sistema para consumo de agentes.
    /// </summary>
    public sealed class XRCoreContext
    {
        private readonly List<DetectionResult> _lastDetections = new(16);

        public XRTaskDefinition CurrentTaskDefinition { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public XRTaskStep CurrentStep { get; private set; }

        public string LastSignal { get; private set; } = string.Empty;
        public GameObject LastSignalSource { get; private set; }

        public float LastDetectionTimestamp { get; private set; }
        public string LastDetectionSource { get; private set; } = string.Empty;
        public IReadOnlyList<DetectionResult> LastDetections => _lastDetections;

        public float LastUpdateTime { get; private set; }

        public XRCoreContextSnapshot CreateSnapshot()
        {
            return new XRCoreContextSnapshot(
                CurrentTaskDefinition,
                CurrentStepIndex,
                CurrentStep,
                LastSignal,
                LastSignalSource,
                LastDetectionTimestamp,
                LastDetectionSource,
                _lastDetections,
                LastUpdateTime);
        }

        internal void Apply(XRTaskStartedEvent evt)
        {
            CurrentTaskDefinition = evt.TaskDefinition;
            CurrentStepIndex = 0;
            CurrentStep = CurrentTaskDefinition != null && CurrentTaskDefinition.Steps.Count > 0
                ? CurrentTaskDefinition.Steps[0]
                : null;
            LastUpdateTime = Time.time;
        }

        internal void Apply(XRTaskStepChangedEvent evt)
        {
            CurrentTaskDefinition = evt.TaskDefinition;
            CurrentStepIndex = evt.StepIndex;
            CurrentStep = evt.Step;
            LastUpdateTime = Time.time;
        }

        internal void Apply(XRTaskCompletedEvent evt)
        {
            CurrentTaskDefinition = evt.TaskDefinition;
            CurrentStepIndex = -1;
            CurrentStep = null;
            LastUpdateTime = Time.time;
        }

        internal void Apply(XRSignalEvent evt)
        {
            LastSignal = evt.Signal ?? string.Empty;
            LastSignalSource = evt.Source;
            LastUpdateTime = Time.time;
        }

        internal void Apply(XRDetectionEvent evt)
        {
            LastDetectionTimestamp = evt.Timestamp;
            LastDetectionSource = evt.Source ?? string.Empty;

            _lastDetections.Clear();
            if (evt.Detections == null || evt.Count <= 0)
            {
                LastUpdateTime = Time.time;
                return;
            }

            int count = Mathf.Min(evt.Count, evt.Detections.Length);
            for (int i = 0; i < count; i++)
            {
                _lastDetections.Add(evt.Detections[i]);
            }

            LastUpdateTime = Time.time;
        }
    }
}
