using UnityEngine;

namespace XRCore.Core
{
    [CreateAssetMenu(menuName = "XRCore/Settings", fileName = "XRCoreSettings")]
    public sealed class XRCoreSettings : ScriptableObject
    {
        [Header("Bootstrap")]
        [SerializeField] private XRCoreBootstrapMode bootstrapMode = XRCoreBootstrapMode.AutoOnSceneLoad;

        [Header("Diagnostics")]
        [SerializeField] private XRCoreDiagnosticsMode diagnosticsMode = XRCoreDiagnosticsMode.Minimal;

        [Header("Debug")]
        [SerializeField] private bool enableLogs = true;
        [SerializeField] private bool enableWarnings = true;
        [SerializeField] private bool enableErrors = true;

        [Header("Agent Defaults")]
        [SerializeField, Min(0.05f)] private float agentEvaluationTickRateSeconds = 0.25f;
        [SerializeField] private bool agentEvaluateImmediatelyOnCriticalEvents = true;
        [SerializeField] private bool agentLogInstructions = true;
        [SerializeField] private bool agentPublishInstructionsToEventBus = true;
        [SerializeField] private bool agentSuppressRepeatedMessages = true;
        [SerializeField, Min(0f)] private float agentRepeatedMessageCooldownSeconds = 3f;

        [Header("Instruction Presenter Defaults")]
        [SerializeField] private bool presenterLogReceivedInstructions = false;

        public XRCoreBootstrapMode BootstrapMode => bootstrapMode;
        public XRCoreDiagnosticsMode DiagnosticsMode => diagnosticsMode;

        public bool EnableLogs => enableLogs;
        public bool EnableWarnings => enableWarnings;
        public bool EnableErrors => enableErrors;

        public float AgentEvaluationTickRateSeconds => agentEvaluationTickRateSeconds;
        public bool AgentEvaluateImmediatelyOnCriticalEvents => agentEvaluateImmediatelyOnCriticalEvents;
        public bool AgentLogInstructions => agentLogInstructions;
        public bool AgentPublishInstructionsToEventBus => agentPublishInstructionsToEventBus;
        public bool AgentSuppressRepeatedMessages => agentSuppressRepeatedMessages;
        public float AgentRepeatedMessageCooldownSeconds => agentRepeatedMessageCooldownSeconds;

        public bool PresenterLogReceivedInstructions => presenterLogReceivedInstructions;
    }
}
