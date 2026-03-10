using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.Agents
{
    public sealed class XRGuideAgent : MonoBehaviour
    {
        public const string TriggerTaskStepChanged = "task.step_changed";
        public const string TriggerTaskCompleted = "task.completed";
        public const string TriggerDetectionUpdated = "vision.detections";
        public const string TriggerSignalReceived = "interaction.signal";

        [Header("Behaviours")]
        [SerializeField] private List<XRGuideAgentBehaviour> behaviours = new();
        [SerializeField] private XRGuideReasonerBase reasoner;

        [Header("Output")]
        [SerializeField] private bool publishInstructionsToEventBus = true;
        [SerializeField] private bool logInstructions = true;
        [SerializeField, Min(0.05f)] private float evaluationTickRateSeconds = 0.25f;
        [SerializeField] private bool evaluateImmediatelyOnCriticalEvents = true;
        [SerializeField] private bool suppressRepeatedMessages = true;
        [SerializeField, Min(0f)] private float repeatedMessageCooldownSeconds = 3f;

        private readonly XRCoreContext _context = new();
        private readonly AgentMessageDeduplicator _messageDeduplicator = new();
        private readonly Dictionary<XRGuideAgentBehaviour, float> _lastExecutionByBehaviour = new();
        private readonly List<string> _pendingTriggers = new();
        private List<XRGuideAgentBehaviour> _orderedBehaviours = new();
        private float _nextEvaluationTime;
        private float _lastTickTime = -1f;

        public int LastEvaluatedBehaviourCount { get; private set; }
        public string LastDecisionTrigger { get; private set; } = string.Empty;
        public string LastSelectedBehaviourName { get; private set; } = string.Empty;
        public float LastDecisionTimestamp { get; private set; }

        public XRCoreContextSnapshot ContextSnapshot => _context.CreateSnapshot();

        public event System.Action<AgentInstructionEvent> OnInstructionGenerated;

        public void ApplySettings(XRCoreSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            evaluationTickRateSeconds = Mathf.Max(0.05f, settings.AgentEvaluationTickRateSeconds);
            evaluateImmediatelyOnCriticalEvents = settings.AgentEvaluateImmediatelyOnCriticalEvents;
            logInstructions = settings.AgentLogInstructions;
            publishInstructionsToEventBus = settings.AgentPublishInstructionsToEventBus;
            suppressRepeatedMessages = settings.AgentSuppressRepeatedMessages;
            repeatedMessageCooldownSeconds = settings.AgentRepeatedMessageCooldownSeconds;
        }

        private void OnValidate()
        {
            RebuildBehaviourOrder();
        }

        private void OnEnable()
        {
            RebuildBehaviourOrder();
            _pendingTriggers.Clear();
            _messageDeduplicator.Reset();
            _nextEvaluationTime = Time.time + evaluationTickRateSeconds;

            XRCoreEventBus.Subscribe<XRTaskStartedEvent>(HandleTaskStarted);
            XRCoreEventBus.Subscribe<XRTaskStepChangedEvent>(HandleTaskStepChanged);
            XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(HandleTaskCompleted);
            XRCoreEventBus.Subscribe<XRDetectionEvent>(HandleDetection);
            XRCoreEventBus.Subscribe<XRSignalEvent>(HandleSignal);
        }

        private void OnDisable()
        {
            XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(HandleTaskStarted);
            XRCoreEventBus.Unsubscribe<XRTaskStepChangedEvent>(HandleTaskStepChanged);
            XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(HandleTaskCompleted);
            XRCoreEventBus.Unsubscribe<XRDetectionEvent>(HandleDetection);
            XRCoreEventBus.Unsubscribe<XRSignalEvent>(HandleSignal);
        }

        private void Update()
        {
            if (_pendingTriggers.Count == 0 || Time.time < _nextEvaluationTime)
            {
                return;
            }

            _nextEvaluationTime = Time.time + evaluationTickRateSeconds;
            EvaluatePendingTriggers();
        }

        private void HandleTaskStarted(XRTaskStartedEvent evt)
        {
            _context.Apply(evt);
        }

        private void HandleTaskStepChanged(XRTaskStepChangedEvent evt)
        {
            _context.Apply(evt);
            EnqueueTrigger(TriggerTaskStepChanged, critical: true);
        }

        private void HandleTaskCompleted(XRTaskCompletedEvent evt)
        {
            _context.Apply(evt);
            EnqueueTrigger(TriggerTaskCompleted, critical: true);
        }

        private void HandleDetection(XRDetectionEvent evt)
        {
            _context.Apply(evt);
            EnqueueTrigger(TriggerDetectionUpdated, critical: false);
        }

        private void HandleSignal(XRSignalEvent evt)
        {
            _context.Apply(evt);
            bool critical = !string.IsNullOrWhiteSpace(evt.Signal) && evt.Signal.StartsWith("error.");
            EnqueueTrigger(TriggerSignalReceived, critical);
        }

        private void EnqueueTrigger(string trigger, bool critical)
        {
            if (string.IsNullOrWhiteSpace(trigger))
            {
                return;
            }

            if (!_pendingTriggers.Contains(trigger))
            {
                _pendingTriggers.Add(trigger);
            }

            if (critical && evaluateImmediatelyOnCriticalEvents)
            {
                EvaluatePendingTriggers();
                _nextEvaluationTime = Time.time + evaluationTickRateSeconds;
            }
        }

        private void EvaluatePendingTriggers()
        {
            float now = Time.time;
            float delta = _lastTickTime < 0f ? 0f : now - _lastTickTime;
            _lastTickTime = now;
            XRCoreEventBus.Publish(new XRGuideAgentTickEvent(now, delta, _pendingTriggers.Count));

            while (_pendingTriggers.Count > 0)
            {
                string trigger = _pendingTriggers[0];
                _pendingTriggers.RemoveAt(0);

                if (TryEmitInstruction(trigger))
                {
                    return;
                }
            }
        }

        private bool TryEmitInstruction(string trigger)
        {
            var snapshot = _context.CreateSnapshot();
            float now = Time.time;
            int evaluatedBehaviours = 0;

            if (reasoner != null && reasoner.TryCreateInstruction(
                    snapshot,
                    trigger,
                    now,
                    out var reasonerInstruction,
                    out var decisionSource))
            {
                if (ShouldEmitInstruction(trigger, reasonerInstruction, now))
                {
                    LastEvaluatedBehaviourCount = 0;
                    LastDecisionTrigger = trigger;
                    LastSelectedBehaviourName = string.IsNullOrWhiteSpace(decisionSource) ? reasoner.name : decisionSource;
                    LastDecisionTimestamp = now;
                    PublishInstruction(reasonerInstruction);
                    XRCoreEventBus.Publish(new XRGuideBehaviourExecutedEvent(
                        LastSelectedBehaviourName,
                        int.MaxValue,
                        trigger,
                        reasonerInstruction.Message,
                        now));
                    XRCoreEventBus.Publish(new XRGuideBehaviourEvaluationEvent(
                        trigger,
                        0,
                        LastSelectedBehaviourName,
                        now));
                    return true;
                }
            }

            for (int i = 0; i < _orderedBehaviours.Count; i++)
            {
                var behaviour = _orderedBehaviours[i];
                if (behaviour == null || IsInCooldown(behaviour, now))
                {
                    continue;
                }

                evaluatedBehaviours++;
                if (!behaviour.TryCreateInstruction(snapshot, trigger, out var instruction))
                {
                    continue;
                }

                if (!ShouldEmitInstruction(trigger, instruction, now))
                {
                    continue;
                }

                _lastExecutionByBehaviour[behaviour] = now;
                LastEvaluatedBehaviourCount = evaluatedBehaviours;
                LastDecisionTrigger = trigger;
                LastSelectedBehaviourName = behaviour.name;
                LastDecisionTimestamp = now;
                PublishInstruction(instruction);
                XRCoreEventBus.Publish(new XRGuideBehaviourExecutedEvent(
                    behaviour.name,
                    behaviour.Priority,
                    trigger,
                    instruction.Message,
                    now));
                XRCoreEventBus.Publish(new XRGuideBehaviourEvaluationEvent(
                    trigger,
                    evaluatedBehaviours,
                    behaviour.name,
                    now));
                return true;
            }

            LastEvaluatedBehaviourCount = evaluatedBehaviours;
            LastDecisionTrigger = trigger;
            LastSelectedBehaviourName = string.Empty;
            LastDecisionTimestamp = now;
            XRCoreEventBus.Publish(new XRGuideBehaviourEvaluationEvent(
                trigger,
                evaluatedBehaviours,
                string.Empty,
                now));
            return false;
        }

        private bool ShouldEmitInstruction(string trigger, AgentInstructionEvent instruction, float now)
        {
            if (!suppressRepeatedMessages)
            {
                return true;
            }

            return _messageDeduplicator.ShouldEmit(
                trigger,
                instruction.Message,
                now,
                repeatedMessageCooldownSeconds);
        }

        private void PublishInstruction(AgentInstructionEvent instruction)
        {
            if (logInstructions)
            {
                XRCoreDebug.Log($"[GuideAgent] {instruction.Trigger} -> {instruction.Message}");
            }

            OnInstructionGenerated?.Invoke(instruction);
            if (publishInstructionsToEventBus)
            {
                XRCoreEventBus.Publish(instruction);
            }
        }

        private bool IsInCooldown(XRGuideAgentBehaviour behaviour, float now)
        {
            if (behaviour.CooldownSeconds <= 0f)
            {
                return false;
            }

            if (!_lastExecutionByBehaviour.TryGetValue(behaviour, out var lastTime))
            {
                return false;
            }

            return now - lastTime < behaviour.CooldownSeconds;
        }

        private void RebuildBehaviourOrder()
        {
            _orderedBehaviours = behaviours
                .Where(b => b != null)
                .OrderByDescending(b => b.Priority)
                .ToList();
        }

        public float GetBehaviourLastExecutionTime(XRGuideAgentBehaviour behaviour)
        {
            if (behaviour == null || !_lastExecutionByBehaviour.TryGetValue(behaviour, out var time))
            {
                return -1f;
            }

            return time;
        }
    }
}
