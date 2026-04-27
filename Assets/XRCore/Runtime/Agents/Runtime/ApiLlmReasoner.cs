using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Agents
{
    [CreateAssetMenu(menuName = "XRCore/Agents/Reasoners/API LLM", fileName = "ApiLlmReasoner")]
    public sealed class ApiLlmReasoner : XRGuideReasonerBase
    {
        [SerializeField] private string endpointUrl = "https://api.example.com/v1/chat/completions";
        [SerializeField] private string apiKeyEnvVar = "XRCORE_LLM_API_KEY";
        [SerializeField] private bool emitPlaceholderWhenUnavailable = false;
        [SerializeField] private bool preferActiveTaskInstruction = true;
        [SerializeField] private bool suppressVisionTriggerPlaceholders = true;
        [SerializeField] private string placeholderMessage = "I am processing the instruction.";
        [SerializeField] private string taskCompletedMessage = "Task completed. Look away from Cube_A to restart the demo loop.";
        [SerializeField] private XRGuideInstructionChannel placeholderChannel = XRGuideInstructionChannel.Text;

        private readonly Queue<string> _queuedResponses = new();

        public string EndpointUrl => endpointUrl;
        public string ApiKeyEnvVar => apiKeyEnvVar;

        public void QueueResponse(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _queuedResponses.Enqueue(message);
            }
        }

        public override bool TryCreateInstruction(
            XRCoreContextSnapshot context,
            string trigger,
            float now,
            out AgentInstructionEvent instruction,
            out string decisionSource)
        {
            if (suppressVisionTriggerPlaceholders &&
                string.Equals(trigger, XRGuideAgent.TriggerDetectionUpdated, System.StringComparison.Ordinal))
            {
                instruction = default;
                decisionSource = string.Empty;
                return false;
            }

            if (_queuedResponses.Count > 0)
            {
                string message = _queuedResponses.Dequeue();
                instruction = new AgentInstructionEvent(message, XRGuideInstructionChannel.Text, trigger, now);
                decisionSource = "api-llm:queued-response";
                return true;
            }

            if (emitPlaceholderWhenUnavailable && !string.IsNullOrWhiteSpace(placeholderMessage))
            {
                string message = ResolvePlaceholderMessage(context, trigger);
                instruction = new AgentInstructionEvent(message, placeholderChannel, trigger, now);
                decisionSource = "api-llm:placeholder";
                return true;
            }

            instruction = default;
            decisionSource = string.Empty;
            return false;
        }

        private string ResolvePlaceholderMessage(XRCoreContextSnapshot context, string trigger)
        {
            if (trigger == XRGuideAgent.TriggerTaskCompleted)
            {
                return taskCompletedMessage;
            }

            if (preferActiveTaskInstruction && context.CurrentStep != null && !string.IsNullOrWhiteSpace(context.CurrentStep.Instruction))
            {
                return context.CurrentStep.Instruction;
            }

            if (!string.IsNullOrWhiteSpace(placeholderMessage))
            {
                return placeholderMessage;
            }

            return "Waiting for the next step.";
        }
    }
}
