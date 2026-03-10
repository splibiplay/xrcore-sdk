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
        [SerializeField] private string placeholderMessage = "Estoy procesando la instruccion.";
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
            if (_queuedResponses.Count > 0)
            {
                string message = _queuedResponses.Dequeue();
                instruction = new AgentInstructionEvent(message, XRGuideInstructionChannel.Text, trigger, now);
                decisionSource = "api-llm:queued-response";
                return true;
            }

            if (emitPlaceholderWhenUnavailable && !string.IsNullOrWhiteSpace(placeholderMessage))
            {
                instruction = new AgentInstructionEvent(placeholderMessage, placeholderChannel, trigger, now);
                decisionSource = "api-llm:placeholder";
                return true;
            }

            instruction = default;
            decisionSource = string.Empty;
            return false;
        }
    }
}
