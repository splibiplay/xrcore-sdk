using UnityEngine;
using XRCore.Agents;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.UI
{
    /// <summary>
    /// Overlay de diagnostico runtime para inspeccionar estado del framework.
    /// </summary>
    public sealed class XRCoreDiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect panelRect = new Rect(20f, 120f, 720f, 170f);
        [SerializeField] private int maxDetectionLabels = 6;
        [SerializeField] private XRCoreDiagnosticsMode diagnosticsMode = XRCoreDiagnosticsMode.Minimal;
        [SerializeField] private XRCoreRuntimeStats runtimeStats;

        private string _taskName = "-";
        private int _stepIndex = -1;
        private string _stepTitle = "-";
        private string _lastTrigger = "-";
        private string _lastBehaviour = "-";
        private string _lastInstruction = "-";
        private int _behaviourExecutions;
        private int _detectionCount;
        private string _detectionLabels = "-";
        private readonly System.Text.StringBuilder _labelBuilder = new(128);

        public void ApplySettings(XRCoreSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            diagnosticsMode = settings.DiagnosticsMode;
            visible = diagnosticsMode != XRCoreDiagnosticsMode.Disabled;
        }

        private void OnEnable()
        {
            XRCoreEventBus.Subscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Subscribe<XRTaskStepChangedEvent>(OnTaskStepChanged);
            XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(OnTaskCompleted);
            XRCoreEventBus.Subscribe<XRDetectionEvent>(OnDetectionUpdated);
            XRCoreEventBus.Subscribe<XRGuideBehaviourExecutedEvent>(OnBehaviourExecuted);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Unsubscribe<XRTaskStepChangedEvent>(OnTaskStepChanged);
            XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(OnTaskCompleted);
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(OnDetectionUpdated);
            XRCoreEventBus.Unsubscribe<XRGuideBehaviourExecutedEvent>(OnBehaviourExecuted);
        }

        private void OnGUI()
        {
            if (!visible || diagnosticsMode == XRCoreDiagnosticsMode.Disabled)
            {
                return;
            }

            if (diagnosticsMode == XRCoreDiagnosticsMode.Minimal)
            {
                GUI.Box(panelRect,
                    "XRCore Diagnostics\n" +
                    $"Task: {_taskName} | Step: {_stepIndex} ({_stepTitle})\n" +
                    $"Detections: {_detectionCount} [{_detectionLabels}]");
                return;
            }

            string runtimeStatsText = runtimeStats != null
                ? $"\nEvents/s: {runtimeStats.EventsPerSecond:0.0} | Detections/s: {runtimeStats.DetectionsPerSecond:0.0}" +
                  $"\nBehaviour eval/s: {runtimeStats.BehaviourEvaluationsPerSecond:0.0} | Agent tick Hz: {runtimeStats.AgentTickRateHz:0.0}"
                : string.Empty;

            GUI.Box(panelRect,
                "XRCore Diagnostics\n" +
                $"Task: {_taskName} | Step: {_stepIndex} ({_stepTitle})\n" +
                $"Detections: {_detectionCount} [{_detectionLabels}]\n" +
                $"Behaviour executions: {_behaviourExecutions}\n" +
                $"Last behaviour: {_lastBehaviour}\n" +
                $"Last trigger: {_lastTrigger}\n" +
                $"Last instruction: {_lastInstruction}" +
                runtimeStatsText);
        }

        private void OnTaskStarted(XRTaskStartedEvent evt)
        {
            _taskName = evt.TaskDefinition != null ? evt.TaskDefinition.name : "-";
            _stepIndex = 0;
            _stepTitle = evt.TaskDefinition != null && evt.TaskDefinition.Steps.Count > 0
                ? evt.TaskDefinition.Steps[0]?.Title ?? "-"
                : "-";
        }

        private void OnTaskStepChanged(XRTaskStepChangedEvent evt)
        {
            _taskName = evt.TaskDefinition != null ? evt.TaskDefinition.name : _taskName;
            _stepIndex = evt.StepIndex + 1;
            _stepTitle = evt.Step != null ? evt.Step.Title : "-";
        }

        private void OnTaskCompleted(XRTaskCompletedEvent evt)
        {
            _taskName = evt.TaskDefinition != null ? evt.TaskDefinition.name : _taskName;
            _stepIndex = -1;
            _stepTitle = "Completed";
        }

        private void OnDetectionUpdated(XRDetectionEvent evt)
        {
            _detectionCount = Mathf.Max(0, evt.Count);
            if (evt.Detections == null || evt.Count <= 0)
            {
                _detectionLabels = "-";
                return;
            }

            int max = Mathf.Min(maxDetectionLabels, evt.Count, evt.Detections.Length);
            _labelBuilder.Clear();
            for (int i = 0; i < max; i++)
            {
                if (i > 0)
                {
                    _labelBuilder.Append(", ");
                }

                _labelBuilder.Append(evt.Detections[i].Label);
            }

            _detectionLabels = _labelBuilder.ToString();
        }

        private void OnBehaviourExecuted(XRGuideBehaviourExecutedEvent evt)
        {
            _behaviourExecutions++;
            _lastBehaviour = $"{evt.BehaviourName} (p{evt.BehaviourPriority})";
            _lastTrigger = evt.Trigger ?? "-";
            _lastInstruction = evt.InstructionMessage ?? "-";
        }
    }
}
