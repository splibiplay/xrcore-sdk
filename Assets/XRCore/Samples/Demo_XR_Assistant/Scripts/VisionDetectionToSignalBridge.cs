using UnityEngine;
using XRCore.Agents;
using XRCore.Core;
using XRCore.Interaction;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.Samples
{
    /// <summary>
    /// Converts detections into signals to advance tasks.
    /// </summary>
    public sealed class VisionDetectionToSignalBridge : MonoBehaviour
    {
        private enum InstructionRoutingMode
        {
            UseAgentOnly = 0,
            UseBridgeFallback = 1
        }

        [SerializeField] private XRInteractionSignalEmitter emitter;
        [SerializeField] private XRTaskRunner taskRunner;
        [SerializeField] private string targetLabel = "object_a";
        [SerializeField] private string signalToEmit = XRCoreSignalRegistry.VisionDetectObjectA;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 0.8f;
        [SerializeField] private bool logEmittedSignals = true;
        [SerializeField] private bool emitOnlyWhileTaskRunning = true;
        [SerializeField] private bool restartTaskOnLookAwayAfterCompletion = true;

        [Header("Instruction Routing")]
        [SerializeField] private InstructionRoutingMode instructionRouting = InstructionRoutingMode.UseBridgeFallback;
        [SerializeField] private bool publishInstructionFallback = true;
        [SerializeField] private XRGuideInstructionChannel instructionChannel = XRGuideInstructionChannel.Text;
        [SerializeField] private string lookAtMessage = "Look at the cube.";
        [SerializeField] private string lookAwayMessage = "You are looking at the cube. Look away.";

        [Header("Target Locking")]
        [SerializeField, Min(0f)] private float targetAcquireSeconds = 0.12f;
        [SerializeField, Min(0f)] private float targetLostGraceSeconds = 0.2f;

        [Header("Fallback (scene raycast)")]
        [SerializeField] private bool enableRaycastFallback = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private string fallbackTargetObjectName = "Cube_A";
        [SerializeField, Min(0.1f)] private float fallbackMaxDistance = 8f;

        private readonly DetectionResult[] _fallbackDetections = new DetectionResult[1];
        private float _lastEmitTime = -999f;
        private bool _targetLocked;
        private float _targetCandidateSince = -1f;
        private float _lastTargetSeenTime = -999f;
        private bool _taskCompleted;
        private bool _pendingRestartOnLookAway;
        private bool _lookAtPromptEmitted;

        private void OnEnable()
        {
            if (taskRunner == null)
            {
                taskRunner = FindFirstObjectByType<XRTaskRunner>();
            }

            XRCoreEventBus.Subscribe<XRDetectionEvent>(OnDetectionEvent);
            XRCoreEventBus.Subscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(OnTaskCompleted);

            _lookAtPromptEmitted = false;
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(OnDetectionEvent);
            XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(OnTaskStarted);
            XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(OnTaskCompleted);
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

            if (!ShouldEmitForTaskState())
            {
                return;
            }

            int count = Mathf.Min(evt.Count, evt.Detections.Length);
            for (int i = 0; i < count; i++)
            {
                if (!IsTargetLabelMatch(evt.Detections[i].Label))
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

        private void Update()
        {
            if (!enableRaycastFallback || emitter == null || string.IsNullOrWhiteSpace(signalToEmit))
            {
                return;
            }

            Camera cam = ResolveCamera();
            if (cam == null || string.IsNullOrWhiteSpace(fallbackTargetObjectName))
            {
                return;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool isLookingAtTarget = Physics.Raycast(ray, out RaycastHit hit, fallbackMaxDistance)
                && hit.collider != null
                && string.Equals(hit.collider.name, fallbackTargetObjectName, System.StringComparison.Ordinal);

            UpdateTargetLockState(isLookingAtTarget, hit);

            if (!_targetLocked && !_lookAtPromptEmitted && taskRunner != null && taskRunner.IsRunning)
            {
                EmitInstruction(lookAtMessage, XRGuideAgent.TriggerTaskStepChanged);
                _lookAtPromptEmitted = true;
            }

            if (_taskCompleted && _pendingRestartOnLookAway && !_targetLocked && taskRunner != null)
            {
                _pendingRestartOnLookAway = false;
                _taskCompleted = false;
                taskRunner.StartTask();
            }
        }

        private void UpdateTargetLockState(bool isLookingAtTarget, RaycastHit hit)
        {
            float now = Time.time;

            if (isLookingAtTarget)
            {
                _lastTargetSeenTime = now;

                if (!_targetLocked)
                {
                    if (_targetCandidateSince < 0f)
                    {
                        _targetCandidateSince = now;
                    }

                    if (now - _targetCandidateSince >= targetAcquireSeconds)
                    {
                        _targetLocked = true;
                        TryEmitSignal(hit, "acquire");
                    }
                }
                else
                {
                    // Keep emitting at cooldown while target remains locked.
                    // This guarantees step completion for tasks with MinDurationSeconds.
                    TryEmitSignal(hit, "locked");
                }

                return;
            }

            _targetCandidateSince = -1f;
            if (!_targetLocked)
            {
                return;
            }

            if (now - _lastTargetSeenTime < targetLostGraceSeconds)
            {
                return;
            }

            _targetLocked = false;
        }

        private void TryEmitSignal(RaycastHit hit, string reason)
        {
            if (Time.time - _lastEmitTime < cooldownSeconds || !ShouldEmitForTaskState())
            {
                return;
            }

            _lastEmitTime = Time.time;
            PublishFallbackDetection(hit);
            emitter.RaiseSignal(signalToEmit);
            if (logEmittedSignals)
            {
                XRCoreDebug.Log($"[VisionDetectionToSignalBridge] fallback {reason} {fallbackTargetObjectName} -> {signalToEmit}");
            }
        }

        private void OnTaskStarted(XRTaskStartedEvent _)
        {
            _taskCompleted = false;
            _pendingRestartOnLookAway = false;
            _lastEmitTime = -999f;
            _lookAtPromptEmitted = false;

            if (!_targetLocked)
            {
                EmitInstruction(lookAtMessage, XRGuideAgent.TriggerTaskStepChanged);
                _lookAtPromptEmitted = true;
            }
        }

        private void OnTaskCompleted(XRTaskCompletedEvent _)
        {
            _taskCompleted = true;
            _pendingRestartOnLookAway = restartTaskOnLookAwayAfterCompletion && _targetLocked;
            _lookAtPromptEmitted = false;
            EmitInstruction(lookAwayMessage, XRGuideAgent.TriggerTaskCompleted);
        }

        private bool ShouldEmitForTaskState()
        {
            if (!emitOnlyWhileTaskRunning)
            {
                return true;
            }

            if (taskRunner == null)
            {
                return true;
            }

            return taskRunner.IsRunning;
        }

        private void PublishFallbackDetection(RaycastHit hit)
        {
            var detection = new Detection(
                label: targetLabel,
                confidence: 1f,
                boundingBox: new Rect(hit.point.x, hit.point.z, 0.1f, 0.1f),
                timestamp: Time.time);

            _fallbackDetections[0] = new DetectionResult("vision.fallback", detection);
            XRCoreEventBus.Publish(new XRDetectionEvent(_fallbackDetections, 1, Time.time, "vision.fallback"));
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                return targetCamera;
            }

            targetCamera = Object.FindFirstObjectByType<Camera>();
            return targetCamera;
        }

        private bool IsTargetLabelMatch(string detectedLabel)
        {
            if (string.IsNullOrWhiteSpace(detectedLabel))
            {
                return false;
            }

            if (string.Equals(detectedLabel, targetLabel, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string normalizedDetected = NormalizeLabel(detectedLabel);
            string normalizedTarget = NormalizeLabel(targetLabel);
            return normalizedDetected == normalizedTarget;
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string lowered = value.Trim().ToLowerInvariant();
            return lowered.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
        }

        private void EmitInstruction(string message, string trigger)
        {
            bool shouldUseBridgeFallback = instructionRouting == InstructionRoutingMode.UseBridgeFallback;
            if (!shouldUseBridgeFallback || !publishInstructionFallback || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            XRCoreEventBus.Publish(new AgentInstructionEvent(message, instructionChannel, trigger, Time.time));
        }
    }
}
