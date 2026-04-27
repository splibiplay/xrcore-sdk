using UnityEngine;
using XRCore.Agents;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.UI
{
    /// <summary>
    /// Estadisticas runtime del framework para diagnostico en dispositivo.
    /// </summary>
    public sealed class XRCoreRuntimeStats : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float updateWindowSeconds = 1f;

        public float EventsPerSecond { get; private set; }
        public float DetectionsPerSecond { get; private set; }
        public float BehaviourEvaluationsPerSecond { get; private set; }
        public float AgentTickRateHz { get; private set; }

        private int _eventCount;
        private int _detectionCount;
        private int _behaviourEvaluationCount;
        private int _agentTickCount;
        private float _agentTickDeltaAccum;
        private float _windowStartTime;

        private void OnEnable()
        {
            _windowStartTime = Time.time;

            XRCoreEventBus.Subscribe<XRSignalEvent>(OnSignal);
            XRCoreEventBus.Subscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Subscribe<XRTaskStepChangedEvent>(OnTaskStepChanged);
            XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(OnTaskCompleted);
            XRCoreEventBus.Subscribe<XRDetectionEvent>(OnDetection);
            XRCoreEventBus.Subscribe<AgentInstructionEvent>(OnInstruction);
            XRCoreEventBus.Subscribe<XRGuideBehaviourExecutedEvent>(OnBehaviourExecuted);
            XRCoreEventBus.Subscribe<XRGuideBehaviourEvaluationEvent>(OnBehaviourEvaluation);
            XRCoreEventBus.Subscribe<XRGuideAgentTickEvent>(OnAgentTick);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRSignalEvent>(OnSignal);
            XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Unsubscribe<XRTaskStepChangedEvent>(OnTaskStepChanged);
            XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(OnTaskCompleted);
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(OnDetection);
            XRCoreEventBus.Unsubscribe<AgentInstructionEvent>(OnInstruction);
            XRCoreEventBus.Unsubscribe<XRGuideBehaviourExecutedEvent>(OnBehaviourExecuted);
            XRCoreEventBus.Unsubscribe<XRGuideBehaviourEvaluationEvent>(OnBehaviourEvaluation);
            XRCoreEventBus.Unsubscribe<XRGuideAgentTickEvent>(OnAgentTick);
        }

        private void Update()
        {
            float elapsed = Time.time - _windowStartTime;
            if (elapsed < updateWindowSeconds)
            {
                return;
            }

            float safeElapsed = Mathf.Max(0.0001f, elapsed);
            EventsPerSecond = _eventCount / safeElapsed;
            DetectionsPerSecond = _detectionCount / safeElapsed;
            BehaviourEvaluationsPerSecond = _behaviourEvaluationCount / safeElapsed;

            if (_agentTickCount > 0)
            {
                float avgDelta = _agentTickDeltaAccum / _agentTickCount;
                AgentTickRateHz = avgDelta > 0f ? 1f / avgDelta : 0f;
            }
            else
            {
                AgentTickRateHz = 0f;
            }

            _eventCount = 0;
            _detectionCount = 0;
            _behaviourEvaluationCount = 0;
            _agentTickCount = 0;
            _agentTickDeltaAccum = 0f;
            _windowStartTime = Time.time;
        }

        private void OnSignal(XRSignalEvent _) => _eventCount++;
        private void OnTaskStarted(XRTaskStartedEvent _) => _eventCount++;
        private void OnTaskStepChanged(XRTaskStepChangedEvent _) => _eventCount++;
        private void OnTaskCompleted(XRTaskCompletedEvent _) => _eventCount++;
        private void OnInstruction(AgentInstructionEvent _) => _eventCount++;
        private void OnBehaviourExecuted(XRGuideBehaviourExecutedEvent _) => _eventCount++;

        private void OnDetection(XRDetectionEvent evt)
        {
            _eventCount++;
            _detectionCount += Mathf.Max(0, evt.Count);
        }

        private void OnBehaviourEvaluation(XRGuideBehaviourEvaluationEvent evt)
        {
            _behaviourEvaluationCount += Mathf.Max(0, evt.EvaluatedBehaviours);
        }

        private void OnAgentTick(XRGuideAgentTickEvent evt)
        {
            _agentTickCount++;
            _agentTickDeltaAccum += Mathf.Max(0f, evt.DeltaSeconds);
        }
    }
}
